using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Domain.Entities;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// Applies local database migrations and idempotently seeds the configured default user at startup.
/// </summary>
internal sealed class SqliteDatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<DefaultUserSeedOptions> defaultUserSeedOptions,
    ILogger<SqliteDatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying local SQLite database migrations.");

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Local SQLite database migrations applied.");

        DefaultUserSeedOptions seedOptions = defaultUserSeedOptions.Value;
        if (!seedOptions.IsEnabled)
        {
            logger.LogDebug("Default-user seed is disabled for this composition.");
            return;
        }

        Guid uniqueIdentifier = seedOptions.UniqueIdentifier
            ?? throw new InvalidOperationException("Default-user seed is enabled without a unique identifier.");

        // Seed under the named system scope — startup has no ambient user (LADR-010 / NFR-041).
        scope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
        await EnsureDefaultUserAsync(dbContext, uniqueIdentifier, logger, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Ensures the configured default user exists exactly once (idempotent on unique identifier).
    /// </summary>
    internal static async Task EnsureDefaultUserAsync(
        SmoothAiStockAnalysisDbContext dbContext,
        Guid uniqueIdentifier,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Users()
            .AnyAsync(user => user.UniqueIdentifier == uniqueIdentifier, cancellationToken);

        if (exists)
        {
            logger.LogDebug("Default user already present; seed skipped.");
            return;
        }

        logger.LogInformation("Seeding the configured default user.");
        dbContext.Users().Add(UserRecord.FromDomain(User.Create(uniqueIdentifier)));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Configured default user seed completed.");
        }
        catch (DbUpdateException ex)
        {
            // Concurrent startups can race past the existence check; accept the unique-index
            // conflict only when the configured identity is already present. Clearing the
            // change tracker discards the failed insert so it cannot resurface on save; the
            // re-query below hits the database directly and never consults the tracker
            // (AnyAsync translates to SQL EXISTS, so hygiene only — not a correctness requirement).
            dbContext.ChangeTracker.Clear();
            bool existsAfterConflict = await dbContext.Users()
                .AnyAsync(user => user.UniqueIdentifier == uniqueIdentifier, cancellationToken);
            if (!existsAfterConflict)
            {
                throw new InvalidOperationException(
                    $"Default-user seed failed for '{uniqueIdentifier}' (DefaultUser:UniqueIdentifier) " +
                    "without a concurrent winner; the database rejected the insert but no matching row exists. " +
                    "Inspect the inner provider exception for the underlying constraint or storage failure.",
                    ex);
            }

            logger.LogDebug("Default user already present; seed skipped.");
        }
    }
}
