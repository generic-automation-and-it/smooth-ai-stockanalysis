# LADR-014: Lossless NodaTime mappings for SQLite

**Status:** Accepted
**Date:** July 2026

## Context

The service stores data in SQLite, while NodaTime's first-class EF Core integration is for Npgsql/PostgreSQL. NFR-051 requires every stored instant to be UTC; NFR-052 prohibits fixed offsets in local-time business rules; and NFR-055 requires explicit instant, local-date, and zoned semantics.

The representation must round-trip all NodaTime precision. In particular, `Instant` has nanosecond precision, so a .NET or Unix tick representation would silently discard up to 99 nanoseconds.

## Decision

Use NodaTime 3.3 and custom EF Core `ValueConverter`s registered globally through `SmoothAiStockAnalysisDbContext.ConfigureConventions`.

- `Instant` is persisted as lossless, invariant, UTC ISO-8601 `TEXT` using `InstantPattern.ExtendedIso`.
- `LocalDate` is persisted as invariant `year|month|day|calendarId` `TEXT`, preserving its calendar system as well as its date fields.
- `ZonedDateTime` is persisted as `instant|tzdbZoneId|calendarId` `TEXT`. The instant uses the same lossless UTC representation; the zone is a TZDB IANA identifier; and the offset/local view is reconstructed from that instant and named zone.

The production Host registers NodaTime `IClock` as `SystemClock.Instance`. Domain business windows accept an `Instant` (or an explicitly supplied `IClock`) and convert only at the rule boundary through a named TZDB zone.

## Alternatives considered

**Unix ticks as an SQLite integer.** Rejected: ticks have 100-nanosecond precision and lose valid `Instant` data.

**A fixed offset or `DateTimeOffset` for the delivery window.** Rejected: it cannot represent the Europe/Paris DST change and shifts the business window seasonally.

**Npgsql.NodaTime or a provider-specific integration package.** Rejected: it targets PostgreSQL, not the selected SQLite provider.

## Consequences

- SQLite values remain inspectable text and `Instant` values remain unambiguously UTC and lossless.
- A zoned value's historical local rendering follows the application's installed TZDB data after an upgrade; the stored source of truth remains the instant plus named zone, never a captured fixed offset.
- Future NodaTime persistence properties automatically use the same global representation. A per-property mapping may override it only with a documented reason.
- Real-file SQLite component tests cover `Instant`, `LocalDate`, and ambiguous fall-back `ZonedDateTime` values. A migration is not required until a feature adds a persisted property of one of these types.
