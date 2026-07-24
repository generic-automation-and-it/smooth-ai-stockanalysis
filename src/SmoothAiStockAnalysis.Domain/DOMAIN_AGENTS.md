# DOMAIN_AGENTS.md

## TL;DR

Pure domain model — entities, aggregate roots, and value objects. Zero external dependencies and no I/O.

## Non-Negotiables

- **No outward dependencies.** Domain references no other project and no infrastructure packages (EF Core, ASP.NET, HTTP, serialization). It is the innermost Clean Architecture layer — everything depends on it, it depends on nothing.
- **No I/O or framework concerns.** No persistence, network, logging, or DI registration here — those belong in Infrastructure/Host.
- **Enforce invariants at construction.** Guard required state in constructors/factory methods so an entity cannot exist in an invalid state. Value objects are immutable and compared by value.

## Key Behaviors

- `Documents/IVersionedDocument.cs` is the dependency-neutral contract for a domain document whose serialized shape evolves independently of its database column. It owns the meaning of `SchemaVersion`; Infrastructure serializes it through LADR-015 without introducing an outward Domain dependency.

## Requirements

### User identity foundation

- The Domain `User` owns two identifiers with different purposes: a database-assigned `long Id` used internally by relationships, and a non-empty, stable `Guid UniqueIdentifier` that may be exposed outside the persistence boundary. The GUID is an identifier, not an authentication credential or authorization token.
- A newly created user has the normal transient `Id == 0`; reconstituting a persisted user requires a positive internal ID. Creation and reconstitution APIs reject an empty external identifier or missing metadata.
- `UserMetadata` implements `IVersionedDocument`. Its initial document contract is version `1` and contains no preference business fields; later features add preferences without moving serialization into Domain.
- Domain metadata remains serialization-free. It does not carry `[JsonExtensionData]`, `JsonElement`, serializer options, EF annotations, or SQLite representations. Infrastructure owns the persistence document and translates it explicitly to and from the Domain model.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — empty Clean Architecture domain skeleton (`Entities/`, `ValueObjects/`). | — |
| 2026-07-24 | Added the dependency-neutral versioned-document contract consumed by the SQLite JSON persistence adapter. | #59 |
| 2026-07-24 | Added the dual-identifier `User` model and version-1 serialization-free Domain metadata with invariant-preserving creation and reconstitution. | #60 |
