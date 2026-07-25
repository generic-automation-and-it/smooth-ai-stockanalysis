# LADR-020: Per-level test execution and architecture gate

**Status:** Accepted
**Date:** July 2026

## Context

NFR-069 requires three test levels (unit, component, integration) that are distinguishable and separately runnable. NFR-090 requires layer dependency rules to be checked in the build. Before this decision the PR gate ran every test project inside one composite step that started Aspire/WireMock first — so the cheapest level paid for a container runtime — and layer rules existed only as prose.

## Decision

1. **Per-level scripts** under `.github/actions/test-with-coverage/` (`run-level.sh unit|component|integration`, `merge-coverage.sh`, shared `common.sh`). The same scripts are the local and CI entry points.
2. **One PR-gate job, three named test steps** (Unit → Component → Integration) plus a Merge coverage step. Later levels still run when an earlier level fails (`if: !cancelled() && steps.build.outcome == 'success'`) so the checks stay distinguishable.
3. **No level starts WireMock by default.** `AspireFixture` probes the well-known endpoint and starts its own AppHost when nothing answers, so pre-warming is an optimisation, never a requirement. `run-level.sh integration` pre-warms only when `PREWARM_WIREMOCK=1`; CI leaves it unset while no integration test opts into `AspireCollection`. All three levels therefore run with no container runtime present (NFR-074).
4. **Parallel-within-level** project execution (catalogue pattern), safe because `SqliteTestDatabase` uses a Guid path per process. Measured on the unit level: **9.3 s sequential → 3.5 s parallel (~2.6×)**, and the sequential baseline excluded coverage collection, so the real margin is wider.
5. **L0 `Architecture.UnitTest`** project with `NetArchTest.Rules` 1.3.2 enforces inward layer edges (NetArchTest) and Domain's NodaTime-only external rule (assembly-reference allow-list on `DomainAssembly.GetReferencedAssemblies()`).
6. **Coverage Include** narrowed to the four product assemblies so test/architecture projects are not instrumented as product code.

## Alternatives considered

**xunit traits / `--filter` categories.** Rejected: requires annotating every test and adds indirection when projects already map 1:1 onto levels (NFR-092).

**Solution filters (`.slnf`) alone.** Rejected: useful for IDE selection but do not own Aspire lifecycle, fail-accumulation, coverage merge, or CI step naming.

**Three GitHub Actions jobs.** Rejected for this worktask: extra setup cost, harder shared coverage merge, and conflicts with the single-job stance retained from WT-10-01. Revisit only if wall-clock demands it.

**Keep integration-first order with WireMock always on.** Rejected: contradicts freeing L0 from containers (NFR-069/074 intent).

**Pre-warm WireMock unconditionally for the integration level.** Rejected: no integration test opts into `AspireCollection`, so this provisions a container for zero consumers and makes L2 fail outright on a machine without Docker — the same objection that removed WireMock from L0, applied one level down. Because `AspireFixture` probes before starting, the pre-warm buys latency and not capability, so making it opt-in has no silent failure mode: forget the flag and the fixture starts WireMock itself, slightly slower.

**Skip parallel-within-level.** Rejected after confirming SQLite isolation is per-process unique; parallel matches the proven catalogue pattern and measured ~2.6× on the unit level.

## Consequences

- Developers run one level with one documented command; **all three** pass with `DOCKER_HOST=unix:///nonexistent` today.
- When the first WireMock-consuming test lands it works with no CI change; setting `PREWARM_WIREMOCK=1` on the Integration step then becomes a speed optimisation to weigh, not a correctness fix.
- PR checks expose Unit / Component / Integration as separate step names and upload per-level test-result artifacts.
- A Domain → Infrastructure reference fails the unit level with a rule-named message.
- WT-10-03/04 continue to extend the same job; they must not collapse the three test steps back into one.
