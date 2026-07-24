using System.Text.Json;
using System.Text.Json.Serialization;
using SmoothAiStockAnalysis.Domain.Documents;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

/// <summary>
/// Infrastructure representation of a user's versioned metadata document.
/// </summary>
/// <remarks>
/// Serialization and forward-compatible field retention remain at the persistence boundary;
/// the corresponding Domain document contains no JSON concerns.
/// </remarks>
internal sealed class UserMetadataDocument : IVersionedDocument
{
    /// <summary>
    /// Gets or sets the serialized document contract version.
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets fields written by a newer document contract.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ForwardCompatibleFields { get; set; }

    /// <summary>
    /// Creates the persistence representation of Domain metadata.
    /// </summary>
    public static UserMetadataDocument FromDomain(UserMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return new UserMetadataDocument { SchemaVersion = metadata.SchemaVersion };
    }

    /// <summary>
    /// Creates Domain metadata from this persistence document.
    /// </summary>
    public UserMetadata ToDomain() => UserMetadata.Reconstitute(SchemaVersion);

    /// <summary>
    /// Applies understood Domain state without discarding unknown persisted fields.
    /// </summary>
    public void ApplyDomainState(UserMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (metadata.SchemaVersion < SchemaVersion)
        {
            throw new InvalidOperationException(
                "A persisted metadata document cannot be downgraded to an earlier schema version.");
        }

        SchemaVersion = metadata.SchemaVersion;
    }
}
