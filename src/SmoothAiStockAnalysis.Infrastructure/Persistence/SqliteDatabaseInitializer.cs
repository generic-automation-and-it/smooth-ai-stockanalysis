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
        logger.LogInformation("Initializing the local SQLite database.");

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        logger.LogInformation("Local SQLite database initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
