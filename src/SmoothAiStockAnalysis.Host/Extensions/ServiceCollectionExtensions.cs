using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Application.Configuration;
using SmoothAiStockAnalysis.Host.Configuration;

namespace SmoothAiStockAnalysis.Host.Extensions;

/// <summary>
/// Host composition extensions for the F-004 settings catalogue and two-layer resolver.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds the F-004 settings catalogue sections and registers the <see cref="IApplicationDefaults"/>
    /// façade. The composition root should call this once after <c>DefaultUser</c> validation.
    /// </summary>
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AnalysisDefaultsOptions>(configuration.GetSection(AnalysisDefaultsOptions.SectionName));
        services.Configure<CostCapsOptions>(configuration.GetSection(CostCapsOptions.SectionName));
        services.Configure<FxMultipliersOptions>(configuration.GetSection(FxMultipliersOptions.SectionName));
        services.Configure<CycleOptions>(configuration.GetSection(CycleOptions.SectionName));
        services.Configure<ProviderOptions>(configuration.GetSection(ProviderOptions.SectionName));

        services.AddSingleton<IApplicationDefaults, ApplicationDefaults>();

        return services;
    }
}
