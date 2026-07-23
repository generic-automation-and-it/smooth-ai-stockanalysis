# TEST_FRAMEWORK_AGENTS.md

## TL;DR

Shared xunit.v3 test fixtures and helpers reused across the L0/L1/L2 test projects. This is a library (`IsTestProject=false`) — it contains no tests.

## Non-Negotiables

- **Keep it generic and domain-agnostic.** No references to feature code or concrete domain types; fixtures are reusable scaffolding only.
- **No `[Fact]`/`[Theory]` here.** `IsTestProject` is `false`; tests live in the `*.UnitTest` / `*.ComponentTest` / `*.IntegrationTest` projects that reference this one.

## Key Behaviors

- **`WebAppFixture<TProgram>`** wraps `WebApplicationFactory<TProgram>` and gives each integration fixture an isolated on-disk SQLite database. Because xunit.v3 compiles test assemblies as executables (each gets its own auto-generated `Program`), an integration test must reference the Host with `Aliases="HostApp"` and close the fixture as `WebAppFixture<HostApp::Program>` to avoid an ambiguous `Program`.
- **`SqliteTestDatabase`** creates a unique temporary file database with pooling disabled, then deletes the database, WAL, and shared-memory files at disposal. It is the only shared persistence fixture; tests must not require a container runtime.
- **`ServiceProviderFixture`** builds an isolated `IServiceCollection`/`IServiceProvider` for L0/L1 tests and routes logging to the test output via `XUnitLoggerFactory`.
- **`XUnitLogger*`** bridges `ILogger` to xunit's `ITestOutputHelper`, with optional per-category minimum levels.
- **`PriorityOrderer` + `[TestPriority]`** order test cases when sequencing matters; opt in with `[TestCaseOrderer(typeof(PriorityOrderer))]` on the test class.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — lean fixtures (`ServiceProviderFixture`, `WebAppFixture<TProgram>`), xunit output logging, and test-case ordering helpers. | — |
| 2026-07-23 | Replaced container-backed fixtures with isolated SQLite test files. | #6 |
