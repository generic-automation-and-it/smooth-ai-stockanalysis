# LADR-002: On-disk SQLite over in-memory with periodic snapshots

**Status:** Accepted
**Date:** July 2026

## Context

The system runs on a Raspberry Pi 4 with 1 GB of RAM, booting from an SD card. SD cards have finite write endurance, and sustained database writes killing the card is among the most common ways a Pi project dies.

Cached reads must return in under 500 ms. A month of analysis history is retained per company.

A hybrid was proposed: hold the database in memory for speed, snapshot it to the card every hour, and reload at startup. Full persistence safety was explicitly declared non-critical, so losing up to an hour of data was acceptable. A document store was also raised as an alternative.

## Decision

**On-disk SQLite**, with write-ahead journaling, relaxed synchronous writes, one batched transaction per analysis cycle, and a retention job pruning beyond one month.

The in-memory hybrid is rejected.

## Rationale

The hybrid backfires on its own terms. A periodic full snapshot rewrites the *entire* database every time. At a 200 MB database that is roughly 4.8 GB written per day. Write-ahead journaling with one transaction per cycle writes only changed pages — plausibly twenty to fifty times less. The proposed remedy for card wear would have increased it.

The read-speed argument also dissolves: the operating system's page cache already keeps frequently-read database pages in memory. The intended benefit arrives for free, without spending the runtime's memory budget on a 1 GB device.

Finally, in-memory SQLite survives only as long as a connection stays open, forcing a single long-lived connection for the application's lifetime — which fights the scoped context lifetime the data-access framework expects.

## Alternatives considered

**Document store (LiteDB, RavenDB, Redis).** Rejected: LiteDB costs the data-access framework already specified; RavenDB will not fit 1 GB; Redis adds a process and still persists to the same card.

## Consequences

- Read-latency targets are met with no special measures.
- Migrating to solid-state storage later is a connection-string change, not a redesign.
- A retention job is mandatory, not optional — it is what keeps the working set small.
- Batching writes into one transaction per cycle becomes a design obligation for every stage.
