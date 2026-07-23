# Testing Strategy

## Test levels

| Level | Label | Projects | Dependencies | Description |
|---|---|---|---|---|
| L0 | Unit | `*.UnitTest` | None | Isolated logic, no I/O. |
| L1 | Component | `Application.ComponentTest`, `Infrastructure.ComponentTest` | Isolated SQLite files where persistence is involved | End-to-end behaviour within a layer. |
| L2 | Integration | `Host.IntegrationTest` | Isolated SQLite file | Full Host stack through `WebApplicationFactory`. |

All levels run without a container runtime or another external service.

## Shared fixtures

Fixtures live in `tests/SmoothAiStockAnalysis.TestFramework/`.

### SqliteTestDatabase

`SqliteTestDatabase.Create()` allocates a unique, on-disk database file below the operating-system temporary directory. It disables pooling and removes the `.db`, `-wal`, and `-shm` files on disposal. Use it for a real SQLite test without cross-test locking or container setup.

### WebAppFixture&lt;T&gt;

`WebAppFixture<TProgram>` starts a `WebApplicationFactory<TProgram>` with an isolated SQLite database. It remains generic so L2 tests must reference the Host with `Aliases="HostApp"` and close the fixture as `WebAppFixture<HostApp::Program>` to disambiguate xunit.v3's generated `Program`.

Override `ConfigureTestServices(IServiceCollection)` when a test needs to replace a Host service. The Host integration fixture replaces its DbContext options with the isolated connection string because the minimal Host currently reads its configuration at startup.

## Running tests

```bash
# All levels — no container runtime required
dotnet test smooth-ai-stockanalysis.slnx

# L0 only
dotnet test tests/SmoothAiStockAnalysis.Domain.UnitTest
dotnet test tests/SmoothAiStockAnalysis.Application.UnitTest
dotnet test tests/SmoothAiStockAnalysis.Infrastructure.UnitTest
dotnet test tests/SmoothAiStockAnalysis.Host.UnitTest

# L1 component tests
dotnet test tests/SmoothAiStockAnalysis.Application.ComponentTest
dotnet test tests/SmoothAiStockAnalysis.Infrastructure.ComponentTest

# L2 integration tests
dotnet test tests/SmoothAiStockAnalysis.Host.IntegrationTest
```
