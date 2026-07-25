# NFR-056 – NFR-062: Resource footprint

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-056 | The complete system operates within the device's memory, including the operating system | 1 GB total | Critical |
| NFR-057 | The system runs as a single process | One service | Critical |
| NFR-058 | No container runtime is required in production or in local development | None | Critical |
| NFR-059 | No message broker, cache server or orchestration server | None | Critical |
| NFR-060 | Storage writes are minimised to preserve the memory card | Incremental, batched per cycle | High |
| NFR-061 | Retained data is pruned so the working set stays bounded | One-month retention | High |
| NFR-062 | Migrating storage to a solid-state device is a configuration change | No redesign | Medium |

## Rationale

**These are constraints, not preferences.** The target device has 1 GB of RAM shared between the operating system and the application, and boots from a memory card with finite write endurance. That hardware is the design authority for the whole system, and NFR-057 through NFR-059 exist because every additional process competes directly with the runtime for memory that is not there.

Three components were considered and rejected on this basis alone: a distributed cache server, a message broker, and a managed workflow orchestration platform. Each was individually reasonable and each would have consumed a meaningful share of the budget.

NFR-060 corrects a plausible mistake. Reducing card wear by holding the database in memory and snapshotting periodically was proposed and rejected — a periodic full snapshot rewrites the entire database each time, whereas incremental journaling writes only changed pages. The intuitive remedy would have increased wear substantially.

## Verification

- Memory footprint observed under sustained operation on the target device, not in development.
- Storage write volume measured over a representative period.

## Related

- `docs/hlds/mvp/ladrs/002-on-disk-sqlite-over-in-memory-snapshots.md`
- `docs/hlds/mvp/ladrs/003-defer-managed-workflow-orchestration.md`
- `docs/hlds/mvp/ladrs/011-memory-only-caching.md`
