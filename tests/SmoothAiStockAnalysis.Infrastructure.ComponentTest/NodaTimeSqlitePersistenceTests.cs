using Microsoft.EntityFrameworkCore;
using NodaTime;
using SmoothAiStockAnalysis.Infrastructure.Persistence;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

public sealed class NodaTimeSqlitePersistenceTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task RoundTripsNodaTimeValuesThroughAnOnDiskSqliteDatabase()
    {
        Instant instant = Instant.FromUtc(2026, 3, 29, 0, 59, 59) + Duration.FromNanoseconds(123_456_789);
        var localDate = new LocalDate(2024, 2, 29, CalendarSystem.Julian);
        DateTimeZone paris = DateTimeZoneProviders.Tzdb["Europe/Paris"];
        ZonedDateTime earlierFallBack = Instant.FromUtc(2026, 10, 25, 0, 30).InZone(paris);
        ZonedDateTime laterFallBack = Instant.FromUtc(2026, 10, 25, 1, 30).InZone(paris);

        await using (var writeContext = CreateContext(_database.ConnectionString))
        {
            await writeContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            writeContext.TimeRoundTripRecords.AddRange(
                new TimeRoundTripRecord { Instant = instant, LocalDate = localDate, ZonedDateTime = earlierFallBack },
                new TimeRoundTripRecord { Instant = instant, LocalDate = localDate, ZonedDateTime = laterFallBack });
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = CreateContext(_database.ConnectionString);
        List<TimeRoundTripRecord> records = await readContext.TimeRoundTripRecords
            .OrderBy(record => record.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        records.Count.ShouldBe(2);
        records.ShouldAllBe(record => record.Instant == instant && record.LocalDate == localDate);
        AssertZonedValue(records[0].ZonedDateTime, earlierFallBack);
        AssertZonedValue(records[1].ZonedDateTime, laterFallBack);
    }

    [Fact]
    public async Task StoresInstantsAsUtcIsoTextWithoutLosingNanoseconds()
    {
        Instant instant = Instant.FromUtc(2026, 3, 29, 0, 59, 59) + Duration.FromNanoseconds(1);

        await using (var writeContext = CreateContext(_database.ConnectionString))
        {
            await writeContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            writeContext.TimeRoundTripRecords.Add(new TimeRoundTripRecord
            {
                Instant = instant,
                LocalDate = new LocalDate(2024, 2, 29),
                ZonedDateTime = instant.InUtc()
            });
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection(_database.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT typeof(Instant), Instant FROM time_round_trip_records LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        (await reader.ReadAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        reader.GetString(0).ShouldBe("text");
        reader.GetString(1).ShouldBe("2026-03-29T00:59:59.000000001Z");
    }

    private static TimeRoundTripDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TimeRoundTripDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new TimeRoundTripDbContext(options);
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private static void AssertZonedValue(ZonedDateTime actual, ZonedDateTime expected)
    {
        actual.ToInstant().ShouldBe(expected.ToInstant());
        actual.Zone.Id.ShouldBe(expected.Zone.Id);
        actual.Calendar.Id.ShouldBe(expected.Calendar.Id);
        actual.LocalDateTime.ShouldBe(expected.LocalDateTime);
        actual.Offset.ShouldBe(expected.Offset);
    }

    private sealed class TimeRoundTripDbContext(DbContextOptions<TimeRoundTripDbContext> options) : SmoothAiStockAnalysisDbContext(options)
    {
        public DbSet<TimeRoundTripRecord> TimeRoundTripRecords => Set<TimeRoundTripRecord>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
            base.ConfigureConventions(configurationBuilder);

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TimeRoundTripRecord>().ToTable("time_round_trip_records");
    }

    private sealed class TimeRoundTripRecord
    {
        public int Id { get; init; }

        public Instant Instant { get; init; }

        public LocalDate LocalDate { get; init; }

        public ZonedDateTime ZonedDateTime { get; init; }
    }
}
