using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

/// <summary>
/// Registers the lossless NodaTime-to-SQLite mappings shared by every persistence model.
/// </summary>
internal static class NodaTimeSqliteConventions
{
    /// <summary>
    /// Configures all NodaTime values as canonical SQLite text representations.
    /// </summary>
    internal static void Configure(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Instant>().HaveConversion<InstantSqliteValueConverter>();
        configurationBuilder.Properties<LocalDate>().HaveConversion<LocalDateSqliteValueConverter>();
        configurationBuilder.Properties<ZonedDateTime>().HaveConversion<ZonedDateTimeSqliteValueConverter>();
    }
}
