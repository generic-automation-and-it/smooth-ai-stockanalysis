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
    /// Binds the <c>CostCaps</c> section from the supplied configuration and validates each cap
    /// is strictly positive (NFR-025, NFR-047). Zero or negative caps would silently disable a
    /// stage, which the worktask-02 contract forbids.
    /// </summary>
    public static CostCapsOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new CostCapsOptions();
        configuration.GetSection(SectionName).Bind(options);
        options.Validate();
        return options;
    }

    private void Validate()
    {
        RequirePositive(Event, nameof(Event));
        RequirePositive(Fundamental, nameof(Fundamental));
        RequirePositive(Reasoning, nameof(Reasoning));
        RequirePositive(Delivery, nameof(Delivery));
    }

    private static void RequirePositive(int value, string leaf)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException(
                $"Configuration value '{SectionName}:{leaf}' must be strictly positive.");
        }
    }

    internal CostCaps ToDefaults() => new(Event, Fundamental, Reasoning, Delivery);
}
