# Non-Functional Requirements

Quality attributes and operating constraints for **smooth-ai-stockanalysis**.

The business requirements (`docs/brd.md`) state *what* the system must do. These state *how well* it must do it, and under what constraints. The high level design (`docs/wiki/hld.md`) summarises these and links here; this folder is the source of truth.

## Reading these

Each document covers one quality attribute and lists numbered requirements with a target, a rationale, and how the requirement is verified. Requirements are globally numbered so they can be referenced from issues, decision records and design documents.

Where a requirement follows from a recorded decision, the decision record is linked. Where it exists because of a hardware or commercial constraint rather than a preference, that is stated — those are the ones that must not be quietly relaxed.

## Index

| Document | Attribute | IDs |
|---|---|---|
| [001](001-performance.md) | Performance and responsiveness | NFR-001 – NFR-005 |
| [002](002-resilience.md) | Resilience and fault tolerance | NFR-006 – NFR-012 |
| [003](003-caching-and-data-efficiency.md) | Caching and data efficiency | NFR-013 – NFR-018 |
| [004](004-provider-portability.md) | Provider portability | NFR-019 – NFR-023 |
| [005](005-cost-and-quota-control.md) | Cost and quota control | NFR-024 – NFR-030 |
| [006](006-durability-and-concurrency.md) | Durability and concurrency | NFR-031 – NFR-036 |
| [007](007-security-and-data-isolation.md) | Security and data isolation | NFR-037 – NFR-044 |
| [008](008-configurability.md) | Configurability | NFR-045 – NFR-050 |
| [009](009-time-correctness.md) | Time correctness | NFR-051 – NFR-055 |
| [010](010-resource-footprint.md) | Resource footprint | NFR-056 – NFR-062 |
| [011](011-observability.md) | Observability | NFR-063 – NFR-068 |
| [012](012-testability.md) | Testability | NFR-069 – NFR-075 |
| [013](013-deployability.md) | Deployability and operations | NFR-076 – NFR-082 |
| [014](014-documentation-and-openness.md) | Documentation and openness | NFR-083 – NFR-089 |
| [015](015-maintainability.md) | Maintainability | NFR-090 – NFR-095 |

## The three constraints everything else bends around

**1 GB of RAM.** The target device is the design authority. It is why there is one process, one database file, no message broker, no cache server and no orchestration platform. Requirements in [010](010-resource-footprint.md) are not preferences.

**Free data allowances.** Caching lifetimes and stage caps are not optimisations — they are what makes the commercial model work at all. See [003](003-caching-and-data-efficiency.md) and [005](005-cost-and-quota-control.md).

**A public repository with no authentication.** The system runs on a private home network and has no access control, while its source is public. Everything in [007](007-security-and-data-isolation.md) follows from holding those two facts together.

## Related

- `docs/brd.md` — business requirements and delivery milestones
- `docs/wiki/hld.md` — high level design
- `docs/hlds/mvp/ladrs/` — architecture decision records
