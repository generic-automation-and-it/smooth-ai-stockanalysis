# LADR-010: User identity and isolation from first release

**Status:** Accepted
**Date:** July 2026

## Context

Phase 1 serves exactly one user, with one outbound email address and no authentication — the system runs on a private home network. A dashboard with sign-in and role-based access is planned for Phase 3.

The question was whether to build single-user now and introduce user identity when it is needed.

## Decision

**Build multi-user-ready from the first release.** A user record with metadata, a user reference on every user-owned entity, and isolation enforced globally at the data-access layer.

Phase 1 seeds a single user. Nothing assumes there is only one.

Delivered in the foundation milestone, not deferred.

## Rationale

Retrofitting a tenant key across an established schema is among the more painful migrations available — every table, every query, every unique constraint, with no safe intermediate state. Adding the column while there are no rows costs almost nothing.

It also aligns naturally with the planned authentication work, which then adds a front door rather than a data model.

## Consequences

**A shared-versus-owned split must be decided per table.** Market data, company financials, news, computed indicators and sector aggregates are *shared* — scoping them per user would multiply subscription costs by user count and defeat the caching strategy entirely. Watchlists, analysis history, recommendations, alerts and preferences are *owned*.

**There is no ambient user.** Everything runs in a background worker on a timer, so there is no request from which to infer the current user. Two things follow: an explicit user scope set by the pipeline when producing owned results, and a deliberate system scope for shared ingestion that bypasses the filter. This is the part that leaks if it is not designed on purpose.

**Uniqueness constraints become composite** — scoped by user rather than global. Free now, a migration later.

**The user's metadata is a versioned document.** It is stored as a JSON `TEXT` column carrying an explicit schema-version marker (NFR-048) through the value-converter representation decided in [LADR-015](015-json-document-columns-via-value-converter-on-sqlite.md), not as a native provider mapping. This keeps preferences an opaque, forward-compatible payload rather than an EF-owned entity graph.

Isolation is a property of the data layer rather than of each developer's diligence.
