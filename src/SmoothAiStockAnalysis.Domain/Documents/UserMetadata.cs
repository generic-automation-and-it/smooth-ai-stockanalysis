namespace SmoothAiStockAnalysis.Domain.Documents;

/// <summary>
/// Versioned user metadata whose fields can evolve without changing its persistence column.
/// </summary>
/// <remarks>
/// Schema version 2 adds the typed preference fields that the F-004 catalogue resolves against
/// (NFR-045). Schema version 1 metadata is read as version 2 with all preferences null — the
/// preference fields are an additive change and an empty override is the correct
/// fall-through-to-default state for a legacy user. See LADR-015 for the version-bump contract.
/// </remarks>
public sealed class UserMetadata : IVersionedDocument
{
    /// <summary>
    /// The schema version assigned to newly created metadata.
    /// </summary>
    public const int CurrentSchemaVersion = 2;

    private UserMetadata(
        int schemaVersion,
        decimal? companySizeFloor,
        decimal? minAverageDailyVolume,
        int? minDaysTraded,
        decimal? scoringWeightEvent,
        decimal? scoringWeightFundamental,
        decimal? scoringWeightSentiment,
        int? holdingHorizonDays,
        int? costCapEvent,
        int? costCapFundamental,
        int? costCapReasoning,
        int? costCapDelivery,
        decimal? fxUsdEur,
        decimal? fxUsdGbp,
        decimal? fxUsdJpy,
        TimeSpan? cycleInterval,
        string? deliveryWindowTimeZoneId,
        string? deliveryWindowStart,
        string? deliveryWindowEnd,
        string? providerReasoning,
        string? reasoningModel,
        string? providerMarketData,
        string? marketDataModel)
    {
        SchemaVersion = schemaVersion;
        CompanySizeFloor = companySizeFloor;
        MinAverageDailyVolume = minAverageDailyVolume;
        MinDaysTraded = minDaysTraded;
        ScoringWeightEvent = scoringWeightEvent;
        ScoringWeightFundamental = scoringWeightFundamental;
        ScoringWeightSentiment = scoringWeightSentiment;
        HoldingHorizonDays = holdingHorizonDays;
        CostCapEvent = costCapEvent;
        CostCapFundamental = costCapFundamental;
        CostCapReasoning = costCapReasoning;
        CostCapDelivery = costCapDelivery;
        FxUsdEur = fxUsdEur;
        FxUsdGbp = fxUsdGbp;
        FxUsdJpy = fxUsdJpy;
        CycleInterval = cycleInterval;
        DeliveryWindowTimeZoneId = deliveryWindowTimeZoneId;
        DeliveryWindowStart = deliveryWindowStart;
        DeliveryWindowEnd = deliveryWindowEnd;
        ProviderReasoning = providerReasoning;
        ReasoningModel = reasoningModel;
        ProviderMarketData = providerMarketData;
        MarketDataModel = marketDataModel;
    }

    /// <inheritdoc />
    public int SchemaVersion { get; }

    /// <summary>
    /// Gets the user override for the company-size floor in account currency, or <c>null</c> if
    /// unset. Unset means fall through to the application default (NFR-045).
    /// </summary>
    public decimal? CompanySizeFloor { get; }

    /// <summary>
    /// Gets the user override for the minimum average daily volume, or <c>null</c> if unset.
    /// </summary>
    public decimal? MinAverageDailyVolume { get; }

    /// <summary>
    /// Gets the user override for the minimum number of days traded, or <c>null</c> if unset.
    /// </summary>
    public int? MinDaysTraded { get; }

    /// <summary>
    /// Gets the user override for the event scoring weight, or <c>null</c> if unset.
    /// </summary>
    public decimal? ScoringWeightEvent { get; }

    /// <summary>
    /// Gets the user override for the fundamental scoring weight, or <c>null</c> if unset.
    /// </summary>
    public decimal? ScoringWeightFundamental { get; }

    /// <summary>
    /// Gets the user override for the sentiment scoring weight, or <c>null</c> if unset.
    /// </summary>
    public decimal? ScoringWeightSentiment { get; }

    /// <summary>
    /// Gets the user override for the holding horizon in days, or <c>null</c> if unset.
    /// </summary>
    public int? HoldingHorizonDays { get; }

    /// <summary>
    /// Gets the user override for the event-detection stage cap, or <c>null</c> if unset.
    /// </summary>
    public int? CostCapEvent { get; }

    /// <summary>
    /// Gets the user override for the fundamental-screening stage cap, or <c>null</c> if unset.
    /// </summary>
    public int? CostCapFundamental { get; }

    /// <summary>
    /// Gets the user override for the reasoning stage cap, or <c>null</c> if unset.
    /// </summary>
    public int? CostCapReasoning { get; }

    /// <summary>
    /// Gets the user override for the delivery stage cap, or <c>null</c> if unset.
    /// </summary>
    public int? CostCapDelivery { get; }

    /// <summary>
    /// Gets the user override for the USD→EUR conversion multiplier, or <c>null</c> if unset.
    /// </summary>
    public decimal? FxUsdEur { get; }

    /// <summary>
    /// Gets the user override for the USD→GBP conversion multiplier, or <c>null</c> if unset.
    /// </summary>
    public decimal? FxUsdGbp { get; }

