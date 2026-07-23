namespace SmoothAiStockAnalysis.Application.Common.Persistence;

/// <summary>
/// Commits all writes produced by one analysis cycle as a single database transaction.
/// </summary>
/// <remarks>
/// A future analysis pipeline calls this once per cycle. Repositories resolved in the same
/// scope share the DbContext and must not call SaveChanges themselves.
/// </remarks>
public interface IAnalysisCycleUnitOfWork
{
    /// <summary>
    /// Executes and commits the cycle's writes as one transaction.
    /// </summary>
    Task ExecuteAsync(
        Func<CancellationToken, Task> writeCycle,
        CancellationToken cancellationToken = default);
}
