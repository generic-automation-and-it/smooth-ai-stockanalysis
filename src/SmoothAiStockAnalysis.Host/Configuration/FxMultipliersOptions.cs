using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Application.Configuration;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Section bound for the static currency conversion multipliers (NFR-050).
/// </summary>
/// <remarks>
/// Refresh is deliberately deferred. A several-percent drift on a coarse size threshold is
/// immaterial, so a periodic refresh with a change threshold is a future, low-priority
/// requirement rather than a now-built one. The placeholders below are non-secret tunables;
/// credentials (none here) never belong in committed configuration (NFR-043/044).
/// </remarks>
public sealed class FxMultipliersOptions : CatalogueSectionOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "FxMultipliers";

    /// <inheritdoc />
    protected override string ConfigurationSectionName => SectionName;

    /// <summary>USD → EUR multiplier. Default 0.92.</summary>
    public decimal UsdEur { get; set; } = 0.92m;

    /// <summary>USD → GBP multiplier. Default 0.79.</summary>
    public decimal UsdGbp { get; set; } = 0.79m;

    /// <summary>USD → JPY multiplier. Default 150.0.</summary>
    public decimal UsdJpy { get; set; } = 150.0m;

    /// <summary>
    /// Binds the <c>FxMultipliers</c> section from the supplied configuration and validates
    /// each multiplier is strictly positive (NFR-050, NFR-047). Zero or negative multipliers
    /// would silently zero or invert the size-floor conversion.
    /// </summary>
    public static FxMultipliersOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new FxMultipliersOptions();
        configuration.GetSection(SectionName).Bind(options);
        options.Validate();
        return options;
    }

    private void Validate()
    {
        RequirePositive(UsdEur, nameof(UsdEur));
        RequirePositive(UsdGbp, nameof(UsdGbp));
        RequirePositive(UsdJpy, nameof(UsdJpy));
    }

    internal FxMultipliers ToDefaults() => new(UsdEur, UsdGbp, UsdJpy);
}
