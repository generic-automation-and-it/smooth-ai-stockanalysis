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
    /// Registers the Infrastructure composition. The connection string is resolved
    /// when a <see cref="SmoothAiStockAnalysisDbContext"/> is created so later
    /// configuration providers, including integration-test overrides, are honoured.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services) =>
        RegisterInfrastructurePersistence(
            services,
            serviceProvider => serviceProvider.GetRequiredService<IConfiguration>()
                .GetConnectionString("SmoothAiStockAnalysis")
                ?? throw new InvalidOperationException("Missing connection string 'SmoothAiStockAnalysis'."));

    /// <summary>
    /// Registers the file-backed SQLite database and its lifecycle services.
    /// </summary>
    public static IServiceCollection AddInfrastructurePersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return RegisterInfrastructurePersistence(services, _ => connectionString);
    }

    private static IServiceCollection RegisterInfrastructurePersistence(
        IServiceCollection services,
        Func<IServiceProvider, string> connectionStringFactory)
    {
        services.AddSingleton<SqlitePragmaConnectionInterceptor>();
        services.AddScoped<DataAccessScopeAccessor>();
        services.AddScoped<IDataAccessScope>(sp => sp.GetRequiredService<DataAccessScopeAccessor>());
        services.AddScoped<IDataAccessScopeSetter>(sp => sp.GetRequiredService<DataAccessScopeAccessor>());
        services.AddScoped<ISystemDataAccessScope>(sp => sp.GetRequiredService<DataAccessScopeAccessor>());
        services.AddDbContext<SmoothAiStockAnalysisDbContext>((serviceProvider, options) =>
        {
            string connectionString = NormalizeConnectionString(connectionStringFactory(serviceProvider));
            EnsureDatabaseDirectoryExists(connectionString);
            options.UseSqlite(connectionString);
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(serviceProvider.GetRequiredService<SqlitePragmaConnectionInterceptor>());
        });
        // Resolve the context with its scope accessor so the global user-isolation filter is active.
        services.AddScoped(sp => new SmoothAiStockAnalysisDbContext(
            sp.GetRequiredService<Microsoft.EntityFrameworkCore.DbContextOptions<SmoothAiStockAnalysisDbContext>>(),
            sp.GetRequiredService<DataAccessScopeAccessor>()));
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

    private static string NormalizeConnectionString(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (builder.DataSource is not ":memory:" && !Path.IsPathFullyQualified(builder.DataSource))
        {
            builder.DataSource = Path.GetFullPath(builder.DataSource, AppContext.BaseDirectory);
        }

        return builder.ToString();
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
