using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Domain.Documents;
using SmoothAiStockAnalysis.Domain.Entities;
using SmoothAiStockAnalysis.Infrastructure.Extensions;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

public sealed class UserSchemaMigrationTests : IAsyncDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task InitialMigrationCreatesTheExpectedUserSchema()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        // Schema/setup tests deliberately use the named system scope so owned-row setup is not blocked by isolation.
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await dbContext.Database.MigrateAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);

        string[] appliedMigrations = [.. await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)];
        appliedMigrations.Length.ShouldBe(2);
        appliedMigrations[0].ShouldEndWith("_InitialUserSchema");
        appliedMigrations[1].ShouldEndWith("_SnakeCaseNamingConvention");

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        DbConnection connection = dbContext.Database.GetDbConnection();

        IReadOnlyList<ColumnDefinition> columns = await ReadColumnsAsync(connection, cancellationToken);
        columns.ShouldBe(
        [
            new ColumnDefinition("id", "INTEGER", IsRequired: true, IsPrimaryKey: true),
            new ColumnDefinition("metadata", "TEXT", IsRequired: true, IsPrimaryKey: false),
            new ColumnDefinition("unique_identifier", "TEXT", IsRequired: true, IsPrimaryKey: false)
        ]);
        columns.ShouldNotContain(column => column.Name == "user_id");

        IndexDefinition index = await ReadUniqueIdentifierIndexAsync(connection, cancellationToken);
        index.ShouldBe(new IndexDefinition("ix_user_record_unique_identifier", IsUnique: true, "unique_identifier"));

        string createTableSql = await SqliteTestHelpers.ExecuteScalarAsync<string>(
            connection,
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'user_record';",
            cancellationToken);
        createTableSql.ShouldContain("CONSTRAINT \"pk_user_record\" PRIMARY KEY");
    }

    [Fact]
    public async Task PersistsGeneratedIdsAndMapsVersionedMetadata()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        // Schema/setup tests deliberately use the named system scope so owned-row setup is not blocked by isolation.
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await dbContext.Database.MigrateAsync(cancellationToken);

        Guid firstIdentifier = Guid.NewGuid();
        Guid secondIdentifier = Guid.NewGuid();
        UserRecord firstRecord = UserRecord.FromDomain(User.Create(firstIdentifier));
        UserRecord secondRecord = UserRecord.FromDomain(User.Create(secondIdentifier));

        dbContext.Users().AddRange(firstRecord, secondRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        firstRecord.Id.ShouldBeGreaterThan(0);
        secondRecord.Id.ShouldBeGreaterThan(0);
        secondRecord.Id.ShouldNotBe(firstRecord.Id);

        User firstUser = firstRecord.ToDomain();
        firstUser.Id.ShouldBe(firstRecord.Id);
        firstUser.UniqueIdentifier.ShouldBe(firstIdentifier);
        firstUser.Metadata.SchemaVersion.ShouldBe(UserMetadata.CurrentSchemaVersion);

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        DbConnection connection = dbContext.Database.GetDbConnection();
        string metadataStorage = await SqliteTestHelpers.ExecuteScalarAsync<string>(
            connection,
            """
            SELECT typeof(metadata) || ':' || json_extract(metadata, '$.schemaVersion')
            FROM user_record
            WHERE id = 1;
            """,
            cancellationToken);
        metadataStorage.ShouldBe($"text:{UserMetadata.CurrentSchemaVersion}");
    }

    [Fact]
    public async Task UniqueIdentifierIndexRejectsDuplicates()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        // Schema/setup tests deliberately use the named system scope so owned-row setup is not blocked by isolation.
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await dbContext.Database.MigrateAsync(cancellationToken);

        Guid duplicateIdentifier = Guid.NewGuid();
        dbContext.Users().Add(UserRecord.FromDomain(User.Create(duplicateIdentifier)));
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Users().Add(UserRecord.FromDomain(User.Create(duplicateIdentifier)));

        await Should.ThrowAsync<DbUpdateException>(() => dbContext.SaveChangesAsync(cancellationToken));
    }

    [Fact]
    public async Task ReadModifyWritePreservesUnknownMetadataFields()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        // Schema/setup tests deliberately use the named system scope so owned-row setup is not blocked by isolation.
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await dbContext.Database.MigrateAsync(cancellationToken);

        Guid uniqueIdentifier = Guid.NewGuid();
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO user_record (unique_identifier, metadata)
             VALUES ({uniqueIdentifier}, {"""{"schemaVersion":2,"futurePreference":"retained"}"""});
             """,
            cancellationToken);

        UserRecord record = await dbContext.Users().SingleAsync(cancellationToken);
        User reconstituted = record.ToDomain();
        reconstituted.Metadata.SchemaVersion.ShouldBe(2);
        record.Metadata.ForwardCompatibleFields.ShouldNotBeNull();
        record.Metadata.ForwardCompatibleFields.ShouldContainKey("futurePreference");

        User updated = User.Reconstitute(
            record.Id,
            record.UniqueIdentifier,
            UserMetadata.Reconstitute(3));
        record.ApplyDomainState(updated);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        string storedMetadata = await SqliteTestHelpers.ExecuteScalarAsync<string>(
            dbContext.Database.GetDbConnection(),
            "SELECT metadata FROM user_record;",
            cancellationToken);
        storedMetadata.ShouldContain("\"schemaVersion\":3");
        storedMetadata.ShouldContain("\"futurePreference\":\"retained\"");
    }

    [Fact]
    public void CreatingARecordFromAPersistedUserIsRejected()
    {
        User persistedUser = User.Reconstitute(
            42,
            Guid.NewGuid(),
            UserMetadata.Create());

        var exception = Should.Throw<ArgumentException>(
            () => UserRecord.FromDomain(persistedUser));

        exception.ParamName.ShouldBe("user");
    }

    [Fact]
    public void ApplyingDomainStateRejectsMetadataSchemaVersionRegression()
    {
        // The persisted document is newer than the Domain state being applied — the Domain
        // factory cannot downgrade the schema version (LADR-015, NFR-048).
        var document = new UserMetadataDocument
        {
            SchemaVersion = UserMetadata.CurrentSchemaVersion + 1,
            ForwardCompatibleFields = new Dictionary<string, JsonElement>
            {
                ["futurePreference"] = JsonSerializer.Deserialize<JsonElement>("\"retained\"")
            }
        };

        Should.Throw<InvalidOperationException>(
            () => document.ApplyDomainState(UserMetadata.Create()));

        document.SchemaVersion.ShouldBe(UserMetadata.CurrentSchemaVersion + 1);
        document.ForwardCompatibleFields.ShouldContainKey("futurePreference");
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructurePersistence(_database.ConnectionString);
        return services.BuildServiceProvider();
    }

    private static async Task<IReadOnlyList<ColumnDefinition>> ReadColumnsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('user_record');";
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        var columns = new List<ColumnDefinition>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnDefinition(
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt64(3) == 1,
                reader.GetInt64(5) == 1));
        }

        return columns;
    }

    private static async Task<IndexDefinition> ReadUniqueIdentifierIndexAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using DbCommand indexListCommand = connection.CreateCommand();
        indexListCommand.CommandText = "PRAGMA index_list('user_record');";
        await using DbDataReader indexListReader = await indexListCommand.ExecuteReaderAsync(cancellationToken);

        string? indexName = null;
        bool isUnique = false;
        while (await indexListReader.ReadAsync(cancellationToken))
        {
            if (indexListReader.GetString(1) == "ix_user_record_unique_identifier")
            {
                indexName = indexListReader.GetString(1);
                isUnique = indexListReader.GetInt64(2) == 1;
                break;
            }
        }

        indexName.ShouldNotBeNull();
        await indexListReader.DisposeAsync();

        await using DbCommand indexInfoCommand = connection.CreateCommand();
        indexInfoCommand.CommandText = $"PRAGMA index_info('{indexName}');";
        await using DbDataReader indexInfoReader = await indexInfoCommand.ExecuteReaderAsync(cancellationToken);
        (await indexInfoReader.ReadAsync(cancellationToken)).ShouldBeTrue();
        string columnName = indexInfoReader.GetString(2);
        (await indexInfoReader.ReadAsync(cancellationToken)).ShouldBeFalse();

        return new IndexDefinition(indexName, isUnique, columnName);
    }

    private sealed record ColumnDefinition(string Name, string Type, bool IsRequired, bool IsPrimaryKey);

    private sealed record IndexDefinition(string Name, bool IsUnique, string ColumnName);
}
