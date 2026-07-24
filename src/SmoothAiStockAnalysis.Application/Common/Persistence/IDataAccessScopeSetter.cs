namespace SmoothAiStockAnalysis.Application.Common.Persistence;

/// <summary>
/// Sets the explicit data-access scope for the current unit of work.
/// </summary>
/// <remarks>
/// Background execution (the analysis pipeline, ingestion, retention) sets the scope deliberately
/// before resolving repositories or a DbContext. Nothing supplies a user implicitly.
/// </remarks>
public interface IDataAccessScopeSetter
{
    /// <summary>Sets the scope that governs subsequent data access in this DI scope.</summary>
    void SetScope(DataAccessScope scope);
}
