namespace SmoothAiStockAnalysis.Application.Common.Persistence;

/// <summary>
/// The deliberate, named system scope for shared ingestion that bypasses user isolation (NFR-042).
/// </summary>
/// <remarks>
/// This is a separate interface from <see cref="IDataAccessScopeSetter"/> so the bypass is a
/// distinct, auditable dependency. Ordinary feature execution never receives it; only shared
/// ingestion takes it. It is the only sanctioned way to read across users — feature code must not
/// use EF Core's <c>IgnoreQueryFilters</c>.
/// </remarks>
public interface ISystemDataAccessScope
{
    /// <summary>Enters the system scope for shared ingestion in this DI scope.</summary>
    void EnterSystemScope();
}
