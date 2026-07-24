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
    /// <remarks>
    /// Catalogue sections are bound, range-validated, and composed eagerly so invalid deploy
    /// configuration fails during Host composition (NFR-047), matching the <c>DefaultUser</c>
    /// fail-fast style rather than waiting for the first settings resolve mid-cycle. The Host
    /// registers only the validated <see cref="IApplicationDefaults"/> singleton — it does not
    /// also expose unvalidated <c>IOptions&lt;T&gt;</c> section bindings, so there is a single
    /// validated configuration path.
    /// </remarks>
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AnalysisDefaultsOptions analysis = AnalysisDefaultsOptions.FromConfiguration(configuration);
        CostCapsOptions costCaps = CostCapsOptions.FromConfiguration(configuration);
        FxMultipliersOptions fxMultipliers = FxMultipliersOptions.FromConfiguration(configuration);
        CycleOptions cycle = CycleOptions.FromConfiguration(configuration);
        ProviderOptions provider = ProviderOptions.FromConfiguration(configuration);

        // Eager composition validates the default delivery window (zone + HH:mm) after each
        // section's own FromConfiguration range checks have already run.
        var applicationDefaults = new ApplicationDefaults(
            analysis,
            costCaps,
            fxMultipliers,
            cycle,
            provider);

        services.AddSingleton<IApplicationDefaults>(applicationDefaults);

        return services;
    }
}
