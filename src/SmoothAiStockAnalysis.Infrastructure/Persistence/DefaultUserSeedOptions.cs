namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// Host-validated identity used to idempotently seed the Phase-1 default user at startup.
/// </summary>
/// <remarks>
/// Production Host always supplies a non-empty identifier. Component tests that only exercise
/// persistence primitives may register <see cref="None"/> so migrate runs without seeding.
/// </remarks>
public sealed class DefaultUserSeedOptions
{
    /// <summary>
    /// Gets options that disable default-user seeding (migrate-only startup).
    /// </summary>
    public static DefaultUserSeedOptions None { get; } = new(uniqueIdentifier: null);

    private DefaultUserSeedOptions(Guid? uniqueIdentifier) => UniqueIdentifier = uniqueIdentifier;

    /// <summary>
    /// Initializes seed options with a non-empty external unique identifier.
    /// </summary>
    public DefaultUserSeedOptions(Guid uniqueIdentifier)
    {
        if (uniqueIdentifier == Guid.Empty)
        {
            throw new ArgumentException(
                "A default-user seed unique identifier must not be empty.",
                nameof(uniqueIdentifier));
        }

        UniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// Gets the stable external unique identifier of the configured default user, or
    /// <see langword="null"/> when seeding is disabled.
    /// </summary>
    public Guid? UniqueIdentifier { get; }

    /// <summary>
    /// Gets whether startup should seed a default user after migrations.
    /// </summary>
    public bool IsEnabled => UniqueIdentifier is not null;
}
