# Lightweight Architecture Decision Records

Decisions taken during requirements gathering for **smooth-ai-stockanalysis**, recorded so that the reasoning survives the conversation that produced it.

These are *lightweight* records: short, one decision each, written to be read in under two minutes. They state what was decided and — more importantly — what was rejected and why, so that a decision is not silently reversed by someone who never met the argument.

## Format

Each record carries a status, the context that forced a choice, the decision itself, the alternatives weighed, and the consequences accepted. Where a decision is provisional, it names the condition under which it should be revisited.

| Status | Meaning |
|---|---|
| **Accepted** | In force. Build to this. |
| **Completed** | Implemented for the scope recorded in the decision. |
| **Deferred** | Considered and consciously postponed. Revisit condition stated. |
| **Superseded** | Replaced. The replacing record is named. |

## Index

| # | Decision | Status |
|---|---|---|
| [LADR-001](001-clean-architecture-with-vertical-slices.md) | Clean architecture with vertical feature slices | Completed |
| [LADR-002](002-on-disk-sqlite-over-in-memory-snapshots.md) | On-disk SQLite over in-memory with periodic snapshots | Completed |
| [LADR-003](003-defer-managed-workflow-orchestration.md) | Defer managed workflow orchestration | Deferred |
| [LADR-004](004-compute-technical-indicators-internally.md) | Compute technical indicators internally | Accepted |
| [LADR-005](005-event-driven-funnel-over-valuation-screening.md) | Event-driven funnel over valuation-led screening | Accepted |
| [LADR-006](006-one-time-fork-of-template.md) | One-time fork of the development template | Completed |
| [LADR-007](007-visible-docs-folder.md) | Visible documentation folder | Completed |
| [LADR-008](008-direct-agent-rules-over-path-scoped.md) | Direct agent rules over path-scoped rules | Accepted |
| [LADR-009](009-email-as-sole-delivery-channel.md) | Email as sole delivery channel | Accepted |
| [LADR-010](010-user-identity-from-first-release.md) | User identity and isolation from first release | Accepted |
| [LADR-011](011-memory-only-caching.md) | Memory-only caching, no cache server | Accepted |
| [LADR-012](012-liquidity-median-excluding-catalyst-day.md) | Liquidity measured excluding the catalyst day | Accepted |
| [LADR-013](013-abstracted-ai-reasoning-provider.md) | Abstracted AI reasoning provider | Accepted |
| [LADR-014](014-lossless-nodatime-mappings-for-sqlite.md) | Lossless NodaTime mappings for SQLite | Completed |

## Related documents

- `docs/brd.md` — business requirements and delivery milestones
- `docs/wiki/hld.md` — high level design
- `docs/hlds/mvp/nfr/` — non-functional requirements
