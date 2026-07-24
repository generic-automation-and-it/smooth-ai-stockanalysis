using NodaTime;
using NodaTime.Text;
using SmoothAiStockAnalysis.Application.Common.Configuration;
using SmoothAiStockAnalysis.Application.Configuration;
using SmoothAiStockAnalysis.Domain.Documents;
using SmoothAiStockAnalysis.Domain.Time;

namespace SmoothAiStockAnalysis.Application.UnitTest;

/// <summary>
/// L0 coverage of the pure two-layer merge that lives in <see cref="SettingsResolver.Resolve"/>
/// (NFR-045). Override path, default path, and partial override are exercised across the
/// typed-shape variety required by the worktask acceptance criteria.
/// </summary>
public sealed class EffectiveSettingsTests
{
    private static readonly IApplicationDefaults Defaults = new TestDefaults(
        Analysis: new AnalysisDefaults(
            CompanySizeFloor: 250_000_000m,
            MinAverageDailyVolume: 100_000m,
            MinDaysTraded: 30,
            ScoringWeightEvent: 0.50m,
            ScoringWeightFundamental: 0.30m,
            ScoringWeightSentiment: 0.20m,
            HoldingHorizonDays: 90),
        CostCaps: new CostCaps(Event: 50, Fundamental: 20, Reasoning: 10, Delivery: 5),
        Fx: new FxMultipliers(UsdEur: 0.92m, UsdGbp: 0.79m, UsdJpy: 150.0m),
        Cycle: new CycleDefaults(
            Interval: TimeSpan.FromMinutes(15),
            DeliveryWindowTimeZoneId: "Europe/Paris",
            DeliveryWindowStart: "07:00",
            DeliveryWindowEnd: "22:00"),
        Provider: new ProviderDefaults(
            Reasoning: "OpenAI",
            ReasoningModel: "gpt-4o-mini",
            MarketData: "OpenAI",
            MarketDataModel: "gpt-4o-mini"));

    [Fact]
    public void EmptyMetadataResolvesToApplicationDefaults()
    {
        EffectiveSettings resolved = Resolve(UserMetadata.Create());

        resolved.Analysis.CompanySizeFloor.ShouldBe(Defaults.Analysis.CompanySizeFloor);
        resolved.Analysis.HoldingHorizonDays.ShouldBe(90);
        resolved.CostCaps.Event.ShouldBe(50);
        resolved.CostCaps.Reasoning.ShouldBe(10);
        resolved.Fx.UsdEur.ShouldBe(0.92m);
        resolved.Cycle.Interval.ShouldBe(TimeSpan.FromMinutes(15));
        resolved.DeliveryWindow.TimeZoneId.ShouldBe("Europe/Paris");
        resolved.Provider.Reasoning.ShouldBe("OpenAI");
    }

    [Fact]
    public void FullOverrideReplacesEveryValue()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(
            companySizeFloor: 1_000_000_000m,
            minAverageDailyVolume: 250_000m,
            minDaysTraded: 60,
            scoringWeightEvent: 0.7m,
            scoringWeightFundamental: 0.2m,
            scoringWeightSentiment: 0.1m,
            holdingHorizonDays: 180,
            costCapEvent: 100,
            costCapFundamental: 40,
            costCapReasoning: 20,
            costCapDelivery: 10,
            fxUsdEur: 0.95m,
            fxUsdGbp: 0.82m,
            fxUsdJpy: 155.0m,
            cycleInterval: TimeSpan.FromMinutes(5),
            deliveryWindowTimeZoneId: "America/New_York",
            deliveryWindowStart: "08:30",
            deliveryWindowEnd: "17:00",
            providerReasoning: "Anthropic",
            reasoningModel: "claude-haiku-4-5-20251001",
            providerMarketData: "Anthropic",
            marketDataModel: "claude-haiku-4-5-20251001");

        EffectiveSettings resolved = Resolve(metadata);

