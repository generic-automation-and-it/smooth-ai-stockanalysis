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
    public void RegistersTheRetentionHostedService()
    {
        var services = new ServiceCollection();

        services.AddInfrastructurePersistence("Data Source=:memory:");

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(AnalysisHistoryRetentionHostedService));
    }

    [Fact]
    public void RejectsNegativeRetention()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new AnalysisHistoryRetentionOptions { RetentionMonths = -1 });
    }

    [Fact]
    public void RejectsZeroRetention()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new AnalysisHistoryRetentionOptions { RetentionMonths = 0 });
    }

    [Fact]
    public void RegistersRetentionJobAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddInfrastructurePersistence("Data Source=:memory:");

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IAnalysisHistoryRetentionJob)
            && descriptor.ImplementationType == typeof(AnalysisHistoryRetentionJob)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void ConfigureBindingOverridesDefault()
    {
        var services = new ServiceCollection();
        services.AddInfrastructurePersistence("Data Source=:memory:");
        services.Configure<AnalysisHistoryRetentionOptions>(o => o.RetentionMonths = 6);

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IOptions<AnalysisHistoryRetentionOptions>>()
          .Value.RetentionMonths.ShouldBe(6);
    }
}
