using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Retention;

/// <summary>
/// Runs the mandatory retention job daily once timestamped history exists.
/// </summary>
internal sealed class AnalysisHistoryRetentionHostedService(
    IAnalysisHistoryRetentionJob retentionJob,
    ILogger<AnalysisHistoryRetentionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // First tick fires after RunInterval, so the first retention run is one
        // day after Host startup; restart the service for an out-of-band sweep.
        using var timer = new PeriodicTimer(RunInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogInformation("Analysis-history retention started.");
            await retentionJob.PruneExpiredHistoryAsync(stoppingToken);
            logger.LogInformation("Analysis-history retention completed.");
        }
    }
}
