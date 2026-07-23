# LADR-013: Abstracted AI reasoning provider

**Status:** Accepted
**Date:** July 2026

## Context

The recommendation engine uses a language model to rank candidates and produce rationale, bull and bear cases, risks and a suggested holding period.

The stated requirement is provider *and* model selection per task, across both major vendors, for cost and reasoning-depth optimisation — a cheaper model for routine scans, a stronger one for final analysis.

An intermediate architecture proposal named a single vendor as the reasoning layer. This was explicitly retracted by the owner as an error.

## Decision

**Abstract at the reasoning boundary.** The application layer depends on an interface; provider and model are resolved from configuration.

Both major vendors are supported. The first implementation uses one vendor's SDK, with the other adopted if the first proves inadequate. Model identifier and provider endpoint live in deployment configuration; credentials live in environment variables.

## Rationale

Both vendors' official .NET SDKs expose a common chat abstraction, so the interface is a natural seam rather than an invented one. Configuration-driven selection means changing provider or model is a deployment change, not a code change — which is what per-task cost optimisation requires in practice.

Note that this delivers chat with tool calling, not an agent framework. The reasoning loop, its state and its persistence belong to the application.

## Consequences

- Spend ceiling and credit-exhaustion handling live in the adapter, so features never encounter a billing concern.
- At most ten candidates reach reasoning per cycle, so cost is bounded regardless of provider choice.
- **Vendor-specific capabilities are not available through a common interface.** Anything one vendor offers and the other does not sits outside the abstraction, or forces a leak.

## Open

A routing gateway offering a single vendor-compatible endpoint across many models would collapse this to one client plus a model identifier in configuration — simpler, and a natural fit for per-task routing, at the cost of vendor-native features. Unresolved; decide before the recommendation engine milestone.
