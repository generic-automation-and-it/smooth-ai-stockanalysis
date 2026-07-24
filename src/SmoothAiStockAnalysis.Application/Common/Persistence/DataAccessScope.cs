namespace SmoothAiStockAnalysis.Application.Common.Persistence;

/// <summary>
/// The explicit, deliberately-set execution scope for a unit of data access.
/// </summary>
/// <remarks>
/// There is no ambient user (LADR-010): background work sets this scope explicitly through
/// <see cref="IDataAccessScopeSetter"/> before touching user-owned data. The scope is a value so
/// it can be validated at creation and passed across the Application/Infrastructure boundary
/// without either layer depending on the other.
/// </remarks>
public readonly record struct DataAccessScope
{
    private readonly long? _userId;

    private DataAccessScope(DataAccessScopeKind kind, long? userId)
    {
        Kind = kind;
        _userId = userId;
    }

    /// <summary>Gets whether this is a <see cref="DataAccessScopeKind.User"/> or <see cref="DataAccessScopeKind.System"/> scope.</summary>
    public DataAccessScopeKind Kind { get; }

    /// <summary>
    /// Gets the tenant key for a user scope. Throws when accessed on a non-user scope so a
    /// system scope is never silently treated as "user 0".
    /// </summary>
    public long UserId =>
        Kind == DataAccessScopeKind.User && _userId is { } userId
            ? userId
            : throw new InvalidOperationException("A system scope has no user tenant key.");

    /// <summary>Creates a scope bound to one user's tenant key.</summary>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="userId"/> is not positive.</exception>
    public static DataAccessScope ForUser(long userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), userId, "A user scope requires a positive tenant key.");
        }

        return new DataAccessScope(DataAccessScopeKind.User, userId);
    }

    /// <summary>Creates the named system scope used for shared ingestion (NFR-042).</summary>
    public static DataAccessScope System() => new(DataAccessScopeKind.System, null);
}
