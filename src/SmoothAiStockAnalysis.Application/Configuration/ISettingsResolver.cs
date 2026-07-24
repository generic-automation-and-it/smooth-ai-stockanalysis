namespace SmoothAiStockAnalysis.Application.Configuration;

/// <summary>
/// Resolves effective per-user settings from application defaults and user-metadata overrides
/// (NFR-045, HLD §7.2). This is the only sanctioned way for feature code to read effective
/// settings — ad-hoc <c>if (pref)</c> branching is prohibited.
/// </summary>
public interface ISettingsResolver
{
    /// <summary>
    /// Returns the effective settings for the user identified by <paramref name="userId"/>.
    /// </summary>
    /// <param name="userId">The user's internal tenant key.</param>
    /// <param name="cancellationToken">Token observed across the metadata load.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// When <paramref name="userId"/> is not positive. The resolver does not invent an ambient
    /// user (LADR-010, NFR-041).
    /// </exception>
    Task<EffectiveSettings> ResolveForUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the effective settings from already-loaded metadata. Pure: no I/O, no DI.
    /// Exposed so feature code that has the metadata in hand (or tests) can reuse the merge
    /// without re-querying.
    /// </summary>
    EffectiveSettings Resolve(IApplicationDefaults defaults, Domain.Documents.UserMetadata metadata);
}
