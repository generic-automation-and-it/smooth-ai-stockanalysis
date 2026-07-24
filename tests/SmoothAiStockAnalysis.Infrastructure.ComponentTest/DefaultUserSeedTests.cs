using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Domain.Entities;
using SmoothAiStockAnalysis.Infrastructure.Extensions;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;
using Xunit.v3;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

/// <summary>
/// L1 proof that migrate+seed creates the configured default user once and is idempotent
/// against an isolated SQLite file on the production persistence stack (T-022 / LADR-018).
/// </summary>
public sealed class DefaultUserSeedTests : IAsyncDisposable
{
    private static readonly Guid ConfiguredIdentifier =
        Guid.Parse("11111111-1111-4111-8111-111111111111");

    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task MigrateAndSeedCreatesTheConfiguredUserOnce()
    {
        await using ServiceProvider provider = CreateProvider(ConfiguredIdentifier);
        await RunInitializerAsync(provider);
        await RunInitializerAsync(provider);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        var context = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

        List<UserRecord> users = await context.Users().ToListAsync(TestContext.Current.CancellationToken);
        users.Count.ShouldBe(1);
        users[0].UniqueIdentifier.ShouldBe(ConfiguredIdentifier);
        users[0].Id.ShouldBeGreaterThan(0);
        users[0].Metadata.SchemaVersion.ShouldBe(1);
    }

    [Fact]
    public async Task EnsureDefaultUserIsIdempotentWithoutHostedService()
    {
        await using ServiceProvider provider = CreateProvider(ConfiguredIdentifier);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        var context = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        await SqliteDatabaseInitializer.EnsureDefaultUserAsync(
            context,
            ConfiguredIdentifier,
            NullLogger.Instance,
            TestContext.Current.CancellationToken);
        await SqliteDatabaseInitializer.EnsureDefaultUserAsync(
            context,
            ConfiguredIdentifier,
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        (await context.Users().CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task EnsureDefaultUserAcceptsUniqueIndexConflictWhenConfiguredIdentityAlreadyExists()
    {
        await using ServiceProvider provider = CreateProvider(ConfiguredIdentifier);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        var context = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Pre-seed a user with the configured identifier to force a unique-index conflict on save.
        context.Users().Add(UserRecord.FromDomain(User.Create(ConfiguredIdentifier)));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Second call must swallow the conflict and not throw.
        await SqliteDatabaseInitializer.EnsureDefaultUserAsync(
            context, ConfiguredIdentifier, NullLogger.Instance, TestContext.Current.CancellationToken);

        (await context.Users().CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task SeededUserIsIsolatedFromADifferentUserScope()
    {
        await using ServiceProvider provider = CreateProvider(ConfiguredIdentifier);
        await RunInitializerAsync(provider);

        long seededUserId;
        long otherUserId;
        await using (AsyncServiceScope setup = provider.CreateAsyncScope())
        {
            setup.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
            var context = setup.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
            UserRecord seeded = await context.Users().SingleAsync(TestContext.Current.CancellationToken);
            seededUserId = seeded.Id;

            var other = UserRecord.FromDomain(User.Create(Guid.NewGuid()));
            context.Users().Add(other);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            otherUserId = other.Id;
        }

        // No feature predicate: isolation is only the global filter under the other user's scope.
        await using AsyncServiceScope otherScope = provider.CreateAsyncScope();
        otherScope.ServiceProvider.GetRequiredService<IDataAccessScopeSetter>()
            .SetScope(DataAccessScope.ForUser(otherUserId));
        var isolated = otherScope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

        List<UserRecord> visible = await isolated.Users().ToListAsync(TestContext.Current.CancellationToken);
        visible.Select(user => user.Id).ShouldBe([otherUserId]);
        visible.ShouldNotContain(user => user.Id == seededUserId);
    }

    private async Task RunInitializerAsync(ServiceProvider provider)
    {
        var initializer = new SqliteDatabaseInitializer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<DefaultUserSeedOptions>>(),
            NullLogger<SqliteDatabaseInitializer>.Instance);
        await initializer.StartAsync(TestContext.Current.CancellationToken);
    }

    private ServiceProvider CreateProvider(Guid defaultUserUniqueIdentifier)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructurePersistence(_database.ConnectionString, defaultUserUniqueIdentifier);
        return services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync() => await _database.DisposeAsync();
}
