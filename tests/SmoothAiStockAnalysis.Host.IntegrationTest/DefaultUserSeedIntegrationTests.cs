using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Domain.Documents;
using SmoothAiStockAnalysis.Domain.Entities;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Host.IntegrationTest;

/// <summary>
/// L2 proof that Host startup migrates, validates configuration, and seeds the configured
/// default user once against an isolated SQLite file (T-022 / T-023 / #66 / #67).
/// </summary>
public sealed class DefaultUserSeedIntegrationTests(HostWebAppFixture fixture)
    : IClassFixture<HostWebAppFixture>
{
    private static readonly Guid PlaceholderUniqueIdentifier =
        Guid.Parse("00000000-0000-4000-8000-000000000001");

    [Fact]
    public async Task HostStartupSeedsTheConfiguredDefaultUserIdempotently()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await using (AsyncServiceScope scope = fixture.RootServices.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
            var context = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

            // Assert on the configured identity rather than total row count: sibling tests in this
            // class fixture may insert additional users into the shared isolated database.
            List<UserRecord> seeded = await context.Users()
                .Where(user => user.UniqueIdentifier == PlaceholderUniqueIdentifier)
                .ToListAsync(cancellationToken);
            seeded.Count.ShouldBe(1);
            seeded[0].Metadata.SchemaVersion.ShouldBe(UserMetadata.CurrentSchemaVersion);
        }

        // Re-run the production seed path against the already-started database (restart proxy).
        var initializer = new SqliteDatabaseInitializer(
            fixture.RootServices.GetRequiredService<IServiceScopeFactory>(),
            fixture.RootServices.GetRequiredService<IOptions<DefaultUserSeedOptions>>(),
            NullLogger<SqliteDatabaseInitializer>.Instance);
        await initializer.StartAsync(cancellationToken);
        await initializer.StartAsync(cancellationToken);

        await using AsyncServiceScope afterRestart = fixture.RootServices.CreateAsyncScope();
        afterRestart.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        var afterContext = afterRestart.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        (await afterContext.Users()
            .CountAsync(user => user.UniqueIdentifier == PlaceholderUniqueIdentifier, cancellationToken))
            .ShouldBe(1);
    }

    [Fact]
    public async Task OwnedDataQueryUnderDifferentUserReturnsNoSeededRowsWithoutPredicate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        long seededUserId;
        long otherUserId;
        await using (AsyncServiceScope setup = fixture.RootServices.CreateAsyncScope())
        {
            setup.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
            var context = setup.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

            UserRecord seeded = await context.Users()
                .SingleAsync(user => user.UniqueIdentifier == PlaceholderUniqueIdentifier, cancellationToken);
            seededUserId = seeded.Id;

            var other = UserRecord.FromDomain(User.Create(Guid.NewGuid()));
            context.Users().Add(other);
            await context.SaveChangesAsync(cancellationToken);
            otherUserId = other.Id;
        }

        await using AsyncServiceScope otherScope = fixture.RootServices.CreateAsyncScope();
        otherScope.ServiceProvider.GetRequiredService<IDataAccessScopeSetter>()
            .SetScope(DataAccessScope.ForUser(otherUserId));
        var isolated = otherScope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

        // Global filter only — no feature Where clause.
        List<UserRecord> visible = await isolated.Users().ToListAsync(cancellationToken);
        visible.Select(user => user.Id).ShouldBe([otherUserId]);
        visible.ShouldNotContain(user => user.Id == seededUserId);
    }
}