        resolved.Analysis.CompanySizeFloor.ShouldBe(1_000_000_000m);
        resolved.Analysis.HoldingHorizonDays.ShouldBe(180);
        resolved.CostCaps.Reasoning.ShouldBe(20);
        resolved.Fx.UsdEur.ShouldBe(0.95m);
        resolved.Cycle.Interval.ShouldBe(TimeSpan.FromMinutes(5));
        resolved.DeliveryWindow.TimeZoneId.ShouldBe("America/New_York");
        resolved.DeliveryWindow.Start.ShouldBe(new LocalTime(8, 30));
        resolved.DeliveryWindow.End.ShouldBe(new LocalTime(17, 0));
        resolved.Provider.Reasoning.ShouldBe("Anthropic");
    }

    [Fact]
    public void PartialOverrideFallsThroughToDefault()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(
            companySizeFloor: 750_000_000m,
            holdingHorizonDays: 120);

        EffectiveSettings resolved = Resolve(metadata);

        // Overridden
        resolved.Analysis.CompanySizeFloor.ShouldBe(750_000_000m);
        resolved.Analysis.HoldingHorizonDays.ShouldBe(120);

        // Default fall-through
        resolved.Analysis.MinAverageDailyVolume.ShouldBe(Defaults.Analysis.MinAverageDailyVolume);
        resolved.Analysis.ScoringWeightEvent.ShouldBe(Defaults.Analysis.ScoringWeightEvent);
        resolved.CostCaps.Event.ShouldBe(Defaults.CostCaps.Event);
        resolved.CostCaps.Reasoning.ShouldBe(Defaults.CostCaps.Reasoning);
        resolved.Fx.UsdJpy.ShouldBe(Defaults.FxMultipliers.UsdJpy);
        resolved.Cycle.Interval.ShouldBe(Defaults.Cycle.Interval);
        resolved.DeliveryWindow.TimeZoneId.ShouldBe(Defaults.Cycle.DeliveryWindowTimeZoneId);
    }

    [Fact]
    public void ZeroNumericOverridesAreHonouredAndDoNotFallThrough()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(
            companySizeFloor: 0m,
            costCapReasoning: 0,
            cycleInterval: TimeSpan.Zero);

        EffectiveSettings resolved = Resolve(metadata);

        // Null means unset; zero is a deliberate override (NFR-045).
        resolved.Analysis.CompanySizeFloor.ShouldBe(0m);
        resolved.CostCaps.Reasoning.ShouldBe(0);
        resolved.Cycle.Interval.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void DeliveryWindowOverrideUsesProvidedStrings()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(
            deliveryWindowStart: "08:30",
            deliveryWindowEnd: "17:00");

        EffectiveSettings resolved = Resolve(metadata);

        resolved.DeliveryWindow.Start.ShouldBe(new LocalTime(8, 30));
        resolved.DeliveryWindow.End.ShouldBe(new LocalTime(17, 0));
        // Time zone falls through because the override didn't supply one.
        resolved.DeliveryWindow.TimeZoneId.ShouldBe(Defaults.Cycle.DeliveryWindowTimeZoneId);
    }

    [Fact]
    public void DeliveryWindowOverrideRejectsMalformedTime()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(
            deliveryWindowStart: "not-a-time");

        Should.Throw<ArgumentException>(() => Resolve(metadata));
    }

    [Fact]
    public void DeliveryWindowOverrideRejectsUnknownTimeZone()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(
            deliveryWindowTimeZoneId: "Not/AZone");

        Should.Throw<ArgumentException>(() => Resolve(metadata));
    }

    [Fact]
    public void StringProviderOverridesFallThroughOnBlank()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(
            providerReasoning: "  ");

        EffectiveSettings resolved = Resolve(metadata);

        resolved.Provider.Reasoning.ShouldBe(Defaults.Provider.Reasoning);
    }

    [Fact]
    public void DecimalOverridesFallThroughOnNull()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(
            fxUsdEur: null);

        EffectiveSettings resolved = Resolve(metadata);

        resolved.Fx.UsdEur.ShouldBe(Defaults.FxMultipliers.UsdEur);
    }

    [Fact]
    public void IntOverridesFallThroughOnNull()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(
            costCapReasoning: null);

        EffectiveSettings resolved = Resolve(metadata);

        resolved.CostCaps.Reasoning.ShouldBe(Defaults.CostCaps.Reasoning);
    }

    [Fact]
    public async Task ResolveForUserAsyncRejectsNonPositiveUserId()
    {
        var resolver = new SettingsResolver(Defaults, new ThrowingMetadataProvider());

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => resolver.ResolveForUserAsync(0, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => resolver.ResolveForUserAsync(-1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResolveForUserAsyncLoadsMetadataAndMerges()
    {
        UserMetadata metadata = UserMetadata.Create().WithPreferences(companySizeFloor: 900_000_000m);
        var provider = new FixedMetadataProvider(metadata);
        var resolver = new SettingsResolver(Defaults, provider);

        EffectiveSettings resolved = await resolver.ResolveForUserAsync(42, TestContext.Current.CancellationToken);

        resolved.Analysis.CompanySizeFloor.ShouldBe(900_000_000m);
        resolved.CostCaps.Event.ShouldBe(Defaults.CostCaps.Event);
        provider.LastRequestedUserId.ShouldBe(42);
    }

    private static EffectiveSettings Resolve(UserMetadata metadata)
    {
        var resolver = new SettingsResolver(Defaults, new ThrowingMetadataProvider());
        return resolver.Resolve(Defaults, metadata);
    }

    private sealed class ThrowingMetadataProvider : IUserMetadataProvider
    {
        public Task<UserMetadata> GetForUserAsync(long userId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Resolve(defaults, metadata) must not load from the port.");
    }

    private sealed class FixedMetadataProvider(UserMetadata metadata) : IUserMetadataProvider
    {
        public long? LastRequestedUserId { get; private set; }

        public Task<UserMetadata> GetForUserAsync(long userId, CancellationToken cancellationToken = default)
        {
            LastRequestedUserId = userId;
            return Task.FromResult(metadata);
        }
    }

    private sealed record TestDefaults(
        AnalysisDefaults Analysis,
        CostCaps CostCaps,
        FxMultipliers Fx,
        CycleDefaults Cycle,
        ProviderDefaults Provider) : IApplicationDefaults
    {
        public FxMultipliers FxMultipliers => Fx;
        public DeliveryWindow GetDefaultDeliveryWindow() =>
            new(
                Cycle.DeliveryWindowTimeZoneId,
                LocalTimePattern.CreateWithInvariantCulture("HH:mm").Parse(Cycle.DeliveryWindowStart).Value,
                LocalTimePattern.CreateWithInvariantCulture("HH:mm").Parse(Cycle.DeliveryWindowEnd).Value);
    }
}
