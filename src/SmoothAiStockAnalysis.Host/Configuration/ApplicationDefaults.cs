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
    private static readonly LocalTimePattern LocalTimePattern =
        LocalTimePattern.CreateWithInvariantCulture("HH:mm");

    private readonly CycleDefaults _cycle;
    private readonly DeliveryWindow _defaultDeliveryWindow;

    /// <summary>
    /// Composes the façade from the bound section options. Cross-section validation (delivery
    /// window parse and TZDB lookup) runs here so a bad deploy config fails when the catalogue
    /// is composed, not mid-cycle (NFR-047).
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
        _defaultDeliveryWindow = CreateDefaultDeliveryWindow(_cycle);
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
    public DeliveryWindow GetDefaultDeliveryWindow() => _defaultDeliveryWindow;

    internal static DeliveryWindow CreateDefaultDeliveryWindow(CycleDefaults cycle)
    {
        ArgumentNullException.ThrowIfNull(cycle);

        LocalTime start = ParseLocalTime(cycle.DeliveryWindowStart, CycleOptions.DeliveryWindowStartPath);
        LocalTime end = ParseLocalTime(cycle.DeliveryWindowEnd, CycleOptions.DeliveryWindowEndPath);

        try
        {
            return new DeliveryWindow(cycle.DeliveryWindowTimeZoneId, start, end);
        }
        catch (ArgumentException exception) when (exception is not ArgumentNullException)
        {
            throw new InvalidOperationException(
                $"Configuration values '{CycleOptions.DeliveryWindowTimeZoneIdPath}', '{CycleOptions.DeliveryWindowStartPath}', and '{CycleOptions.DeliveryWindowEndPath}' must form a valid delivery window.",
                exception);
        }
    }

    private static LocalTime ParseLocalTime(string value, string configurationPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{configurationPath}' is required and must use the HH:mm format.");
        }

        ParseResult<LocalTime> result = LocalTimePattern.Parse(value);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Configuration value '{configurationPath}' must use the HH:mm format.",
                result.Exception);
        }

        return result.Value;
    }
}
