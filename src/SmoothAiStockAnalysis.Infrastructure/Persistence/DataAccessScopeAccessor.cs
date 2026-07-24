using SmoothAiStockAnalysis.Application.Common.Persistence;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence;

/// <summary>
/// Scoped holder for the explicit <see cref="DataAccessScope"/> (LADR-017).
/// </summary>
/// <remarks>
/// Registered with a scoped lifetime so one holder serves the DbContext and every repository in
/// the same unit of work. Background execution sets the scope deliberately through
/// <see cref="IDataAccessScopeSetter"/> (or <see cref="ISystemDataAccessScope"/> for shared
/// ingestion) before querying; nothing supplies a user implicitly (LADR-010, NFR-041).
/// </remarks>
internal sealed class DataAccessScopeAccessor : IDataAccessScope, IDataAccessScopeSetter, ISystemDataAccessScope
{
    private DataAccessScope? _current;

    /// <summary>
    /// Gets the current scope. Throws when no scope has been set so an owned-data query fails
    /// closed instead of silently applying to nobody (NFR-041).
    /// </summary>
    public DataAccessScope Current =>
        _current ?? throw new InvalidOperationException(
            "No data-access scope is set. Background execution must set an explicit user or system scope before querying.");

    /// <inheritdoc />
    public void SetScope(DataAccessScope scope) => _current = scope;

    /// <inheritdoc />
    public void EnterSystemScope() => _current = DataAccessScope.System();
}
