# Testing Strategy

## Test levels

| Level | Label | Projects | May touch | Must not | Infrastructure | Command |
|---|---|---|---|---|---|---|
| L0 | Unit | `*.UnitTest`, `Architecture.UnitTest` | In-process logic, pure domain, fakes | Network, disk I/O as product behaviour, containers | **None** | `bash .github/actions/test-with-coverage/run-level.sh unit` |
| L1 | Component | `Application.ComponentTest`, `Infrastructure.ComponentTest` | One layer end-to-end; EF in-memory (Application) or isolated SQLite files (Infrastructure) | Live providers, shared DB servers | No container runtime today | `bash .github/actions/test-with-coverage/run-level.sh component` |
| L2 | Integration | `Host.IntegrationTest` | Full Host via `WebApplicationFactory` + isolated SQLite | Live providers | **None today.** A test that opts into `AspireCollection` starts WireMock itself; CI can pre-warm it with `PREWARM_WIREMOCK=1` | `bash .github/actions/test-with-coverage/run-level.sh integration` |

The three levels are **distinguishable and separately runnable** (NFR-069). CI reports each as its own named step and uploads a per-level test-results artifact. See [LADR-020](../hlds/mvp/ladrs/020-per-level-test-execution-and-architecture-gate.md).

### Architecture tests (L0)

`tests/SmoothAiStockAnalysis.Architecture.UnitTest` uses `NetArchTest.Rules` to enforce inward layer dependencies (NFR-090). It runs in the unit level with no I/O. Details and the list of rules **not** mechanically enforced: [`ARCHITECTURE_AGENTS.md`](../../tests/SmoothAiStockAnalysis.Architecture.UnitTest/ARCHITECTURE_AGENTS.md).

## Shared fixtures

Fixtures live in `tests/SmoothAiStockAnalysis.TestFramework/`.

### SqliteTestDatabase

`new SqliteTestDatabase()` allocates a unique, on-disk database file below the operating-system temporary directory (`Guid` file name per instance/process). It disables pooling and removes the `.db`, `-wal`, `-shm`, and `-journal` files on disposal. L1 test classes own one instance and implement `IAsyncDisposable`; because xUnit creates a new test-class instance for each test, this preserves per-test database isolation. Unique paths also make **parallel-within-level** CI execution safe.

### WebAppFixture&lt;T&gt;

`WebAppFixture<TProgram>` starts a `WebApplicationFactory<TProgram>` with an isolated SQLite database. It remains generic so L2 tests must reference the Host with `Aliases="HostApp"` and close the fixture as `WebAppFixture<HostApp::Program>` to disambiguate xunit.v3's generated `Program`.

Override `ConfigureTestServices(IServiceCollection)` when a test needs to replace a Host service. The Host integration fixture replaces its DbContext options with the isolated connection string because the minimal Host currently reads its configuration at startup.

### AspireFixture and WireMockAdminClient

`AspireFixture` is an opt-in collection fixture for tests that need an external HTTP stub. It reuses WireMock at `http://127.0.0.1:19091` when the CI action has pre-warmed it; otherwise it starts `SmoothAiStockAnalysis.TestFramework.Aspire` and reads the WireMock endpoint from Aspire.

The Aspire AppHost provisions WireMock only. PostgreSQL and Redis are not test dependencies, and persistence remains isolated SQLite. Use `WireMockAdminClient` to reset mappings/request history and install JSON stubs.

Because the collection definition lives in the shared test-framework assembly, downstream xunit.v3 tests select it by type:

```csharp
using SmoothAiStockAnalysis.TestFramework.Fixtures;
using Xunit.v3;

[Collection<AspireCollection>]
public sealed class ExternalApiTests(AspireFixture aspire)
{
    [Fact]
    public async Task Uses_stubbed_response()
    {
        await using WireMockAdminClient wireMock = aspire.CreateWireMockAdminClient();
        await wireMock.ResetAsync();
        await wireMock.StubJsonResponseAsync("GET", "/example", new { value = "stubbed" });

        // Configure the client under test with aspire.WireMockBaseUrl.
    }
}
```

## Running tests

Prefer the per-level scripts (same path CI uses; requires bash — Linux/macOS/WSL or Git Bash on Windows). Build Release first when using `--no-build` inside the scripts after a local `dotnet build -c Release`. On Windows without bash, use `dotnet test` against a project or the solution instead.

```bash
dotnet build smooth-ai-stockanalysis.slnx -c Release

# L0 only — no Docker / no network required
DOCKER_HOST=unix:///nonexistent bash .github/actions/test-with-coverage/run-level.sh unit

# L1 component tests — no WireMock
bash .github/actions/test-with-coverage/run-level.sh component

# L2 integration tests — also container-free, until a test opts into AspireCollection
DOCKER_HOST=unix:///nonexistent bash .github/actions/test-with-coverage/run-level.sh integration

# ...optionally with WireMock pre-warmed (starts Aspire, stops it on exit)
PREWARM_WIREMOCK=1 bash .github/actions/test-with-coverage/run-level.sh integration

# Merge cobertura into artifacts/coverage/ (after any combination of levels)
bash .github/actions/test-with-coverage/merge-coverage.sh

# Full suite convenience wrapper (all three levels + merge)
bash .github/actions/test-with-coverage/run.sh
```

Individual projects remain valid:

```bash
dotnet test tests/SmoothAiStockAnalysis.Domain.UnitTest
dotnet test tests/SmoothAiStockAnalysis.Application.ComponentTest
dotnet test tests/SmoothAiStockAnalysis.Host.IntegrationTest
```

```bash
# All projects via the solution; tests that opt into AspireFixture require a container runtime
dotnet test smooth-ai-stockanalysis.slnx

# Pre-warm WireMock through the Aspire AppHost (optional locally)
dotnet run --project tests/SmoothAiStockAnalysis.TestFramework.Aspire
```
