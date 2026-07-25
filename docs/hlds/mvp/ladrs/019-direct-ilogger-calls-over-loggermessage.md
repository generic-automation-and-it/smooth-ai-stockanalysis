# LADR-019: Direct ILogger calls over LoggerMessage delegates

**Status:** Accepted
**Date:** July 2026

## Context

Enabling the SDK analyzers as a build gate surfaced CA1848 on every `ILogger.Log*` extension call — twenty diagnostics across roughly ten call sites, all of them Infrastructure startup, seeding, retention, and unit-of-work rollback paths.

CA1848's remedy is source-generated `LoggerMessage` partial methods: a declared partial signature per message, an attribute carrying the event id, level, and template, and a generated implementation. It exists to avoid boxing and template parsing on hot logging paths.

## Decision

Set `dotnet_diagnostic.CA1848.severity = suggestion` in `.editorconfig`. Keep explicit `logger.LogInformation` / `LogDebug` / `LogError` call sites.

## Rationale

The performance argument is real but does not apply here. These are not hot paths — they log once at startup, once per retention sweep, once per rollback. The allocation CA1848 eliminates is measured against logging volumes this system does not produce.

The cost, by contrast, does apply. Every message becomes a partial-method declaration separated from its call site, which is precisely the indirection NFR-092 disfavours for a codebase whose maintenance is largely AI-authored. A reader following a startup failure would move from the call site to a declaration to a generated implementation to recover a string that was previously in front of them.

Suppressing this at the severity level rather than per call site keeps the decision in one reviewable place, and leaves the analyzer visible as a suggestion so a genuinely hot logging path can still adopt `LoggerMessage` deliberately.

## Consequences

- Logging stays readable at the call site, and backend logging conventions remain concerned with level selection rather than mechanism.
- High-volume logging introduced later can adopt `LoggerMessage` on its own merits without flipping the repository-wide severity — but doing so should update this record.
- The build gate does not enforce .NET's documented logging performance guidance. This is a deliberate NFR-092 interpretation, not an oversight, and NFR-094 requires it to stay recorded here.
