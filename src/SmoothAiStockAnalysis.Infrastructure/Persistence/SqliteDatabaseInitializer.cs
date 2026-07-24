using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// Applies local database migrations when the service starts.
/// </summary>
internal sealed class SqliteDatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<SqliteDatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying local SQLite database migrations.");

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Local SQLite database migrations applied.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
