# LADR-012: Liquidity measured excluding the catalyst day

**Status:** Accepted
**Date:** July 2026

## Context

The target is roughly a 10% move within days to two weeks. That target implicitly favours smaller, more volatile companies — which is also where thin trading and price manipulation concentrate.

Company size alone does not establish that a position can be exited. Size and liquidity diverge most sharply in exactly the target region: Nordic mid-caps frequently have a small proportion of shares available to trade.

## Decision

Four measures, all configurable:

- **Minimum average daily traded value: €3 million.** Value rather than share count, so it compares across markets using the existing currency conversion.
- **Measured as a median across the trailing twenty sessions, excluding the catalyst day.**
- **Participation cap:** a prospective position may not exceed 10% of daily traded value.
- **Low turnover** relative to available float is a *flag in the evidence*, never an exclusion.

## Rationale

**Why value, not share count.** Five hundred thousand shares at €0.80 is not liquidity; thirty thousand shares at €120 is. Share counts mislead.

**Why exclude the catalyst day — the decision that matters most.** The system fires on catalysts, and catalysts spike volume. Computing average volume over a window containing the event day means every thin company looks liquid precisely when it is being evaluated, because the very event that surfaced it inflated its volume. A median across prior sessions describes a normal day — which is the day the position will be sold on. This choice affects candidate quality more than the threshold value does.

**Why turnover is a flag, not a filter.** Turnover ratio fails badly at the top end. Europe's largest and most liquid companies turn over a small fraction of their shares daily simply because their share counts are enormous. A percentage floor would exclude exactly the stable large companies the size threshold is there to admit.

## Consequences

- Twenty sessions of price history are required before a company can be assessed. Newly listed companies are unassessable for roughly a month — accepted.
- The participation cap has no practical effect in Phase 1, since positions are not tracked. It becomes meaningful in Phase 2.
- **The bid-ask spread remains unaddressed.** It is the other half of exit cost and rarely appears in free data. Recorded as a known gap; the liquidity floor compensates only partially.
