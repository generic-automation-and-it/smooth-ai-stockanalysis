# LADR-006: One-time fork of the development template

**Status:** Accepted
**Date:** July 2026

## Context

The project starts from an internal .NET development template providing clean architecture, source-generated messaging, validation pipeline, structured logging and tracing, interactive API documentation, a test stack, and agent configuration shared across several AI coding tools.

Several of the project's requirements diverge from it. The template ships PostgreSQL provisioned through an orchestration tool requiring a container runtime; this project needs SQLite on a 1 GB device with no production container runtime. Aspire is retained only in the test stack to provision WireMock for external-HTTP tests. The documentation folder is hidden; this repository is public and wants it visible.

Whether these divergences are cheap or expensive depends entirely on one question: will the project track upstream changes?

## Decision

**One-time fork. No upstream tracking.** The template is a starting point, not a dependency.

## Rationale

Tracking upstream would make every structural divergence a recurring merge conflict, and would argue for tolerating the PostgreSQL and hidden-documentation choices rather than changing them. Without tracking, both become free.

The template is a productivity accelerator for day one, not a shared platform.

## Consequences

- Free to swap PostgreSQL for SQLite, remove production and database-container orchestration while retaining a WireMock-only test AppHost, rename throughout, and restructure at will — with no future merge cost.
- **This decision unlocks LADR-002 and LADR-007.** Both would be questionable under upstream tracking.
- Upstream improvements and fixes will not arrive automatically. Anything worth having must be ported deliberately.
- The template is placeholder-named throughout its solution and project files, so a rename pass is a real first task rather than a detail.
