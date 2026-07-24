using SmoothAiStockAnalysis.Domain.Time;

namespace SmoothAiStockAnalysis.Application.Configuration;

/// <summary>
/// The typed application defaults that the F-004 two-layer resolver merges with per-user
/// overrides (NFR-045, HLD §7.2).
/// </summary>
/// <remarks>
/// This is the single source of application-side defaults consumed by <see cref="ISettingsResolver"/>.
/// Values are non-secret tunables only; credentials live in environment variables and never enter
/// this contract (NFR-043/044).
/// </remarks>
public interface IApplicationDefaults
{
    /// <summary>
    /// Gets the user-recognition thresholds and scoring weightings.
    /// </summary>
    AnalysisDefaults Analysis { get; }

    /// <summary>
    /// Gets the per-cycle stage caps (NFR-025).
    /// </summary>
    CostCaps CostCaps { get; }

    /// <summary>
    /// Gets the static currency conversion multipliers (NFR-050).
    /// </summary>
    FxMultipliers FxMultipliers { get; }

    /// <summary>
    /// Gets the cycle scheduling tunables.
    /// </summary>
    CycleDefaults Cycle { get; }

    /// <summary>
    /// Gets the non-secret provider and model selection (NFR-021).
    /// </summary>
    ProviderDefaults Provider { get; }

    /// <summary>
    /// Builds the default <see cref="DeliveryWindow"/> before any user override is applied.
    /// </summary>
    DeliveryWindow GetDefaultDeliveryWindow();
}

/// <summary>
/// User-recognition thresholds and scoring weightings. Defaults are documented alongside each
/// value (NFR-049).
/// </summary>
public sealed record AnalysisDefaults(
    decimal CompanySizeFloor,
    decimal MinAverageDailyVolume,
    int MinDaysTraded,
    decimal ScoringWeightEvent,
    decimal ScoringWeightFundamental,
    decimal ScoringWeightSentiment,
    int HoldingHorizonDays);

/// <summary>
/// Per-cycle stage caps. Default values follow NFR-025 (50 / 20 / 10 / 5).
/// </summary>
public sealed record CostCaps(
    int Event,
    int Fundamental,
    int Reasoning,
    int Delivery);

/// <summary>
/// Static USD-to-target currency conversion multipliers (NFR-050). Refresh is intentionally
/// deferred — drift on a coarse size threshold is immaterial, so a periodic refresh with a
/// change threshold is a future, low-priority requirement rather than a now-built one.
/// </summary>
public sealed record FxMultipliers(
    decimal UsdEur,
    decimal UsdGbp,
    decimal UsdJpy);

/// <summary>
/// Cycle scheduling tunables: the interval between cycles and the delivery window.
/// </summary>
/// <param name="Interval">The cycle interval.</param>
/// <param name="DeliveryWindowTimeZoneId">The TZDB IANA zone used to evaluate the window.</param>
/// <param name="DeliveryWindowStart">The inclusive start time in <c>HH:mm</c> format.</param>
/// <param name="DeliveryWindowEnd">The exclusive end time in <c>HH:mm</c> format.</param>
public sealed record CycleDefaults(
    TimeSpan Interval,
    string DeliveryWindowTimeZoneId,
    string DeliveryWindowStart,
    string DeliveryWindowEnd);

/// <summary>
/// Non-secret provider and model selection (NFR-021, NFR-043/044). Credentials are not part
/// of this contract.
/// </summary>
public sealed record ProviderDefaults(
    string Reasoning,
    string ReasoningModel,
    string MarketData,
    string MarketDataModel);
