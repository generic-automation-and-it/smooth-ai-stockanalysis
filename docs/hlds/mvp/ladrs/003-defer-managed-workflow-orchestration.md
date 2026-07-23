# LADR-003: Defer managed workflow orchestration

**Status:** Deferred — revisit condition stated below
**Date:** July 2026

## Context

A durable workflow platform was raised as a hoped-for requirement, with a strong fit on paper: scheduled execution for agents that poll periodically, retry handling for models returning inconsistent responses, survival across crashes and network timeouts without losing progress, and human-in-the-loop pauses for approval.

Every one of those maps onto something this system needs.

## Decision

**Defer.** Scheduling and durability are built on components the system already requires: a background service with an interval timer, a persisted run lock, and per-stage state written to the local store.

## Rationale

**Cost.** The managed service carries a minimum of roughly $100 per month with no free production tier. That exceeds the entire data-provider budget — for infrastructure supporting a workload of forty-eight cycles a day. It contradicts the free-tiers-first posture directly. Signup credits cover roughly ten months, after which the cost recurs indefinitely.

**Self-hosting is not an escape.** A self-hosted cluster requires its own database — Cassandra, PostgreSQL or MySQL — plus the operational burden of running a distributed system. Neither fits a 1 GB Raspberry Pi.

**The properties are obtainable cheaply.** Skip-if-running comes from a run lock. Resume-after-crash comes from persisted stage state. Both are already required for event de-duplication and analysis history, so they cost nothing additional.

## Consequences

- **No human-in-the-loop capability.** Acceptable: the human acts on an email, not inside a workflow.
- **Every stage must be idempotent and resumable.** This is good discipline regardless, and it is what makes later adoption a substitution rather than a rewrite.
- Retry and backoff are implemented in provider adapters instead of being inherited from the platform.
- The system carries no external infrastructure dependency, which suits a private home network.

## Revisit when

Volume, complexity or a genuine need for human-in-the-loop approval justifies roughly $1,200 per year — or if a free or self-hostable alternative appears that fits the hardware.
