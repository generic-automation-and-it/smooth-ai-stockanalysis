using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Extensions;
using SmoothAiStockAnalysis.Infrastructure.Persistence;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

public sealed class SqlitePersistenceTests
{
    [Fact]
    public async Task ConfiguresWalAndNormalSynchronousWrites()
    {
        await using var database = SqliteTestDatabase.Create();
        await using ServiceProvider serviceProvider = CreateServiceProvider(database.ConnectionString);
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);

        DbConnection connection = dbContext.Database.GetDbConnection();
        (await ExecuteScalarAsync<string>(connection, "PRAGMA journal_mode;", TestContext.Current.CancellationToken))
            .ToLowerInvariant()
            .ShouldBe("wal");
        (await ExecuteScalarAsync<long>(connection, "PRAGMA synchronous;", TestContext.Current.CancellationToken))
            .ShouldBe(1);
    }

    [Fact]
    public async Task UnitOfWorkCommitsOneCycleOfWrites()
    {
        await using var database = SqliteTestDatabase.Create();
        await using ServiceProvider serviceProvider = CreateServiceProvider(database.ConnectionString);
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IAnalysisCycleUnitOfWork>();

        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE persistence_probe (value TEXT NOT NULL);",
            TestContext.Current.CancellationToken);

        await unitOfWork.ExecuteAsync(
            cancellationToken => dbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO persistence_probe (value) VALUES ('committed');",
                cancellationToken),
            TestContext.Current.CancellationToken);

        await dbContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        string value = await ExecuteScalarAsync<string>(
            dbContext.Database.GetDbConnection(),
            "SELECT value FROM persistence_probe;",
            TestContext.Current.CancellationToken);

        value.ShouldBe("committed");
    }

    [Fact]
    public async Task UnitOfWorkRollsBackWhenCycleFails()
    {
        await using var database = SqliteTestDatabase.Create();
        await using ServiceProvider serviceProvider = CreateServiceProvider(database.ConnectionString);
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IAnalysisCycleUnitOfWork>();

        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE persistence_probe (value TEXT NOT NULL);",
            TestContext.Current.CancellationToken);

        await Should.ThrowAsync<InvalidOperationException>(() => unitOfWork.ExecuteAsync(
            async cancellationToken =>
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "INSERT INTO persistence_probe (value) VALUES ('rolled-back');",
                    cancellationToken);
                throw new InvalidOperationException("Cycle failed.");
            },
            TestContext.Current.CancellationToken));

        await dbContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        long rowCount = await ExecuteScalarAsync<long>(
            dbContext.Database.GetDbConnection(),
            "SELECT COUNT(*) FROM persistence_probe;",
            TestContext.Current.CancellationToken);

        rowCount.ShouldBe(0);
    }

    private static ServiceProvider CreateServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddInfrastructurePersistence(connectionString);

        return services.BuildServiceProvider();
    }

    private static async Task<T> ExecuteScalarAsync<T>(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        object nonNullResult = result ?? throw new InvalidOperationException("The SQL command returned no value.");
        return (T)Convert.ChangeType(nonNullResult, typeof(T));
    }
}
