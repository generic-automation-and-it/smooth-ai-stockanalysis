using SmoothAiStockAnalysis.Application.Common.Persistence;

namespace SmoothAiStockAnalysis.Application.UnitTest;

public sealed class DataAccessScopeTests
{
    [Fact]
    public void ForUserCreatesAUserScopeWithTheTenantKey()
    {
        DataAccessScope scope = DataAccessScope.ForUser(42);

        scope.Kind.ShouldBe(DataAccessScopeKind.User);
        scope.UserId.ShouldBe(42);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ForUserRejectsNonPositiveTenantKeys(long userId)
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => DataAccessScope.ForUser(userId));
        exception.ParamName.ShouldBe("userId");
    }

    [Fact]
    public void SystemCreatesANamedSystemScope()
    {
        DataAccessScope scope = DataAccessScope.System();
        scope.Kind.ShouldBe(DataAccessScopeKind.System);
    }

    [Fact]
    public void UserIdThrowsOnSystemScope()
    {
        DataAccessScope scope = DataAccessScope.System();
        Should.Throw<InvalidOperationException>(() => _ = scope.UserId);
    }
}
