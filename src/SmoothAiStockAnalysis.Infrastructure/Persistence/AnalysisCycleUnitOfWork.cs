using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmoothAiStockAnalysis.Application.Common.Persistence;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// Commits all writes produced by one analysis cycle as a single SQLite transaction.
/// The work is executed through EF Core's <see cref="IExecutionStrategy"/> so the
/// provider can attach retry or resilience policies without changing this seam;
/// the SQLite provider supplies a non-retrying strategy.
/// </summary>
internal sealed class AnalysisCycleUnitOfWork(
    SmoothAiStockAnalysisDbContext dbContext,
    ILogger<AnalysisCycleUnitOfWork> logger) : IAnalysisCycleUnitOfWork
{
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> writeCycle,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writeCycle);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await writeCycle(cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackEx)
                {
                    logger.LogError(
                        rollbackEx,
                        "Rollback failed after an analysis-cycle write error; surfacing the original exception.");
                }

                dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }
}
