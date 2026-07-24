using NodaTime;
using NodaTime.Text;
using SmoothAiStockAnalysis.Domain.Time;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Configuration values for the daily recommendation-delivery window.
/// </summary>
public sealed class DeliveryWindowOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "DeliveryWindow";

    private static readonly LocalTimePattern LocalTimePattern =
        LocalTimePattern.CreateWithInvariantCulture("HH:mm");

    /// <summary>
    /// Gets or sets the TZDB IANA zone used to evaluate the window.
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Paris";

    /// <summary>
    /// Gets or sets the inclusive local start time in <c>HH:mm</c> format.
    /// </summary>
    public string Start { get; set; } = "07:00";

    /// <summary>
    /// Gets or sets the exclusive local end time in <c>HH:mm</c> format.
    /// </summary>
    public string End { get; set; } = "22:00";

    /// <summary>
    /// Creates a validated domain window from configuration values.
    /// </summary>
    public DeliveryWindow ToDeliveryWindow() => new(
        TimeZoneId,
        ParseLocalTime(Start, nameof(Start)),
        ParseLocalTime(End, nameof(End)));

    /// <summary>
    /// Binds a delivery-window configuration section, retaining the product defaults for omitted values.
    /// </summary>
    public static DeliveryWindowOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new DeliveryWindowOptions();
        configuration.GetSection(SectionName).Bind(options);
        return options;
    }

    private static LocalTime ParseLocalTime(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        ParseResult<LocalTime> result = LocalTimePattern.Parse(value);
        if (!result.Success)
        {
            throw new ArgumentException("Time must use the HH:mm format.", parameterName, result.Exception);
        }

        return result.Value;
    }
}
