namespace SmoothAiStockAnalysis.Application.Common.Persistence;

/// <summary>
/// Reads the data-access scope currently in effect for this DI scope.
/// </summary>
public interface IDataAccessScope
{
    /// <summary>Gets the current scope.</summary>
    DataAccessScope Current { get; }
}
