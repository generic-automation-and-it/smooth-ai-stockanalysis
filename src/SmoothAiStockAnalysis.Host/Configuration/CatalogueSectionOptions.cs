namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Shared bind-time validation behaviour for F-004 catalogue section options.
/// Concrete sections own their keys and range rules; this base owns the LADR-018
/// message contract so each section stays a real type rather than a static-helper consumer.
/// </summary>
public abstract class CatalogueSectionOptions
{
    /// <summary>Gets the configuration section name used in validation messages.</summary>
    protected abstract string ConfigurationSectionName { get; }

    /// <summary>Requires <paramref name="value"/> to be strictly greater than zero.</summary>
    protected void RequirePositive(decimal value, string leaf)
    {
        if (value <= 0m)
        {
            throw new InvalidOperationException(
                $"Configuration value '{ConfigurationSectionName}:{leaf}' must be strictly positive.");
        }
    }

    /// <summary>Requires <paramref name="value"/> to be strictly greater than zero.</summary>
    protected void RequirePositive(int value, string leaf)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException(
                $"Configuration value '{ConfigurationSectionName}:{leaf}' must be strictly positive.");
        }
    }

    /// <summary>Requires <paramref name="value"/> to lie in the closed unit interval [0, 1].</summary>
    protected void RequireUnitInterval(decimal value, string leaf)
    {
        if (value < 0m || value > 1m)
        {
            throw new InvalidOperationException(
                $"Configuration value '{ConfigurationSectionName}:{leaf}' must be in the [0, 1] interval.");
        }
    }

    /// <summary>Requires <paramref name="value"/> to be non-null and non-whitespace.</summary>
    protected void RequireNonBlank(string value, string leaf)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{ConfigurationSectionName}:{leaf}' is required and must be non-blank.");
        }
    }
}