    /// <summary>
    /// Gets the user override for the USD→JPY conversion multiplier, or <c>null</c> if unset.
    /// </summary>
    public decimal? FxUsdJpy { get; }

    /// <summary>
    /// Gets the user override for the analysis cycle interval, or <c>null</c> if unset.
    /// </summary>
    public TimeSpan? CycleInterval { get; }

    /// <summary>
    /// Gets the user override for the delivery window TZDB IANA time-zone identifier, or
    /// <c>null</c> if unset. Stored as a string so a malformed value cannot fail the
    /// metadata round-trip; the resolver parses and validates when composing the effective
    /// <see cref="Time.DeliveryWindow"/>.
    /// </summary>
    public string? DeliveryWindowTimeZoneId { get; }

    /// <summary>
    /// Gets the user override for the delivery window inclusive start time in <c>HH:mm</c>
    /// format, or <c>null</c> if unset.
    /// </summary>
    public string? DeliveryWindowStart { get; }

    /// <summary>
    /// Gets the user override for the delivery window exclusive end time in <c>HH:mm</c>
    /// format, or <c>null</c> if unset.
    /// </summary>
    public string? DeliveryWindowEnd { get; }

    /// <summary>
    /// Gets the user override for the reasoning provider name, or <c>null</c> if unset.
    /// </summary>
    public string? ProviderReasoning { get; }

    /// <summary>
    /// Gets the user override for the reasoning model identifier, or <c>null</c> if unset.
    /// </summary>
    public string? ReasoningModel { get; }

    /// <summary>
    /// Gets the user override for the market-data provider name, or <c>null</c> if unset.
    /// </summary>
    public string? ProviderMarketData { get; }

    /// <summary>
    /// Gets the user override for the market-data model identifier, or <c>null</c> if unset.
    /// </summary>
    public string? MarketDataModel { get; }

    /// <summary>
    /// Creates empty metadata at the current schema version. All preferences are unset and
    /// resolve to application defaults.
    /// </summary>
    public static UserMetadata Create() => new(CurrentSchemaVersion, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

    /// <summary>
    /// Reconstitutes metadata with the supplied persisted schema version. All preference
    /// fields start null; the Infrastructure document carries the values and applies them
    /// through <see cref="WithPreferences"/>. The schema version is preserved so the legacy
    /// v1 additive-migration policy is applied by the Infrastructure document rather than by
    /// the Domain factory.
    /// </summary>
    public static UserMetadata Reconstitute(int schemaVersion)
    {
        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(schemaVersion),
                schemaVersion,
                "A metadata schema version must be positive.");
        }

        return new UserMetadata(
            schemaVersion,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null);
    }

    /// <summary>
    /// Returns a new instance with the same schema version and the supplied preferences applied.
    /// </summary>
    /// <remarks>
    /// Intended for the Infrastructure document translator. Passing a null argument leaves the
    /// corresponding preference unset (so the application default is used at resolution time).
    /// </remarks>
    public UserMetadata WithPreferences(
        decimal? companySizeFloor = null,
        decimal? minAverageDailyVolume = null,
        int? minDaysTraded = null,
        decimal? scoringWeightEvent = null,
        decimal? scoringWeightFundamental = null,
        decimal? scoringWeightSentiment = null,
        int? holdingHorizonDays = null,
        int? costCapEvent = null,
        int? costCapFundamental = null,
        int? costCapReasoning = null,
        int? costCapDelivery = null,
        decimal? fxUsdEur = null,
        decimal? fxUsdGbp = null,
        decimal? fxUsdJpy = null,
        TimeSpan? cycleInterval = null,
        string? deliveryWindowTimeZoneId = null,
        string? deliveryWindowStart = null,
        string? deliveryWindowEnd = null,
        string? providerReasoning = null,
        string? reasoningModel = null,
        string? providerMarketData = null,
        string? marketDataModel = null) =>
        new(
            SchemaVersion,
            companySizeFloor ?? CompanySizeFloor,
            minAverageDailyVolume ?? MinAverageDailyVolume,
            minDaysTraded ?? MinDaysTraded,
            scoringWeightEvent ?? ScoringWeightEvent,
            scoringWeightFundamental ?? ScoringWeightFundamental,
            scoringWeightSentiment ?? ScoringWeightSentiment,
            holdingHorizonDays ?? HoldingHorizonDays,
            costCapEvent ?? CostCapEvent,
            costCapFundamental ?? CostCapFundamental,
            costCapReasoning ?? CostCapReasoning,
            costCapDelivery ?? CostCapDelivery,
            fxUsdEur ?? FxUsdEur,
            fxUsdGbp ?? FxUsdGbp,
            fxUsdJpy ?? FxUsdJpy,
            cycleInterval ?? CycleInterval,
            deliveryWindowTimeZoneId ?? DeliveryWindowTimeZoneId,
            deliveryWindowStart ?? DeliveryWindowStart,
            deliveryWindowEnd ?? DeliveryWindowEnd,
            providerReasoning ?? ProviderReasoning,
            reasoningModel ?? ReasoningModel,
            providerMarketData ?? ProviderMarketData,
            marketDataModel ?? MarketDataModel);
}
