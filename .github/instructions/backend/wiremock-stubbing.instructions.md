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
- Keep the well-known CI endpoint at `http://127.0.0.1:19091`; the coverage action pre-warms it once for the ordered test suite.

## Changelog

> AI loading note: Skip this section during routine task execution. Use it only when updating this rule file.

| Date | Change |
|:-----|:-------|
| 2026-07-23 | Restored WireMock-only Aspire orchestration and shared stubbing conventions. |
