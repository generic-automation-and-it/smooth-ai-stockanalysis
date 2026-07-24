using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Configurations;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the local SQLite database.
/// </summary>
/// <remarks>
/// Domain entities are introduced by their owning features. NodaTime value mappings are
/// registered globally so future persisted entities use the same lossless representation.
/// The global snake_case naming convention (LADR-016) is applied wherever production options
/// are built (Host DI registration and the design-time factory), so every entity yields
/// lower_snake_case table, column, key, and index names without per-entity configuration.
/// Derived test probe contexts must call <c>UseSnakeCaseNamingConvention()</c> on their own
/// options builder to inherit the same convention, and must add probe entities by overriding
/// <see cref="OnModelCreatingCore"/> so the global isolation filter still sees them.
///
/// User-owned data is isolated by a global query filter (LADR-017). The filter reads
/// <see cref="UserIsolationTenantKey"/> — a member on this context instance, which EF Core
/// remaps to the current context at query time — so each unit of work filters on its own scope.
/// Owned-data queries throw (fail closed) when no explicit scope is set; shared reference data is
/// unfiltered in every scope.
/// </remarks>
public class SmoothAiStockAnalysisDbContext : DbContext
{
    private readonly DataAccessScopeAccessor? _scopeAccessor;

    /// <summary>
    /// Initializes the context without a scope accessor (design-time tooling and probe contexts
    /// that only build the model). Owned-data queries on such a context fail closed.
    /// </summary>
    public SmoothAiStockAnalysisDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Initializes the context with the scoped data-access scope accessor that drives isolation.
    /// </summary>
    internal SmoothAiStockAnalysisDbContext(DbContextOptions options, DataAccessScopeAccessor scopeAccessor)
        : base(options) =>
        _scopeAccessor = scopeAccessor;

    /// <summary>
    /// Gets the tenant key the user-isolation filter applies, or <see langword="null"/> for the
    /// system scope's deliberate bypass.
    /// </summary>
    /// <remarks>
    /// Read by EF Core during query compilation against the current context instance. A non-null
    /// value is inlined as a constant so each scope filters on its own key; <see langword="null"/>
    /// (system scope) short-circuits the filter to a no-op. The getter throws when no scope is set,
    /// so an owned-data query with no explicit valid scope fails closed (NFR-041) rather than
    /// silently applying to nobody.
    /// </remarks>
    internal long? UserIsolationTenantKey =>
        CurrentScope.Kind == DataAccessScopeKind.System
            ? null
            : CurrentScope.UserId;

    /// <summary>
    /// Gets the current scope. Throws when no scope is set and no accessor is present.
    /// </summary>
    private DataAccessScope CurrentScope =>
        _scopeAccessor is not null
            ? _scopeAccessor.Current
            : throw new InvalidOperationException(
                "No data-access scope is available. Resolve the context through DI so a scope accessor is present, and set an explicit scope before querying.");

    /// <summary>
    /// Gets the user tenant-root set without exposing a convention-discovered
    /// <see cref="DbSet{TEntity}"/> property to derived probe contexts.
    /// </summary>
    internal DbSet<UserRecord> Users() => Set<UserRecord>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        NodaTimeSqliteConventions.Configure(configurationBuilder);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmoothAiStockAnalysisDbContext).Assembly);

        // Derived probe contexts add test-only entities here, before filters scan the model, so
        // owned dependents configured through ConfigureUserOwnedDependent still receive isolation.
        OnModelCreatingCore(modelBuilder);

        // The filter references this context instance's UserIsolationTenantKey member, so EF Core
        // evaluates it per current context at query time (no first-value caching across scopes).
        modelBuilder.ApplyDataAccessScopeFilters();
    }

    /// <summary>
    /// Extension point for derived probe contexts to register additional entities before the
    /// global user-isolation filter is applied.
    /// </summary>
    protected virtual void OnModelCreatingCore(ModelBuilder modelBuilder)
    {
    }
}
