# ASPIRE_AGENTS.md

## TL;DR

Aspire test AppHost that provisions WireMock for external-API tests. Persistence remains isolated SQLite and is never hosted here.

## Non-Negotiables

- Keep this AppHost WireMock-only unless a future requirement explicitly adds another external test dependency.
- Do not add PostgreSQL, Redis, Npgsql, or Respawn; database tests use the SQLite fixtures in `SmoothAiStockAnalysis.TestFramework`.
- Keep WireMock on the well-known local port `19091`. That address is the contract `AspireFixture` probes before starting its own host, and the one an optional CI pre-warm binds.

## Key Behaviors

- `WireMockTestDependency` is the public resource-name, port, and default-URL contract shared with the reusable test fixture.
- `DistributedApplicationBuilderExtensions.AddWireMockTestDependency` declares the `wiremock/wiremock` container from that shared contract.
- CI does **not** start this AppHost by default. `run-level.sh integration` pre-warms it only when `PREWARM_WIREMOCK=1`, which CI leaves unset while no test opts into `AspireCollection` (LADR-020). Pre-warming waits for `http://127.0.0.1:19091/__admin/health` and terminates the AppHost afterwards; skipping it is safe because `AspireFixture` probes that endpoint and starts its own AppHost when nothing answers.
- Tests can opt into `AspireFixture` when they need the WireMock endpoint or admin client; tests without external HTTP dependencies remain container-free.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-07-23 | Restored Aspire as a WireMock-only test dependency host. | #252 |
| 2026-07-24 | Centralized the WireMock resource contract for downstream fixtures. | #252 |
| 2026-07-25 | CI pre-warm limited to the integration level; unit/component stay container-free. | #83 / WT-10-02 |
| 2026-07-25 | Pre-warm made opt-in (`PREWARM_WIREMOCK`); no level requires a container runtime until a test opts into `AspireCollection`. | #83 / WT-10-02 |
