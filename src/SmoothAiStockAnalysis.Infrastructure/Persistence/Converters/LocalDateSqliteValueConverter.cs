using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts local dates to an invariant text form that also retains calendar identity.
/// </summary>
internal sealed class LocalDateSqliteValueConverter : ValueConverter<LocalDate, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateSqliteValueConverter"/> class.
    /// </summary>
    public LocalDateSqliteValueConverter()
        : base(date => Format(date), value => Parse(value))
    {
    }

    private static string Format(LocalDate date) => string.Join(
        '|',
        date.Year.ToString(CultureInfo.InvariantCulture),
        date.Month.ToString(CultureInfo.InvariantCulture),
        date.Day.ToString(CultureInfo.InvariantCulture),
        date.Calendar.Id);

    private static LocalDate Parse(string value)
    {
        string[] parts = value.Split('|', StringSplitOptions.None);
        if (parts.Length != 4)
        {
            throw new FormatException("The SQLite local-date value is invalid.");
        }

        return new LocalDate(
            int.Parse(parts[0], CultureInfo.InvariantCulture),
            int.Parse(parts[1], CultureInfo.InvariantCulture),
            int.Parse(parts[2], CultureInfo.InvariantCulture),
            CalendarSystem.ForId(parts[3]));
    }
}
