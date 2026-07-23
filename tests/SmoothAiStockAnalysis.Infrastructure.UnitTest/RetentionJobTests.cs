using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SmoothAiStockAnalysis.Infrastructure.Extensions;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Retention;

namespace SmoothAiStockAnalysis.Infrastructure.UnitTest;

public sealed class RetentionJobTests
{
    [Fact]
    public async Task RetainsOneMonthAndPerformsNoPruningUntilHistoryExists()
    {
        var job = new AnalysisHistoryRetentionJob(
            Options.Create(new AnalysisHistoryRetentionOptions()));

        job.RetentionMonths.ShouldBe(1);
        await job.PruneExpiredHistoryAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void RegistersTheRetentionHostedService()
    {
        var services = new ServiceCollection();

        services.AddInfrastructurePersistence("Data Source=:memory:");

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(AnalysisHistoryRetentionHostedService));
    }
}
