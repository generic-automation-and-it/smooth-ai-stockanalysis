# ASPIRE_AGENTS.md

## TL;DR

Aspire test AppHost that provisions WireMock for external-API tests. Persistence remains isolated SQLite and is never hosted here.

## Non-Negotiables

- Keep this AppHost WireMock-only unless a future requirement explicitly adds another external test dependency.
- Do not add PostgreSQL, Redis, Npgsql, or Respawn; database tests use the SQLite fixtures in `SmoothAiStockAnalysis.TestFramework`.
- Keep WireMock on the well-known local port `19091` so CI can pre-warm it once for all ordered test projects.

## Key Behaviors

- `WireMockTestDependency` is the public resource-name, port, and default-URL contract shared with the reusable test fixture.
- `DistributedApplicationBuilderExtensions.AddWireMockTestDependency` declares the `wiremock/wiremock` container from that shared contract.
- CI starts this AppHost only for the **integration** level (`run-level.sh integration`), waits for `http://127.0.0.1:19091/__admin/health`, runs Host integration tests, and terminates the AppHost. Unit and component levels never start it (LADR-020).
- Tests can opt into `AspireFixture` when they need the WireMock endpoint or admin client; tests without external HTTP dependencies remain container-free.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-07-23 | Restored Aspire as a WireMock-only test dependency host. | #252 |
| 2026-07-24 | Centralized the WireMock resource contract for downstream fixtures. | #252 |
| 2026-07-25 | CI pre-warm limited to the integration level; unit/component stay container-free. | #83 / WT-10-02 |
