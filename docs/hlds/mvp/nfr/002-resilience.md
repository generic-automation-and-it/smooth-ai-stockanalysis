# NFR-006 – NFR-012: Resilience and fault tolerance

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-006 | Transient provider failures are retried with backoff | Bounded attempts, then fall through | High |
| NFR-007 | Provider rate limits are detected and honoured rather than hammered | No sustained limit breach | High |
| NFR-008 | Where an alternative provider exists for a data category, the system fails over to it | Configurable ordering per category | High |
| NFR-009 | A failed cycle leaves stage state consistent and resumable | No partial-stage corruption | Critical |
| NFR-010 | Malformed or incomplete model output does not fail the cycle | Degrade, record, continue | High |
| NFR-011 | An unavailable provider degrades output rather than halting the system | Publish with what is available, or publish nothing | High |
| NFR-012 | The system recovers unattended from a device restart | No manual intervention | Critical |

## Rationale

The system runs unattended on domestic hardware and a domestic connection, calling half a dozen third-party services on free allowances. Every one of those is an unreliable dependency, and none of them are reliable enough to treat failure as exceptional.

NFR-010 deserves separate mention. The reasoning layer is the only non-deterministic component, and it can return well-formed nonsense as easily as it can return an error. Treating a bad response as a cycle failure would make the most valuable stage the most fragile one.

NFR-011 interacts with BR-33: publishing nothing is always a legitimate outcome, so degradation has a safe floor. The system is never under pressure to produce output it cannot justify.

## Verification

- Failover tested with the primary provider deliberately unavailable.
- Crash resume tested by terminating the process mid-cycle.
- Device restart recovery tested as part of deployment acceptance.

## Related

- `docs/adr/003-defer-managed-workflow-orchestration.md`
- `docs/adr/013-abstracted-ai-reasoning-provider.md`
- BR-43 (provider failover), BR-33 (publish nothing)
