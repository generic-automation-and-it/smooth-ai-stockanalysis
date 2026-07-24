extern alias HostApp;

using Microsoft.Extensions.Hosting;
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

}
