using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Application.Configuration;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Section bound for the user-recognition thresholds and scoring weightings (NFR-049).
/// </summary>
public sealed class AnalysisDefaultsOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "Analysis";

    /// <summary>Minimum company size in account currency. Default 250,000,000.</summary>
    public decimal CompanySizeFloor { get; set; } = 250_000_000m;

    /// <summary>Minimum average daily volume. Default 100,000.</summary>
    public decimal MinAverageDailyVolume { get; set; } = 100_000m;

    /// <summary>Minimum number of days traded. Default 30.</summary>
    public int MinDaysTraded { get; set; } = 30;

    /// <summary>Event scoring weight. Default 0.50.</summary>
    public decimal ScoringWeightEvent { get; set; } = 0.50m;

    /// <summary>Fundamental scoring weight. Default 0.30.</summary>
    public decimal ScoringWeightFundamental { get; set; } = 0.30m;

    /// <summary>Sentiment scoring weight. Default 0.20.</summary>
    public decimal ScoringWeightSentiment { get; set; } = 0.20m;

    /// <summary>Default holding horizon in days. Default 90.</summary>
    public int HoldingHorizonDays { get; set; } = 90;

    /// <summary>
    /// Binds the <c>Analysis</c> section from the supplied configuration.
    /// </summary>
    public static AnalysisDefaultsOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new AnalysisDefaultsOptions();
        configuration.GetSection(SectionName).Bind(options);
        return options;
    }

    internal AnalysisDefaults ToDefaults() => new(
        CompanySizeFloor,
        MinAverageDailyVolume,
        MinDaysTraded,
        ScoringWeightEvent,
        ScoringWeightFundamental,
        ScoringWeightSentiment,
        HoldingHorizonDays);
}
