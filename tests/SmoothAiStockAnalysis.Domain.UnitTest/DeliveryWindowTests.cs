using NodaTime;
using SmoothAiStockAnalysis.Domain.Time;

namespace SmoothAiStockAnalysis.Domain.UnitTest;

public sealed class DeliveryWindowTests
{
    private static readonly DeliveryWindow Window = new("Europe/Paris", new LocalTime(7, 0), new LocalTime(22, 0));

    [Fact]
    public void ContainsUsesInclusiveStartAndExclusiveEnd()
    {
        Window.Contains(Instant.FromUtc(2026, 3, 29, 4, 59, 59)).ShouldBeFalse();
        Window.Contains(Instant.FromUtc(2026, 3, 29, 5, 0)).ShouldBeTrue();
        Window.Contains(Instant.FromUtc(2026, 3, 29, 19, 59, 59)).ShouldBeTrue();
        Window.Contains(Instant.FromUtc(2026, 3, 29, 20, 0)).ShouldBeFalse();
    }

    [Fact]
    public void ContainsUsesEuropeParisOffsetAfterSpringForward()
    {
        Window.Contains(Instant.FromUtc(2026, 3, 28, 5, 30)).ShouldBeFalse();
        Window.Contains(Instant.FromUtc(2026, 3, 29, 5, 30)).ShouldBeTrue();
    }

    [Fact]
    public void ContainsUsesEuropeParisOffsetAfterFallBack()
    {
        Window.Contains(Instant.FromUtc(2026, 10, 24, 5, 30)).ShouldBeTrue();
        Window.Contains(Instant.FromUtc(2026, 10, 25, 5, 30)).ShouldBeFalse();
    }

    [Fact]
    public void ContainsCurrentInstantUsesTheSuppliedClock()
    {
        var clock = new StubClock(Instant.FromUtc(2026, 3, 29, 5, 30));

        Window.ContainsCurrentInstant(clock).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Invalid/Zone")]
    [InlineData("")]
    [InlineData(null!)]
    public void ConstructorRejectsUnknownTimeZone(string timeZoneId)
    {
        Should.Throw<ArgumentException>(() => new DeliveryWindow(timeZoneId, new LocalTime(7, 0), new LocalTime(22, 0)));
    }

    [Fact]
    public void ConstructorRejectsWindowsThatDoNotEndAfterTheyStart()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new DeliveryWindow("Europe/Paris", new LocalTime(22, 0), new LocalTime(7, 0)));
        Should.Throw<ArgumentOutOfRangeException>(() => new DeliveryWindow("Europe/Paris", new LocalTime(7, 0), new LocalTime(7, 0)));
    }

    [Fact]
    public void EqualsAndGetHashCodeUseAllThreeComponents()
    {
        var a = new DeliveryWindow("Europe/Paris", new LocalTime(7, 0), new LocalTime(22, 0));
        var same = new DeliveryWindow("Europe/Paris", new LocalTime(7, 0), new LocalTime(22, 0));
        var otherZone = new DeliveryWindow("America/New_York", new LocalTime(7, 0), new LocalTime(22, 0));
        var otherStart = new DeliveryWindow("Europe/Paris", new LocalTime(7, 30), new LocalTime(22, 0));
        var otherEnd = new DeliveryWindow("Europe/Paris", new LocalTime(7, 0), new LocalTime(21, 0));

        a.Equals(same).ShouldBeTrue();
        a.Equals((object)same).ShouldBeTrue();
        a.GetHashCode().ShouldBe(same.GetHashCode());

        a.Equals(otherZone).ShouldBeFalse();
        a.Equals(otherStart).ShouldBeFalse();
        a.Equals(otherEnd).ShouldBeFalse();
        a.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperatorsHandleNullsSymmetrically()
    {
        DeliveryWindow? a = new("Europe/Paris", new LocalTime(7, 0), new LocalTime(22, 0));
        DeliveryWindow? same = new("Europe/Paris", new LocalTime(7, 0), new LocalTime(22, 0));
        DeliveryWindow? other = new("America/New_York", new LocalTime(7, 0), new LocalTime(22, 0));

        (a == same).ShouldBeTrue();
        (a == other).ShouldBeFalse();
        (a != same).ShouldBeFalse();
        (a != other).ShouldBeTrue();
        (a == null).ShouldBeFalse();
        (null == a).ShouldBeFalse();
        (null == null).ShouldBeTrue();
    }

    private sealed class StubClock(Instant currentInstant) : IClock
    {
        public Instant GetCurrentInstant() => currentInstant;
    }
}
