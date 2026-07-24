using System.Text.Json;
using System.Text.Json.Serialization;
using SmoothAiStockAnalysis.Domain.Documents;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

/// <summary>
/// Infrastructure representation of a user's versioned metadata document.
/// </summary>
/// <remarks>
/// Serialization and forward-compatible field retention remain at the persistence boundary;
/// the corresponding Domain document contains no JSON concerns. Schema version 2 adds typed
/// preference fields consumed by the F-004 settings catalogue (NFR-045); a persisted v1
/// document reads as a v2 document with all preferences unset so a legacy user behaves as
/// "no overrides" until the next save.
/// </remarks>
internal sealed class UserMetadataDocument : IVersionedDocument
{
    /// <summary>
    /// Gets or sets the serialized document contract version.
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>User override for the company-size floor; <c>null</c> when unset.</summary>
    public decimal? CompanySizeFloor { get; set; }

    /// <summary>User override for the minimum average daily volume; <c>null</c> when unset.</summary>
    public decimal? MinAverageDailyVolume { get; set; }

    /// <summary>User override for the minimum number of days traded; <c>null</c> when unset.</summary>
    public int? MinDaysTraded { get; set; }

    /// <summary>User override for the event scoring weight; <c>null</c> when unset.</summary>
    public decimal? ScoringWeightEvent { get; set; }

    /// <summary>User override for the fundamental scoring weight; <c>null</c> when unset.</summary>
    public decimal? ScoringWeightFundamental { get; set; }

    /// <summary>User override for the sentiment scoring weight; <c>null</c> when unset.</summary>
    public decimal? ScoringWeightSentiment { get; set; }

    /// <summary>User override for the holding horizon in days; <c>null</c> when unset.</summary>
    public int? HoldingHorizonDays { get; set; }

    /// <summary>User override for the event-detection stage cap; <c>null</c> when unset.</summary>
    public int? CostCapEvent { get; set; }

    /// <summary>User override for the fundamental-screening stage cap; <c>null</c> when unset.</summary>
    public int? CostCapFundamental { get; set; }

    /// <summary>User override for the reasoning stage cap; <c>null</c> when unset.</summary>
    public int? CostCapReasoning { get; set; }

    /// <summary>User override for the delivery stage cap; <c>null</c> when unset.</summary>
    public int? CostCapDelivery { get; set; }

    /// <summary>User override for the USD→EUR multiplier; <c>null</c> when unset.</summary>
    public decimal? FxUsdEur { get; set; }

    /// <summary>User override for the USD→GBP multiplier; <c>null</c> when unset.</summary>
    public decimal? FxUsdGbp { get; set; }

    /// <summary>User override for the USD→JPY multiplier; <c>null</c> when unset.</summary>
    public decimal? FxUsdJpy { get; set; }

    /// <summary>User override for the cycle interval; <c>null</c> when unset.</summary>
    public TimeSpan? CycleInterval { get; set; }

    /// <summary>User override for the delivery window TZDB IANA time-zone identifier; <c>null</c> when unset.</summary>
    public string? DeliveryWindowTimeZoneId { get; set; }

    /// <summary>User override for the delivery window inclusive start time (HH:mm); <c>null</c> when unset.</summary>
    public string? DeliveryWindowStart { get; set; }

    /// <summary>User override for the delivery window exclusive end time (HH:mm); <c>null</c> when unset.</summary>
    public string? DeliveryWindowEnd { get; set; }

    /// <summary>User override for the reasoning provider name; <c>null</c> when unset.</summary>
    public string? ProviderReasoning { get; set; }

    /// <summary>User override for the reasoning model identifier; <c>null</c> when unset.</summary>
    public string? ReasoningModel { get; set; }

    /// <summary>User override for the market-data provider name; <c>null</c> when unset.</summary>
    public string? ProviderMarketData { get; set; }

    /// <summary>User override for the market-data model identifier; <c>null</c> when unset.</summary>
    public string? MarketDataModel { get; set; }

    /// <summary>
    /// Gets or sets fields written by a newer document contract.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ForwardCompatibleFields { get; set; }

