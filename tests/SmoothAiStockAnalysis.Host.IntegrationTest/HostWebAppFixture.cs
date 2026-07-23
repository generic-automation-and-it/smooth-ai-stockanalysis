extern alias HostApp;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.TestFramework.Fixtures;

namespace SmoothAiStockAnalysis.Host.IntegrationTest;

/// <summary>
/// Closes the generic <see cref="WebAppFixture{TProgram}"/> over the Host's entry point.
/// The <c>HostApp</c> extern alias disambiguates the Host's <c>Program</c> from the test
/// assembly's own auto-generated <c>Program</c> (xunit.v3 compiles test projects as executables).
/// </summary>
public sealed class HostWebAppFixture : WebAppFixture<HostApp::Program>
{
    protected override bool RemoveHostedServices => false;

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<DbContextOptions<SmoothAiStockAnalysisDbContext>>();
        services.AddDbContext<SmoothAiStockAnalysisDbContext>(options =>
            options.UseSqlite(DatabaseConnectionString));
    }
}
