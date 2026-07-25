# ARCHITECTURE_AGENTS.md

## TL;DR

L0 NetArchTest suite that structurally enforces inward layer dependencies (NFR-090 / LADR-001). It is part of the **unit** level and must never require I/O or WireMock.

## Non-Negotiables

- Keep this project in the **unit** level (`run-level.sh unit`). Do not move it to component/integration.
- Reference **all four** layer assemblies so a Domain → Infrastructure edge is loadable and detectable.
- Failure messages must name the **rule** and the **offending types/assemblies** — never a bare "expected true".
- Do not treat a green run as proof of every architecture rule in the prose docs. Only the assertions listed under Key Behaviors are mechanical.

## System Context

NFR-090 requires layer dependency rules to be checked in the build. LADR-001 states clean-architecture dependencies point inward. This project is that check. It runs beside the other `*.UnitTest` projects in the PR gate's **Unit tests** step.

The permitted "depends on" edges are Host → {Application, Infrastructure, Domain}, Infrastructure → {Application, Domain}, and Application → Domain. No diagram is included: the sanctioned types are C4Context (external integrations), sequence (multi-step flows with side effects), and ER (entity relationships), and a layer-dependency graph is none of those.

## Architecture Decisions

### LADR-020 — Per-level test execution and architecture gate

**Status:** Accepted. This project is NFR-090's verification clause, delivered as an L0 `NetArchTest.Rules` suite inside the unit level rather than a build-time analyzer or a prose rule. See [LADR-020](../../docs/hlds/mvp/ladrs/020-per-level-test-execution-and-architecture-gate.md).

### Assembly allow-list over `OnlyHaveDependenciesOn` for Domain purity

**Status:** Accepted. **Date:** 2026-07-25. **Context:** "Domain's only external dependency is NodaTime" needs a mechanical check. NetArchTest's `OnlyHaveDependenciesOn` operates on the *type* graph, where BCL facades (`System.Runtime`, `System.Private.CoreLib`) surface inconsistently and make the assertion brittle across SDK versions. **Decision:** assert on `DomainAssembly.GetReferencedAssemblies()` — the package graph — filtering `System.*` and allow-listing NodaTime. **Rejected alternative:** `OnlyHaveDependenciesOn` with a facade allow-list, which is the idiomatic NetArchTest call but fails or passes for reasons unrelated to the invariant. **Consequences:** the check matches how root `AGENTS.md` states the rule (packages, not types), and a forbidden *package* is caught even when no type from it is referenced. It will not catch a forbidden type reached through an already-allowed assembly — the other four tests cover that.

## Key Behaviors

### Enforced

| Rule | Mechanism |
|---|---|
| Domain ↛ Application, Infrastructure, Host | NetArchTest `ShouldNot().HaveDependencyOnAny(...)` |
| Domain's only non-BCL external assembly is NodaTime | Assembly reference allow-list on `DomainAssembly.GetReferencedAssemblies()` |
| Application ↛ Infrastructure, Host | NetArchTest |
| Infrastructure ↛ Host | NetArchTest |
| Host ↛ EF Core assemblies (no direct `DbContext` usage) | `NetArchTest.Rules.Types.InAssembly(HostAssembly).ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")` (NetArchTest's `NamespaceTree` matches by namespace segment, so this also covers `Microsoft.EntityFrameworkCore.Relational` and `.Sqlite`) |

### Not mechanically enforced

Treat these as review responsibilities, not gate coverage. The backlog for closing them is under Migration Plans.

- Vertical-slice folder shape inside Application (`Features/<Name>/`).
- "No business rules in Infrastructure" beyond assembly edges.
- Host endpoints must go through Mediator.
- Package versions / central package management hygiene.
- Generator-emitted sub-namespaces that re-export types from a forbidden layer (`HaveDependencyOnAny` matches by namespace prefix, not by type; see `LayerBoundaryTests.cs` comments).

## Test References

- L0: `tests/SmoothAiStockAnalysis.Architecture.UnitTest/LayerBoundaryTests.cs`

## Quality Constraints

- Analyzer-clean under `TreatWarningsAsErrors=true` (PascalCase test method names; no CA1707 underscores).
- Excluded from product coverage instrumentation via the narrowed Include filter in `common.sh`.

## Migration Plans

Each entry under "Not mechanically enforced" is a candidate gate, not a permanent gap. Before promoting one, weigh it against NFR-092 — a brittle reflection assertion that fails for the wrong reason is worse than an honest gap.

- **Mediator-only endpoints** — blocked on Host having any endpoints at all. Add the assertion in the same PR as the first endpoint, while the shape is fresh.
- **Vertical-slice folder shape** — would need reflection over Mediator handler types to map handler → folder. Revisit once `Features/` holds enough slices for the convention to be worth machine-checking.
- **Central package management hygiene** — better served by a build-time check on `Directory.Packages.props` than by a runtime test; out of this project's scope.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-07-25 | Created — NetArchTest layer boundary suite for NFR-090 | #83 / WT-10-02 |
| 2026-07-25 | ai-review: added Architecture Decisions + Migration Plans, dropped the unsanctioned `flowchart` diagram, trimmed the dead Domain allow-list entries | #83 / PR #269 |
