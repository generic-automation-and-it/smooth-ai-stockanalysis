# NFR-024 – NFR-030: Cost and quota control

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-024 | Consumption is tracked per provider against its allowance | Recorded per call | High |
| NFR-025 | Stage caps bound the work performed per cycle | 50 / 20 / 10 / 5 by default, configurable | Critical |
| NFR-026 | Reasoning cost per cycle is bounded regardless of market activity | ≤ 10 candidates reach the model | Critical |
| NFR-027 | A spend ceiling halts reasoning cleanly and notifies the owner | No silent overspend | Critical |
| NFR-028 | Exhausted-credit responses are handled explicitly | Recognised, not treated as a generic error | High |
| NFR-029 | Occasions where a provider limit constrained output are recorded | Evidence base for the cost decision | High |
| NFR-030 | No paid subscription is taken without recorded evidence that a free limit constrained output | Evidence required | High |

## Rationale

NFR-025 and NFR-026 are the load-bearing requirements of the entire commercial model. Because at most ten candidates ever reach the reasoning layer, the most expensive part of the system costs the same on the busiest trading day as on the quietest. Cost becomes a function of configuration rather than of the market, which is what makes an unattended system on free allowances a sane proposition.

NFR-029 and NFR-030 exist to keep the free-first posture honest. Without recorded evidence, "the free tier feels limiting" becomes an argument for a $2,000-a-year subscription. With it, the upgrade decision has a factual basis — and the analysis already indicates the ordering: the cheapest upgrade unlocks a stated requirement, while the most expensive one may not be needed at all.

## Verification

- Quota consumption reported per provider per period.
- Constraint occasions reviewed at the cost-evaluation milestone.
- Spend ceiling tested by setting it deliberately low.

## Related

- `docs/brd.md` §8 (subscription strategy and paid upgrade analysis)
- `docs/hlds/mvp/ladrs/003-defer-managed-workflow-orchestration.md`
- BR-41, BR-42, BR-44
