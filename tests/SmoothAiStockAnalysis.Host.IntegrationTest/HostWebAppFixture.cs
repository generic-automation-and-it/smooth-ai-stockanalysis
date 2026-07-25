extern alias HostApp;

using Microsoft.Extensions.Hosting;
using SmoothAiStockAnalysis.TestFramework.Fixtures;

namespace SmoothAiStockAnalysis.Host.IntegrationTest;

/// <summary>
/// Closes the generic <see cref="WebAppFixture{TProgram}"/> over the Host's entry point.
/// The <c>HostApp</c> extern alias disambiguates the Host's <c>Program</c> from the test
/// assembly's own auto-generated <c>Program</c> (xunit.v3 compiles test projects as executables).
/// </summary>
public sealed class HostWebAppFixture : WebAppFixture<HostApp::Program>
{
    /// <summary>
    /// Keeps the Host's <see cref="IHostedService"/> registrations intact so startup-time
    /// initialisation (e.g. <c>SqliteDatabaseInitializer</c>) runs against the isolated database.
    /// </summary>
    protected override bool RemoveHostedServices => false;

    /// <summary>
    /// Supplies a non-placeholder OpenAI API key so the Host's credential validation passes
    /// during integration-test boot. The committed <c>appsettings.json</c> carries the
    /// placeholder token (NFR-044); integration tests must override it.
    /// </summary>
    /// <remarks>
    /// The minimal-hosting <c>WebApplicationFactory</c> runs <c>Program.cs</c> before
    /// <c>ConfigureAppConfiguration</c> overrides are applied, so the credential override is
    /// supplied via environment variable instead. <c>WebApplication.CreateBuilder</c> reads
    /// environment variables during initialization, making the value available to
    /// <c>AddConfiguration(builder.Configuration)</c>.
    /// </remarks>
    protected override Dictionary<string, string?> ConfigurationOverrides => new()
    {
        [HostApp::SmoothAiStockAnalysis.Host.Configuration.CredentialsOptions.OpenAiApiKeyPath] = "integration-test-openai-api-key",
    };

    /// <summary>
    /// Sets the OpenAI API key environment variable before the factory is created so
    /// <c>Program.cs</c> can read it during <c>WebApplication.CreateBuilder</c>.
    /// </summary>
    protected override Task EnrichConfigurationAsync(Dictionary<string, string?> overrides)
    {
        Environment.SetEnvironmentVariable(
            HostApp::SmoothAiStockAnalysis.Host.Configuration.CredentialsOptions.OpenAiApiKeyEnvironmentVariable,
            "integration-test-openai-api-key");
        return Task.CompletedTask;
    }
}
