using System.Globalization;

namespace SmoothAiStockAnalysis.Host.Configuration;

/// <summary>
/// Deployment configuration for the single Phase-1 default user identity (T-022 / LADR-018).
/// </summary>
/// <remarks>
/// The configured GUID is a stable external identifier, not a credential or authorization token
/// (LADR-010, NFR-037). Credentials never belong in this section (NFR-043/044).
/// </remarks>
public sealed class DefaultUserOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "DefaultUser";

    /// <summary>
    /// Gets the configuration key for the external unique identifier (section-relative).
    /// </summary>
    public const string UniqueIdentifierKey = "UniqueIdentifier";

    /// <summary>
    /// Gets the full configuration path reported on validation failures.
    /// </summary>
    public const string UniqueIdentifierPath = SectionName + ":" + UniqueIdentifierKey;

    /// <summary>
    /// Gets or sets the stable external unique identifier for the default user.
    /// </summary>
    public string UniqueIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Binds and validates the default-user section, failing fast with the configuration key name.
    /// </summary>
    public static DefaultUserOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new DefaultUserOptions();
        configuration.GetSection(SectionName).Bind(options);
        _ = options.GetValidatedUniqueIdentifier();
        return options;
    }

    /// <summary>
    /// Parses and validates <see cref="UniqueIdentifier"/> as a non-empty GUID.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// When the value is missing, empty, malformed, or <see cref="Guid.Empty"/>. The message names
    /// <see cref="UniqueIdentifierPath"/> and never includes secret material.
    /// </exception>
    public Guid GetValidatedUniqueIdentifier()
    {
        if (string.IsNullOrWhiteSpace(UniqueIdentifier))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Configuration value '{0}' is required and must be a non-empty GUID.",
                    UniqueIdentifierPath));
        }

        if (!Guid.TryParse(UniqueIdentifier, out Guid uniqueIdentifier))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Configuration value '{0}' must be a valid GUID.",
                    UniqueIdentifierPath));
        }

        if (uniqueIdentifier == Guid.Empty)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Configuration value '{0}' must not be an empty GUID.",
                    UniqueIdentifierPath));
        }

        return uniqueIdentifier;
    }
}
