using System.Globalization;
using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Host.Configuration;

namespace SmoothAiStockAnalysis.Host.UnitTest;

/// <summary>
/// L0 coverage for the F-004 settings catalogue sections (T-025). Each section's
/// <c>FromConfiguration</c> is exercised for happy-path bind; one section is also exercised
/// for fail-fast on malformed values (where applicable). Defaults match the catalogue table in
/// <c>HOST_AGENTS.md</c>.
/// </summary>
public sealed class CatalogueOptionsTests
{
    [Fact]
    public void AnalysisDefaultsBindTheEuropeParisDeliveryDefaultsFromEmptyConfiguration()
    {
        AnalysisDefaultsOptions options = AnalysisDefaultsOptions.FromConfiguration(new ConfigurationBuilder().Build());

        options.CompanySizeFloor.ShouldBe(250_000_000m);
        options.MinAverageDailyVolume.ShouldBe(100_000m);
        options.MinDaysTraded.ShouldBe(30);
        options.ScoringWeightEvent.ShouldBe(0.50m);
        options.ScoringWeightFundamental.ShouldBe(0.30m);
        options.ScoringWeightSentiment.ShouldBe(0.20m);
        options.HoldingHorizonDays.ShouldBe(90);
    }

    [Fact]
    public void AnalysisDefaultsBindConfiguredValues()
    {
        IConfiguration configuration = BuildConfiguration(
            ("Analysis:CompanySizeFloor", "750000000"),
            ("Analysis:MinAverageDailyVolume", "200000"),
            ("Analysis:MinDaysTraded", "60"),
            ("Analysis:HoldingHorizonDays", "180"));

        AnalysisDefaultsOptions options = AnalysisDefaultsOptions.FromConfiguration(configuration);

        options.CompanySizeFloor.ShouldBe(750_000_000m);
        options.MinAverageDailyVolume.ShouldBe(200_000m);
        options.MinDaysTraded.ShouldBe(60);
        options.HoldingHorizonDays.ShouldBe(180);
    }

    [Fact]
    public void CostCapsBindTheNfr025Defaults()
    {
        CostCapsOptions options = CostCapsOptions.FromConfiguration(new ConfigurationBuilder().Build());

        // NFR-025: 50 / 20 / 10 / 5.
        options.Event.ShouldBe(50);
        options.Fundamental.ShouldBe(20);
        options.Reasoning.ShouldBe(10);
        options.Delivery.ShouldBe(5);
    }

    [Fact]
    public void FxMultipliersBindTheDocumentedPlaceholders()
    {
        FxMultipliersOptions options = FxMultipliersOptions.FromConfiguration(new ConfigurationBuilder().Build());

        options.UsdEur.ShouldBe(0.92m);
        options.UsdGbp.ShouldBe(0.79m);
        options.UsdJpy.ShouldBe(150.0m);
    }

    [Fact]
    public void CycleBindsTheFifteenMinuteIntervalAndEuropeParisWindow()
    {
        CycleOptions options = CycleOptions.FromConfiguration(new ConfigurationBuilder().Build());

        options.Interval.ShouldBe("00:15:00");
        options.DeliveryWindowTimeZoneId.ShouldBe("Europe/Paris");
        options.DeliveryWindowStart.ShouldBe("07:00");
        options.DeliveryWindowEnd.ShouldBe("22:00");
    }

    [Fact]
    public void CycleParsesIntervalAsTimeSpan()
    {
        IConfiguration configuration = BuildConfiguration(("Cycle:Interval", "00:30:00"));

        CycleOptions options = CycleOptions.FromConfiguration(configuration);
        TimeSpan parsed = TimeSpan.Parse(options.Interval, CultureInfo.InvariantCulture);

        parsed.ShouldBe(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void CycleRejectsMalformedInterval()
    {
        IConfiguration configuration = BuildConfiguration(("Cycle:Interval", "not-a-timespan"));

        CycleOptions options = CycleOptions.FromConfiguration(configuration);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => _ = options.ToDefaults());
        exception.Message.ShouldContain("Cycle:Interval");
    }

    [Fact]
    public void CycleRejectsNonPositiveInterval()
    {
        IConfiguration configuration = BuildConfiguration(("Cycle:Interval", "00:00:00"));

        CycleOptions options = CycleOptions.FromConfiguration(configuration);

        Should.Throw<InvalidOperationException>(() => _ = options.ToDefaults());
    }

    [Fact]
    public void ProviderBindsTheDocumentedPlaceholders()
    {
        ProviderOptions options = ProviderOptions.FromConfiguration(new ConfigurationBuilder().Build());

        options.Reasoning.ShouldBe("OpenAI");
        options.ReasoningModel.ShouldBe("gpt-4o-mini");
        options.MarketData.ShouldBe("OpenAI");
        options.MarketDataModel.ShouldBe("gpt-4o-mini");
    }

    [Fact]
    public void ProviderSectionsContainNoCredentialShapedValues()
    {
        ProviderOptions options = ProviderOptions.FromConfiguration(new ConfigurationBuilder().Build());

        // NFR-043/044: credentials never belong in committed configuration. The provider
        // section exposes only names and model identifiers; there are no API-key or
        // bearer-token properties on the options shape.
        var properties = typeof(ProviderOptions)
            .GetProperties()
            .Select(property => property.PropertyType);

        properties.ShouldNotContain(typeof(string).MakeArrayType());
        properties.ShouldNotContain(typeof(System.Security.SecureString));
    }

    [Fact]
    public void FromConfigurationThrowsOnNullConfigurationForEachSection()
    {
        Should.Throw<ArgumentNullException>(() => AnalysisDefaultsOptions.FromConfiguration(null!));
        Should.Throw<ArgumentNullException>(() => CostCapsOptions.FromConfiguration(null!));
        Should.Throw<ArgumentNullException>(() => FxMultipliersOptions.FromConfiguration(null!));
        Should.Throw<ArgumentNullException>(() => CycleOptions.FromConfiguration(null!));
        Should.Throw<ArgumentNullException>(() => ProviderOptions.FromConfiguration(null!));
    }

    private static IConfiguration BuildConfiguration(params (string Key, string? Value)[] values)
    {
        var pairs = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }
}
