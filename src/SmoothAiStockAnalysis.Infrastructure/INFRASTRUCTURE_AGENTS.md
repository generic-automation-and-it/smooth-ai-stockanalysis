# INFRASTRUCTURE_AGENTS.md

## TL;DR

Implements the contracts defined in Application — EF Core + SQLite persistence (`Persistence/`) and external HTTP clients (`Clients/`).

## Non-Negotiables

- **Implements Application interfaces; never the reverse.** Concrete stores/clients here implement `IFoo` from Application. Application must not reference an Infrastructure concrete type.
- **References Application and Domain only** — never Host.
- **EF Core migrations are generated code.** Keep them under `Persistence/Migrations/`; they are marked generated via the root `.editorconfig` glob and generated migration classes should carry `[ExcludeFromCodeCoverage]`. Register the `DbContext` with a scoped lifetime.
- **No business rules.** Infrastructure adapts to the outside world (DB, HTTP, cache); domain decisions stay in Domain, orchestration in Application.

## Key Behaviors

- Persistence is file-backed SQLite. The Host supplies the connection string; the scoped `DbContext` and SQLite-only configuration remain in Infrastructure. See `Persistence/PERSISTENCE_AGENTS.md` for the durability, transaction, retention, and testing conventions.
- The local database is created at Host startup. Each opened SQLite connection applies WAL plus `NORMAL` synchronous writes; the latter is connection-scoped and is verified on an active EF connection.
- Infrastructure implements Application's `IAnalysisCycleUnitOfWork` port. Future cycle orchestration must call it once and let it commit all writes as one transaction.
- `AnalysisHistoryRetentionHostedService` is registered as the mandatory one-month retention shell. It intentionally performs no deletion until timestamped analysis-history entities are introduced; the time foundation owns the date/time representation needed by that future prune operation.
- `SmoothAiStockAnalysisDbContext.ConfigureConventions` globally maps NodaTime persistence values using the lossless SQLite `TEXT` contract in LADR-014. The mapping stays Infrastructure-only; named-zone business rules stay in Domain.
- Versioned structured documents persist as JSON `TEXT` via `Persistence/Converters/VersionedDocumentSqliteValueConverter<TDocument>` (LADR-015), applied per property rather than as a global convention. Prefer it over EF Core's native `.ToJson()` for evolving documents; see `Persistence/PERSISTENCE_AGENTS.md`.

## Packages to add when implementing

`Microsoft.EntityFrameworkCore(.Relational/.Design/.Tools/.Sqlite)`, `NodaTime`, `Refit.HttpClientFactory`, `Microsoft.Extensions.Http.Resilience` — declared centrally in `Directory.Packages.props`.

> **Note on `Microsoft.Extensions.*` references.** Infrastructure declares its own `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Hosting`, and `Microsoft.Extensions.Options` `PackageReference` entries even though the Host re-exports the same surfaces transitively. Infrastructure owns its own DI composition surface; it must not depend on the Host to surface those abstractions, because that would invert the dependency graph.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — empty persistence + clients skeleton (`Clients/`, `Extensions/`, `Persistence/{Configurations,Entities,Migrations,Repositories,Stores,Extensions,DesignTime}/`). | — |
| 2026-07-23 | Added SQLite persistence foundation: connection pragmas, cycle transaction seam, startup initialization, and retention shell. | #6 |
| 2026-07-23 | Recorded the retention shell's boundary with the sibling time foundation. | #6 |
| 2026-07-24 | Registered global, lossless NodaTime SQLite conversions in the persistence context. | #6 |
| 2026-07-24 | Added the LADR-015 per-property JSON-document value converter for versioned structured documents. | #59 |