    /// <summary>
    /// Creates the persistence representation of Domain metadata.
    /// </summary>
    public static UserMetadataDocument FromDomain(UserMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return new UserMetadataDocument
        {
            SchemaVersion = metadata.SchemaVersion,
            CompanySizeFloor = metadata.CompanySizeFloor,
            MinAverageDailyVolume = metadata.MinAverageDailyVolume,
            MinDaysTraded = metadata.MinDaysTraded,
            ScoringWeightEvent = metadata.ScoringWeightEvent,
            ScoringWeightFundamental = metadata.ScoringWeightFundamental,
            ScoringWeightSentiment = metadata.ScoringWeightSentiment,
            HoldingHorizonDays = metadata.HoldingHorizonDays,
            CostCapEvent = metadata.CostCapEvent,
            CostCapFundamental = metadata.CostCapFundamental,
            CostCapReasoning = metadata.CostCapReasoning,
            CostCapDelivery = metadata.CostCapDelivery,
            FxUsdEur = metadata.FxUsdEur,
            FxUsdGbp = metadata.FxUsdGbp,
            FxUsdJpy = metadata.FxUsdJpy,
            CycleInterval = metadata.CycleInterval,
            DeliveryWindowTimeZoneId = metadata.DeliveryWindowTimeZoneId,
            DeliveryWindowStart = metadata.DeliveryWindowStart,
            DeliveryWindowEnd = metadata.DeliveryWindowEnd,
            ProviderReasoning = metadata.ProviderReasoning,
            ReasoningModel = metadata.ReasoningModel,
            ProviderMarketData = metadata.ProviderMarketData,
            MarketDataModel = metadata.MarketDataModel,
        };
    }

    /// <summary>
    /// Creates Domain metadata from this persistence document. The persisted schema version is
    /// preserved so a legacy v1 document reads as a v1 metadata whose preference fields are
    /// all unset — the resolver falls through to application defaults for unset fields, so a
    /// v1 user behaves as "no overrides" until the next <see cref="ApplyDomainState"/>, which
    /// promotes the document to the current schema version.
    /// </summary>
    public UserMetadata ToDomain() =>
        UserMetadata
            .Reconstitute(SchemaVersion)
            .WithPreferences(
                companySizeFloor: CompanySizeFloor,
                minAverageDailyVolume: MinAverageDailyVolume,
                minDaysTraded: MinDaysTraded,
                scoringWeightEvent: ScoringWeightEvent,
                scoringWeightFundamental: ScoringWeightFundamental,
                scoringWeightSentiment: ScoringWeightSentiment,
                holdingHorizonDays: HoldingHorizonDays,
                costCapEvent: CostCapEvent,
                costCapFundamental: CostCapFundamental,
                costCapReasoning: CostCapReasoning,
                costCapDelivery: CostCapDelivery,
                fxUsdEur: FxUsdEur,
                fxUsdGbp: FxUsdGbp,
                fxUsdJpy: FxUsdJpy,
                cycleInterval: CycleInterval,
                deliveryWindowTimeZoneId: DeliveryWindowTimeZoneId,
                deliveryWindowStart: DeliveryWindowStart,
                deliveryWindowEnd: DeliveryWindowEnd,
                providerReasoning: ProviderReasoning,
                reasoningModel: ReasoningModel,
                providerMarketData: ProviderMarketData,
                marketDataModel: MarketDataModel);

    /// <summary>
    /// Applies understood Domain state without discarding unknown persisted fields.
    /// </summary>
    public void ApplyDomainState(UserMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (metadata.SchemaVersion < SchemaVersion)
        {
            throw new InvalidOperationException(
                "A persisted metadata document cannot be downgraded to an earlier schema version.");
        }

        SchemaVersion = metadata.SchemaVersion;
        CompanySizeFloor = metadata.CompanySizeFloor;
        MinAverageDailyVolume = metadata.MinAverageDailyVolume;
        MinDaysTraded = metadata.MinDaysTraded;
        ScoringWeightEvent = metadata.ScoringWeightEvent;
        ScoringWeightFundamental = metadata.ScoringWeightFundamental;
        ScoringWeightSentiment = metadata.ScoringWeightSentiment;
        HoldingHorizonDays = metadata.HoldingHorizonDays;
        CostCapEvent = metadata.CostCapEvent;
        CostCapFundamental = metadata.CostCapFundamental;
        CostCapReasoning = metadata.CostCapReasoning;
        CostCapDelivery = metadata.CostCapDelivery;
        FxUsdEur = metadata.FxUsdEur;
        FxUsdGbp = metadata.FxUsdGbp;
        FxUsdJpy = metadata.FxUsdJpy;
        CycleInterval = metadata.CycleInterval;
        DeliveryWindowTimeZoneId = metadata.DeliveryWindowTimeZoneId;
        DeliveryWindowStart = metadata.DeliveryWindowStart;
        DeliveryWindowEnd = metadata.DeliveryWindowEnd;
        ProviderReasoning = metadata.ProviderReasoning;
        ReasoningModel = metadata.ReasoningModel;
        ProviderMarketData = metadata.ProviderMarketData;
        MarketDataModel = metadata.MarketDataModel;
    }
}
