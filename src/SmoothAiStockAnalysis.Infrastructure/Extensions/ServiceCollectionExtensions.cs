using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Retention;

namespace SmoothAiStockAnalysis.Infrastructure.Extensions;

/// <summary>
/// Registers Infrastructure persistence adapters.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the file-backed SQLite database and its lifecycle services.
    /// </summary>
    public static IServiceCollection AddInfrastructurePersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        EnsureDatabaseDirectoryExists(connectionString);

        services.AddSingleton<SqlitePragmaConnectionInterceptor>();
        services.AddDbContext<SmoothAiStockAnalysisDbContext>((serviceProvider, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<SqlitePragmaConnectionInterceptor>());
        });
        services.AddScoped<IAnalysisCycleUnitOfWork, AnalysisCycleUnitOfWork>();
        services.Configure<AnalysisHistoryRetentionOptions>(_ => { });
        services.AddSingleton<IAnalysisHistoryRetentionJob, AnalysisHistoryRetentionJob>();
        services.AddHostedService<SqliteDatabaseInitializer>();
        services.AddHostedService<AnalysisHistoryRetentionHostedService>();

        return services;
    }

    private static void EnsureDatabaseDirectoryExists(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.Equals(builder.Mode, "Memory", StringComparison.OrdinalIgnoreCase)
            || (builder.DataSource?.StartsWith("file::memory:", StringComparison.OrdinalIgnoreCase) ?? false)
            || builder.DataSource == ":memory:")
        {
            return;
        }

        string? directoryPath = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
