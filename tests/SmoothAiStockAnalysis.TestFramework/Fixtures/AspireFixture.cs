using Aspire.Hosting;
using Aspire.Hosting.Testing;
using SmoothAiStockAnalysis.TestFramework.Aspire;
using Xunit.v3;

namespace SmoothAiStockAnalysis.TestFramework.Fixtures;

public sealed class AspireFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);
    private const string WireMockResourceName = "wiremock";
    private const string DefaultWireMockBaseUrl = "http://127.0.0.1:19091";

    private DistributedApplication? _application;

    public string WireMockBaseUrl { get; private set; } = string.Empty;

    public WireMockAdminClient CreateWireMockAdminClient() =>
        WireMockAdminClient.Create(WireMockBaseUrl);

    public async ValueTask InitializeAsync()
    {
        if (await IsWireMockHealthyAsync(DefaultWireMockBaseUrl))
        {
            WireMockBaseUrl = DefaultWireMockBaseUrl;
            return;
        }

        using var timeout = new CancellationTokenSource(StartupTimeout);
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SmoothAiStockAnalysis_TestFramework_Aspire>(
                ["--no-dashboard"],
                timeout.Token);

        _application = await appHost.BuildAsync(timeout.Token);
        await _application.StartAsync(timeout.Token);
        await _application.ResourceNotifications
            .WaitForResourceHealthyAsync(WireMockResourceName, timeout.Token);

        WireMockBaseUrl = _application
            .GetEndpoint(WireMockResourceName)
            .AbsoluteUri
            .TrimEnd('/');

        if (!await IsWireMockHealthyAsync(WireMockBaseUrl))
        {
            throw new InvalidOperationException(
                $"Aspire started WireMock at '{WireMockBaseUrl}', but its admin health endpoint is unavailable.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }
    }

    private static async Task<bool> IsWireMockHealthyAsync(string baseUrl)
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(2)
            };
            using HttpResponseMessage response = await client.GetAsync($"{baseUrl}/__admin/health");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}

[CollectionDefinition]
public sealed class AspireCollection : ICollectionFixture<AspireFixture>;
