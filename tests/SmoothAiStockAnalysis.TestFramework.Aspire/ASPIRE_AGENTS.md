# ASPIRE_AGENTS.md

## TL;DR

Aspire test AppHost that provisions WireMock for external-API tests. Persistence remains isolated SQLite and is never hosted here.

## Non-Negotiables

- Keep this AppHost WireMock-only unless a future requirement explicitly adds another external test dependency.
- Do not add PostgreSQL, Redis, Npgsql, or Respawn; database tests use the SQLite fixtures in `SmoothAiStockAnalysis.TestFramework`.
- Keep WireMock on the well-known local port `19091` so CI can pre-warm it once for all ordered test projects.

## Key Behaviors

- `DistributedApplicationBuilderExtensions.AddWireMockTestDependency` declares the `wiremock/wiremock` container.
- The CI coverage action starts this AppHost, waits for `http://127.0.0.1:19091/__admin/health`, runs the tests, and terminates the AppHost.
- Tests can opt into `AspireFixture` when they need the WireMock endpoint or admin client; tests without external HTTP dependencies remain container-free.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-07-23 | Restored Aspire as a WireMock-only test dependency host. | #252 |
