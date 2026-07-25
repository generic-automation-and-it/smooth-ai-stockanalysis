# CI/CD

The pipeline is a single PR gate that checks whitespace, builds with analyzers, and runs each test level as its own named step before merge to `main`. No level needs a container runtime today.

## PR Gate

- **Workflow:** `.github/workflows/pr-gate.yml`
- **Agent context:** [`.github/CI_AGENTS.md`](../../.github/CI_AGENTS.md)
- **Triggers:** `pull_request` → `main` (including PR branch updates), `push` → `main`, and manual `workflow_dispatch`.
- **No path filters.** The gate runs on every pull request, whatever it touches — including documentation-only and `.agents/` / `.context/` changes. Two reasons: the job (`build-and-test`) is a required status check, and a path-filtered workflow never creates its check run, so the requirement would stay unreported and the PR unmergeable; and the Secret scan step must see every changed file (NFR-043). The filters were inherited from the reference catalogue port and removed in WT-10-04.
  Verified after that change landed: this documentation-only pull request — which touches no path the
  old filter matched — runs the full gate. Before the change, PR #272 (`.agents/**` only) had no
  `build-and-test` check on it at all.

### Required status checks

The default branch carries an active ruleset: squash-merge only, one approving review, review threads must be resolved, and three required checks:

| Check | Produced by |
|---|---|
| `build-and-test` | `pr-gate.yml` (this workflow) |
| `review / open-code-review-report` | `pipeline-code-review-report.yml` |
| `CodeQL` | GitHub code-scanning default setup |

Any workflow producing a required check must therefore run unconditionally on `pull_request`. Adding a `paths:` filter to one of them silently blocks merges rather than skipping work.

### Steps

1. **Checkout** — `actions/checkout@v4`.
2. **Install .NET SDK** — `actions/setup-dotnet@v4` (version from the `DOTNET_VERSION` env, currently `10.0.x`).
3. **Restore** — `dotnet restore`.
4. **Format** — `dotnet format whitespace smooth-ai-stockanalysis.slnx --verify-no-changes --no-restore`. Fails the job on whitespace drift only. The `whitespace` subcommand is deliberate: bare `dotnet format` also runs the style and analyzer passes, and because Format precedes Build it would report analyzer violations as formatting failures.
5. **Build** — `dotnet build --no-restore --configuration Release`. SDK analyzers and code-style enforcement are enabled via `Directory.Build.props` (`EnableNETAnalyzers`, `AnalysisLevel=10.0-recommended`, `EnforceCodeStyleInBuild`) with `TreatWarningsAsErrors=true`, so every CA and IDE diagnostic fails here. Explicit CA severities live in `.editorconfig`; `AnalysisLevel` is pinned rather than `latest-*` so an SDK bump cannot silently change the enforced set.
6. **Secret scan** — `bash .github/actions/secret-scan/scan.sh` (gitleaks 8.27.2, pinned by SHA-256). Runs after Build so a credential fails fast before the test levels spend their time; honours the same Build gate as the test levels. Scans the **PR commit range** (`base..head` for `pull_request`, `before..after` for `push`, full history for `workflow_dispatch`) using `fetch-depth: 0`; a key committed and removed within the same PR is still caught. Since WT-10-04 removed the path filters, the range covers **every** changed file — `docs/**`, `.agents/**` and `.context/**` included. The allowlist lives in `.gitleaks.toml` (every entry commented); the upstream gitleaks default config is fetched at the same pinned tag and checksum-verified at runtime (see `.github/CI_AGENTS.md` "Secret scanning" for the ruleset-resolution gotcha and the remaining blind spots — B, the path filter, is closed; A, pre-merge-base history, and C, allowlist suppression, are not). The step needs no secret, so fork PRs are scanned identically.
7. **Unit tests** — `bash .github/actions/test-with-coverage/run-level.sh unit`. Domain/Application/Infrastructure/Host unit tests plus `Architecture.UnitTest` — the NetArchTest suite that mechanically enforces inward layer dependencies (Domain depends on nothing outward and on no package but NodaTime; Application not on Infrastructure/Host; Infrastructure not on Host; Host not on EF Core), NFR-090's verification clause. See [`ARCHITECTURE_AGENTS.md`](../../tests/SmoothAiStockAnalysis.Architecture.UnitTest/ARCHITECTURE_AGENTS.md). **No WireMock.** Projects in the level run in parallel; failures accumulate then fail the step.
8. **Component tests** — `run-level.sh component`. Application (EF in-memory) and Infrastructure (isolated SQLite) component projects. **No WireMock** unless a future test opts into Aspire. Runs even if Unit failed (after a successful Build) so the check stays visible.
9. **Integration tests** — `run-level.sh integration`. Runs `Host.IntegrationTest` against isolated SQLite. **No container runtime**: no current test opts into `AspireCollection`, so WireMock is not started. With `PREWARM_WIREMOCK=1` the step starts the WireMock-only Aspire AppHost first, waits for `http://127.0.0.1:19091/__admin/health`, and tears it down afterwards (SIGTERM → SIGKILL → sweep containers matching `name=^wiremock`).
10. **Merge coverage** — `merge-coverage.sh` runs `reportgenerator` over `artifacts/testresults/**/coverage.cobertura.xml` into `artifacts/coverage/`. Include filter targets the four product-assembly name prefixes (Domain, Application, Infrastructure, Host); test/architecture projects must not dilute coverage.
11. **Publish coverage summary** (`if: always()`) — appends `artifacts/coverage/SummaryGithub.md` to the GitHub step summary.
12. **Upload unit test results** (`if: always()`) — `actions/upload-artifact@v4`, name `test-results-unit`, path `artifacts/testresults/unit/`, `if-no-files-found: warn`.
13. **Upload component test results** (`if: always()`) — name `test-results-component`, path `artifacts/testresults/component/`, `if-no-files-found: warn`.
14. **Upload integration test results** (`if: always()`) — name `test-results-integration`, path `artifacts/testresults/integration/`, `if-no-files-found: warn`.
15. **Upload coverage artifacts** (`if: always()`) — name `coverage-report`, path `artifacts/coverage/`, `if-no-files-found: warn`.
16. **Upload secret-scan report** (`if: always()`) — name `secret-scan-report`, path `artifacts/secret-scan/report.json`, `if-no-files-found: warn`.

