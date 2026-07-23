# LADR-011: Memory-only caching, no cache server

**Status:** Accepted
**Date:** July 2026

## Context

Cached reads must return in under 500 ms, and infrequently-changing data must be reused rather than repeatedly purchased — this is what makes free provider allowances workable.

A reference project uses a layered cache combining in-process memory with a distributed cache server, replacing the older split between separate memory and distributed cache abstractions.

## Decision

Adopt the **layered cache abstraction, configured with its in-process layer only.** No cache server.

## Rationale

The distributed layer exists to share cached data across multiple application instances. There is one instance, and there will be one instance for the foreseeable future.

On a 1 GB device, a cache server is a second process competing for memory with the runtime — and it would persist to the same SD card the storage decision works to protect.

Adopting the layered abstraction rather than a bare memory cache costs nothing today and means adding a second layer later is configuration rather than rewriting every call site.

## Consequences

- Cache does not survive a restart. Acceptable: fundamentals refetch cheaply, and a cold start costs one slower cycle.
- Cache size must be bounded explicitly. On a 1 GB device an unbounded cache is a slow memory leak.
- **Cache lifetimes carry real commercial weight.** Company financials change quarterly; caching them for weeks eliminates the overwhelming majority of fundamentals requests on a thirty-minute cycle. Getting these durations right matters more than the caching mechanism does.
