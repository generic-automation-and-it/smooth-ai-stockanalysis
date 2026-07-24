using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Application.Common.Configuration;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Domain.Documents;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.Configuration;

/// <summary>
/// Infrastructure implementation of the user-metadata port. Loads the persisted
/// <see cref="UserRecord"/> through the current <see cref="IDataAccessScope"/> and returns the
/// Domain <see cref="UserMetadata"/>.
/// </summary>
/// <remarks>
/// The caller is responsible for setting the user scope (LADR-010, NFR-041); a missing scope
/// makes the underlying query fail closed rather than return a default user.
/// </remarks>
internal sealed class UserMetadataProvider(SmoothAiStockAnalysisDbContext dbContext) : IUserMetadataProvider
{
    /// <inheritdoc />
    public async Task<UserMetadata> GetForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            userId,
            nameof(userId),
            "A user identifier must be positive.");

        UserMetadataDocument metadata = await dbContext.Users()
            .Where(user => user.Id == userId)
            .Select(user => user.Metadata)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No user is visible to the current data-access scope (id '{userId}').");

        return metadata.ToDomain();
    }
}
