# NFR-001 – NFR-005: Performance and responsiveness

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-001 | A request served entirely from cache returns within a bounded time | < 500 ms | High |
| NFR-002 | A complete analysis cycle finishes well inside the scheduled interval | < 30 min at the default cadence | Critical |
| NFR-003 | Indicator computation across the timing-stage candidate set completes without dominating the cycle | Bounded by stage cap, not by universe size | High |
| NFR-004 | Startup to first serviceable request | < 60 s on the target device | Medium |
| NFR-005 | No user-facing request performs a provider call on the cached path | Zero network calls when cached | High |

## Rationale

NFR-002 is the one that matters. If a cycle cannot finish within its interval, the run lock causes every subsequent cycle to be skipped and the system quietly degrades to running far less often than configured — without failing, and therefore without alerting. Cycle duration is a correctness concern disguised as a performance one.

NFR-001 is met without special measures. The database file is read through the operating system's page cache, which keeps frequently-accessed pages in memory, and hot reference data sits in an in-process cache above that. No part of the cached path touches the network or the disk in the common case.

NFR-003 follows from the funnel: expensive per-company work only ever runs on a capped candidate set, so cost and duration are independent of how eventful the market was.

## Verification

- Cycle duration recorded per run and compared against the configured interval.
- Cached read latency measured on the target device, not on development hardware.
- Skipped-cycle count monitored — a rising count is the signal that NFR-002 is failing.

## Related

- `docs/adr/002-on-disk-sqlite-over-in-memory-snapshots.md`
- `docs/adr/011-memory-only-caching.md`
- BR-41 (stage caps)
