using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    /// Registers the Infrastructure composition by reading the connection string
    /// from <paramref name="configuration"/> and delegating to
    /// <see cref="AddInfrastructurePersistence(IServiceCollection, string)"/>.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddInfrastructurePersistence(
            configuration.GetConnectionString("SmoothAiStockAnalysis")
                ?? throw new InvalidOperationException("Missing connection string 'SmoothAiStockAnalysis'."));

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
        // Intentionally registers a no-op binding so IOptions<AnalysisHistoryRetentionOptions>
        // resolves with the default RetentionMonths = 1 from the class field initializer.
        // Do not remove: required for the Host to resolve IOptions<T> on startup.
        services.AddSingleton<IAnalysisHistoryRetentionJob, AnalysisHistoryRetentionJob>();
        services.AddHostedService<SqliteDatabaseInitializer>();
        services.AddHostedService<AnalysisHistoryRetentionHostedService>();

        return services;
    }

    private static void EnsureDatabaseDirectoryExists(string connectionString)
    {
        string dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
        {
            // In-memory databases have no on-disk directory; the early return is intentional.
            return;
        }

        string? directoryPath = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
