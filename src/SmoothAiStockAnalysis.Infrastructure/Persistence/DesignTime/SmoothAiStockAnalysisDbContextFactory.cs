using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.DesignTime;

/// <summary>
/// Creates the production persistence model for EF Core design-time tooling.
/// </summary>
public sealed class SmoothAiStockAnalysisDbContextFactory
    : IDesignTimeDbContextFactory<SmoothAiStockAnalysisDbContext>
{
    /// <inheritdoc />
    public SmoothAiStockAnalysisDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SmoothAiStockAnalysisDbContext>()
            .UseSqlite("Data Source=:memory:")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SmoothAiStockAnalysisDbContext(options);
    }
}
