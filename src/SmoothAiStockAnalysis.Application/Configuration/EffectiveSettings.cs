using SmoothAiStockAnalysis.Domain.Time;

namespace SmoothAiStockAnalysis.Application.Configuration;

/// <summary>
/// The fully-resolved, immutable settings for one user at a point in time.
/// </summary>
/// <remarks>
/// Produced by <see cref="ISettingsResolver"/> from the catalogue defaults and the user's
/// preference overrides (NFR-045). The shape mirrors <see cref="IApplicationDefaults"/> so
/// feature code can pick the field it needs without navigating a separate config object.
/// </remarks>
public sealed record EffectiveSettings(
    AnalysisSettings Analysis,
    CostCapSettings CostCaps,
    FxSettings Fx,
    CycleSettings Cycle,
    ProviderSettings Provider,
    DeliveryWindow DeliveryWindow);

/// <summary>The resolved analysis tunables for a user.</summary>
public sealed record AnalysisSettings(
    decimal CompanySizeFloor,
    decimal MinAverageDailyVolume,
    int MinDaysTraded,
    decimal ScoringWeightEvent,
    decimal ScoringWeightFundamental,
    decimal ScoringWeightSentiment,
    int HoldingHorizonDays);

/// <summary>The resolved per-cycle stage caps for a user.</summary>
public sealed record CostCapSettings(
    int Event,
    int Fundamental,
    int Reasoning,
    int Delivery);

/// <summary>The resolved FX multipliers for a user.</summary>
public sealed record FxSettings(
    decimal UsdEur,
    decimal UsdGbp,
    decimal UsdJpy);

/// <summary>The resolved cycle scheduling tunables for a user.</summary>
public sealed record CycleSettings(TimeSpan Interval);

/// <summary>The resolved non-secret provider selection for a user.</summary>
public sealed record ProviderSettings(
    string Reasoning,
    string ReasoningModel,
    string MarketData,
    string MarketDataModel);
