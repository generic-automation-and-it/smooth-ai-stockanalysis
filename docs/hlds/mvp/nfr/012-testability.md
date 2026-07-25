# NFR-069 – NFR-075: Testability

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-069 | Three test levels: isolated unit, feature component, and integration | Distinguishable and separately runnable | High |
| NFR-070 | Domain calculations are pure and deterministic | No hidden state, no clock, no network | Critical |
| NFR-071 | Indicator calculations are validated against known reference series | Within stated tolerance | Critical |
| NFR-072 | Provider adapters are tested against recorded responses | No live calls in the suite | Critical |
| NFR-073 | The test suite consumes no provider allowance and passes when markets are closed | Zero external dependency | Critical |
| NFR-074 | Integration tests require no database or orchestration container; persistence is local files | Local storage only | High |
| NFR-075 | The non-deterministic reasoning stage is tested for contract compliance, not for output content | Shape, not substance | High |

## Rationale

NFR-071 follows directly from the decision to compute indicators internally rather than purchase them. Correctness became ours, so these calculations carry the heaviest test weight in the codebase — a subtly wrong moving average produces plausible recommendations that are quietly wrong, which is the worst available failure mode.

NFR-072 and NFR-073 exist for a reason specific to this domain. A suite that calls live providers would consume the same free allowance the product depends on, and would fail every weekend when markets are closed — training everyone to ignore red builds.

NFR-074 was written while replacing the template's PostgreSQL server with SQLite (backlog F-002, T-006/T-007/T-013), and its original wording — "integration tests run without a container runtime" — meant *no database or orchestration container*. Its Target column has always said so. The wording was overtaken by the later decision to keep an Aspire-managed WireMock host as the sole external test dependency, which is a stub for provider HTTP calls, not storage, and which NFR-072/073 actively require something to fill.

The requirement is therefore scoped to what it was always about. Persistence is local files at every level, and no test needs a container to reach a database. WireMock is the one permitted test container, it is started only by tests that opt into `AspireFixture`, and a level whose tests do not opt in must run with no container runtime present — which is what makes the suite usable on a developer machine without Docker and keeps the gate honest about what it actually depends on.

NFR-075 draws the only line available around the reasoning stage. Its output cannot be asserted for content, because the same input may legitimately produce different reasoning. What can be asserted is that the output satisfies its contract — confidence present, both cases present, risks present, horizon within bounds — and that malformed output degrades rather than failing the cycle.

## Verification

- Test levels runnable independently in the build (`run-level.sh unit|component|integration`, LADR-020).
- Suite executes successfully with no network access.
- Every level runs green with no container runtime present, until a test opts into `AspireFixture`.

## Related

- `docs/hlds/mvp/ladrs/004-compute-technical-indicators-internally.md`
- `docs/hlds/mvp/ladrs/013-abstracted-ai-reasoning-provider.md`
- `docs/hlds/mvp/ladrs/020-per-level-test-execution-and-architecture-gate.md` — per-level execution and the WireMock-on-demand rule
