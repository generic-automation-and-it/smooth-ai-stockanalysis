# NFR-013 – NFR-018: Caching and data efficiency

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-013 | Cache lifetimes are matched to how often each data type actually changes | Per-category, explicitly configured | Critical |
| NFR-014 | Reference data is fetched once and shared, never per user | One fetch serves all users | High |
| NFR-015 | Cache size is explicitly bounded | Fits within the device memory budget | High |
| NFR-016 | Cache need not survive restart | Cold start costs at most one slower cycle | Medium |
| NFR-017 | Repeated analysis of the same company within a cycle makes one provider call, not many | Deduplicated within cycle | High |
| NFR-018 | Price history sufficient for the longest indicator period is retained locally | ≥ 200 sessions | High |

## Rationale

NFR-013 is a commercial requirement wearing technical clothing. Company financials change quarterly; caching them for weeks eliminates the overwhelming majority of fundamentals requests on a thirty-minute cycle. Getting these durations right is the difference between free allowances being adequate and being exhausted before lunch. It matters considerably more than the choice of caching mechanism.

NFR-014 is why reference data is deliberately excluded from user isolation. Scoping prices and financials per user would multiply subscription costs by user count and defeat caching entirely — a correctness-shaped decision with a purely commercial motivation.

NFR-015 is a hardware constraint, not tidiness. On a 1 GB device an unbounded cache is a slow memory leak with a delayed failure.

## Verification

- Provider call volume measured with and without caching enabled; the difference is the requirement.
- Memory footprint observed under sustained operation on the target device.

## Related

- `docs/hlds/mvp/ladrs/011-memory-only-caching.md`
- `docs/hlds/mvp/ladrs/010-user-identity-from-first-release.md`
- BR-45 (reuse infrequently-changing data), BR-48 (shared reference data)
