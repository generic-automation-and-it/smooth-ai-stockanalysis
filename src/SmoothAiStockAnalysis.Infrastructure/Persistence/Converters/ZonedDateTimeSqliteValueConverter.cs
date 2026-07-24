using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using NodaTime.Text;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts zoned date-times to lossless UTC instant text plus TZDB zone and calendar identities.
/// </summary>
internal sealed class ZonedDateTimeSqliteValueConverter : ValueConverter<ZonedDateTime, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZonedDateTimeSqliteValueConverter"/> class.
    /// </summary>
    public ZonedDateTimeSqliteValueConverter()
        : base(value => Format(value), value => Parse(value))
    {
    }

    private static string Format(ZonedDateTime value) => string.Join(
        '|',
        InstantPattern.ExtendedIso.Format(value.ToInstant()),
        value.Zone.Id,
        value.Calendar.Id);

    private static ZonedDateTime Parse(string value)
    {
        string[] parts = value.Split('|', StringSplitOptions.None);
        if (parts.Length != 3)
        {
            throw new FormatException("The SQLite zoned-date-time value is invalid.");
        }

        DateTimeZone zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(parts[1])
            ?? throw new FormatException($"The SQLite zoned-date-time zone '{parts[1]}' is unknown.");

        return InstantPattern.ExtendedIso.Parse(parts[0]).Value.InZone(zone, CalendarSystem.ForId(parts[2]));
    }
}
