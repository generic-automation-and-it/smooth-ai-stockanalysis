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
- Persistence currently enters the composition root through `AddInfrastructure()`. That extension resolves the connection string only when EF creates its DbContext options, so configuration providers composed by `WebApplicationFactory` can select the isolated L2 database without replacing those options.
- F-001 verified the solution dependency graph: `SmoothAiStockAnalysis.Domain` has no project references; `SmoothAiStockAnalysis.Application` references Domain; `SmoothAiStockAnalysis.Infrastructure` references Application and Domain to implement application contracts; and this Host references Application, Domain, and Infrastructure. No layer references Host and there are no cycles.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — minimal runnable Host (`Program.cs`, `appsettings(.Development).json`, `Properties/launchSettings.json`) with empty `Configuration/`, `Endpoints/`, `HealthChecks/`, `Workers/`. | — |
| 2026-07-23 | Renamed solution/layers to SmoothAiStockAnalysis and verified the inward dependency graph. | #5 |
| 2026-07-24 | Registered Infrastructure without eagerly reading its connection string, preserving L2 configuration overrides. | #252 |
