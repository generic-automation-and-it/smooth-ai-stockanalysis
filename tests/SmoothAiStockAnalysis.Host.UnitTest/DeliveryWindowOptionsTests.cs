using System.Globalization;
using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Host.Configuration;

namespace SmoothAiStockAnalysis.Host.UnitTest;

public sealed class DeliveryWindowOptionsTests
{
    [Fact]
    public void FromConfigurationUsesTheEuropeParisDeliveryDefaults()
    {
        DeliveryWindowOptions options = DeliveryWindowOptions.FromConfiguration(new ConfigurationBuilder().Build());

        var window = options.ToDeliveryWindow();

        window.TimeZoneId.ShouldBe("Europe/Paris");
        window.Start.ToString("HH:mm", CultureInfo.InvariantCulture).ShouldBe("07:00");
        window.End.ToString("HH:mm", CultureInfo.InvariantCulture).ShouldBe("22:00");
    }

    [Fact]
    public void FromConfigurationBindsConfiguredWindowValues()
    {
        var values = new Dictionary<string, string?>
        {
            ["DeliveryWindow:TimeZoneId"] = "America/New_York",
            ["DeliveryWindow:Start"] = "08:30",
            ["DeliveryWindow:End"] = "17:15"
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        DeliveryWindowOptions options = DeliveryWindowOptions.FromConfiguration(configuration);
        var window = options.ToDeliveryWindow();

        window.TimeZoneId.ShouldBe("America/New_York");
        window.Start.ToString("HH:mm", CultureInfo.InvariantCulture).ShouldBe("08:30");
        window.End.ToString("HH:mm", CultureInfo.InvariantCulture).ShouldBe("17:15");
    }

    [Fact]
    public void ToDeliveryWindowRejectsEndNotAfterStart()
    {
        var options = new DeliveryWindowOptions { Start = "22:00", End = "07:00" };

        Should.Throw<ArgumentOutOfRangeException>(() => options.ToDeliveryWindow());
    }

    [Fact]
    public void ToDeliveryWindowRejectsInvalidTimeFormat()
    {
        var options = new DeliveryWindowOptions { Start = "25:99" };

        Should.Throw<ArgumentException>(() => options.ToDeliveryWindow());
    }

    [Fact]
    public void ToDeliveryWindowRejectsUnknownTimeZone()
    {
        var options = new DeliveryWindowOptions { TimeZoneId = "Not/AZone" };

        Should.Throw<ArgumentException>(() => options.ToDeliveryWindow());
    }

    [Fact]
    public void FromConfigurationThrowsOnNullConfiguration()
    {
        Should.Throw<ArgumentNullException>(
            () => DeliveryWindowOptions.FromConfiguration(null!));
    }
}
