using SmoothAiStockAnalysis.Domain.Documents;

namespace SmoothAiStockAnalysis.Domain.UnitTest;

public sealed class UserMetadataTests
{
    [Fact]
    public void CreateUsesTheCurrentSchemaVersion()
    {
        UserMetadata metadata = UserMetadata.Create();

        metadata.SchemaVersion.ShouldBe(2);
        metadata.SchemaVersion.ShouldBe(UserMetadata.CurrentSchemaVersion);
        metadata.ShouldBeAssignableTo<IVersionedDocument>();
    }

    [Fact]
    public void CreateLeavesAllPreferencesUnset()
    {
        UserMetadata metadata = UserMetadata.Create();

        metadata.CompanySizeFloor.ShouldBeNull();
        metadata.MinAverageDailyVolume.ShouldBeNull();
        metadata.MinDaysTraded.ShouldBeNull();
        metadata.ScoringWeightEvent.ShouldBeNull();
        metadata.ScoringWeightFundamental.ShouldBeNull();
        metadata.ScoringWeightSentiment.ShouldBeNull();
        metadata.HoldingHorizonDays.ShouldBeNull();
        metadata.CostCapEvent.ShouldBeNull();
        metadata.CostCapFundamental.ShouldBeNull();
        metadata.CostCapReasoning.ShouldBeNull();
        metadata.CostCapDelivery.ShouldBeNull();
        metadata.FxUsdEur.ShouldBeNull();
        metadata.FxUsdGbp.ShouldBeNull();
        metadata.FxUsdJpy.ShouldBeNull();
        metadata.CycleInterval.ShouldBeNull();
        metadata.DeliveryWindowTimeZoneId.ShouldBeNull();
        metadata.DeliveryWindowStart.ShouldBeNull();
        metadata.DeliveryWindowEnd.ShouldBeNull();
        metadata.ProviderReasoning.ShouldBeNull();
        metadata.ReasoningModel.ShouldBeNull();
        metadata.ProviderMarketData.ShouldBeNull();
        metadata.MarketDataModel.ShouldBeNull();
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

    [Fact]
    public void WithPreferencesAppliesSuppliedOverrides()
    {
        UserMetadata original = UserMetadata.Create()
            .WithPreferences(companySizeFloor: 500_000_000m, holdingHorizonDays: 120);

        original.CompanySizeFloor.ShouldBe(500_000_000m);
        original.HoldingHorizonDays.ShouldBe(120);
        original.MinDaysTraded.ShouldBeNull();
    }

    [Fact]
    public void WithPreferencesReplacesTheFullPreferenceSnapshot()
    {
        UserMetadata original = UserMetadata.Create()
            .WithPreferences(companySizeFloor: 500_000_000m, holdingHorizonDays: 120);

        UserMetadata replaced = original.WithPreferences(companySizeFloor: 750_000_000m);

        replaced.CompanySizeFloor.ShouldBe(750_000_000m);
        // Omitted optional arguments are null on the result — they do not keep prior values.
        replaced.HoldingHorizonDays.ShouldBeNull();
        replaced.MinDaysTraded.ShouldBeNull();
    }

    [Fact]
    public void WithPreferencesCanClearAnOverrideWithExplicitNull()
    {
        UserMetadata original = UserMetadata.Create()
            .WithPreferences(companySizeFloor: 500_000_000m);

        UserMetadata cleared = original.WithPreferences(companySizeFloor: null);

        cleared.CompanySizeFloor.ShouldBeNull();
    }

    [Fact]
    public void WithPreferencesLeavesSchemaVersionUnchanged()
    {
        UserMetadata original = UserMetadata.Create();
        UserMetadata updated = original.WithPreferences(companySizeFloor: 100m);

        updated.SchemaVersion.ShouldBe(original.SchemaVersion);
    }
}
