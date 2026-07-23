namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Retention;

/// <summary>
/// Retention policy mandated by LADR-002.
/// </summary>
public sealed class AnalysisHistoryRetentionOptions
{
    private int retentionMonths = 1;

    /// <summary>
    /// The number of calendar months of analysis history to retain.
    /// </summary>
    public int RetentionMonths
    {
        get => retentionMonths;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            retentionMonths = value;
        }
    }
}
