# LADR-009: Email as sole delivery channel

**Status:** Accepted
**Date:** July 2026

## Context

Recommendations and alerts were originally to be delivered by both email and WhatsApp, on the reasoning that a phone notification reaches the user faster than an inbox.

## Decision

**Email only.** WhatsApp is out of scope.

## Rationale

WhatsApp's business messaging interface requires business verification with its operator and pre-approved message templates for outbound messages. For a single-user personal tool this is disproportionate — a verification and approval process standing between the system and a notification to its own author.

The urgency argument also weakens under examination: the holding horizon is days to two weeks. A message arriving by inbox rather than by phone alert costs minutes on a decision measured in days.

## Alternatives considered

**A messaging sandbox via a third-party gateway.** Rejected as a workaround carrying its own account, cost and constraints.

**Telegram or Signal**, both dramatically simpler to send to. Not adopted, but noted as the obvious candidates should a phone channel later prove genuinely necessary.

## Consequences

- One delivery path to build, test and monitor.
- Failure notification uses the same channel — which means a channel failure is also invisible. Accepted: no high-availability alerting is required.
- **Phase 2 reuses this channel inbound**, when the user replies stating what was bought and sold. Email being the sole channel makes that a natural extension rather than a second integration.
