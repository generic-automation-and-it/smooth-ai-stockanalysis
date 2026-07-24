using SmoothAiStockAnalysis.Domain.Documents;

namespace SmoothAiStockAnalysis.Application.Common.Configuration;

/// <summary>
/// Loads the versioned metadata document for one user.
/// </summary>
/// <remarks>
/// Defined in Application so the resolver depends on an interface rather than an
/// Infrastructure concrete. Implementations must respect <see cref="Common.Persistence.IDataAccessScope"/>
/// (LADR-010, NFR-041): the scope must already be set on the DI scope before this port is
/// called, and a missing scope must fail-closed rather than return a default user.
/// </remarks>
public interface IUserMetadataProvider
{
    /// <summary>
    /// Returns the metadata for the user identified by <paramref name="userId"/>.
    /// </summary>
    Task<UserMetadata> GetForUserAsync(long userId, CancellationToken cancellationToken = default);
}
