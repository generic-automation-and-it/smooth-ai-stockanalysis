using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Host.Configuration;

namespace SmoothAiStockAnalysis.Host.UnitTest;

/// <summary>
/// L0 coverage for the Host-owned credential options (T-027 / #71). Credentials bind from the
/// same configuration sources as every other section, but the committed appsettings carries
/// placeholder tokens only; real values arrive from environment variables or user-secrets
/// (NFR-043, NFR-044). The validate-when-enabled pattern rejects the placeholder and blank
/// values when the matching provider is selected.
/// </summary>
public sealed class CredentialsOptionsTests
{
    [Fact]
    public void FromConfigurationBindsTheOpenAiApiKeyFromConfiguration()
    {
        IConfiguration configuration = BuildConfiguration(
            (CredentialsOptions.OpenAiApiKeyPath, "test-api-key-from-configuration"));

        CredentialsOptions options = CredentialsOptions.FromConfiguration(configuration);

        options.OpenAi.ApiKey.ShouldBe("test-api-key-from-configuration");
    }

    [Fact]
    public void FromConfigurationBindsEmptyApiKeyWhenSectionIsMissing()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        CredentialsOptions options = CredentialsOptions.FromConfiguration(configuration);

        options.OpenAi.ApiKey.ShouldBe(string.Empty);
    }

    [Fact]
    public void FromConfigurationThrowsOnNullConfiguration()
    {
        Should.Throw<ArgumentNullException>(() => CredentialsOptions.FromConfiguration(null!));
    }

    [Theory]
    [InlineData("OpenAI", "Anthropic")]
    [InlineData("Anthropic", "OpenAI")]
    [InlineData("OpenAI", "OpenAI")]
    [InlineData("openai", "anthropic")]
    [InlineData("ANTHROPIC", "openai")]
    public void ValidateRequiresOpenAiApiKeyWhenOpenAiIsSelectedAsEitherProvider(
        string reasoningProvider,
        string marketDataProvider)
    {
        var provider = new ProviderOptions
        {
            Reasoning = reasoningProvider,
            MarketData = marketDataProvider,
        };
        var credentials = new CredentialsOptions();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => credentials.Validate(provider));

        exception.Message.ShouldContain(CredentialsOptions.OpenAiApiKeyPath);
        exception.Message.ShouldContain(CredentialsOptions.OpenAiApiKeyEnvironmentVariable);
        // LADR-018: never echo the bound value (empty here, but guard the contract).
        exception.Message.ShouldNotContain("   ");
    }

    [Theory]
    [InlineData("Anthropic", "Gemini")]
    [InlineData("Gemini", "Anthropic")]
    [InlineData("Anthropic", "Anthropic")]
    public void ValidateDoesNotRequireOpenAiApiKeyWhenOpenAiIsNotSelected(
        string reasoningProvider,
        string marketDataProvider)
    {
        var provider = new ProviderOptions
        {
            Reasoning = reasoningProvider,
            MarketData = marketDataProvider,
        };
        var credentials = new CredentialsOptions();

        Should.NotThrow(() => credentials.Validate(provider));
    }

    [Fact]
    public void ValidateRejectsTheCommittedPlaceholderToken()
    {
        var provider = new ProviderOptions { Reasoning = "OpenAI", MarketData = "OpenAI" };
        var credentials = new CredentialsOptions
        {
            OpenAi = new OpenAiCredentialOptions { ApiKey = CredentialsOptions.OpenAiApiKeyPlaceholder },
        };

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => credentials.Validate(provider));

        exception.Message.ShouldContain(CredentialsOptions.OpenAiApiKeyPath);
        exception.Message.ShouldContain(CredentialsOptions.OpenAiApiKeyEnvironmentVariable);
        // LADR-018: never echo the bound value.
        exception.Message.ShouldNotContain(CredentialsOptions.OpenAiApiKeyPlaceholder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRejectsBlankApiKey(string blankValue)
    {
        var provider = new ProviderOptions { Reasoning = "OpenAI", MarketData = "OpenAI" };
        var credentials = new CredentialsOptions
        {
            OpenAi = new OpenAiCredentialOptions { ApiKey = blankValue },
        };

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => credentials.Validate(provider));

        exception.Message.ShouldContain(CredentialsOptions.OpenAiApiKeyPath);
    }

    [Fact]
    public void ValidateAcceptsNonPlaceholderApiKeyWhenOpenAiIsEnabled()
    {
        var provider = new ProviderOptions { Reasoning = "OpenAI", MarketData = "OpenAI" };
        var credentials = new CredentialsOptions
        {
            OpenAi = new OpenAiCredentialOptions { ApiKey = "real-api-key-for-test-only" },
        };

        Should.NotThrow(() => credentials.Validate(provider));
    }

    [Fact]
    public void ValidateThrowsOnNullProvider()
    {
        var credentials = new CredentialsOptions();
        Should.Throw<ArgumentNullException>(() => credentials.Validate(null!));
    }

    [Fact]
    public void CredentialsPropertySetContainsNoSecretShapedProperties()
    {
        // NFR-043/044: the credentials options allow-list contains only documented keys.
        // The property set is asserted explicitly so an accidental addition (e.g. "Password")
        // surfaces as a test failure rather than a silent drift.
        string[] allowedOpenAiProperties = [nameof(OpenAiCredentialOptions.ApiKey)];

        string[] actualOpenAiProperties = typeof(OpenAiCredentialOptions)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        actualOpenAiProperties.ShouldBe(
            allowedOpenAiProperties.OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    private static IConfiguration BuildConfiguration(params (string Key, string? Value)[] values)
    {
        var pairs = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }
}
