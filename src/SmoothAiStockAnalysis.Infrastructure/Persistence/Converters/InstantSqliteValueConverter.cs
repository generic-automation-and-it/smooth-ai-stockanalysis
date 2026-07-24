using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using NodaTime.Text;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts instants to their lossless, invariant UTC ISO-8601 representation.
/// </summary>
internal sealed class InstantSqliteValueConverter : ValueConverter<Instant, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InstantSqliteValueConverter"/> class.
    /// </summary>
    public InstantSqliteValueConverter()
        : base(instant => Format(instant), value => Parse(value))
    {
    }

    private static string Format(Instant instant) => InstantPattern.ExtendedIso.Format(instant);

    private static Instant Parse(string value) => InstantPattern.ExtendedIso.Parse(value).Value;
}
