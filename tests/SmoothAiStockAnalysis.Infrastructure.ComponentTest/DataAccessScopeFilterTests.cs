using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Domain.Entities;
using SmoothAiStockAnalysis.Infrastructure.Extensions;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;
using Xunit.v3;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

/// <summary>
/// L1 proof of the LADR-017 global user-isolation filter against an isolated SQLite file,
/// using the production context, filter convention, and DI registration.
/// </summary>
public sealed class DataAccessScopeFilterTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task UserScopeSeesOnlyOwnUserRowsAcrossTwoUsers()
    {
        await using ServiceProvider provider = CreateProvider();
        (long userAId, long userBId) = await SeedTwoUsersAsync(provider);

        // A plain query with NO feature-specific predicate returns only the scoped user's row.
        IReadOnlyList<UserRecord> asUserA = await QueryUsersAsAsync(provider, DataAccessScope.ForUser(userAId));
        IReadOnlyList<UserRecord> asUserB = await QueryUsersAsAsync(provider, DataAccessScope.ForUser(userBId));

        asUserA.Select(r => r.Id).ShouldBe([userAId]);
        asUserB.Select(r => r.Id).ShouldBe([userBId]);
    }

    [Fact]
    public async Task MissingScopeFailsClosedAndExposesNoOwnedRows()
    {
        await using ServiceProvider provider = CreateProvider();
        await SeedTwoUsersAsync(provider);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

        // No scope set: querying the owned tenant root must throw, not return rows.
        await Should.ThrowAsync<InvalidOperationException>(
            () => context.Users().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SystemScopeIsDistinctAndSeesAllUsers()
    {
        await using ServiceProvider provider = CreateProvider();
        (long userAId, long userBId) = await SeedTwoUsersAsync(provider);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        var context = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

        long[] ids = [.. (await context.Users().ToListAsync(TestContext.Current.CancellationToken)).Select(r => r.Id)];
        ids.ShouldBe([userAId, userBId], ignoreOrder: true);
    }

    [Fact]
    public async Task SequentialScopesInOneProcessDoNotLeakTenantKeys()
    {
        await using ServiceProvider provider = CreateProvider();
        (long userAId, long userBId) = await SeedTwoUsersAsync(provider);

        // Two scopes from the same provider/context-type: each must resolve to its own key.
        IReadOnlyList<UserRecord> first = await QueryUsersAsAsync(provider, DataAccessScope.ForUser(userAId));
        IReadOnlyList<UserRecord> second = await QueryUsersAsAsync(provider, DataAccessScope.ForUser(userBId));

        first.Select(r => r.Id).ShouldBe([userAId]);
        second.Select(r => r.Id).ShouldBe([userBId]);
    }

    private static async Task<IReadOnlyList<UserRecord>> QueryUsersAsAsync(ServiceProvider provider, DataAccessScope scope)
    {
        await using AsyncServiceScope diScope = provider.CreateAsyncScope();
        diScope.ServiceProvider.GetRequiredService<IDataAccessScopeSetter>().SetScope(scope);
        var context = diScope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        return await context.Users().ToListAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<(long UserAId, long UserBId)> SeedTwoUsersAsync(ServiceProvider provider)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        // Seeding uses the deliberate system scope (shared ingestion path), not a user scope.
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        var context = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var userA = UserRecord.FromDomain(User.Create(Guid.NewGuid()));
        var userB = UserRecord.FromDomain(User.Create(Guid.NewGuid()));
        context.Users().AddRange(userA, userB);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (userA.Id, userB.Id);
    }

    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructurePersistence(_database.ConnectionString);
        return services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync() => await _database.DisposeAsync();
}
