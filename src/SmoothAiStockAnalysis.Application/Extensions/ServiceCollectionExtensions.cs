using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Application.Common.Configuration;
using SmoothAiStockAnalysis.Application.Configuration;

namespace SmoothAiStockAnalysis.Application.Extensions;

/// <summary>
/// Registers Application-layer services with the host service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the F-004 settings resolver and the user-metadata port to the service collection.
    /// </summary>
    /// <remarks>
    /// The concrete <see cref="IApplicationDefaults"/> is registered by the Host composition
    /// root, where the <c>IOptions&lt;T&gt;</c> instances live; only the contract is owned here.
    /// The resolver is registered as Scoped so it shares the per-unit-of-work lifetime with the
    /// <c>IUserMetadataProvider</c> and the underlying DbContext.
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddScoped<ISettingsResolver, SettingsResolver>();
}
