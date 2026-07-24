using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;
using SmoothAiStockAnalysis.TestFramework.Fixtures;

namespace SmoothAiStockAnalysis.Host.IntegrationTest;

/// <summary>
/// L2 proof that the Host composition root wires explicit data-access scopes and the global
/// user-isolation filter end-to-end against the production DI registration.
/// </summary>
public sealed class DataAccessScopeIntegrationTests(HostWebAppFixture fixture)
    : IClassFixture<HostWebAppFixture>
{
    [Fact]
    public async Task HostCompositionIsolatesUsersAndFailsClosedWithoutScope()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Guid userAIdentifier = Guid.NewGuid();
        Guid userBIdentifier = Guid.NewGuid();
        long userAId;
        long userBId;

        await using (AsyncServiceScope seedScope = fixture.RootServices.CreateAsyncScope())
        {
            // Seeding uses the deliberate system scope so setup is not blocked by isolation.
            seedScope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
            var context = seedScope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

            await context.Database.OpenConnectionAsync(cancellationToken);
            DbConnection connection = context.Database.GetDbConnection();
            await InsertUserAsync(connection, userAIdentifier, cancellationToken);
            await InsertUserAsync(connection, userBIdentifier, cancellationToken);
            userAId = await ReadUserIdAsync(connection, userAIdentifier, cancellationToken);
            userBId = await ReadUserIdAsync(connection, userBIdentifier, cancellationToken);
        }

        await using (AsyncServiceScope userAScope = fixture.RootServices.CreateAsyncScope())
        {
            userAScope.ServiceProvider.GetRequiredService<IDataAccessScopeSetter>()
                .SetScope(DataAccessScope.ForUser(userAId));
            var context = userAScope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

            // No feature-specific predicate: isolation comes only from the global filter.
            long[] ids = [.. (await context.Users().ToListAsync(cancellationToken)).Select(user => user.Id)];
            ids.ShouldBe([userAId]);
        }

        await using (AsyncServiceScope userBScope = fixture.RootServices.CreateAsyncScope())
        {
            userBScope.ServiceProvider.GetRequiredService<IDataAccessScopeSetter>()
                .SetScope(DataAccessScope.ForUser(userBId));
            var context = userBScope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

            long[] ids = [.. (await context.Users().ToListAsync(cancellationToken)).Select(user => user.Id)];
            ids.ShouldBe([userBId]);
        }

        await using (AsyncServiceScope systemScope = fixture.RootServices.CreateAsyncScope())
        {
            systemScope.ServiceProvider.GetRequiredService<ISystemDataAccessScope>().EnterSystemScope();
            var context = systemScope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();

            long[] ids = [.. (await context.Users().ToListAsync(cancellationToken)).Select(user => user.Id)];
            ids.ShouldContain(userAId);
            ids.ShouldContain(userBId);
        }

        await using (AsyncServiceScope missingScope = fixture.RootServices.CreateAsyncScope())
        {
            var context = missingScope.ServiceProvider.GetRequiredService<SmoothAiStockAnalysisDbContext>();
            await Should.ThrowAsync<InvalidOperationException>(
                () => context.Users().ToListAsync(cancellationToken));
        }
    }

    [Fact]
    public void ScopeServicesResolveAsScopedFromTheHostCompositionRoot()
    {
        using IServiceScope scope = fixture.RootServices.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        var setter = sp.GetRequiredService<IDataAccessScopeSetter>();
        var reader = sp.GetRequiredService<IDataAccessScope>();
        var system = sp.GetRequiredService<ISystemDataAccessScope>();

        ReferenceEquals(setter, reader).ShouldBeTrue();
        ReferenceEquals(setter, system).ShouldBeTrue();

        setter.SetScope(DataAccessScope.ForUser(7));
        reader.Current.Kind.ShouldBe(DataAccessScopeKind.User);
        reader.Current.UserId.ShouldBe(7);

        system.EnterSystemScope();
        reader.Current.Kind.ShouldBe(DataAccessScopeKind.System);
    }

    private static async Task InsertUserAsync(
        DbConnection connection,
        Guid uniqueIdentifier,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO user_record (unique_identifier, metadata)
            VALUES ($id, '{"schemaVersion":1}');
            """;
        DbParameter id = command.CreateParameter();
        id.ParameterName = "$id";
        id.Value = uniqueIdentifier.ToString();
        command.Parameters.Add(id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> ReadUserIdAsync(
        DbConnection connection,
        Guid uniqueIdentifier,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM user_record WHERE unique_identifier = $id;";
        DbParameter id = command.CreateParameter();
        id.ParameterName = "$id";
        id.Value = uniqueIdentifier.ToString();
        command.Parameters.Add(id);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }
}
