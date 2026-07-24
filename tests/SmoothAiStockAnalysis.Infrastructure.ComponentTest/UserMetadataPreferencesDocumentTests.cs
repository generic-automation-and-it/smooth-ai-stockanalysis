using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Domain.Documents;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

/// <summary>
/// L1 proof for the F-004 worktask: the production <see cref="UserMetadataDocument"/> with
/// v2 typed preferences round-trips through <see cref="VersionedDocumentSqliteValueConverter{TDocument}"/>
/// against an isolated on-disk SQLite file, retains unknown forward-compatible fields, and
/// surfaces a legacy v1 payload with all preferences unset on read (LADR-015).
/// </summary>
public sealed class UserMetadataPreferencesDocumentTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task RoundTripsTypedPreferencesAndSchemaVersion()
    {
        var document = new UserMetadataDocument
        {
            SchemaVersion = UserMetadata.CurrentSchemaVersion,
            CompanySizeFloor = 750_000_000m,
            HoldingHorizonDays = 120,
            CostCapReasoning = 7,
            FxUsdEur = 0.95m,
            CycleInterval = TimeSpan.FromMinutes(5),
            DeliveryWindowTimeZoneId = "Europe/Paris",
            DeliveryWindowStart = "08:00",
            DeliveryWindowEnd = "20:00",
            ProviderReasoning = "Anthropic",
            ReasoningModel = "claude-haiku-4-5-20251001",
        };

        await using (var writeContext = CreateContext(_database.ConnectionString))
        {
            await writeContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            writeContext.MetadataRecords.Add(new MetadataRecord { Metadata = document });
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = CreateContext(_database.ConnectionString);
        UserMetadataDocument stored = (await readContext.MetadataRecords
            .SingleAsync(TestContext.Current.CancellationToken)).Metadata;

        stored.SchemaVersion.ShouldBe(UserMetadata.CurrentSchemaVersion);
        stored.CompanySizeFloor.ShouldBe(750_000_000m);
        stored.HoldingHorizonDays.ShouldBe(120);
        stored.CostCapReasoning.ShouldBe(7);
        stored.FxUsdEur.ShouldBe(0.95m);
        stored.CycleInterval.ShouldBe(TimeSpan.FromMinutes(5));
        stored.DeliveryWindowTimeZoneId.ShouldBe("Europe/Paris");
        stored.ProviderReasoning.ShouldBe("Anthropic");
    }

    [Fact]
    public async Task ToDomainConvertsDocumentPreferencesToDomainMetadata()
    {
        var document = new UserMetadataDocument
        {
            SchemaVersion = UserMetadata.CurrentSchemaVersion,
            CompanySizeFloor = 1_000_000_000m,
            HoldingHorizonDays = 180,
            FxUsdJpy = 155.0m,
        };

        UserMetadata metadata = document.ToDomain();

        metadata.SchemaVersion.ShouldBe(UserMetadata.CurrentSchemaVersion);
        metadata.CompanySizeFloor.ShouldBe(1_000_000_000m);
        metadata.HoldingHorizonDays.ShouldBe(180);
        metadata.FxUsdJpy.ShouldBe(155.0m);
        // Untouched preferences stay null.
        metadata.MinAverageDailyVolume.ShouldBeNull();
        metadata.ProviderReasoning.ShouldBeNull();
    }

    [Fact]
    public async Task RoundTripPreservesUnknownForwardCompatibleFields()
    {
        const string forwardCompatibleJson =
            """
            {"schemaVersion":2,"companySizeFloor":100000000,"holdingHorizonDays":30,"experimentalScoringWeight":0.42}
            """;

        long id;
        await using (var setupContext = CreateContext(_database.ConnectionString))
        {
            await setupContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        await using (var connection = new SqliteConnection(_database.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO metadata_records (metadata) VALUES ($json); SELECT last_insert_rowid();";
            insert.Parameters.AddWithValue("$json", forwardCompatibleJson);
            id = (long)(await insert.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        }

        await using (var readModifyContext = CreateContext(_database.ConnectionString))
        {
            MetadataRecord record = await readModifyContext.MetadataRecords
                .SingleAsync(entity => entity.Id == id, TestContext.Current.CancellationToken);

            record.Metadata.HoldingHorizonDays.ShouldBe(30);
            record.Metadata.ForwardCompatibleFields.ShouldNotBeNull();
            record.Metadata.ForwardCompatibleFields.ShouldContainKey("experimentalScoringWeight");

            record.Metadata.HoldingHorizonDays = 45;
            await readModifyContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = CreateContext(_database.ConnectionString);
        UserMetadataDocument rewritten = (await verifyContext.MetadataRecords
            .SingleAsync(entity => entity.Id == id, TestContext.Current.CancellationToken)).Metadata;

        rewritten.HoldingHorizonDays.ShouldBe(45);
        rewritten.ForwardCompatibleFields.ShouldNotBeNull();
        rewritten.ForwardCompatibleFields.ShouldContainKey("experimentalScoringWeight");
        rewritten.ForwardCompatibleFields["experimentalScoringWeight"].GetDouble().ShouldBe(0.42);
    }

    [Fact]
    public async Task LegacyVersionOneDocumentReadsWithAllPreferencesUnset()
    {
        const string legacyVersionOneJson = """{"schemaVersion":1}""";

        long id;
        await using (var setupContext = CreateContext(_database.ConnectionString))
        {
            await setupContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        await using (var connection = new SqliteConnection(_database.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO metadata_records (metadata) VALUES ($json); SELECT last_insert_rowid();";
            insert.Parameters.AddWithValue("$json", legacyVersionOneJson);
            id = (long)(await insert.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        }

        await using var readContext = CreateContext(_database.ConnectionString);
        UserMetadataDocument stored = (await readContext.MetadataRecords
            .SingleAsync(entity => entity.Id == id, TestContext.Current.CancellationToken)).Metadata;

        UserMetadata metadata = stored.ToDomain();

        // Legacy v1 document: the persisted version marker is preserved and every preference
        // is unset, so the resolver falls through to application defaults. The next save
        // through ApplyDomainState promotes the document to the current schema version.
        metadata.SchemaVersion.ShouldBe(1);
        metadata.CompanySizeFloor.ShouldBeNull();
        metadata.HoldingHorizonDays.ShouldBeNull();
        metadata.FxUsdEur.ShouldBeNull();
    }

    [Fact]
    public async Task ApplyDomainStatePromotesLegacyVersionToCurrentVersion()
    {
        var legacy = new UserMetadataDocument { SchemaVersion = 1 };
        var updated = UserMetadata.Create().WithPreferences(companySizeFloor: 500m);

        legacy.ApplyDomainState(updated);

        legacy.SchemaVersion.ShouldBe(UserMetadata.CurrentSchemaVersion);
        legacy.CompanySizeFloor.ShouldBe(500m);
    }

    [Fact]
    public async Task ApplyDomainStateRejectsVersionRegression()
    {
        // Persisted document carries a future schema version; the Domain factory cannot
        // downgrade it.
        var current = new UserMetadataDocument { SchemaVersion = UserMetadata.CurrentSchemaVersion + 1 };
        var older = UserMetadata.Create().WithPreferences(companySizeFloor: 1m);

        Should.Throw<InvalidOperationException>(() => current.ApplyDomainState(older));
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private static MetadataDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MetadataDbContext>()
            .UseSqlite(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new MetadataDbContext(options);
    }

    private sealed class MetadataDbContext(DbContextOptions<MetadataDbContext> options)
        : SmoothAiStockAnalysisDbContext(options)
    {
        public DbSet<MetadataRecord> MetadataRecords => Set<MetadataRecord>();

        protected override void OnModelCreatingCore(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MetadataRecord>(entity =>
            {
                var metadataProperty = entity.Property(record => record.Metadata);
                metadataProperty.HasConversion(new VersionedDocumentSqliteValueConverter<UserMetadataDocument>());
                metadataProperty.Metadata.SetValueComparer(
                    new VersionedDocumentSqliteValueComparer<UserMetadataDocument>());
            });
        }
    }

    private sealed class MetadataRecord
    {
        public long Id { get; init; }

        public required UserMetadataDocument Metadata { get; set; }
    }
}
