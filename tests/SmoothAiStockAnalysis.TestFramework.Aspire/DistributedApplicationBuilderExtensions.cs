using Aspire.Hosting;

namespace SmoothAiStockAnalysis.TestFramework.Aspire;

internal static class DistributedApplicationBuilderExtensions
{
    internal const string WireMockResourceName = "wiremock";
    internal const int WireMockPort = 19091;

    internal static void AddWireMockTestDependency(this IDistributedApplicationBuilder builder)
    {
        builder.AddContainer(WireMockResourceName, "wiremock/wiremock")
            .WithHttpEndpoint(port: WireMockPort, targetPort: 8080);
    }
}
