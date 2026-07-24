using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Application.Configuration;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Section bound for non-secret provider and model selection (NFR-021, NFR-043/044).
/// </summary>
/// <remarks>
/// Credentials never belong in this section (NFR-043). Actual API keys are read from
/// environment variables in worktask 03 (T-027 / #71).
/// </remarks>
public sealed class ProviderOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "Provider";

    /// <summary>Reasoning provider name. Placeholder only — credential belongs in env. Default "OpenAI".</summary>
    public string Reasoning { get; set; } = "OpenAI";

    /// <summary>Reasoning model identifier. Default "gpt-4o-mini".</summary>
    public string ReasoningModel { get; set; } = "gpt-4o-mini";

    /// <summary>Market-data provider name. Default "OpenAI".</summary>
    public string MarketData { get; set; } = "OpenAI";

    /// <summary>Market-data model identifier. Default "gpt-4o-mini".</summary>
    public string MarketDataModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Binds the <c>Provider</c> section from the supplied configuration.
    /// </summary>
    public static ProviderOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new ProviderOptions();
        configuration.GetSection(SectionName).Bind(options);
        return options;
    }

    internal ProviderDefaults ToDefaults() => new(Reasoning, ReasoningModel, MarketData, MarketDataModel);
}
