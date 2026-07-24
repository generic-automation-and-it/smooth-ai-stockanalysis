using Aspire.Hosting;
using Aspire.Hosting.Testing;
using SmoothAiStockAnalysis.TestFramework.Aspire;
using Xunit.v3;

namespace SmoothAiStockAnalysis.TestFramework.Fixtures;

public sealed class AspireFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);

    private DistributedApplication? _application;

    public string WireMockBaseUrl { get; private set; } = string.Empty;

    public WireMockAdminClient CreateWireMockAdminClient() =>
        WireMockAdminClient.Create(WireMockBaseUrl);

    public async ValueTask InitializeAsync()
    {
        for (int attempt = 1; attempt <= PreWarmMaxAttempts; attempt++)
        {
            if (await IsWireMockHealthyAsync(WireMockTestDependency.DefaultBaseUrl))
            {
                WireMockBaseUrl = WireMockTestDependency.DefaultBaseUrl;
                return;
            }

            if (attempt < PreWarmMaxAttempts)
            {
                await Task.Delay(PreWarmRetryDelay);
            }
        }

        using var timeout = new CancellationTokenSource(StartupTimeout);
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SmoothAiStockAnalysis_TestFramework_Aspire>(
                ["--no-dashboard"],
                timeout.Token);

        _application = await appHost.BuildAsync(timeout.Token);
        await _application.StartAsync(timeout.Token);
        await _application.ResourceNotifications
            .WaitForResourceHealthyAsync(WireMockTestDependency.ResourceName, timeout.Token);

        WireMockBaseUrl = _application
            .GetEndpoint(WireMockTestDependency.ResourceName)
            .AbsoluteUri
            .TrimEnd('/');

        await EnsureWireMockHealthyAsync(WireMockBaseUrl);
    }

    public async ValueTask DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }
    }

    private const int PreWarmMaxAttempts = 3;
    private static readonly TimeSpan PreWarmRetryDelay = TimeSpan.FromSeconds(1);

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

    private static async Task EnsureWireMockHealthyAsync(string baseUrl)
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(2)
            };
            using HttpResponseMessage response = await client.GetAsync($"{baseUrl}/__admin/health");
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"WireMock admin health endpoint returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"WireMock admin health probe failed with {ex.GetType().Name}: {ex.Message}.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvalidOperationException(
                $"WireMock admin health probe timed out: {ex.Message}.", ex);
        }
    }
}

[CollectionDefinition]
public sealed class AspireCollection : ICollectionFixture<AspireFixture>;
