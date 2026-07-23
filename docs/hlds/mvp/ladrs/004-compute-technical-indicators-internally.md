# LADR-004: Compute technical indicators internally

**Status:** Accepted
**Date:** July 2026

## Context

The system needs relative strength, moving-average convergence, simple and exponential moving averages, Bollinger bands, average true range, volume-weighted average price, momentum and breakout detection.

Several providers sell these precomputed. The system also already holds the open-high-low-close-volume history they are derived from.

## Decision

**Compute all indicators internally** from price history already obtained. Purchase no indicator subscription.

This is one half of the governing principle: *buy proprietary data, compute deterministic values.*

## Rationale

- These are deterministic formulas, not proprietary information. There is nothing to buy that cannot be reproduced exactly.
- Full control over parameters — periods, smoothing, session handling — rather than accepting a vendor's defaults.
- Consistent calculation across every company, with no risk of two providers disagreeing about the same measure.
- No vendor-specific implementation to migrate away from.
- Lower cost, and one fewer rate limit to manage.
- It is what most professional systems do.

## Consequences

- **Correctness is now ours.** Indicator calculations carry the heaviest unit-test weight in the codebase, validated against known reference series.
- Sufficient price history must be retained to compute the longest-period indicator in use — the two-hundred-day moving average sets the floor.
- Adding an indicator becomes a code change rather than a subscription change. Cheaper, but slower on the day.
- Edge cases are ours to handle: splits, dividends, halts, thin sessions, and gaps in history.
