# NFR-045 – NFR-050: Configurability

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-045 | Every tunable value resolves as user preference first, application default second | One pattern, universally applied | High |
| NFR-046 | Tunable values change without rebuilding the application | Configuration or user metadata only | High |
| NFR-047 | Invalid configuration fails at startup, not mid-cycle | Fail fast | High |
| NFR-048 | User metadata carries an explicit version marker | Versioned document | High |
| NFR-049 | Defaults are documented alongside the values they govern | Discoverable | Medium |
| NFR-050 | Currency conversion multipliers are static configuration, reviewed rather than refreshed | Manual review | Medium |

## Rationale

NFR-045 is a single pattern rather than a dozen independent settings. It governs the company size floor, liquidity thresholds, scoring weightings, holding horizon, stage caps, delivery window and cycle interval. Designing it once prevents each of those being solved separately and inconsistently — and it is what makes the Phase 3 dashboard a straightforward editor over user metadata rather than a new subsystem.

NFR-047 matters more than it appears. The system runs unattended on a timer; a configuration error surfacing mid-cycle would appear as an intermittent failure rather than as a misconfiguration, and would be diagnosed the slow way.

NFR-050 accepts drift deliberately. Fixed conversion multipliers become inaccurate as rates move, but for a coarse size threshold a several-percent error is immaterial. A daily refresh with a change threshold is recorded as a future, low-priority requirement rather than built now.

## Verification

- Resolution tested across both the override and default paths.
- Startup validation tested with deliberately invalid configuration.

## Related

- BR-46
- `docs/brd.md` §5.3 (future currency refresh)
