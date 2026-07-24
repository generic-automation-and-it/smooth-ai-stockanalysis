using NodaTime;

namespace SmoothAiStockAnalysis.Domain.Time;

/// <summary>
/// A daily business window expressed in local wall-clock time for a named IANA time zone.
/// </summary>
public sealed class DeliveryWindow : IEquatable<DeliveryWindow>
{
    private readonly DateTimeZone _timeZone;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryWindow"/> class.
    /// </summary>
    /// <param name="timeZoneId">The TZDB IANA time-zone identifier.</param>
    /// <param name="start">The inclusive local start time.</param>
    /// <param name="end">The exclusive local end time.</param>
    public DeliveryWindow(string timeZoneId, LocalTime start, LocalTime end)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);

        _timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId)
            ?? throw new ArgumentException($"Unknown TZDB time-zone identifier '{timeZoneId}'.", nameof(timeZoneId));

        if (end.CompareTo(start) <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "The end time must be later than the start time.");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the canonical TZDB IANA time-zone identifier.
    /// </summary>
    public string TimeZoneId => _timeZone.Id;

    /// <summary>
    /// Gets the inclusive local start time.
    /// </summary>
    public LocalTime Start { get; }

    /// <summary>
    /// Gets the exclusive local end time.
    /// </summary>
    public LocalTime End { get; }

    /// <summary>
    /// Determines whether an instant falls inside this window in its named time zone.
    /// </summary>
    public bool Contains(Instant instant)
    {
        LocalTime localTime = instant.InZone(_timeZone).TimeOfDay;
        return localTime.CompareTo(Start) >= 0 && localTime.CompareTo(End) < 0;
    }

    /// <summary>
    /// Determines whether the current instant supplied by a clock falls inside this window.
    /// </summary>
    public bool ContainsCurrentInstant(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return Contains(clock.GetCurrentInstant());
    }

    public bool Equals(DeliveryWindow? other) =>
        other is not null
        && _timeZone.Id == other._timeZone.Id
        && Start == other.Start
        && End == other.End;

    public override bool Equals(object? obj) => obj is DeliveryWindow w && Equals(w);

    public override int GetHashCode() => HashCode.Combine(_timeZone.Id, Start, End);

    public static bool operator ==(DeliveryWindow? left, DeliveryWindow? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(DeliveryWindow? left, DeliveryWindow? right) =>
        !(left == right);
}
