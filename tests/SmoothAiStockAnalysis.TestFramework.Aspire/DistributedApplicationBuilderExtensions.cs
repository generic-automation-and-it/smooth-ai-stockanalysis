using Aspire.Hosting;

namespace SmoothAiStockAnalysis.TestFramework.Aspire;

internal static class DistributedApplicationBuilderExtensions
{
    internal static void AddWireMockTestDependency(this IDistributedApplicationBuilder builder)
    {
        builder.AddContainer(WireMockTestDependency.ResourceName, "wiremock/wiremock")
            .WithHttpEndpoint(port: WireMockTestDependency.Port, targetPort: 8080);
    }
}
