namespace SmoothAiStockAnalysis.TestFramework.Aspire;

public static class WireMockTestDependency
{
    public const string ResourceName = "wiremock";
    public const int Port = 19091;

    public static string DefaultBaseUrl => $"http://127.0.0.1:{Port}";
}
