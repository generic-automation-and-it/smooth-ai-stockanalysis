using System.Globalization;
using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Application.Configuration;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Section bound for cycle scheduling tunables: the cycle interval and the delivery window.
/// </summary>
public sealed class CycleOptions : CatalogueSectionOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "Cycle";

    /// <summary>Gets the full configuration path for the cycle interval.</summary>
    public const string IntervalPath = SectionName + ":Interval";

    /// <summary>Gets the full configuration path for the delivery-window time zone.</summary>
    public const string DeliveryWindowTimeZoneIdPath = SectionName + ":DeliveryWindowTimeZoneId";

    /// <summary>Gets the full configuration path for the delivery-window start.</summary>
    public const string DeliveryWindowStartPath = SectionName + ":DeliveryWindowStart";

    /// <summary>Gets the full configuration path for the delivery-window end.</summary>
    public const string DeliveryWindowEndPath = SectionName + ":DeliveryWindowEnd";

    private TimeSpan? _validatedInterval;

    /// <inheritdoc />
    protected override string ConfigurationSectionName => SectionName;

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
    /// Binds the <c>Cycle</c> section from the supplied configuration and validates the
    /// interval parses to a strictly positive <see cref="TimeSpan"/> (NFR-008, NFR-047). The
    /// delivery-window time zone and start/end are validated when <see cref="ApplicationDefaults"/>
    /// is composed so the full set of cycle keys is checked before the host builds.
    /// </summary>
    public static CycleOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new CycleOptions();
        configuration.GetSection(SectionName).Bind(options);
        options.Validate();
        return options;
    }

    private void Validate() => _validatedInterval = ParseInterval(Interval);

    /// <summary>
    /// Returns the interval validated during <see cref="FromConfiguration"/>, or parses
    /// <see cref="Interval"/> once when the section was constructed outside the bind path
    /// (for example unit-test helpers).
    /// </summary>
    internal TimeSpan ResolveValidatedInterval() => _validatedInterval ?? ParseInterval(Interval);

    internal CycleDefaults ToDefaults()
    {
        TimeSpan interval = ResolveValidatedInterval();
        return new CycleDefaults(
            Interval: interval,
            DeliveryWindowTimeZoneId: DeliveryWindowTimeZoneId,
            DeliveryWindowStart: DeliveryWindowStart,
            DeliveryWindowEnd: DeliveryWindowEnd);
    }

    private static TimeSpan ParseInterval(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{IntervalPath}' is required and must be a valid TimeSpan in hh:mm:ss format.");
        }

        if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan interval))
        {
            throw new InvalidOperationException(
                $"Configuration value '{IntervalPath}' must be a valid TimeSpan in hh:mm:ss format.");
        }

        if (interval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"Configuration value '{IntervalPath}' must be strictly positive.");
        }

        return interval;
    }
}
