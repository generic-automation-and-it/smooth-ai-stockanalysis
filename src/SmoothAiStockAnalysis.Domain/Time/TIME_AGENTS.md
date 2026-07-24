# TIME_AGENTS.md

## TL;DR

Time rules use NodaTime's explicit `Instant`, `LocalDate`, `LocalTime`, and named `DateTimeZone` types. An instant is evaluated against a business window only after converting it to the configured IANA zone.

## Requirements

- `DeliveryWindow` represents a daily business window using an IANA zone ID and local wall-clock start/end times.
- Product defaults for the window (`Europe/Paris`, `07:00`–`22:00`) live in the F-004 settings catalogue under Host `Cycle:DeliveryWindow*` (see [`HOST_AGENTS.md`](../../SmoothAiStockAnalysis.Host/HOST_AGENTS.md)). Feature code must read the **effective** window from `EffectiveSettings.DeliveryWindow` via `ISettingsResolver`, not from a Host singleton options object — there is no standalone `DeliveryWindow` configuration section after F-004.
- `Contains(Instant)` is deterministic: it converts the supplied instant to the named zone and evaluates a start-inclusive, end-exclusive interval. It must never use a fixed offset, `DateTime.Now`, or `SystemClock` directly.
- Windows do not span midnight in this foundation. Construction rejects equal or reversed bounds and unknown time-zone IDs. A future requirement must explicitly add overnight semantics.
- Code that needs "now" receives NodaTime `IClock`; production composition registers `SystemClock.Instance` and tests supply their own clock.
- NodaTime is the sole permitted external dependency in Domain. It is allowed only for explicit time value semantics; Domain still has no project, persistence, network, framework, or DI dependencies.

## Persistence Boundary

- Infrastructure persists `Instant` as lossless canonical UTC text, never a local or offset value. `LocalDate` and `ZonedDateTime` preserve their calendar identity; a zoned value also preserves its TZDB IANA zone ID.
- Persistence conversion remains Infrastructure-only. See `Persistence/PERSISTENCE_AGENTS.md` and LADR-014 for the storage contract.

## Test Boundary

- L0 tests cover both sides of the Europe/Paris spring-forward and fall-back transitions, and prove a supplied `IClock` is used deterministically.
- L1 tests round-trip time values through an isolated on-disk SQLite database using the production converter convention.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-07-24 | Created the NodaTime time foundation and daily delivery-window contract. | #6 |
| 2026-07-24 | Documented that delivery-window defaults and effective resolution are owned by the F-004 catalogue/`ISettingsResolver`, not a Host singleton options class. | #68, #69 |
