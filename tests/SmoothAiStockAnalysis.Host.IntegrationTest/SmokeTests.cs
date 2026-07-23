using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Infrastructure.Persistence;

namespace SmoothAiStockAnalysis.Host.IntegrationTest;

public sealed class SmokeTests(HostWebAppFixture fixture) : IClassFixture<HostWebAppFixture>
{
    [Fact]
    public async Task HostBootsAndRespondsToHttp()
    {
        using var response = await fixture.HttpClient.GetAsync("/", TestContext.Current.CancellationToken);

        // The template Host registers no endpoints yet, so an un-routed request returns 404 —
        // proving the app booted and the HTTP pipeline is alive.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using IServiceScope scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        (await dbContext.Database.CanConnectAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        File.Exists(fixture.DatabasePath).ShouldBeTrue();
    }
}
