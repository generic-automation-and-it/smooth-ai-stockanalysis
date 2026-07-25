# CI/CD

The pipeline is a single PR gate that checks whitespace, builds with analyzers, and tests every change before it can merge to `main`.

## PR Gate

- **Workflow:** `.github/workflows/pr-gate.yml`
- **Agent context:** [`.github/CI_AGENTS.md`](../../.github/CI_AGENTS.md)
- **Triggers:** `pull_request` → `main` (including PR branch updates), `push` → `main`, and manual `workflow_dispatch`.
- **Path filters (push + pull_request):** `Directory.Packages.props`, `Directory.Build.props`, `.editorconfig`, `.config/dotnet-tools.json`, `*.slnx`, `src/**`, `tests/**`, `.github/actions/**`, `.github/workflows/pr-gate.yml`. Docs-only PRs skip the gate until WT-10-04.

### Steps

1. **Checkout** — `actions/checkout@v4`.
2. **Install .NET SDK** — `actions/setup-dotnet@v4` (version from the `DOTNET_VERSION` env, currently `10.0.x`).
3. **Restore** — `dotnet restore`.
4. **Format** — `dotnet format whitespace smooth-ai-stockanalysis.slnx --verify-no-changes --no-restore`. Fails the job on whitespace drift only. The `whitespace` subcommand is deliberate: bare `dotnet format` also runs the style and analyzer passes, and because Format precedes Build it would report analyzer violations as formatting failures.
5. **Build** — `dotnet build --no-restore --configuration Release`. SDK analyzers and code-style enforcement are enabled via `Directory.Build.props` (`EnableNETAnalyzers`, `AnalysisLevel=10.0-recommended`, `EnforceCodeStyleInBuild`) with `TreatWarningsAsErrors=true`, so every CA and IDE diagnostic fails here. Explicit CA severities live in `.editorconfig`; `AnalysisLevel` is pinned rather than `latest-*` so an SDK bump cannot silently change the enforced set.
6. **Aspire test with coverage** — local action `.github/actions/test-with-coverage`:
   - Starts the WireMock-only Aspire AppHost, waits for `http://127.0.0.1:19091/__admin/health`, and stops the AppHost during action teardown.
   - Requires a container runtime for WireMock only. Infrastructure component and Host integration tests allocate isolated local SQLite files; Application component tests use the EF Core in-memory provider.
   - Restores .NET tools (`dotnet tool restore`) before executing the test suite.
   - Prepares `artifacts/testresults/` and `artifacts/coverage/`.
   - Runs test projects in order: Host integration → Application/Infrastructure component → Domain/Application/Infrastructure/Host unit tests.
   - Generates coverage reports with `dotnet tool run reportgenerator`.
7. **Publish coverage summary** (`if: always()`) — appends `artifacts/coverage/SummaryGithub.md` to the GitHub step summary.
8. **Upload coverage artifacts** (`if: always()`) — uploads `artifacts/coverage/` as `coverage-report`.

## Local equivalents

```bash
dotnet restore
dotnet format whitespace smooth-ai-stockanalysis.slnx --verify-no-changes --no-restore
dotnet build smooth-ai-stockanalysis.slnx -c Release --no-restore
dotnet test  smooth-ai-stockanalysis.slnx
```

## .NET local tools

`.config/dotnet-tools.json` declares the local tool manifest, restored in CI (and locally) with `dotnet tool restore`:

| Tool | Version | Command |
|---|---|---|
| `dotnet-reportgenerator-globaltool` | `5.4.4` | `reportgenerator` |
| `dotnet-ef` | `10.0.8` | `dotnet-ef` |

`dotnet-ef` is pinned to the EF Core runtime version (`Directory.Packages.props`) so the migrations CLI never drifts from the `Microsoft.EntityFrameworkCore.*` packages. Bump both together.
