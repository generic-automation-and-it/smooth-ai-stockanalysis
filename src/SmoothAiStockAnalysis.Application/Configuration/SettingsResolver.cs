using NodaTime;
using NodaTime.Text;
using SmoothAiStockAnalysis.Application.Common.Configuration;
using SmoothAiStockAnalysis.Domain.Documents;
using SmoothAiStockAnalysis.Domain.Time;

namespace SmoothAiStockAnalysis.Application.Configuration;

/// <summary>
/// Default <see cref="ISettingsResolver"/> implementation. Loads the user's metadata through
/// <see cref="IUserMetadataProvider"/>, then performs the pure merge with the application
/// defaults.
/// </summary>
public sealed class SettingsResolver(
    IApplicationDefaults defaults,
    IUserMetadataProvider userMetadataProvider) : ISettingsResolver
{
    private static readonly LocalTimePattern LocalTimePattern =
        LocalTimePattern.CreateWithInvariantCulture("HH:mm");

    /// <inheritdoc />
    public async Task<EffectiveSettings> ResolveForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), userId, "A user identifier must be positive.");
        }

        UserMetadata metadata = await userMetadataProvider
            .GetForUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return Resolve(defaults, metadata);
    }

    /// <inheritdoc />
    public EffectiveSettings Resolve(IApplicationDefaults applicationDefaults, UserMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(applicationDefaults);
        ArgumentNullException.ThrowIfNull(metadata);

        AnalysisSettings analysis = ResolveAnalysis(applicationDefaults, metadata);
        CostCapSettings costCaps = ResolveCostCaps(applicationDefaults, metadata);
        FxSettings fx = ResolveFx(applicationDefaults, metadata);
        CycleSettings cycle = ResolveCycle(applicationDefaults, metadata);
        ProviderSettings provider = ResolveProvider(applicationDefaults, metadata);
        DeliveryWindow deliveryWindow = ResolveDeliveryWindow(applicationDefaults, metadata);

        return new EffectiveSettings(analysis, costCaps, fx, cycle, provider, deliveryWindow);
    }

    private static AnalysisSettings ResolveAnalysis(IApplicationDefaults defaults, UserMetadata metadata) =>
        new(
            CompanySizeFloor: metadata.CompanySizeFloor ?? defaults.Analysis.CompanySizeFloor,
            MinAverageDailyVolume: metadata.MinAverageDailyVolume ?? defaults.Analysis.MinAverageDailyVolume,
            MinDaysTraded: metadata.MinDaysTraded ?? defaults.Analysis.MinDaysTraded,
            ScoringWeightEvent: metadata.ScoringWeightEvent ?? defaults.Analysis.ScoringWeightEvent,
            ScoringWeightFundamental: metadata.ScoringWeightFundamental ?? defaults.Analysis.ScoringWeightFundamental,
            ScoringWeightSentiment: metadata.ScoringWeightSentiment ?? defaults.Analysis.ScoringWeightSentiment,
            HoldingHorizonDays: metadata.HoldingHorizonDays ?? defaults.Analysis.HoldingHorizonDays);

    private static CostCapSettings ResolveCostCaps(IApplicationDefaults defaults, UserMetadata metadata) =>
        new(
            Event: metadata.CostCapEvent ?? defaults.CostCaps.Event,
            Fundamental: metadata.CostCapFundamental ?? defaults.CostCaps.Fundamental,
            Reasoning: metadata.CostCapReasoning ?? defaults.CostCaps.Reasoning,
            Delivery: metadata.CostCapDelivery ?? defaults.CostCaps.Delivery);

    private static FxSettings ResolveFx(IApplicationDefaults defaults, UserMetadata metadata) =>
        new(
            UsdEur: metadata.FxUsdEur ?? defaults.FxMultipliers.UsdEur,
            UsdGbp: metadata.FxUsdGbp ?? defaults.FxMultipliers.UsdGbp,
            UsdJpy: metadata.FxUsdJpy ?? defaults.FxMultipliers.UsdJpy);

    private static CycleSettings ResolveCycle(IApplicationDefaults defaults, UserMetadata metadata) =>
        new(Interval: metadata.CycleInterval ?? defaults.Cycle.Interval);

    private static ProviderSettings ResolveProvider(IApplicationDefaults defaults, UserMetadata metadata) =>
        new(
            Reasoning: OverrideOrDefault(metadata.ProviderReasoning, defaults.Provider.Reasoning),
            ReasoningModel: OverrideOrDefault(metadata.ReasoningModel, defaults.Provider.ReasoningModel),
            MarketData: OverrideOrDefault(metadata.ProviderMarketData, defaults.Provider.MarketData),
            MarketDataModel: OverrideOrDefault(metadata.MarketDataModel, defaults.Provider.MarketDataModel));

    private static DeliveryWindow ResolveDeliveryWindow(IApplicationDefaults defaults, UserMetadata metadata)
    {
        bool hasOverride = !string.IsNullOrWhiteSpace(metadata.DeliveryWindowTimeZoneId)
            || !string.IsNullOrWhiteSpace(metadata.DeliveryWindowStart)
            || !string.IsNullOrWhiteSpace(metadata.DeliveryWindowEnd);

        if (!hasOverride)
        {
            return defaults.GetDefaultDeliveryWindow();
        }

        string timeZoneId = OverrideOrDefault(metadata.DeliveryWindowTimeZoneId, defaults.Cycle.DeliveryWindowTimeZoneId);
        LocalTime start = ParseLocalTime(
            OverrideOrDefault(metadata.DeliveryWindowStart, defaults.Cycle.DeliveryWindowStart),
            nameof(metadata.DeliveryWindowStart));
        LocalTime end = ParseLocalTime(
            OverrideOrDefault(metadata.DeliveryWindowEnd, defaults.Cycle.DeliveryWindowEnd),
            nameof(metadata.DeliveryWindowEnd));

        return new DeliveryWindow(timeZoneId, start, end);
    }

    private static string OverrideOrDefault(string? overrideValue, string defaultValue) =>
        string.IsNullOrWhiteSpace(overrideValue) ? defaultValue : overrideValue;

    private static LocalTime ParseLocalTime(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        ParseResult<LocalTime> result = LocalTimePattern.Parse(value);
        if (!result.Success)
        {
            throw new ArgumentException(
                $"Delivery window time must use the HH:mm format (parameter '{parameterName}').",
                parameterName,
                result.Exception);
        }

        return result.Value;
    }
}
