using System.Net;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Infrastructure.Persistence;

namespace SmoothAiStockAnalysis.Host.IntegrationTest;

public sealed class SmokeTests(HostWebAppFixture fixture) : IClassFixture<HostWebAppFixture>
{
    [Fact]
    public async Task HostBootsAndRespondsToHttp()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var response = await fixture.HttpClient.GetAsync("/", cancellationToken);

        // The template Host registers no endpoints yet, so an un-routed request returns 404 —
        // proving the app booted and the HTTP pipeline is alive.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using IServiceScope scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        (await dbContext.Database.CanConnectAsync(cancellationToken)).ShouldBeTrue();
        File.Exists(fixture.DatabasePath).ShouldBeTrue();

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            DbConnection connection = dbContext.Database.GetDbConnection();
            (await ExecuteScalarAsync<string>(connection, "PRAGMA journal_mode;", cancellationToken))
                .ToLowerInvariant()
                .ShouldBe("wal");
            (await ExecuteScalarAsync<long>(connection, "PRAGMA synchronous;", cancellationToken))
                .ShouldBe(1);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
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
