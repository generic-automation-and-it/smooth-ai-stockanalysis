# LADR-020: Per-level test execution and architecture gate

**Status:** Accepted
**Date:** July 2026

## Context

NFR-069 requires three test levels (unit, component, integration) that are distinguishable and separately runnable. NFR-090 requires layer dependency rules to be checked in the build. Before this decision the PR gate ran every test project inside one composite step that started Aspire/WireMock first — so the cheapest level paid for a container runtime — and layer rules existed only as prose.

## Decision

1. **Per-level scripts** under `.github/actions/test-with-coverage/` (`run-level.sh unit|component|integration`, `merge-coverage.sh`, shared `common.sh`). The same scripts are the local and CI entry points.
2. **One PR-gate job, three named test steps** (Unit → Component → Integration) plus a Merge coverage step. Later levels still run when an earlier level fails (`if: always() && !cancelled()` after a successful Build) so the checks stay distinguishable.
3. **WireMock/Aspire starts only for the integration level.** Unit never touches it. Component stays container-free until a test opts into `AspireCollection`.
4. **Parallel-within-level** project execution (catalogue pattern), safe because `SqliteTestDatabase` uses a Guid path per process.
5. **L0 `Architecture.UnitTest`** project with `NetArchTest.Rules` 1.3.2 enforces inward layer edges and Domain's NodaTime-only external package rule.
6. **Coverage Include** narrowed to the four product assemblies so test/architecture projects are not instrumented as product code.

## Alternatives considered

**xunit traits / `--filter` categories.** Rejected: requires annotating every test and adds indirection when projects already map 1:1 onto levels (NFR-092).

**Solution filters (`.slnf`) alone.** Rejected: useful for IDE selection but do not own Aspire lifecycle, fail-accumulation, coverage merge, or CI step naming.

**Three GitHub Actions jobs.** Rejected for this worktask: extra setup cost, harder shared coverage merge, and conflicts with the single-job stance retained from WT-10-01. Revisit only if wall-clock demands it.

**Keep integration-first order with WireMock always on.** Rejected: contradicts freeing L0 from containers (NFR-069/074 intent).

**Skip parallel-within-level.** Rejected after confirming SQLite isolation is per-process unique; parallel matches the proven catalogue pattern. On 2-core runners the win may be modest; correctness does not depend on it.

## Consequences

- Developers run one level with one documented command; L0 works with `DOCKER_HOST=unix:///nonexistent`.
- PR checks expose Unit / Component / Integration as separate step names and upload per-level test-result artifacts.
- A Domain → Infrastructure reference fails the unit level with a rule-named message.
- WT-10-03/04 continue to extend the same job; they must not collapse the three test steps back into one.
