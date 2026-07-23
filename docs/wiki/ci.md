# CI/CD

The pipeline is a single PR gate that builds and tests every change before it can merge to `main`.

## PR Gate

- **Workflow:** `.github/workflows/pr-gate.yml`
- **Triggers:** `pull_request` → `main` (including PR branch updates), `push` → `main`, and manual `workflow_dispatch`.

### Steps

1. **Checkout** — `actions/checkout@v4`.
2. **Install .NET SDK** — `actions/setup-dotnet@v4` (version from the `DOTNET_VERSION` env, currently `10.0.x`).
3. **Restore** — `dotnet restore smooth-ai-stockanalysis.slnx`.
4. **Build** — `dotnet build --no-restore --configuration Release`.
5. **Test with coverage** — local action `.github/actions/test-with-coverage`:
   - Requires no container runtime or external dependency; Infrastructure component and Host integration tests allocate isolated local SQLite files (Application component tests use the EF Core in-memory provider).
   - Restores .NET tools (`dotnet tool restore`) before executing the test suite.
   - Prepares `artifacts/testresults/` and `artifacts/coverage/`.
   - Runs test projects in order: Host integration → Application/Infrastructure component → Domain/Application/Infrastructure/Host unit tests.
   - Generates coverage reports with `dotnet tool run reportgenerator`.
6. **Publish coverage summary** (`if: always()`) — appends `artifacts/coverage/SummaryGithub.md` to the GitHub step summary.
7. **Upload coverage artifacts** (`if: always()`) — uploads `artifacts/coverage/` as `coverage-report`.

## .NET local tools

`.config/dotnet-tools.json` declares the local tool manifest, restored in CI (and locally) with `dotnet tool restore`:

| Tool | Version | Command |
|---|---|---|
| `dotnet-reportgenerator-globaltool` | `5.4.4` | `reportgenerator` |
| `dotnet-ef` | `10.0.8` | `dotnet-ef` |

`dotnet-ef` is pinned to the EF Core runtime version (`Directory.Packages.props`) so the migrations CLI never drifts from the `Microsoft.EntityFrameworkCore.*` packages. Bump both together.
