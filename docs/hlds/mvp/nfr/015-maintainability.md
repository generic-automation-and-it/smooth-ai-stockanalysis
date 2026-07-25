# NFR-090 – NFR-095: Maintainability

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-090 | Dependencies point inward; the domain knows nothing of providers, storage or delivery | Structurally enforced | Critical |
| NFR-091 | A feature's request, handling, validation and response live together | One folder per feature | High |
| NFR-092 | Code favours explicit, well-named, domain-close constructs over abstraction | Low indirection | High |
| NFR-093 | Shared abstractions are extracted on the third occurrence, not the first | Deliberate duplication tolerated | Medium |
| NFR-094 | Architectural decisions are recorded with their rejected alternatives | One record per decision | High |
| NFR-095 | Every stage of the funnel is added as a self-contained slice | No cross-cutting edits to add a stage | High |

## Rationale

NFR-092 and NFR-093 read like stylistic preferences and are not. A material share of this codebase will be written by AI coding agents working on one feature at a time, and agents perform markedly better against explicit code than against behaviour assembled from indirection. Deliberate duplication is cheaper here than a shared abstraction that fits neither caller — the usual trade-off, weighted differently because of who is doing the writing.

NFR-095 is what makes the milestone plan viable. Each milestone adds one stage of the funnel; if adding a stage required edits across the codebase, the plan would degrade into a sequence of increasingly risky changes rather than a sequence of additions.

NFR-094 exists because this system contains several decisions whose reasoning is not self-evident from the result — excluding the catalyst day from liquidity measurement, rejecting an in-memory database on a device with limited write endurance, deferring an orchestration platform that fits the problem well. Recorded without their rejected alternatives, each of those invites reversal by someone who never encountered the argument.

## Verification

- Layer dependency rules checked in the build.
- New funnel stages reviewed for confinement to a single slice.

## Related

- `docs/adr/001-clean-architecture-with-vertical-slices.md`
- `docs/adr/` (all records)
- `docs/hlds/mvp/ladrs/019-direct-ilogger-calls-over-loggermessage.md` — CA1848 (`LoggerMessage`) kept at suggestion severity as an NFR-092 interpretation for low-volume logging
