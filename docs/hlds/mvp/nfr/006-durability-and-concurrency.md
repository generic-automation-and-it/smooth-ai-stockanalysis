# NFR-031 – NFR-036: Durability and concurrency

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-031 | Cycles never overlap | Enforced by a persisted run lock | Critical |
| NFR-032 | A cycle interrupted by crash or restart resumes from its last completed stage | No full restart | Critical |
| NFR-033 | Each stage is idempotent — re-running it produces the same result | No duplicate side effects | Critical |
| NFR-034 | Writes are batched into one transaction per cycle | Minimises storage write volume | High |
| NFR-035 | Exactly one instance operates against the database at a time | Single instance | Critical |
| NFR-036 | Loss of up to one cycle of data is acceptable; loss of event identity history is not | Differentiated durability | Medium |

## Rationale

These six requirements are what replace a workflow orchestration platform. Skip-if-running, resume-after-crash and per-stage progress are the properties that would have justified one; all three fall out of a run lock plus persisted stage state — infrastructure already required for event de-duplication and analysis history, and therefore free.

NFR-033 is the discipline that keeps that substitution viable. Idempotent stages mean resumption is safe, re-runs are harmless, and adopting an orchestration platform later becomes a substitution rather than a rewrite.

NFR-036 draws a deliberate line. Full persistence safety was declared non-critical — losing a cycle's analysis costs thirty minutes. But losing the record of which events were already analysed would cause the system to re-analyse and re-alert on old news, which damages trust rather than merely wasting a cycle.

## Verification

- Process terminated mid-cycle; resumption asserted from the correct stage.
- Concurrent trigger attempted during a running cycle; skip asserted.
- Stage re-run asserted to produce no duplicate records or duplicate notifications.

## Related

- `docs/hlds/mvp/ladrs/003-defer-managed-workflow-orchestration.md`
- `docs/hlds/mvp/ladrs/002-on-disk-sqlite-over-in-memory-snapshots.md`
- BR-39 (skip if previous incomplete)
