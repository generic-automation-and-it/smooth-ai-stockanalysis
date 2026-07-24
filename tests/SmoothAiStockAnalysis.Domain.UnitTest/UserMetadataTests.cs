using SmoothAiStockAnalysis.Domain.Documents;

namespace SmoothAiStockAnalysis.Domain.UnitTest;

public sealed class UserMetadataTests
{
    [Fact]
    public void CreateUsesTheCurrentSchemaVersion()
    {
        UserMetadata metadata = UserMetadata.Create();

        metadata.SchemaVersion.ShouldBe(1);
        metadata.SchemaVersion.ShouldBe(UserMetadata.CurrentSchemaVersion);
        metadata.ShouldBeAssignableTo<IVersionedDocument>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void ReconstituteAcceptsPositiveSchemaVersions(int schemaVersion)
    {
        UserMetadata metadata = UserMetadata.Reconstitute(schemaVersion);

        metadata.SchemaVersion.ShouldBe(schemaVersion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void ReconstituteRejectsNonPositiveSchemaVersions(int schemaVersion)
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () => UserMetadata.Reconstitute(schemaVersion));

        exception.ParamName.ShouldBe("schemaVersion");
    }
}
