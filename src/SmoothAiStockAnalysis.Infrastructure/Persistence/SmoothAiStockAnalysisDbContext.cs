using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the local SQLite database.
/// </summary>
/// <remarks>
/// Domain entities are introduced by their owning features. NodaTime value mappings are
/// registered globally so future persisted entities use the same lossless representation.
/// </remarks>
public class SmoothAiStockAnalysisDbContext(DbContextOptions options)
    : DbContext(options)
{
    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        NodaTimeSqliteConventions.Configure(configurationBuilder);
}
