# LADR-005: Event-driven funnel over valuation-led screening

**Status:** Accepted
**Date:** July 2026

## Context

Two starting points were available for narrowing thousands of listed companies to a handful worth analysing: screen the whole market on valuation metrics daily, or start from what changed today and let valuation decide whether the change is worth acting on.

## Decision

**Start from catalysts.** Detect market-moving events, identify affected companies, rank by catalyst strength, then validate with fundamentals, confirm with timing, and corroborate with sentiment.

Valuation is a *validation* step, not the entry filter.

## Rationale

Valuation-first fails as an opening filter for reasons that compound:

- Sectors trade at structurally different multiples, so a single threshold compares incomparable things.
- A low earnings multiple frequently marks a deteriorating business rather than a bargain — the market has already priced in the decline.
- High-growth companies routinely carry high or meaningless multiples while continuing to outperform.
- Most decisively: the figures were equally true last week. Opportunities arise from *new* information, and a valuation screen is blind to it.

Starting from catalysts also reduces thousands of companies to a manageable watchlist in a single cheap step, which is what makes the whole economic model work.

## Consequences

- **Companies analysed per cycle becomes an output, not a configuration value.** It depends on how eventful the day was. Stage caps bound it.
- Stage one must be broad and cheap; expensive per-company work only fires on survivors. This is the reason free data allowances are viable at all.
- Quiet days legitimately produce nothing. The system must be comfortable publishing an empty result.
- Catalyst *strength* ranking becomes load-bearing — it decides which fifty of several hundred proceed.
