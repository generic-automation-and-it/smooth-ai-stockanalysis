using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SmoothAiStockAnalysis.Infrastructure.Extensions;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Retention;

namespace SmoothAiStockAnalysis.Infrastructure.UnitTest;

public sealed class RetentionJobTests
{
    [Fact]
    public void DefaultsRetentionToOneMonth()
    {
        var job = new AnalysisHistoryRetentionJob(
            Options.Create(new AnalysisHistoryRetentionOptions()));

        job.RetentionMonths.ShouldBe(1);
    }

    [Fact]
    public async Task PruneCompletesBeforeHistoryExists()
    {
        var job = new AnalysisHistoryRetentionJob(
            Options.Create(new AnalysisHistoryRetentionOptions()));

        await job.PruneExpiredHistoryAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void RegistersTheRetentionHostedServiceAndSingletonJobLifetime()
    {
        using var services = new ServiceCollection()
            .AddInfrastructurePersistence("Data Source=:memory:")
            .BuildServiceProvider();

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(AnalysisHistoryRetentionHostedService));

        var jobDescriptor = services.Single(d => d.ServiceType == typeof(IAnalysisHistoryRetentionJob));
        jobDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }
}
