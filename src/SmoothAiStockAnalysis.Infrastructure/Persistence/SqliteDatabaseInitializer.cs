using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// Creates the local database file when the service starts.
/// </summary>
internal sealed class SqliteDatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<SqliteDatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        string connectionString = dbContext.Database.GetConnectionString();

        logger.LogInformation("Initializing the local SQLite database '{DatabasePath}'.", connectionString);

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        logger.LogInformation("Local SQLite database '{DatabasePath}' initialized.", connectionString);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
