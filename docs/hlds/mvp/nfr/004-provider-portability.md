# NFR-019 – NFR-023: Provider portability

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-019 | All provider responses are normalised to an internal model at the adapter boundary | No exceptions | Critical |
| NFR-020 | No provider-specific type appears in the application or domain layers | Enforced structurally | Critical |
| NFR-021 | Substituting a provider is a configuration change, not a code change, where an adapter exists | Configuration only | High |
| NFR-022 | Which provider served a given result is recorded | Traceable per result | Medium |
| NFR-023 | The same event reported by two providers resolves to one internal event | Deduplicated on identity | Critical |

## Rationale

Provider risk is real and has already materialised once: a primary market data supplier rebranded during requirements gathering, with pricing in transition. Free tiers change terms, suppliers get acquired, and coverage gaps appear where none were documented.

NFR-019 and NFR-020 are the insurance. If normalisation happens at the boundary, replacing a supplier touches one adapter. If provider shapes leak inward, it touches everything.

NFR-023 exists because multiple providers cover overlapping ground by design — the same analyst upgrade may arrive twice. Without identity resolution the system would analyse it twice and alert twice, and the user would lose trust in the alerting before they lost trust in the analysis.

## Verification

- Adapter tests run against recorded provider responses, asserting the normalised output shape.
- Layer dependency rules checked in the build.
- Cross-provider deduplication tested with the same underlying event from two sources.

## Related

- `docs/hlds/mvp/ladrs/013-abstracted-ai-reasoning-provider.md`
- BR-4 (never analyse twice), BR-43 (failover)
