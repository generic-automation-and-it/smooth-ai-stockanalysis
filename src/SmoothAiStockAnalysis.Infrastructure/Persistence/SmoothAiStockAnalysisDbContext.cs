using Microsoft.EntityFrameworkCore;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the local SQLite database.
/// </summary>
/// <remarks>
/// Domain entities are introduced by their owning features. This foundation intentionally
/// contains no business tables or time mappings.
/// </remarks>
public sealed class SmoothAiStockAnalysisDbContext(DbContextOptions<SmoothAiStockAnalysisDbContext> options)
    : DbContext(options)
{
}
