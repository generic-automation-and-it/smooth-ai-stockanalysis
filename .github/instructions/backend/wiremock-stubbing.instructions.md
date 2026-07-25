---
description: 'WireMock test orchestration and stubbing conventions for the Aspire AppHost and shared fixtures'
globs: "tests/SmoothAiStockAnalysis.TestFramework.Aspire/**/*.cs,tests/SmoothAiStockAnalysis.TestFramework/Fixtures/*WireMock*.cs,tests/SmoothAiStockAnalysis.TestFramework/Fixtures/AspireFixture.cs"
paths:
  - "tests/SmoothAiStockAnalysis.TestFramework.Aspire/**/*.cs"
  - "tests/SmoothAiStockAnalysis.TestFramework/Fixtures/*WireMock*.cs"
  - "tests/SmoothAiStockAnalysis.TestFramework/Fixtures/AspireFixture.cs"
applyTo: 'tests/SmoothAiStockAnalysis.TestFramework.Aspire/**/*.cs,tests/SmoothAiStockAnalysis.TestFramework/Fixtures/*WireMock*.cs,tests/SmoothAiStockAnalysis.TestFramework/Fixtures/AspireFixture.cs'
alwaysApply: false
---
# WireMock Test Rules

## Non-Negotiables

- Aspire owns the test WireMock process. Keep `SmoothAiStockAnalysis.TestFramework.Aspire` WireMock-only unless a future requirement explicitly adds another external dependency.
- Do not add PostgreSQL, Redis, Npgsql, or Respawn to the Aspire test host; persistence tests use isolated SQLite files.
- Keep `WireMockAdminClient` as the shared admin API adapter. Tests must not duplicate raw `__admin` request construction.
- A test that changes mappings or request history must reset the WireMock instance before installing its own stubs.
- Keep the well-known CI endpoint at `http://127.0.0.1:19091`; CI pre-warms it for the **integration** level only (`run-level.sh integration`). Unit and component levels must not require it.

> **HLD-12 context.** The dev AppHost previously provisioned its own WireMock container for local end-to-end work; that responsibility was removed in favour of the shared Aspire test dependency described here. If a future requirement reintroduces a dev-AppHost WireMock, align the orchestration with this rule set before merging.

## Changelog

> AI loading note: Skip this section during routine task execution. Use it only when updating this rule file.

| Date | Change |
|:-----|:-------|
| 2026-07-23 | Restored WireMock-only Aspire orchestration and shared stubbing conventions. |
