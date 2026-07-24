# HOST_AGENTS.md

## TL;DR

ASP.NET Core composition root (Minimal API). Wires the application together and exposes endpoints — it holds no business logic.

## Non-Negotiables

- **Keep business logic out of Host.** Endpoints translate HTTP to a Mediator request and back; they contain no domain or orchestration logic.
- **One endpoint per use case** under `Endpoints/`; cross-cutting composition (DI, middleware, observability, problem-details) lives in `Configuration/`.
- **`Program` ends with `public partial class Program { }`** so integration tests can target it via `WebApplicationFactory<Program>`.
- **References Application, Domain, and Infrastructure** — it is the only project that composes all layers.

## Key Behaviors

- The template `Program.cs` is a bare bootstrap (`CreateBuilder → Build → Run`) with no registered endpoints, so any un-routed request returns `404` — this is exactly what the Host integration smoke test asserts. Replace it with real composition (Serilog, OpenAPI/Scalar, health checks, `AddApplication`/`AddInfrastructure`, endpoint mapping) as features land.
- Persistence enters the composition root through `AddInfrastructure(defaultUserUniqueIdentifier)` after Host fail-fast validation of `DefaultUser`. That extension resolves the connection string only when EF creates its DbContext options, so configuration providers composed by `WebApplicationFactory` can select the isolated L2 database without replacing those options. The Host-validated GUID is registered as `DefaultUserSeedOptions` for the startup initializer.
- F-001 verified the solution dependency graph: `SmoothAiStockAnalysis.Domain` has no project references; `SmoothAiStockAnalysis.Application` references Domain; `SmoothAiStockAnalysis.Infrastructure` references Application and Domain to implement application contracts; and this Host references Application, Domain, and Infrastructure. No layer references Host and there are no cycles.

## Data-access scopes

- Host remains the composition root only: `AddInfrastructure()` registers the scoped `IDataAccessScopeSetter` / `IDataAccessScope` / `ISystemDataAccessScope` and the DbContext that applies the global isolation filter.
- Host does not set a user scope itself. Background workers / future pipeline code set the scope deliberately after resolving the DI scope. No HTTP ambient user is assumed in Phase 1.

## Requirements

### Phase-1 default user configuration (T-022 / #66)

- Committed deployment configuration documents section `DefaultUser` with non-secret placeholder identity only (`DefaultUser:UniqueIdentifier`). Override at deploy time with `DefaultUser__UniqueIdentifier` (NFR-044, NFR-080). No credentials belong in this section (NFR-043).
- Host binds and validates the section at startup (same fail-fast pattern as `DeliveryWindow`). Missing, empty, malformed, or `Guid.Empty` values throw before the host builds, and the exception message names `DefaultUser:UniqueIdentifier` (NFR-047). Validated identity is registered for Infrastructure seeding; Host does not write to the database.
- Phase 1 has **no authentication**. The configured GUID is a stable external identifier for the single seeded tenant (LADR-010), not a credential, session token, or authorization claim. Future sign-in adds a front door; it does not replace this seed contract.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — minimal runnable Host (`Program.cs`, `appsettings(.Development).json`, `Properties/launchSettings.json`) with empty `Configuration/`, `Endpoints/`, `HealthChecks/`, `Workers/`. | — |
| 2026-07-23 | Renamed solution/layers to SmoothAiStockAnalysis and verified the inward dependency graph. | #5 |
| 2026-07-24 | Registered Infrastructure without eagerly reading its connection string, preserving L2 configuration overrides. | #252 |
| 2026-07-24 | Documented Host composition of explicit data-access scopes and the global isolation filter (no ambient user). | #62, #63, #64 |
| 2026-07-24 | Documented Phase-1 default-user configuration keys, fail-fast validation, and identity-vs-auth boundary for seed work. | #66, #67, #7 |
