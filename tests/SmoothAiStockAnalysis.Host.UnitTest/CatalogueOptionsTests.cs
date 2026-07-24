using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmoothAiStockAnalysis.Application.Configuration;
using SmoothAiStockAnalysis.Host.Configuration;
using SmoothAiStockAnalysis.Host.Extensions;

namespace SmoothAiStockAnalysis.Host.UnitTest;

/// <summary>
/// L0 coverage for the F-004 settings catalogue sections (T-025). Each section's
/// <c>FromConfiguration</c> is exercised for happy-path bind and fail-fast on invalid values
/// (NFR-008, NFR-047). Defaults match the catalogue table in <c>HOST_AGENTS.md</c>.
/// </summary>
public sealed class CatalogueOptionsTests
{
    [Fact]
    public void AnalysisDefaultsBindFromEmptyConfiguration()
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

    [Theory]
    [InlineData("Analysis:CompanySizeFloor", "0")]
    [InlineData("Analysis:CompanySizeFloor", "-1")]
    [InlineData("Analysis:MinAverageDailyVolume", "0")]
    [InlineData("Analysis:MinAverageDailyVolume", "-100")]
    [InlineData("Analysis:MinDaysTraded", "0")]
    [InlineData("Analysis:MinDaysTraded", "-7")]
    [InlineData("Analysis:HoldingHorizonDays", "0")]
    [InlineData("Analysis:HoldingHorizonDays", "-30")]
    public void AnalysisDefaultsRejectNonPositiveNumericAndInt(string key, string value)
    {
        IConfiguration configuration = BuildConfiguration((key, value));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => AnalysisDefaultsOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(key);
        exception.Message.ShouldNotContain(value);
    }

    [Theory]
    [InlineData("Analysis:ScoringWeightEvent", "-0.01")]
    [InlineData("Analysis:ScoringWeightEvent", "1.01")]
    [InlineData("Analysis:ScoringWeightFundamental", "-0.50")]
    [InlineData("Analysis:ScoringWeightFundamental", "2.00")]
    [InlineData("Analysis:ScoringWeightSentiment", "1.50")]
    public void AnalysisDefaultsRejectOutOfUnitIntervalScoringWeight(string key, string value)
    {
        IConfiguration configuration = BuildConfiguration((key, value));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => AnalysisDefaultsOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(key);
        exception.Message.ShouldNotContain(value);
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

    [Theory]
    [InlineData("CostCaps:Event", "0")]
    [InlineData("CostCaps:Event", "-1")]
    [InlineData("CostCaps:Fundamental", "0")]
    [InlineData("CostCaps:Reasoning", "0")]
    [InlineData("CostCaps:Delivery", "-5")]
    public void CostCapsRejectNonPositiveStageCaps(string key, string value)
    {
        IConfiguration configuration = BuildConfiguration((key, value));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => CostCapsOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(key);
        exception.Message.ShouldNotContain(value);
    }

    [Fact]
    public void FxMultipliersBindTheDocumentedPlaceholders()
    {
        FxMultipliersOptions options = FxMultipliersOptions.FromConfiguration(new ConfigurationBuilder().Build());

        options.UsdEur.ShouldBe(0.92m);
        options.UsdGbp.ShouldBe(0.79m);
        options.UsdJpy.ShouldBe(150.0m);
    }

    [Theory]
    [InlineData("FxMultipliers:UsdEur", "0")]
    [InlineData("FxMultipliers:UsdEur", "-0.92")]
    [InlineData("FxMultipliers:UsdGbp", "0")]
    [InlineData("FxMultipliers:UsdJpy", "-1.5")]
    public void FxMultipliersRejectNonPositiveMultipliers(string key, string value)
    {
        IConfiguration configuration = BuildConfiguration((key, value));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => FxMultipliersOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(key);
        exception.Message.ShouldNotContain(value);
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
    public void CycleRejectsMalformedIntervalAtSectionBind()
    {
        IConfiguration configuration = BuildConfiguration(("Cycle:Interval", "not-a-timespan"));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => CycleOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(CycleOptions.IntervalPath);
        exception.Message.ShouldNotContain("not-a-timespan");
    }

    [Fact]
    public void CycleRejectsBlankIntervalAtSectionBind()
    {
        IConfiguration configuration = BuildConfiguration(("Cycle:Interval", "   "));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => CycleOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(CycleOptions.IntervalPath);
    }

    [Fact]
    public void CycleRejectsNonPositiveIntervalAtSectionBind()
    {
        IConfiguration configuration = BuildConfiguration(("Cycle:Interval", "00:00:00"));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => CycleOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(CycleOptions.IntervalPath);
    }

    [Fact]
    public void ApplicationDefaultsRejectsMalformedDeliveryWindowStart()
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => CreateApplicationDefaults(deliveryWindowStart: "not-a-time"));

        exception.Message.ShouldContain(CycleOptions.DeliveryWindowStartPath);
        exception.Message.ShouldNotContain("not-a-time");
    }

