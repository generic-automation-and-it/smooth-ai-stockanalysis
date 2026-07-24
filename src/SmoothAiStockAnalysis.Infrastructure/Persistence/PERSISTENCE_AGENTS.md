# PERSISTENCE_AGENTS.md

## TL;DR

Persistence is an Infrastructure-only, on-disk SQLite foundation that batches each future analysis cycle into one transaction.

## Non-Negotiables

- Keep the application database file-backed; do not use in-memory SQLite for the running service.
- A future analysis cycle must call `IAnalysisCycleUnitOfWork` once; repositories in that scope must not independently save or commit.
- Keep SQLite and EF Core types out of Domain and Application, and do not add time conversions here—the time-foundation worktask owns that decision.

## Architecture Decisions

### LADR-002 — On-disk SQLite over in-memory snapshots

**Status:** Accepted. **Context:** the target device has constrained memory and finite storage-write endurance. **Decision:** use one on-disk SQLite database with WAL, relaxed synchronous writes, one transaction per cycle, and mandatory retention. **Consequences:** the operating-system page cache serves hot reads; an in-memory database or independent per-stage commits would undermine the durability and write-volume constraints.

## Key Behaviors

- `SqlitePragmaConnectionInterceptor` applies WAL and `synchronous=NORMAL` whenever a SQLite connection opens. WAL persists with the database; synchronous mode is connection-scoped, so verify it on an open EF connection.
- `SqliteDatabaseInitializer` creates the empty local database at Host startup with `EnsureCreatedAsync`. There is no migration until a feature introduces the first persisted entity.
- `AnalysisHistoryRetentionHostedService` runs the mandatory retention seam daily with a one-calendar-month policy. It is deliberately a no-op until timestamped analysis-history entities arrive in F-003/M3.

## Test References

- **L1:** `Infrastructure.ComponentTest/SqlitePersistenceTests.cs` verifies the real-file connection settings and the transaction commit/rollback boundary. `AppliesProductionPragmasToFreshConnectionWithoutEnsureCreated` proves the PRAGMA invariant is applied by `SqlitePragmaConnectionInterceptor` alone — on a scope that never calls `EnsureCreatedAsync` — so it stays green independently of whichever path creates the database.
- **L2:** `Host.IntegrationTest/SmokeTests.cs` starts the Host against an isolated SQLite file and verifies that the production connection interceptor still applies WAL and `synchronous=NORMAL`.

### L2 fixture override

`HostWebAppFixture.ConfigureTestServices` re-registers
`DbContextOptions<SmoothAiStockAnalysisDbContext>` and reattaches
`SqlitePragmaConnectionInterceptor` because `Program.cs` evaluates
the connection string at builder construction — before the
in-memory configuration override applied via
`WithWebHostBuilder.ConfigureAppConfiguration` runs — so the test
override cannot reach `AddInfrastructurePersistence(connectionString)`.
Do **not** remove this override as redundant; the regression that
removed it failed PR Gate run `30040969698`.

## Quality Constraints

- NFR-034 requires one transaction per analysis cycle. See [NFR-034](../../../docs/hlds/mvp/nfr/006-durability-and-concurrency.md) for the single-transaction boundary.
- NFR-078 and NFR-079 require local operation and tests with no container runtime or external service. See [NFR-078](../../../docs/hlds/mvp/nfr/013-deployability.md) and [NFR-079](../../../docs/hlds/mvp/nfr/011-observability.md).

## Package Notes

- `Microsoft.Data.Sqlite` is the only `Directory.Packages.props` entry referenced exclusively from `tests/SmoothAiStockAnalysis.TestFramework/`. The runtime still pulls it transitively through `Microsoft.EntityFrameworkCore.Sqlite`, but the test framework needs it directly to construct the `SqliteConnectionStringBuilder` used in `SqliteTestDatabase`. Bump and audit together.

## Migration Plans

When the first feature adds a persisted entity, introduce the initial migration under `Migrations/` and mark generated migration classes with `[ExcludeFromCodeCoverage]`. The sibling time-foundation change may extend the DbContext with its separately decided NodaTime converters; do not pre-empt that representation here.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-07-23 | Documented the SQLite durability, transaction, retention, test, and migration conventions. | #6 |
| 2026-07-23 | Restructured the context to the repository AGENTS quality standard. | #252 |
| 2026-07-23 | Documented the L2 SQLite connection-invariant coverage. | #252 |
| 2026-07-23 | Documented why the L2 `DbContextOptions` replacement is required and not redundant. | #252 |
| 2026-07-24 | Fixed Quality Constraints NFR links to `docs/hlds/mvp/nfr/`; documented the fresh-connection PRAGMA test that decouples the invariant from `EnsureCreatedAsync`. | #252 |
