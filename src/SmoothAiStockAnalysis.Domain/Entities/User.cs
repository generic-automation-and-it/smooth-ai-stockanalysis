using SmoothAiStockAnalysis.Domain.Documents;

namespace SmoothAiStockAnalysis.Domain.Entities;

/// <summary>
/// Identifies a user whose owned data is isolated from other users.
/// </summary>
public sealed class User
{
    private User(long id, Guid uniqueIdentifier, UserMetadata metadata)
    {
        Id = id;
        UniqueIdentifier = uniqueIdentifier;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the database-assigned identifier. A value of zero represents a transient user.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Gets the stable identifier that may cross the persistence boundary.
    /// </summary>
    public Guid UniqueIdentifier { get; }

    /// <summary>
    /// Gets the user's versioned metadata.
    /// </summary>
    public UserMetadata Metadata { get; }

    /// <summary>
    /// Creates a transient user with metadata at the current schema version.
    /// </summary>
    public static User Create(Guid uniqueIdentifier)
    {
        ThrowIfEmpty(uniqueIdentifier);
        return new User(0, uniqueIdentifier, UserMetadata.Create());
    }

    /// <summary>
    /// Reconstitutes a persisted user.
    /// </summary>
    public static User Reconstitute(long id, Guid uniqueIdentifier, UserMetadata metadata)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "A persisted user identifier must be positive.");
        }

        ThrowIfEmpty(uniqueIdentifier);
        ArgumentNullException.ThrowIfNull(metadata);

        return new User(id, uniqueIdentifier, metadata);
    }

    private static void ThrowIfEmpty(Guid uniqueIdentifier)
    {
        if (uniqueIdentifier == Guid.Empty)
        {
            throw new ArgumentException("A user unique identifier must not be empty.", nameof(uniqueIdentifier));
        }
    }
}
