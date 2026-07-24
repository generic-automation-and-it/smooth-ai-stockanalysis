# LADR-018: Startup default-user seed from deployment configuration

**Status:** Completed
**Date:** July 2026

## Context

Phase 1 serves exactly one user and has no authentication (LADR-010, NFR-037). Worktasks under #7 delivered the dual-identifier user model, `user_record` schema, migrations, ownership helpers, and global isolation with explicit scopes (LADR-017). The remaining gap was creating the configured tenant at deploy time without a user-management API (backlog T-022) and closing isolation evidence on the production startup path (T-023).

Constraints:

- Invalid configuration must fail at startup, not mid-cycle (NFR-047).
- Committed configuration may document shape with placeholders only; credentials never land in the repository (NFR-043/044).
- There is no ambient user; startup and background work must set scope deliberately (NFR-041/042).
- Seed must be idempotent across restarts so unattended devices do not duplicate the tenant (NFR-077).

## Decision

1. **Host validates** section `DefaultUser` with key `UniqueIdentifier` (non-empty GUID) at process start, in a fail-fast style analogous to `DeliveryWindow` (also validated at process start). The exception message names `DefaultUser:UniqueIdentifier` and does not echo invalid payload values.
2. **Committed placeholder** `00000000-0000-4000-8000-000000000001` documents shape; deploy overrides via `DefaultUser__UniqueIdentifier`.
3. **Infrastructure seeds** after `MigrateAsync` inside `SqliteDatabaseInitializer`, under `ISystemDataAccessScope`. Lookup/insert is keyed by `unique_identifier`; present → no-op; absent → `User.Create` / `UserRecord.FromDomain` with metadata schema version 1.
4. **Component-test compositions** may disable seed by omitting the identifier (`DefaultUserSeedOptions.None`) so migrate-only tests stay focused.

The configured GUID remains an external identity only — not a credential, session, or authorization claim. Future authentication adds a front door; it does not replace this seed contract.

## Alternatives considered

**Seed from a hard-coded constant in code.** Rejected: defeats deploy-time configurability (NFR-046/080) and makes multi-environment identity awkward.

**Separate hosted service ordered after migrate.** Rejected for Phase 1: ASP.NET hosted-service order is registration-sensitive; keep migrate+seed in one initializer to avoid ordering fragility.

**Seed under a user scope.** Rejected: the tenant does not exist yet, and startup must not invent an ambient user (LADR-010).

## Consequences

- A clean deployment migrates, validates, and creates one configured user; a second start does not duplicate it.
- Misconfiguration fails before any analysis cycle runs.
- Future background features will resolve the seeded internal `Id` and call `IDataAccessScopeSetter.SetScope(DataAccessScope.ForUser(id))` explicitly — there is no ambient user lookup.
- Story #7 closes with L0 config validation, L1 seed idempotence + isolation, L2 Host startup against isolated SQLite, and the existing shared-data inverse L1 proof.
