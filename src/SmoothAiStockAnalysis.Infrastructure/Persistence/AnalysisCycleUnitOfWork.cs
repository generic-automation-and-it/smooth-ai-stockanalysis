using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Application.Common.Persistence;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

internal sealed class AnalysisCycleUnitOfWork(SmoothAiStockAnalysisDbContext dbContext) : IAnalysisCycleUnitOfWork
{
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> writeCycle,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writeCycle);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await writeCycle(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
