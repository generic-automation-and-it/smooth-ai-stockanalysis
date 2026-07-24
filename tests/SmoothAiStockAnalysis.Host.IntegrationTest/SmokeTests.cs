using System.Net;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.TestFramework.Fixtures;

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
            (await SqliteTestHelpers.ExecuteScalarAsync<string>(connection, "PRAGMA journal_mode;", cancellationToken))
                .ToLowerInvariant()
                .ShouldBe("wal");
            (await SqliteTestHelpers.ExecuteScalarAsync<long>(connection, "PRAGMA synchronous;", cancellationToken))
                .ShouldBe(1);
            (await SqliteTestHelpers.ExecuteScalarAsync<long>(
                connection,
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'user_record';",
                cancellationToken)).ShouldBe(1);
            (await SqliteTestHelpers.ExecuteScalarAsync<long>(
                connection,
                """
                SELECT COUNT(*)
                FROM "__EFMigrationsHistory"
                WHERE migration_id LIKE '%_InitialUserSchema'
                   OR migration_id LIKE '%_SnakeCaseNamingConvention';
                """,
                cancellationToken)).ShouldBe(2);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
