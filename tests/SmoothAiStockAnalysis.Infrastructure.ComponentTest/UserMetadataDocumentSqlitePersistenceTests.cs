using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Domain.Documents;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

/// <summary>
/// Proof for the T-015 / #59 spike: a versioned metadata document round-trips through the
/// production <see cref="VersionedDocumentSqliteValueConverter{TDocument}"/> against an isolated on-disk
/// SQLite file, without EF InMemory or a live provider. The document type here is a test-only probe;
/// the production user-metadata document is introduced by worktask 02 (T-016).
/// </summary>
public sealed class UserMetadataDocumentSqlitePersistenceTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task RoundTripsVersionMarkerAndRepresentativePreferenceValues()
    {
        var document = new ProbeMetadataDocument
        {
            SchemaVersion = 1,
            CompanySizeFloor = 250_000_000.55m,
            HoldingHorizonDays = 90,
            DeliveryWindowZone = "Europe/Paris"
        };

        await using (var writeContext = CreateContext(_database.ConnectionString))
        {
            await writeContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            writeContext.MetadataRecords.Add(new MetadataRecord { Metadata = document });
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = CreateContext(_database.ConnectionString);
        ProbeMetadataDocument stored = (await readContext.MetadataRecords
            .SingleAsync(TestContext.Current.CancellationToken)).Metadata;

        stored.SchemaVersion.ShouldBe(1);
        stored.CompanySizeFloor.ShouldBe(250_000_000.55m);
        stored.HoldingHorizonDays.ShouldBe(90);
        stored.DeliveryWindowZone.ShouldBe("Europe/Paris");
    }

    [Fact]
    public async Task PreservesUnknownForwardCompatibleFieldsAcrossAReadModifyWriteCycle()
    {
        const string forwardCompatibleJson =
            """
            {"schemaVersion":2,"companySizeFloor":100000000,"holdingHorizonDays":30,"deliveryWindowZone":"Europe/Oslo","experimentalScoringWeight":0.42}
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
            insert.CommandText = "INSERT INTO metadata_records (Metadata) VALUES ($json); SELECT last_insert_rowid();";
            insert.Parameters.AddWithValue("$json", forwardCompatibleJson);
            id = (long)(await insert.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        }

        await using (var readModifyContext = CreateContext(_database.ConnectionString))
        {
            MetadataRecord record = await readModifyContext.MetadataRecords
                .SingleAsync(entity => entity.Id == id, TestContext.Current.CancellationToken);

            record.Metadata.SchemaVersion.ShouldBe(2);
            record.Metadata.DeliveryWindowZone.ShouldBe("Europe/Oslo");
            record.Metadata.ForwardCompatibleFields.ShouldNotBeNull();
            record.Metadata.ForwardCompatibleFields.ShouldContainKey("experimentalScoringWeight");

            record.Metadata.HoldingHorizonDays = 45;
            await readModifyContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = CreateContext(_database.ConnectionString);
        ProbeMetadataDocument rewritten = (await verifyContext.MetadataRecords
            .SingleAsync(entity => entity.Id == id, TestContext.Current.CancellationToken)).Metadata;

        rewritten.HoldingHorizonDays.ShouldBe(45);
        rewritten.ForwardCompatibleFields.ShouldNotBeNull();
        rewritten.ForwardCompatibleFields.ShouldContainKey("experimentalScoringWeight");
        rewritten.ForwardCompatibleFields["experimentalScoringWeight"].GetDouble().ShouldBe(0.42);
    }

    [Fact]
    public async Task StoresTheDocumentAsInspectableJsonTextCarryingTheVersionMarker()
    {
        await using (var writeContext = CreateContext(_database.ConnectionString))
        {
            await writeContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            writeContext.MetadataRecords.Add(new MetadataRecord
            {
                Metadata = new ProbeMetadataDocument
                {
                    SchemaVersion = 1,
                    CompanySizeFloor = 5m,
                    HoldingHorizonDays = 7,
                    DeliveryWindowZone = "Europe/Paris"
                }
            });
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var connection = new SqliteConnection(_database.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT typeof(Metadata), Metadata FROM metadata_records LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        (await reader.ReadAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        reader.GetString(0).ShouldBe("text");
        reader.GetString(1).ShouldContain("\"schemaVersion\":1");
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private static MetadataDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MetadataDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new MetadataDbContext(options);
    }

    private sealed class MetadataDbContext(DbContextOptions<MetadataDbContext> options)
        : SmoothAiStockAnalysisDbContext(options)
    {
        public DbSet<MetadataRecord> MetadataRecords => Set<MetadataRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<MetadataRecord>(entity =>
            {
                entity.ToTable("metadata_records");
                var metadataProperty = entity.Property(record => record.Metadata);
                metadataProperty.HasConversion(new VersionedDocumentSqliteValueConverter<ProbeMetadataDocument>());
                metadataProperty.Metadata.SetValueComparer(
                    new VersionedDocumentSqliteValueComparer<ProbeMetadataDocument>());
            });
    }

    private sealed class MetadataRecord
    {
        public long Id { get; init; }

        public required ProbeMetadataDocument Metadata { get; set; }
    }

    /// <summary>
    /// Test-only stand-in for the future user-metadata document. It exercises the version marker,
    /// representative preference values, and forward-compatible unknown-field retention.
    /// </summary>
    private sealed class ProbeMetadataDocument : IVersionedDocument
    {
        public int SchemaVersion { get; init; }

        public decimal CompanySizeFloor { get; init; }

        public int HoldingHorizonDays { get; set; }

        public string DeliveryWindowZone { get; init; } = string.Empty;

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ForwardCompatibleFields { get; set; }
    }
}
