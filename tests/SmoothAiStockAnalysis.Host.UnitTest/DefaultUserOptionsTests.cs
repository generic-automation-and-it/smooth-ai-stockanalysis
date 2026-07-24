using Microsoft.Extensions.Configuration;
using SmoothAiStockAnalysis.Host.Configuration;

namespace SmoothAiStockAnalysis.Host.UnitTest;

public sealed class DefaultUserOptionsTests
{
    private static readonly Guid PlaceholderUniqueIdentifier =
        Guid.Parse("00000000-0000-4000-8000-000000000001");

    [Fact]
    public void FromConfigurationBindsAndValidatesTheConfiguredIdentifier()
    {
        IConfiguration configuration = BuildConfiguration(
            ("DefaultUser:UniqueIdentifier", PlaceholderUniqueIdentifier.ToString()));

        DefaultUserOptions options = DefaultUserOptions.FromConfiguration(configuration);

        options.GetValidatedUniqueIdentifier().ShouldBe(PlaceholderUniqueIdentifier);
    }

    [Fact]
    public void FromConfigurationRejectsMissingUniqueIdentifier()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => DefaultUserOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(DefaultUserOptions.UniqueIdentifierPath);
    }

    [Fact]
    public void FromConfigurationRejectsEmptyUniqueIdentifier()
    {
        IConfiguration configuration = BuildConfiguration(("DefaultUser:UniqueIdentifier", "   "));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => DefaultUserOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(DefaultUserOptions.UniqueIdentifierPath);
    }

    [Fact]
    public void FromConfigurationRejectsMalformedUniqueIdentifier()
    {
        IConfiguration configuration = BuildConfiguration(("DefaultUser:UniqueIdentifier", "not-a-guid"));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => DefaultUserOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(DefaultUserOptions.UniqueIdentifierPath);
        // Fail-fast messages name the key; they must not echo the invalid secret-like payload.
        exception.Message.ShouldNotContain("not-a-guid");
    }

    [Fact]
    public void FromConfigurationRejectsEmptyGuid()
    {
        IConfiguration configuration = BuildConfiguration(
            ("DefaultUser:UniqueIdentifier", Guid.Empty.ToString()));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => DefaultUserOptions.FromConfiguration(configuration));

        exception.Message.ShouldContain(DefaultUserOptions.UniqueIdentifierPath);
    }

    [Fact]
    public void FromConfigurationThrowsOnNullConfiguration()
    {
        Should.Throw<ArgumentNullException>(() => DefaultUserOptions.FromConfiguration(null!));
    }

    private static IConfiguration BuildConfiguration(params (string Key, string? Value)[] values)
    {
        var pairs = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }
}
