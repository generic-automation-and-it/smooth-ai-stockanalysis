using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;
using SmoothAiStockAnalysis.Application.Configuration;
using SmoothAiStockAnalysis.Domain.Time;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// The Host-side composition of <see cref="IApplicationDefaults"/>: the catalogue façade fed
/// by the section <c>IOptions&lt;T&gt;</c> instances. Constructed once at startup.
/// </summary>
public sealed class ApplicationDefaults : IApplicationDefaults
{
    private readonly CycleDefaults _cycle;

    /// <summary>
    /// Composes the façade from the bound section options. Validation that crosses sections
    /// (e.g. DeliveryWindow parsing) lives here so the per-section binders stay simple.
    /// </summary>
    public ApplicationDefaults(
        IOptions<AnalysisDefaultsOptions> analysis,
        IOptions<CostCapsOptions> costCaps,
        IOptions<FxMultipliersOptions> fxMultipliers,
        IOptions<CycleOptions> cycle,
        IOptions<ProviderOptions> provider)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(costCaps);
        ArgumentNullException.ThrowIfNull(fxMultipliers);
        ArgumentNullException.ThrowIfNull(cycle);
        ArgumentNullException.ThrowIfNull(provider);

        Analysis = analysis.Value.ToDefaults();
        CostCaps = costCaps.Value.ToDefaults();
        FxMultipliers = fxMultipliers.Value.ToDefaults();
        _cycle = cycle.Value.ToDefaults();
        Provider = provider.Value.ToDefaults();
    }

    /// <inheritdoc />
    public AnalysisDefaults Analysis { get; }

    /// <inheritdoc />
    public CostCaps CostCaps { get; }

    /// <inheritdoc />
    public FxMultipliers FxMultipliers { get; }

    /// <inheritdoc />
    public CycleDefaults Cycle => _cycle;

    /// <inheritdoc />
    public ProviderDefaults Provider { get; }

    /// <inheritdoc />
    public DeliveryWindow GetDefaultDeliveryWindow() =>
        new(
            _cycle.DeliveryWindowTimeZoneId,
            ParseLocalTime(_cycle.DeliveryWindowStart, nameof(_cycle.DeliveryWindowStart)),
            ParseLocalTime(_cycle.DeliveryWindowEnd, nameof(_cycle.DeliveryWindowEnd)));

    private static readonly LocalTimePattern LocalTimePattern =
        LocalTimePattern.CreateWithInvariantCulture("HH:mm");

    private static LocalTime ParseLocalTime(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        ParseResult<LocalTime> result = LocalTimePattern.Parse(value);
        if (!result.Success)
        {
            throw new ArgumentException(
                $"Delivery window time must use the HH:mm format (parameter '{parameterName}').",
                parameterName,
                result.Exception);
        }

        return result.Value;
    }
}
