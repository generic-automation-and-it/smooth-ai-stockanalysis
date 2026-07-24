namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Retention;

/// <summary>
/// Prunes analysis history that exceeds the configured retention policy.
/// </summary>
internal interface IAnalysisHistoryRetentionJob
{
    Task PruneExpiredHistoryAsync(CancellationToken cancellationToken = default);
}
