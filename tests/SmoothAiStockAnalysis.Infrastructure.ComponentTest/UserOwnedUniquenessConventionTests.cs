using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmoothAiStockAnalysis.Domain.Entities;
using SmoothAiStockAnalysis.Infrastructure.ComponentTest.Persistence;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Configurations;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.ComponentTest;

/// <summary>
/// Proves the reusable user-owned ownership and composite-uniqueness helpers that future
/// owned dependents must use (T-021/#65). Production has no owned dependents yet, so this
/// uses the shared <see cref="OwnershipProbeFixture"/> probe model.
/// </summary>
public sealed class UserOwnedUniquenessConventionTests(OwnershipProbeFixture fixture)
    : IClassFixture<OwnershipProbeFixture>
{
    [Fact]
    public async Task UserOwnedHelperCreatesCompositeUniqueIndexAndAllowsCrossUserDuplicates()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await using (OwnershipProbeDbContext writeContext = fixture.CreateContext(DataAccessScope.System()))
        {
            IEntityType ownedEntity = writeContext.Model.FindEntityType(typeof(OwnedProbeRecord))!;
            IReadOnlyList<IIndex> uniqueIndexes = [.. ownedEntity.GetIndexes().Where(index => index.IsUnique)];
            uniqueIndexes.Count.ShouldBe(1);
            uniqueIndexes[0].Properties.Select(property => property.Name).ToArray()
                .ShouldBe(
                [
                    UserOwnedEntityTypeBuilderExtensions.OwnershipForeignKeyName,
                    nameof(OwnedProbeRecord.Ticker)
                ]);

            IForeignKey ownershipFk = ownedEntity.GetForeignKeys().Single();
            ownershipFk.Properties.Select(property => property.Name)
                .ShouldBe([UserOwnedEntityTypeBuilderExtensions.OwnershipForeignKeyName]);
            ownershipFk.PrincipalEntityType.ClrType.ShouldBe(typeof(UserRecord));
            ownershipFk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);

            writeContext.Users().AddRange(
                UserRecord.FromDomain(User.Create(Guid.NewGuid())),
                UserRecord.FromDomain(User.Create(Guid.NewGuid())));
            await writeContext.SaveChangesAsync(cancellationToken);

            long firstUserId = writeContext.Users().OrderBy(user => user.Id).First().Id;
            long secondUserId = writeContext.Users().OrderBy(user => user.Id).Last().Id;

            writeContext.OwnedProbeRecords.AddRange(
                new OwnedProbeRecord { UserId = firstUserId, Ticker = "AAPL" },
                new OwnedProbeRecord { UserId = secondUserId, Ticker = "AAPL" });
            await writeContext.SaveChangesAsync(cancellationToken);

            writeContext.OwnedProbeRecords.Add(new OwnedProbeRecord { UserId = firstUserId, Ticker = "AAPL" });
            await Should.ThrowAsync<DbUpdateException>(() => writeContext.SaveChangesAsync(cancellationToken));
        }

        CompositeIndex index = await fixture.ReadOwnedProbeUniqueIndexAsync(cancellationToken);
        index.Name.ShouldBe("ix_owned_probe_records_user_id_ticker");
        index.IsUnique.ShouldBeTrue();
        index.Columns.ShouldBe(["user_id", "ticker"]);
    }

    [Fact]
    public void HasUserScopedUniqueIndexRejectsEmptyOrOwnershipKeyNaturalKeys()
    {
        Should.Throw<ArgumentException>(() =>
        {
            var modelBuilder = new ModelBuilder();
            EntityTypeBuilder<OwnedProbeRecord> entity = modelBuilder.Entity<OwnedProbeRecord>();
            entity.ConfigureUserOwnedDependent();
            entity.HasUserScopedUniqueIndex();
        }).ParamName.ShouldBe("naturalKeyPropertyNames");

        Should.Throw<ArgumentException>(() =>
        {
            var modelBuilder = new ModelBuilder();
            EntityTypeBuilder<OwnedProbeRecord> entity = modelBuilder.Entity<OwnedProbeRecord>();
            entity.ConfigureUserOwnedDependent();
            entity.HasUserScopedUniqueIndex(UserOwnedEntityTypeBuilderExtensions.OwnershipForeignKeyName);
        }).ParamName.ShouldBe("naturalKeyPropertyNames");
    }
}