## AI review pipelines

Two further workflows run alongside the gate and are documented in [AI Tooling](ai-tooling.md): `pipeline-code-review-report.yml` (a thin caller into upstream `smooth-ai-report-review`, producing the required `review / open-code-review-report` check and one consolidated review per run) and `pipeline-ai-analyse.yml` (a bounded low/medium self-fix loop). Neither is path-filtered. The supply-chain trade-off of calling upstream at a moving ref is recorded in [LADR-021](../hlds/mvp/ladrs/021-live-upstream-call-for-ai-review-tooling.md).

## Local equivalents

Requires bash (Linux/macOS/WSL or Git Bash on Windows). On Windows without bash, use `dotnet test` against a project or the solution instead.

```bash
dotnet restore
dotnet format whitespace smooth-ai-stockanalysis.slnx --verify-no-changes --no-restore
dotnet build smooth-ai-stockanalysis.slnx -c Release --no-restore
bash .github/actions/secret-scan/scan.sh

bash .github/actions/test-with-coverage/run-level.sh unit
bash .github/actions/test-with-coverage/run-level.sh component
bash .github/actions/test-with-coverage/run-level.sh integration
bash .github/actions/test-with-coverage/merge-coverage.sh   # runs `dotnet tool restore` itself
```

`merge-coverage.sh` restores the local tool manifest before invoking `reportgenerator`, so no separate `dotnet tool restore` is needed — but it only does so after finding at least one cobertura file. Run a level first.

## .NET local tools

`.config/dotnet-tools.json` declares the local tool manifest, restored in CI (and locally) with `dotnet tool restore`:

| Tool | Version | Command |
|---|---|---|
| `dotnet-reportgenerator-globaltool` | `5.4.4` | `reportgenerator` |
| `dotnet-ef` | `10.0.8` | `dotnet-ef` |

`dotnet-ef` is pinned to the EF Core runtime version (`Directory.Packages.props`) so the migrations CLI never drifts from the `Microsoft.EntityFrameworkCore.*` packages. Bump both together.
