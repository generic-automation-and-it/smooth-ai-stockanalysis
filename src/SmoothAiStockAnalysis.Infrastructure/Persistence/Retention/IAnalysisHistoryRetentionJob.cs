namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Retention;

/// <summary>
/// Prunes analysis history that exceeds the configured retention policy.
/// </summary>
public interface IAnalysisHistoryRetentionJob
{
    Task PruneExpiredHistoryAsync(CancellationToken cancellationToken = default);
}
