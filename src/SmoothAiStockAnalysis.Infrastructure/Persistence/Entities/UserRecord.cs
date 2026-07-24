using SmoothAiStockAnalysis.Domain.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core persistence representation of the user tenant root.
/// </summary>
internal sealed class UserRecord
{
    private UserRecord()
    {
    }

    private UserRecord(long id, Guid uniqueIdentifier, UserMetadataDocument metadata)
    {
        Id = id;
        UniqueIdentifier = uniqueIdentifier;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the compact internal database identity.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Gets the stable externally exposable identifier.
    /// </summary>
    public Guid UniqueIdentifier { get; private set; }

    /// <summary>
    /// Gets the versioned persistence metadata document.
    /// </summary>
    public UserMetadataDocument Metadata { get; private set; } = null!;

    /// <summary>
    /// Creates a persistence record from a Domain user.
    /// </summary>
    public static UserRecord FromDomain(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id != 0)
        {
            throw new ArgumentException(
                "A new persistence record can only be created from a transient user.",
                nameof(user));
        }

        return new UserRecord(
            user.Id,
            user.UniqueIdentifier,
            UserMetadataDocument.FromDomain(user.Metadata));
    }

    /// <summary>
    /// Reconstitutes the Domain user represented by this persisted record.
    /// </summary>
    public User ToDomain() =>
        User.Reconstitute(Id, UniqueIdentifier, Metadata.ToDomain());

    /// <summary>
    /// Applies mutable Domain state while retaining forward-compatible persistence fields.
    /// </summary>
    public void ApplyDomainState(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (Id != user.Id)
        {
            throw new InvalidOperationException("Cannot apply state from a user with a different internal identity.");
        }

        if (UniqueIdentifier != user.UniqueIdentifier)
        {
            throw new InvalidOperationException("Cannot change a user's external unique identifier.");
        }

        Metadata.ApplyDomainState(user.Metadata);
    }
}
