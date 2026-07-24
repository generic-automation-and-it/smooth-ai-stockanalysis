using Microsoft.EntityFrameworkCore;
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
/// options builder to inherit the same convention.
/// </remarks>
public class SmoothAiStockAnalysisDbContext(DbContextOptions options)
    : DbContext(options)
{
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
    }
}
