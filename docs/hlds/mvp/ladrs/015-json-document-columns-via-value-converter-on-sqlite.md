# LADR-015: Structured documents as JSON text via a value converter on SQLite

**Status:** Accepted
**Date:** July 2026

## Context

LADR-010 introduces a user-metadata document, and NFR-048 requires that document to carry an explicit version marker. NFR-045 resolves every tunable value as *user preference first, application default second*, so the metadata document is read and written **as a whole per user** — it is never queried field-by-field.

The open question (T-015 / #59) was the production representation of that document on EF Core + SQLite: a **native provider mapping** versus a **custom converter/serializer**. The two concrete candidates were EF Core's owned-entity JSON mapping (`OwnsOne(...).ToJson()`) and a custom `ValueConverter<TDocument, string>` serializing to JSON `TEXT`.

The representation must round-trip losslessly, carry an explicit schema version, safely retain forward-compatible/unknown fields written by a newer version, stay inspectable, keep migration cost low, and be provable against a real SQLite file (LADR-002, LADR-014, NFR-069–074).

## Decision

Persist a versioned structured document as a **single canonical JSON `TEXT` column** through a reusable `VersionedDocumentSqliteValueConverter<TDocument>` backed by `System.Text.Json`.

- `TDocument` must implement the Domain's `IVersionedDocument` — an explicit `int SchemaVersion` member (NFR-048). The versioning rule is enforced at the type level without making Domain depend on Infrastructure.
- The document should retain unknown members through a `[JsonExtensionData]` property, so a field written by a newer schema version survives a read-modify-write cycle.
- The stored serialization contract is fixed by `SqliteJsonSerialization.Default` (`Infrastructure/Persistence/Converters/SqliteJsonSerialization.cs`; camelCase names, compact output). Changing those options changes the on-disk representation and is a schema-version concern.
- The converter is applied **per property** in the owning entity's model configuration. It is not a global convention (unlike the LADR-014 NodaTime converters), because only document-typed properties are mapped this way.
- Each mapped mutable document also receives `VersionedDocumentSqliteValueComparer<TDocument>`, which snapshots and compares canonical JSON so an in-place edit is detected by EF Core change tracking.

## Alternatives considered

**EF Core native owned-entity JSON mapping (`OwnsOne(...).ToJson()`).** Rejected:

- It is an EF-owned graph whose persistence and update behavior is driven by mapped CLR properties; it provides no opaque-payload contract for retaining unknown members via `[JsonExtensionData]`. That is insufficient for this document's forward-compatibility requirement.
- It scatters the document's shape across an EF-owned entity graph and ties its evolution to EF model changes and migrations; the version marker becomes just another owned column rather than a self-describing payload field.
- The SQLite provider's support for querying into JSON columns is limited, and this document does not need it — it is resolved wholesale per user.

**A document store (LiteDB/RavenDB/Redis) or a second persistence mechanism.** Rejected: out of bounds — LADR-002 keeps SQLite the single store. A JSON `TEXT` column adds no second mechanism.

## Consequences

- The document stays an opaque, self-versioned, inspectable text payload, consistent with the lossless-text approach in LADR-014. `decimal`, `int`, and `string` preferences round-trip exactly.
- Adding a preference is a **document-version change, not an EF model migration**. No column migration is required until a feature adds the property that carries the document.
- Forward compatibility is the document's responsibility (its `[JsonExtensionData]` member), not the converter's; a document without one will drop unknown fields.
- The downstream user-metadata work will define a Domain type implementing `IVersionedDocument` (with a `[JsonExtensionData]` member for forward-compatible fields) and apply both `VersionedDocumentSqliteValueConverter<TMetadata>` and `VersionedDocumentSqliteValueComparer<TMetadata>` to the owning property. The versioning rule and serialization contract are settled here; the downstream work chooses the document's shape.
- A real-file SQLite component test proves the version marker, representative preference values, unknown-field retention across a read-modify-write cycle, and the inspectable `text` column. No EF InMemory or live provider is used.
