extern alias HostApp;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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
    /// <summary>
    /// Keeps the Host's <see cref="IHostedService"/> registrations intact so startup-time
    /// initialisation (e.g. <c>SqliteDatabaseInitializer</c>) runs against the isolated database.
    /// </summary>
    protected override bool RemoveHostedServices => false;

    /// <summary>
    /// Re-registers <see cref="SmoothAiStockAnalysisDbContext"/> options with the
    /// fixture's <c>DatabaseConnectionString</c> and reattaches
    /// <see cref="SqlitePragmaConnectionInterceptor"/>. Required because
    /// <c>Program.cs</c> reads the connection string at builder construction
    /// (before <c>ConfigureAppConfiguration</c> runs), so the earlier
    /// configuration override cannot influence
    /// <c>AddInfrastructurePersistence(connectionString)</c>. See
    /// PERSISTENCE_AGENTS.md → "L2 fixture override".
    /// </summary>
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);
        services.RemoveAll<DbContextOptions<SmoothAiStockAnalysisDbContext>>();
        services.AddDbContext<SmoothAiStockAnalysisDbContext>((serviceProvider, options) =>
        {
            options.UseSqlite(DatabaseConnectionString);
            options.AddInterceptors(
                serviceProvider.GetRequiredService<SqlitePragmaConnectionInterceptor>());
        });
    }
}
