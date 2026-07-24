using SmoothAiStockAnalysis.Domain.Documents;
using SmoothAiStockAnalysis.Domain.Entities;

namespace SmoothAiStockAnalysis.Domain.UnitTest;

public sealed class UserTests
{
    [Fact]
    public void CreateReturnsATransientUserWithCurrentMetadata()
    {
        Guid uniqueIdentifier = Guid.NewGuid();

        User user = User.Create(uniqueIdentifier);

        user.Id.ShouldBe(0);
        user.UniqueIdentifier.ShouldBe(uniqueIdentifier);
        user.Metadata.SchemaVersion.ShouldBe(UserMetadata.CurrentSchemaVersion);
    }

    [Fact]
    public void CreateRejectsAnEmptyUniqueIdentifier()
    {
        var exception = Should.Throw<ArgumentException>(() => User.Create(Guid.Empty));

        exception.ParamName.ShouldBe("uniqueIdentifier");
    }

    [Fact]
    public void ReconstitutePreservesPersistedState()
    {
        Guid uniqueIdentifier = Guid.NewGuid();
        UserMetadata metadata = UserMetadata.Reconstitute(3);

        User user = User.Reconstitute(42, uniqueIdentifier, metadata);

        user.Id.ShouldBe(42);
        user.UniqueIdentifier.ShouldBe(uniqueIdentifier);
        user.Metadata.ShouldBeSameAs(metadata);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReconstituteRejectsANonPositiveId(long id)
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () => User.Reconstitute(id, Guid.NewGuid(), UserMetadata.Create()));

        exception.ParamName.ShouldBe("id");
    }

    [Fact]
    public void ReconstituteRejectsAnEmptyUniqueIdentifier()
    {
        var exception = Should.Throw<ArgumentException>(
            () => User.Reconstitute(1, Guid.Empty, UserMetadata.Create()));

        exception.ParamName.ShouldBe("uniqueIdentifier");
    }

    [Fact]
    public void ReconstituteRejectsMissingMetadata()
    {
        var exception = Should.Throw<ArgumentNullException>(
            () => User.Reconstitute(1, Guid.NewGuid(), null!));

        exception.ParamName.ShouldBe("metadata");
    }
}
