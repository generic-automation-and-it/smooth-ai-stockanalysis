# DOMAIN_AGENTS.md

## TL;DR

Pure domain model — entities, aggregate roots, and value objects. Zero external dependencies and no I/O.

## Non-Negotiables

- **No outward dependencies.** Domain references no other project and no infrastructure packages (EF Core, ASP.NET, HTTP, serialization). It is the innermost Clean Architecture layer — everything depends on it, it depends on nothing.
- **No I/O or framework concerns.** No persistence, network, logging, or DI registration here — those belong in Infrastructure/Host.
- **Enforce invariants at construction.** Guard required state in constructors/factory methods so an entity cannot exist in an invalid state. Value objects are immutable and compared by value.

## Key Behaviors

- `Documents/IVersionedDocument.cs` is the dependency-neutral contract for a domain document whose serialized shape evolves independently of its database column. It owns the meaning of `SchemaVersion`; Infrastructure serializes it through LADR-015 without introducing an outward Domain dependency.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — empty Clean Architecture domain skeleton (`Entities/`, `ValueObjects/`). | — |
| 2026-07-24 | Added the dependency-neutral versioned-document contract consumed by the SQLite JSON persistence adapter. | #59 |
