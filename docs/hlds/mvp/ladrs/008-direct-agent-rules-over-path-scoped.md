# LADR-008: Direct agent rules over path-scoped rules

**Status:** Accepted — revisit condition stated below
**Date:** July 2026

## Context

A reference project organises its AI agent rules by scope, with separate rule sets activated according to which paths an agent is working in — a backend set, and others alongside it. The question was whether to adopt that arrangement or keep rules direct and always loaded.

## Decision

**Keep rules direct.** All agent rules load always. No path scoping in Phase 1.

## Rationale

Path scoping earns its complexity when an agent works across genuinely different technology stacks and the context budget needs protecting from irrelevant guidance.

Phase 1 is backend-only .NET. A backend-scoped rule set would match essentially every file in the repository — paying the indirection while receiving none of the benefit.

Scoping also introduces a failure mode that direct rules do not have: a rule silently fails to load because a path did not match, and nobody notices until the output is wrong in a way nobody can explain.

## Consequences

- Every rule applies to every agent interaction. This is correct while there is one stack, and becomes wrong when there are two.
- The rule set must stay small enough that always-loading it does not crowd out the working context. If rules grow large, revisit early.
- Reversal is cheap. The rules themselves are unchanged; only their activation moves.

## Revisit when

Phase 3 introduces the user dashboard and, with it, frontend paths — at which point the two stacks make scoping worth its cost.
