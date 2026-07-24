using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Configurations;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;
using Xunit.v3;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest.Persistence;

/// <summary>
/// Shared L1 fixture for the user-owned ownership/uniqueness probe model.
/// Owns the isolated SQLite file and the production-derived probe <see cref="DbContext"/>.
/// </summary>
public sealed class OwnershipProbeFixture : IAsyncLifetime
{
    private readonly SqliteTestDatabase _database = new();

    public string ConnectionString => _database.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await using OwnershipProbeDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
    }

    public OwnershipProbeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<OwnershipProbeDbContext>()
            .UseSqlite(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new OwnershipProbeDbContext(options);
    }

    public async Task<CompositeIndex> ReadOwnedProbeUniqueIndexAsync(CancellationToken cancellationToken)
    {
        await using OwnershipProbeDbContext context = CreateContext();
        await context.Database.OpenConnectionAsync(cancellationToken);
        DbConnection connection = context.Database.GetDbConnection();

        await using DbCommand indexListCommand = connection.CreateCommand();
        indexListCommand.CommandText = "PRAGMA index_list('owned_probe_records');";
        await using DbDataReader indexListReader = await indexListCommand.ExecuteReaderAsync(cancellationToken);

        string? indexName = null;
        bool isUnique = false;
        const string expectedIndexName = "ix_owned_probe_records_user_id_ticker";
        while (await indexListReader.ReadAsync(cancellationToken))
        {
            string candidate = indexListReader.GetString(1);
            if (candidate == expectedIndexName)
            {
                indexName = candidate;
                isUnique = indexListReader.GetInt64(2) == 1;
                break;
            }
        }

        if (indexName is null)
        {
            throw new InvalidOperationException(
                $"Expected a composite unique index named '{expectedIndexName}' on owned_probe_records (user_id, ticker).");
        }

        await using DbCommand indexInfoCommand = connection.CreateCommand();
        indexInfoCommand.CommandText = $"PRAGMA index_info('{indexName}');";
        await using DbDataReader indexInfoReader = await indexInfoCommand.ExecuteReaderAsync(cancellationToken);

        var columns = new List<string>();
        while (await indexInfoReader.ReadAsync(cancellationToken))
        {
            columns.Add(indexInfoReader.GetString(2));
        }

        return new CompositeIndex(indexName, isUnique, columns);
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();
}

/// <summary>
/// Production model plus one test-only owned dependent configured through the shared helpers.
/// </summary>
public sealed class OwnershipProbeDbContext(DbContextOptions options)
    : SmoothAiStockAnalysisDbContext(options)
{
    public DbSet<OwnedProbeRecord> OwnedProbeRecords => Set<OwnedProbeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OwnedProbeRecord>(entity =>
        {
            entity.ConfigureUserOwnedDependent();
            entity.Property(record => record.Ticker).IsRequired();
            entity.HasUserScopedUniqueIndex(nameof(OwnedProbeRecord.Ticker));
        });
    }
}

/// <summary>
/// Test-only owned dependent used to prove the composite uniqueness extension path.
/// </summary>
public sealed class OwnedProbeRecord
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Ticker { get; set; } = string.Empty;
}

/// <summary>
/// Physical SQLite unique-index shape observed for the owned probe table.
/// </summary>
public sealed record CompositeIndex(string Name, bool IsUnique, IReadOnlyList<string> Columns);
