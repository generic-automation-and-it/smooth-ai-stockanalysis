# LADR-001: Clean architecture with vertical feature slices

**Status:** Completed
**Date:** July 2026

## Context

The solution needs a structure that holds up as thirteen milestones are added to it, and that a fresh contributor — human or AI agent — can navigate without a tour.

A material share of the code will be authored by AI coding agents working on one feature at a time. Agents perform noticeably better against code that is explicit, well named and close to the domain than against heavily abstracted code where behaviour is assembled from indirection. Whatever structure is chosen has to make a single feature's full extent visible in one place.

The adopted template already ships this arrangement, and the reference project reports the combination working well in exactly this setting.

## Decision

Two patterns, at two different levels.

**Clean architecture at the solution level.** Four projects — Domain, Application, Infrastructure, Host — with dependencies pointing inward. The domain knows nothing of providers, storage or email.

**Vertical feature slices inside the Application layer.** Each feature lives in its own folder holding its request, handler, validation and response together, rather than being distributed across technical folders named for what things *are*.

## Alternatives considered

**Technical layering throughout** — folders for Handlers, Validators, Models. Rejected: a single feature scatters across the tree, so every change touches four directories and agents must reconstruct the feature by search.

**Vertical slices alone, without the outer architecture.** Rejected: nothing then prevents a slice reaching directly for a provider client or a database context, and the domain stops being independently testable.

## Consequences

- Dependency direction is enforced structurally, not by convention or review.
- A feature's blast radius is one folder. This matters most when an agent is editing it unattended.
- Some duplication across slices is expected and accepted. Two slices doing similar things is cheaper than a shared abstraction that neither fits.
- Premature extraction of shared helpers is discouraged. Wait for the third occurrence.
- Domain calculations — indicators, scoring, liquidity, currency conversion — stay pure and carry the heaviest test weight.
