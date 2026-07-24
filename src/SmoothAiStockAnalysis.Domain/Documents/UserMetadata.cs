namespace SmoothAiStockAnalysis.Domain.Documents;

/// <summary>
/// Versioned user metadata whose fields can evolve without changing its persistence column.
/// </summary>
public sealed class UserMetadata : IVersionedDocument
{
    /// <summary>
    /// The schema version assigned to newly created metadata.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    private UserMetadata(int schemaVersion)
    {
        SchemaVersion = schemaVersion;
    }

    /// <inheritdoc />
    public int SchemaVersion { get; }

    /// <summary>
    /// Creates metadata at the current schema version.
    /// </summary>
    public static UserMetadata Create() => new(CurrentSchemaVersion);

    /// <summary>
    /// Reconstitutes metadata with a positive persisted schema version.
    /// </summary>
    public static UserMetadata Reconstitute(int schemaVersion)
    {
        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(schemaVersion),
                schemaVersion,
                "A metadata schema version must be positive.");
        }

        return new UserMetadata(schemaVersion);
    }
}
