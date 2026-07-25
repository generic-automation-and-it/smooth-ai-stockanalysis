# CI/CD

The pipeline is a single PR gate that checks whitespace, builds with analyzers, and runs each test level as its own named step before merge to `main`.

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
6. **Unit tests** — `bash .github/actions/test-with-coverage/run-level.sh unit`. Domain/Application/Infrastructure/Host unit tests plus `Architecture.UnitTest` (NFR-090). **No WireMock.** Projects in the level run in parallel; failures accumulate then fail the step.
7. **Component tests** — `run-level.sh component`. Application (EF in-memory) and Infrastructure (isolated SQLite) component projects. **No WireMock** unless a future test opts into Aspire. Runs even if Unit failed (after a successful Build) so the check stays visible.
8. **Integration tests** — `run-level.sh integration`. Starts the WireMock-only Aspire AppHost, waits for `http://127.0.0.1:19091/__admin/health`, runs `Host.IntegrationTest`, tears down Aspire (SIGTERM → SIGKILL → `docker rm -f wiremock`).
9. **Merge coverage** — `merge-coverage.sh` runs `reportgenerator` over `artifacts/testresults/**/coverage.cobertura.xml` into `artifacts/coverage/`. Include filter targets the four product-assembly name prefixes (Domain, Application, Infrastructure, Host); test/architecture projects must not dilute coverage.
10. **Publish coverage summary** (`if: always()`) — appends `artifacts/coverage/SummaryGithub.md` to the GitHub step summary.
11. **Upload artifacts** (`if: always()`) — `test-results-unit`, `test-results-component`, `test-results-integration`, and `coverage-report`.

## Local equivalents

```bash
dotnet restore
dotnet format whitespace smooth-ai-stockanalysis.slnx --verify-no-changes --no-restore
dotnet build smooth-ai-stockanalysis.slnx -c Release --no-restore

bash .github/actions/test-with-coverage/run-level.sh unit
bash .github/actions/test-with-coverage/run-level.sh component
bash .github/actions/test-with-coverage/run-level.sh integration
bash .github/actions/test-with-coverage/merge-coverage.sh
```

## .NET local tools

`.config/dotnet-tools.json` declares the local tool manifest, restored in CI (and locally) with `dotnet tool restore`:

| Tool | Version | Command |
|---|---|---|
| `dotnet-reportgenerator-globaltool` | `5.4.4` | `reportgenerator` |
| `dotnet-ef` | `10.0.8` | `dotnet-ef` |

`dotnet-ef` is pinned to the EF Core runtime version (`Directory.Packages.props`) so the migrations CLI never drifts from the `Microsoft.EntityFrameworkCore.*` packages. Bump both together.
