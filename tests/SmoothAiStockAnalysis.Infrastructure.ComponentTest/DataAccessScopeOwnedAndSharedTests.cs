using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Domain.Entities;
using SmoothAiStockAnalysis.Infrastructure.ComponentTest.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;
using Xunit.v3;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

/// <summary>
/// L1 proof that the LADR-017 global filter isolates owned <em>dependents</em> (filtered on
/// <c>UserId</c>) and never filters shared reference data, against an isolated SQLite file using
/// the production context/filter convention via <see cref="ScopeProbeDbContext"/>.
/// </summary>
public sealed class DataAccessScopeOwnedAndSharedTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task OwnedDependentIsFilteredToTheScopedUser()
    {
        (long userAId, long userBId) = await SeedAsync();

        IReadOnlyList<ScopeOwnedProbeRecord> asA = await QueryOwnedAsAsync(DataAccessScope.ForUser(userAId));
        IReadOnlyList<ScopeOwnedProbeRecord> asB = await QueryOwnedAsAsync(DataAccessScope.ForUser(userBId));

        asA.Select(r => r.UserId).Distinct().ShouldBe([userAId]);
        asB.Select(r => r.UserId).Distinct().ShouldBe([userBId]);
        asA.ShouldAllBe(r => r.Ticker == "AAA");
        asB.ShouldAllBe(r => r.Ticker == "BBB");
    }

    [Fact]
    public async Task OwnedDependentQueryWithNoScopeFailsClosed()
    {
        await SeedAsync();

        await using ScopeProbeDbContext context = CreateContext(); // no accessor => no scope
        await Should.ThrowAsync<InvalidOperationException>(
            () => context.ScopeOwnedProbeRecords.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SystemScopeSeesAllOwnedDependentsAcrossUsers()
    {
        (long userAId, long userBId) = await SeedAsync();

        await using ScopeProbeDbContext context = CreateContext(DataAccessScope.System());
        long[] userIds = [.. (await context.ScopeOwnedProbeRecords.ToListAsync(TestContext.Current.CancellationToken))
            .Select(r => r.UserId)
            .Distinct()];
        userIds.ShouldBe([userAId, userBId], ignoreOrder: true);
    }

    [Fact]
    public async Task SharedReferenceDataIsUnfilteredInUserSystemAndNoScope()
    {
        (long userAId, _) = await SeedAsync();

        // User scope
        await using (ScopeProbeDbContext asUser = CreateContext(DataAccessScope.ForUser(userAId)))
        {
            (await asUser.ScopeSharedProbeRecords.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
        }

        // System scope
        await using (ScopeProbeDbContext asSystem = CreateContext(DataAccessScope.System()))
        {
            (await asSystem.ScopeSharedProbeRecords.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
        }

        // No scope at all: shared data must still be queryable (it is not owned).
        await using (ScopeProbeDbContext noScope = CreateContext())
        {
            (await noScope.ScopeSharedProbeRecords.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
        }
    }

    private async Task<IReadOnlyList<ScopeOwnedProbeRecord>> QueryOwnedAsAsync(DataAccessScope scope)
    {
        await using ScopeProbeDbContext context = CreateContext(scope);
        return await context.ScopeOwnedProbeRecords.ToListAsync(TestContext.Current.CancellationToken);
    }

    private ScopeProbeDbContext CreateContext(DataAccessScope? scope = null)
    {
        var options = new DbContextOptionsBuilder<ScopeProbeDbContext>()
            .UseSqlite(_database.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        if (scope is null)
        {
            return new ScopeProbeDbContext(options);
        }

        var accessor = new DataAccessScopeAccessor();
        var context = new ScopeProbeDbContext(options, accessor);
        context.SetScope(scope.Value);
        return context;
    }

    private async Task<(long UserAId, long UserBId)> SeedAsync()
    {
        await using ScopeProbeDbContext context = CreateContext(DataAccessScope.System());
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var userA = UserRecord.FromDomain(User.Create(Guid.NewGuid()));
        var userB = UserRecord.FromDomain(User.Create(Guid.NewGuid()));
        context.Users().AddRange(userA, userB);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.ScopeOwnedProbeRecords.AddRange(
            new ScopeOwnedProbeRecord { UserId = userA.Id, Ticker = "AAA" },
            new ScopeOwnedProbeRecord { UserId = userB.Id, Ticker = "BBB" });
        context.ScopeSharedProbeRecords.AddRange(
            new ScopeSharedProbeRecord { Symbol = "MSFT" },
            new ScopeSharedProbeRecord { Symbol = "AAPL" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (userA.Id, userB.Id);
    }

    public async ValueTask DisposeAsync() => await _database.DisposeAsync();
}
