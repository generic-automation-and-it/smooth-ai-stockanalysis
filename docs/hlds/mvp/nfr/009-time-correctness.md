# NFR-051 – NFR-055: Time correctness

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-051 | All instants are stored in coordinated universal time | UTC, without exception | Critical |
| NFR-052 | Business rules involving local time use a named timezone with local times, never a fixed offset | Named zone required | Critical |
| NFR-053 | Behaviour does not shift across daylight-saving transitions | Verified across a boundary | Critical |
| NFR-054 | Trading sessions and market calendars are handled per market, not assumed | Per-market | High |
| NFR-055 | Date and time handling uses a library with explicit zone and instant semantics | No ambiguous types | High |

## Rationale

NFR-052 is the requirement that would otherwise be discovered the hard way. The delivery window is expressed in Central European Time, which is one offset for part of the year and another for the rest. Storing it as a fixed offset means the window silently moves by an hour twice a year — a defect that appears seasonally, affects only edge-of-window deliveries, and is close to impossible to diagnose from a log.

NFR-055 is why an explicit date and time library was specified. Distinguishing an instant from a local date-time from a zoned date-time in the type system prevents an entire category of error that ordinary date types invite.

A known technical uncertainty attaches here: the chosen library's first-class integration targets a different storage engine than the one selected, so the conversion approach must be established during the foundation milestone rather than assumed.

## Verification

- Delivery-window behaviour is tested across the Europe/Paris spring-forward and fall-back boundaries, including cases that fail under either fixed CET offset.
- Real SQLite storage round-trips nanosecond-precision instants, calendar-aware local dates, and both ambiguous fall-back zoned values.
- `Instant` storage is asserted as lossless UTC ISO-8601 text. See LADR-014 for the complete representation.

## Related

- [LADR-002](../ladrs/002-on-disk-sqlite-over-in-memory-snapshots.md)
- [LADR-014](../ladrs/014-lossless-nodatime-mappings-for-sqlite.md)
- BR-36 (delivery window)
