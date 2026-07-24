# LADR-017: Explicit data-access scopes and a global user-isolation query filter

**Status:** Accepted
**Date:** July 2026

## Context

NFR-039–042 (and BR-47/48, backlog T-018–T-020 under #7) require user-owned data to be isolated at the data-access layer rather than per feature, shared reference data to stay unfiltered, background execution to set an explicit user scope with no ambient user, and a deliberate named system scope for shared ingestion. LADR-010 already established that there is no request to infer a user from: everything runs in a background worker on a timer.

Two technical tensions shaped the design:

1. **Fail-closed vs. silently unscoped.** NFR-041's rationale states a filter that silently applies to nobody is worse than no filter, because it looks like protection. A missing scope must make owned-data queries fail loudly, never return all rows and never return an empty-but-valid-looking result.
2. **EF Core query-filter parameterization.** A filter that reads a *computed* property on a captured scope holder is parameterized by EF Core and can cache the first-seen value per context type, leaking user A's tenant key into user B's queries. The filter must read a *field-backed* member so EF inlines it as a constant per context instance.

## Decision

Define an explicit scope contract in Application (`Common/Persistence/`) and implement it in Infrastructure:

- `DataAccessScope` (`readonly record struct`) with `Kind` (`User` | `System`) and a validated `UserId`. `DataAccessScope.ForUser(id)` rejects non-positive ids; `DataAccessScope.System()` names the ingestion scope.
- `IDataAccessScopeSetter.SetScope(scope)` is the deliberate, feature-facing entry point. `IDataAccessScope.Current` exposes the resolved scope. `ISystemDataAccessScope` is a **separate interface** for the system bypass so ordinary feature execution cannot reach it without explicitly taking the dependency.
- Infrastructure's `DataAccessScopeAccessor` (scoped DI lifetime) implements all three. It stores the scope in a settable field; the EF filter reads a getter (`UserIsolationTenantKey`) on the context instance that resolves through a private `CurrentScope` getter which throws `InvalidOperationException` when no scope is set. EF Core re-evaluates that member on the current context per query, so the tenant key is inlined as a per-context constant and two sequential scopes in one process each see their own key. The system scope resolves `UserIsolationTenantKey` to `null`, which the filter recognizes as the deliberate short-circuit (NFR-040 / BR-48).
- Filters are applied globally in `OnModelCreating`: the tenant root `UserRecord` filters on `Id == CurrentUserId`; every entity marked user-owned (via the `ConfigureUserOwnedDependent` helper, which now also stamps an `IsUserOwned` model annotation) filters on `UserId == CurrentUserId`. Shared reference entities carry no annotation and no filter, so they remain queryable in every scope (NFR-040 / BR-48).

## Alternatives considered

**`IgnoreQueryFilters` in feature code.** Prohibited: it is an un-auditable bypass scattered across features. The system scope is the only sanctioned bypass and is a named, greppable interface.

**AsyncLocal / thread-local ambient scope.** Rejected: it is the ambient-user assumption LADR-010 forbids. An ambient scope, by definition, is the very thing the explicit `IDataAccessScopeSetter` was introduced to prevent.

**Computed-property filter (parameterized).** Rejected: the EF Core first-value cache can apply one user's key to another user's query.

**Fail-open or return-empty on missing scope.** Rejected: both silently look like protection (NFR-041 rationale).

## Consequences

- A query made under user A cannot return user B's rows even when a feature author forgets a user predicate.
- Owned-data queries with no valid scope throw; they cannot expose rows.
- The system scope is distinct from a user scope and is the only way to read across users, and it is unavailable by accident.
- Two sequential scopes in one process resolve to their own tenant keys (covered by a regression test).
- Scope-setting orchestration stays out of Domain; Host remains the composition root; the scoped DbContext / `IAnalysisCycleUnitOfWork` transaction model is unchanged.
