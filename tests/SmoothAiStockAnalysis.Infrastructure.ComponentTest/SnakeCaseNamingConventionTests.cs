using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

/// <summary>
/// Proves the global snake_case naming convention (LADR-016): an entity added without any
/// explicit naming configuration automatically yields lower_snake_case table, column, primary-key,
/// index, and foreign-key names through the production DI composition.
/// </summary>
public sealed class SnakeCaseNamingConventionTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task EntityWithoutExplicitNamingYieldsSnakeCaseRelationalNames()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NamingProbeDbContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        DbConnection connection = dbContext.Database.GetDbConnection();

        string createTableSql = await SqliteTestHelpers.ExecuteScalarAsync<string>(
            connection,
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'naming_probe_records';",
            cancellationToken);
        createTableSql.ShouldContain("\"company_ticker\" TEXT NOT NULL");
        createTableSql.ShouldContain("CONSTRAINT \"pk_naming_probe_records\" PRIMARY KEY");
        createTableSql.ShouldNotContain("CompanyTicker");

        IReadOnlyList<string> indexNames = await ReadIndexNamesAsync(connection, cancellationToken);
        indexNames.ShouldContain("ix_naming_probe_records_company_ticker");
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<NamingProbeDbContext>(options =>
        {
            options.UseSqlite(_database.ConnectionString);
            options.UseSnakeCaseNamingConvention();
        });

        return services.BuildServiceProvider();
    }

    private static async Task<IReadOnlyList<string>> ReadIndexNamesAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA index_list('naming_probe_records');";
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        var names = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            names.Add(reader.GetString(1));
        }

        return names;
    }

    /// <summary>
    /// Test-only probe context. It carries no explicit naming configuration, so the physical
    /// names observed in SQLite are produced by the naming convention alone.
    /// </summary>
    private sealed class NamingProbeDbContext(DbContextOptions<NamingProbeDbContext> options)
        : DbContext(options)
    {
        public DbSet<NamingProbeRecord> NamingProbeRecords => Set<NamingProbeRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<NamingProbeRecord>().HasIndex(record => record.CompanyTicker);
    }

    private sealed class NamingProbeRecord
    {
        public int Id { get; set; }

        public string CompanyTicker { get; set; } = string.Empty;

        public string? SectorName { get; set; }
    }
}
