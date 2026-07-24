using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Application.Configuration;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Section bound for the per-cycle stage caps. Defaults follow NFR-025 (50 / 20 / 10 / 5).
/// </summary>
public sealed class CostCapsOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "CostCaps";

    /// <summary>Event-detection stage cap. Default 50.</summary>
    public int Event { get; set; } = 50;

    /// <summary>Fundamental-screening stage cap. Default 20.</summary>
    public int Fundamental { get; set; } = 20;

    /// <summary>Reasoning stage cap. Default 10 (NFR-026).</summary>
    public int Reasoning { get; set; } = 10;

    /// <summary>Delivery stage cap. Default 5.</summary>
    public int Delivery { get; set; } = 5;

    /// <summary>
    /// Binds the <c>CostCaps</c> section from the supplied configuration.
    /// </summary>
    public static CostCapsOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new CostCapsOptions();
        configuration.GetSection(SectionName).Bind(options);
        return options;
    }

    internal CostCaps ToDefaults() => new(Event, Fundamental, Reasoning, Delivery);
}
