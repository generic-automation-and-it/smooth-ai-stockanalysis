using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Configurations;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest.Persistence;

/// <summary>
/// Production model plus a test-only owned dependent and a test-only shared reference entity, used
/// to prove the global isolation filter applies to owned dependents and never to shared data.
/// Production has no owned dependents or shared reference tables yet, so this probe exercises the
/// production filter convention rather than a reimplemented test-only filter.
/// </summary>
public sealed class ScopeProbeDbContext : SmoothAiStockAnalysisDbContext
{
    private readonly DataAccessScopeAccessor? _accessor;

    public ScopeProbeDbContext(DbContextOptions options)
        : base(options)
    {
    }

    internal ScopeProbeDbContext(DbContextOptions options, DataAccessScopeAccessor accessor)
        : base(options, accessor) =>
        _accessor = accessor;

    public DbSet<ScopeOwnedProbeRecord> ScopeOwnedProbeRecords => Set<ScopeOwnedProbeRecord>();

    public DbSet<ScopeSharedProbeRecord> ScopeSharedProbeRecords => Set<ScopeSharedProbeRecord>();

    /// <summary>Sets the explicit scope on this context's accessor (test convenience).</summary>
    internal void SetScope(DataAccessScope scope) =>
        (_accessor ?? throw new InvalidOperationException("This probe context has no scope accessor."))
            .SetScope(scope);

    /// <inheritdoc />
    protected override void OnModelCreatingCore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScopeOwnedProbeRecord>(entity =>
        {
            entity.ConfigureUserOwnedDependent();
            entity.Property(record => record.Ticker).IsRequired();
        });

        // Shared reference data: no ownership key, no filter.
        modelBuilder.Entity<ScopeSharedProbeRecord>(entity => entity.Property(record => record.Symbol).IsRequired());
    }
}

/// <summary>Test-only user-owned dependent (filtered on UserId).</summary>
public sealed class ScopeOwnedProbeRecord
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Ticker { get; set; } = string.Empty;
}

/// <summary>Test-only shared reference entity (never user-filtered).</summary>
public sealed class ScopeSharedProbeRecord
{
    public long Id { get; set; }

    public string Symbol { get; set; } = string.Empty;
}
