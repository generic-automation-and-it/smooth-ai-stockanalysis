using Microsoft.Extensions.Configuration;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Section bound for provider API keys and other credential material (NFR-043, NFR-044).
/// </summary>
/// <remarks>
/// <para>
/// Credentials bind from the same configuration sources as every other section, but the
/// committed <c>appsettings.json</c> carries <strong>placeholder tokens only</strong>
/// (<see cref="OpenAiApiKeyPlaceholder"/>). Real values arrive at deploy time from environment
/// variables (e.g. <c>CREDENTIALS__OPENAI__APIKEY</c>) or, for local development, from
/// <c>dotnet user-secrets</c> (the Host project declares <c>UserSecretsId</c>). ASP.NET Core's
/// default configuration sources override JSON with env vars, so the deploy-time value wins
/// over the committed placeholder without any code change (NFR-080).
/// </para>
/// <para>
/// Credentials never enter <c>IApplicationDefaults</c> (NFR-043/044). The <c>Provider</c>
/// section carries non-secret provider and model selection; this section carries the matching
/// secret. The Host composition root validates credentials against the enabled provider and
/// registers the singleton so future Infrastructure clients can resolve it.
/// </para>
/// </remarks>
public sealed class CredentialsOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "Credentials";

    /// <summary>
    /// Placeholder token committed to <c>appsettings.json</c> for the OpenAI API key. A real
    /// deploy-time value arrives via environment variable <c>CREDENTIALS__OPENAI__APIKEY</c> or
    /// <c>dotnet user-secrets</c>; startup validation treats the placeholder as "not configured".
    /// </summary>
    public const string OpenAiApiKeyPlaceholder = "{{CREDENTIALS__OPENAI__APIKEY}}";

    /// <summary>
    /// Environment variable name that supplies the OpenAI API key at deploy time. The double
    /// underscore is the ASP.NET Core hierarchical separator (<c>__</c> → <c>:</c>).
    /// </summary>
    public const string OpenAiApiKeyEnvironmentVariable = "CREDENTIALS__OPENAI__APIKEY";

    /// <summary>Gets the full configuration path reported in validation messages.</summary>
    public const string OpenAiApiKeyPath = SectionName + ":OpenAi:ApiKey";

    /// <summary>OpenAI provider credentials.</summary>
    public OpenAiCredentialOptions OpenAi { get; set; } = new();

    /// <summary>
    /// Binds the <c>Credentials</c> section from the supplied configuration. Real values arrive
    /// from environment variables or user-secrets; the committed <c>appsettings.json</c> carries
    /// <see cref="OpenAiApiKeyPlaceholder"/> only (NFR-044).
    /// </summary>
    public static CredentialsOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new CredentialsOptions();
        configuration.GetSection(SectionName).Bind(options);
        return options;
    }

    /// <summary>
    /// Validates credentials against the enabled provider selection. A provider's credential is
    /// required when that provider is selected in <paramref name="provider"/>; the committed
    /// placeholder token is treated as "not configured" (NFR-043, NFR-047). The validation
    /// message names the configuration path and the environment variable; it never echoes the
    /// bound value (LADR-018).
    /// </summary>
    public void Validate(ProviderOptions provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (IsOpenAiEnabled(provider))
        {
            RequireOpenAiApiKey();
        }
    }

    private static bool IsOpenAiEnabled(ProviderOptions provider) =>
        string.Equals(provider.Reasoning, "OpenAI", StringComparison.OrdinalIgnoreCase)
        || string.Equals(provider.MarketData, "OpenAI", StringComparison.OrdinalIgnoreCase);

    private void RequireOpenAiApiKey()
    {
        string value = OpenAi.ApiKey;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{OpenAiApiKeyPath}' is required when provider 'OpenAI' is enabled. "
                + $"Set environment variable '{OpenAiApiKeyEnvironmentVariable}' or use 'dotnet user-secrets'.");
        }

        if (string.Equals(value, OpenAiApiKeyPlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Configuration value '{OpenAiApiKeyPath}' still carries the committed placeholder token. "
                + $"Set environment variable '{OpenAiApiKeyEnvironmentVariable}' or use 'dotnet user-secrets' "
                + "with a real credential before starting the host.");
        }
    }
}

/// <summary>
/// OpenAI provider credential material. The property set is an allow-list (NFR-043/044); only
/// the documented <see cref="ApiKey"/> is permitted.
/// </summary>
public sealed class OpenAiCredentialOptions
{
    /// <summary>OpenAI API key. Default empty (placeholder token committed in appsettings.json).</summary>
    public string ApiKey { get; set; } = string.Empty;
}