    [Fact]
    public void ApplicationDefaultsRejectsUnknownDeliveryWindowTimeZone()
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => CreateApplicationDefaults(deliveryWindowTimeZoneId: "Not/AZone"));

        exception.Message.ShouldContain(CycleOptions.DeliveryWindowTimeZoneIdPath);
    }

    [Fact]
    public void ApplicationDefaultsParsesTheDefaultDeliveryWindowEagerly()
    {
        ApplicationDefaults defaults = CreateApplicationDefaults();

        defaults.GetDefaultDeliveryWindow().TimeZoneId.ShouldBe("Europe/Paris");
        defaults.GetDefaultDeliveryWindow().ShouldBeSameAs(defaults.GetDefaultDeliveryWindow());
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

    [Theory]
    [InlineData("Provider:Reasoning", "")]
    [InlineData("Provider:Reasoning", "   ")]
    [InlineData("Provider:ReasoningModel", "")]
    [InlineData("Provider:MarketData", "   ")]
    [InlineData("Provider:MarketDataModel", "")]
    public void ProviderRejectsBlankProviderAndModelKnobs(string key, string value)
    {
        IConfiguration configuration = BuildConfiguration((key, value));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => ProviderOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(key);
    }

    [Fact]
    public void ProviderSectionsContainNoCredentialShapedProperties()
    {
        string[] allowed =
        [
            nameof(ProviderOptions.Reasoning),
            nameof(ProviderOptions.ReasoningModel),
            nameof(ProviderOptions.MarketData),
            nameof(ProviderOptions.MarketDataModel),
        ];

        string[] actual = typeof(ProviderOptions)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // NFR-043/044: the provider catalogue section is an allow-list of non-secret knobs only.
        actual.ShouldBe(allowed.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        actual.ShouldNotContain(name =>
            name.Contains("Key", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Credential", StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public void AddConfigurationRejectsInvalidCycleInterval()
    {
        IConfiguration configuration = BuildConfiguration(("Cycle:Interval", "00:00:00"));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => AddConfigurationForTest(configuration));

        exception.Message.ShouldContain(CycleOptions.IntervalPath);
    }

    [Fact]
    public void AddConfigurationRejectsInvalidDeliveryWindowStart()
    {
        IConfiguration configuration = BuildConfiguration(("Cycle:DeliveryWindowStart", "not-a-time"));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => AddConfigurationForTest(configuration));

        exception.Message.ShouldContain(CycleOptions.DeliveryWindowStartPath);
        exception.Message.ShouldNotContain("not-a-time");
    }

    [Fact]
    public void AddConfigurationRejectsNegativeCostCap()
    {
        IConfiguration configuration = BuildConfiguration(("CostCaps:Event", "-1"));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => AddConfigurationForTest(configuration));

        exception.Message.ShouldContain("CostCaps:Event");
        exception.Message.ShouldNotContain("-1");
    }

    [Fact]
    public void AddConfigurationAcceptsCommittedDefaults()
    {
        IServiceProvider provider = AddConfigurationForTest(new ConfigurationBuilder().Build());
        IApplicationDefaults defaults = provider.GetRequiredService<IApplicationDefaults>();

        defaults.CostCaps.Event.ShouldBe(50);
        defaults.GetDefaultDeliveryWindow().TimeZoneId.ShouldBe("Europe/Paris");
    }

    private static ApplicationDefaults CreateApplicationDefaults(
        string deliveryWindowTimeZoneId = "Europe/Paris",
        string deliveryWindowStart = "07:00",
        string deliveryWindowEnd = "22:00")
    {
        var cycle = new CycleOptions
        {
            Interval = "00:15:00",
            DeliveryWindowTimeZoneId = deliveryWindowTimeZoneId,
            DeliveryWindowStart = deliveryWindowStart,
            DeliveryWindowEnd = deliveryWindowEnd,
        };

        return new ApplicationDefaults(
            Options.Create(new AnalysisDefaultsOptions()),
            Options.Create(new CostCapsOptions()),
            Options.Create(new FxMultipliersOptions()),
            Options.Create(cycle),
            Options.Create(new ProviderOptions()));
    }

    private static IServiceProvider AddConfigurationForTest(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddConfiguration(configuration);
        return services.BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration(params (string Key, string? Value)[] values)
    {
        var pairs = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }
}
