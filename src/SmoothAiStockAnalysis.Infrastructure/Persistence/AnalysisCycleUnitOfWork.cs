using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Application.Common.Persistence;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// Commits all writes produced by one analysis cycle as a single SQLite transaction,
/// routed through EF Core's <see cref="IExecutionStrategy"/> so retries and provider
/// resilience policies compose correctly.
/// </summary>
internal sealed class AnalysisCycleUnitOfWork(SmoothAiStockAnalysisDbContext dbContext) : IAnalysisCycleUnitOfWork
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
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
