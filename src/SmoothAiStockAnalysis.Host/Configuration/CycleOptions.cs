using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Application.Configuration;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Section bound for cycle scheduling tunables: the cycle interval and the delivery window.
/// </summary>
public sealed class CycleOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "Cycle";

    /// <summary>
    /// Gets or sets the interval between analysis cycles in <c>hh:mm:ss</c> format.
    /// Default 15 minutes.
    /// </summary>
    public string Interval { get; set; } = "00:15:00";

    /// <summary>The TZDB IANA zone used to evaluate the delivery window. Default Europe/Paris.</summary>
    public string DeliveryWindowTimeZoneId { get; set; } = "Europe/Paris";

    /// <summary>The inclusive local start time in <c>HH:mm</c> format. Default 07:00.</summary>
    public string DeliveryWindowStart { get; set; } = "07:00";

    /// <summary>The exclusive local end time in <c>HH:mm</c> format. Default 22:00.</summary>
    public string DeliveryWindowEnd { get; set; } = "22:00";

    /// <summary>
    /// Binds the <c>Cycle</c> section from the supplied configuration.
    /// </summary>
    public static CycleOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new CycleOptions();
        configuration.GetSection(SectionName).Bind(options);
        return options;
    }

    internal CycleDefaults ToDefaults() => new(
        Interval: ParseInterval(Interval),
        DeliveryWindowTimeZoneId: DeliveryWindowTimeZoneId,
        DeliveryWindowStart: DeliveryWindowStart,
        DeliveryWindowEnd: DeliveryWindowEnd);

    private static TimeSpan ParseInterval(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!TimeSpan.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out TimeSpan interval))
        {
            throw new InvalidOperationException(
                $"Configuration value 'Cycle:Interval' must be a valid TimeSpan in hh:mm:ss format (received '{value}').");
        }

        if (interval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Configuration value 'Cycle:Interval' must be strictly positive.");
        }

        return interval;
    }
}
