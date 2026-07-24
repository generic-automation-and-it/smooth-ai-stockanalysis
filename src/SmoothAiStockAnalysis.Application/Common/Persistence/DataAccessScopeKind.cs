namespace SmoothAiStockAnalysis.Application.Common.Persistence;

/// <summary>
/// Distinguishes a per-user execution scope from the named system ingestion scope.
/// </summary>
public enum DataAccessScopeKind
{
    /// <summary>A scope bound to one user's tenant key.</summary>
    User,

    /// <summary>The deliberate system scope used for shared ingestion (NFR-042).</summary>
    System,
}
