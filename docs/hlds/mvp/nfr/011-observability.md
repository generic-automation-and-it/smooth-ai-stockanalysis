# NFR-063 – NFR-068: Observability

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-063 | Structured logging throughout | Queryable, not free text | High |
| NFR-064 | Tracing across a cycle and its provider calls | End-to-end per cycle | Medium |
| NFR-065 | Cycle failure notifies the owner by email | Every failure | High |
| NFR-066 | Failure notifications carry enough context to diagnose without device access | Self-sufficient | High |
| NFR-067 | Provider consumption telemetry is retained for the cost decision | Retained, not just logged | High |
| NFR-068 | No high-availability alerting, dashboard or uptime target is required | Explicitly out of scope | Low |

## Rationale

The operational contract is deliberately modest, and NFR-068 records that as a decision rather than a gap. This is a personal tool on domestic hardware; an availability target would imply an on-call response that does not exist.

NFR-066 carries most of the practical weight. The device is headless and on a home network, so a notification saying only that something failed forces the owner to go and look. A notification that says which stage, which provider and which error usually does not.

NFR-067 distinguishes telemetry from logs. Quota consumption is not operational noise — it is the evidence base for the subscription decision, and it must survive log rotation to be useful at the cost-evaluation milestone.

## Verification

- Failure notification verified by inducing a failure deliberately.
- Consumption telemetry asserted to persist beyond log retention.

## Related

- BR-37 (failure notification), BR-42 (quota tracking)
- `docs/hlds/mvp/nfr/005-cost-and-quota-control.md`
