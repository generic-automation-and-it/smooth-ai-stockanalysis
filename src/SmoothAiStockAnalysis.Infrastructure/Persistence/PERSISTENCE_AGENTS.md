# PERSISTENCE_AGENTS.md

## TL;DR

Persistence uses one on-disk SQLite database file. It is an Infrastructure concern; Application uses the `IAnalysisCycleUnitOfWork` port and never references EF Core or SQLite.

## Durability and transactions

- `SqlitePragmaConnectionInterceptor` configures every opened connection with WAL journaling and `synchronous=NORMAL`, implementing LADR-002. WAL is persistent per database; synchronous mode is connection-scoped, so assert it on an open provider connection.
- `AnalysisCycleUnitOfWork` begins one transaction, executes the supplied cycle writes, calls `SaveChangesAsync` once, then commits. Repositories in that cycle must share the scoped `DbContext` and must not save independently (NFR-034).
- `SqliteDatabaseInitializer` uses `EnsureCreatedAsync` because M1 has no entities or migration. Add migrations under `Migrations/` when a feature first introduces a persisted entity; generated migrations require `[ExcludeFromCodeCoverage]`.

## Retention

`AnalysisHistoryRetentionHostedService` is registered with a one-calendar-month policy and runs the retention-job seam daily. It intentionally performs no deletion until F-003/M3 introduces timestamped analysis-history entities. Do not add date/time conversion here; the time-foundation worktask owns that choice.

## Tests

L1 and L2 use `SqliteTestDatabase`, which creates a unique temporary on-disk file per fixture and deletes the database, WAL, and shared-memory files on disposal. Tests require no container runtime or external service.

## Related decisions

- `docs/hlds/mvp/ladrs/002-on-disk-sqlite-over-in-memory-snapshots.md`
- `docs/hlds/mvp/nfr/006-durability-and-concurrency.md`
- `docs/hlds/mvp/nfr/013-deployability.md`
