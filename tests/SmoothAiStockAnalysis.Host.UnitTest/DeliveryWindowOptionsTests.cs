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
        window.Start.ToString("HH:mm", null).ShouldBe("07:00");
        window.End.ToString("HH:mm", null).ShouldBe("22:00");
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
        window.Start.ToString("HH:mm", null).ShouldBe("08:30");
        window.End.ToString("HH:mm", null).ShouldBe("17:15");
    }
}
