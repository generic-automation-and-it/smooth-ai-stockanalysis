# Testing Strategy

## Test levels

| Level | Label | Projects | Dependencies | Description |
|---|---|---|---|---|
| L0 | Unit | `*.UnitTest` | None | Isolated logic, no I/O. |
| L1 | Component | `Application.ComponentTest`, `Infrastructure.ComponentTest` | Isolated SQLite files where persistence is involved | End-to-end behaviour within a layer. |
| L2 | Integration | `Host.IntegrationTest` | Isolated SQLite file | Full Host stack through `WebApplicationFactory`. |

Database tests run without a database container or external persistence service. Tests that exercise external HTTP integrations can opt into the Aspire-managed WireMock container.

## Shared fixtures

Fixtures live in `tests/SmoothAiStockAnalysis.TestFramework/`.

### SqliteTestDatabase

`SqliteTestDatabase.Create()` allocates a unique, on-disk database file below the operating-system temporary directory. It disables pooling and removes the `.db`, `-wal`, and `-shm` files on disposal. Use it for a real SQLite test without cross-test locking or container setup.

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

```bash
# All projects; tests that opt into AspireFixture require a container runtime
dotnet test smooth-ai-stockanalysis.slnx

# Pre-warm WireMock through the Aspire AppHost
dotnet run --project tests/SmoothAiStockAnalysis.TestFramework.Aspire

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
