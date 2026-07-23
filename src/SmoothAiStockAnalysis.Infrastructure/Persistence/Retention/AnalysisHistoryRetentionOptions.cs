namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Retention;

/// <summary>
/// Retention policy mandated by LADR-002.
/// </summary>
public sealed class AnalysisHistoryRetentionOptions
{
    /// <summary>
    /// The number of calendar months of analysis history to retain.
    /// </summary>
    public int RetentionMonths { get; init; } = 1;
}
