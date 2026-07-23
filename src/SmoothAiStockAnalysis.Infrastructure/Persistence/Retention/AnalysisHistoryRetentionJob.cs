using Microsoft.Extensions.Options;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Retention;

/// <summary>
/// Retention-job seam. Pruning is added with timestamped analysis-history entities in F-003/M3.
/// </summary>
internal sealed class AnalysisHistoryRetentionJob(IOptions<AnalysisHistoryRetentionOptions> options)
    : IAnalysisHistoryRetentionJob
{
    internal int RetentionMonths => options.Value.RetentionMonths;

    public Task PruneExpiredHistoryAsync(CancellationToken cancellationToken = default)
    {
        // No timestamped analysis-history entity exists yet. NodaTime: see time-foundation worktask.
        return Task.CompletedTask;
    }
}
