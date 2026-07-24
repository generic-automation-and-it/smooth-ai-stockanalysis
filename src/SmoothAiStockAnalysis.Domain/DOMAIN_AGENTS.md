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
- `UserMetadata` implements `IVersionedDocument`. Its current document contract is version `2`, which adds the typed preference fields resolved by the F-004 two-layer resolver (NFR-045). All preference fields are nullable on the Domain model: a stored `null` is the only explicit "unset" signal. Numeric zero and `TimeSpan.Zero` are real overrides, never sentinels. For string preferences, Domain stores the value as written; at resolve time `SettingsResolver` treats blank/whitespace strings as unset and falls through to the catalogue default (see [`CONFIGURATION_AGENTS.md`](../SmoothAiStockAnalysis.Application/CONFIGURATION_AGENTS.md)).
- Domain metadata remains serialization-free. It does not carry `[JsonExtensionData]`, `JsonElement`, serializer options, EF annotations, or SQLite representations. Infrastructure owns the persistence document and translates it explicitly to and from the Domain model. The `WithPreferences` factory returns a new immutable instance, so Domain code never mutates a metadata object — Infrastructure owns the apply-state side of the document round-trip.

### F-004 settings catalogue preference shape

- `UserMetadata` carries one nullable typed property per catalogue key declared in Host `Configuration/` (see the canonical defaults table in [`HOST_AGENTS.md`](../SmoothAiStockAnalysis.Host/HOST_AGENTS.md) — Domain does not own default literals).
- Property map (override shape only; defaults live in deployment config):

  | Catalogue key | Domain property | Type |
  |---|---|---|
  | `Analysis:CompanySizeFloor` | `CompanySizeFloor` | `decimal?` |
  | `Analysis:MinAverageDailyVolume` | `MinAverageDailyVolume` | `decimal?` |
  | `Analysis:MinDaysTraded` | `MinDaysTraded` | `int?` |
  | `Analysis:ScoringWeightEvent` | `ScoringWeightEvent` | `decimal?` |
  | `Analysis:ScoringWeightFundamental` | `ScoringWeightFundamental` | `decimal?` |
  | `Analysis:ScoringWeightSentiment` | `ScoringWeightSentiment` | `decimal?` |
  | `Analysis:HoldingHorizonDays` | `HoldingHorizonDays` | `int?` |
  | `CostCaps:Event` | `CostCapEvent` | `int?` |
  | `CostCaps:Fundamental` | `CostCapFundamental` | `int?` |
  | `CostCaps:Reasoning` | `CostCapReasoning` | `int?` |
  | `CostCaps:Delivery` | `CostCapDelivery` | `int?` |
  | `FxMultipliers:UsdEur` | `FxUsdEur` | `decimal?` |
  | `FxMultipliers:UsdGbp` | `FxUsdGbp` | `decimal?` |
  | `FxMultipliers:UsdJpy` | `FxUsdJpy` | `decimal?` |
  | `Cycle:Interval` | `CycleInterval` | `TimeSpan?` |
  | `Cycle:DeliveryWindowTimeZoneId` | `DeliveryWindowTimeZoneId` | `string?` |
  | `Cycle:DeliveryWindowStart` | `DeliveryWindowStart` | `string?` |
  | `Cycle:DeliveryWindowEnd` | `DeliveryWindowEnd` | `string?` |
  | `Provider:Reasoning` | `ProviderReasoning` | `string?` |
  | `Provider:ReasoningModel` | `ReasoningModel` | `string?` |
  | `Provider:MarketData` | `ProviderMarketData` | `string?` |
  | `Provider:MarketDataModel` | `MarketDataModel` | `string?` |

- `WithPreferences(...)` is a **full preference snapshot replace**, not a field-level patch: every argument is written through as given, and an omitted/explicit `null` argument means the corresponding preference is **unset** on the result (it does not keep the previous value). Callers that need a partial update must restate preferences they want to retain. This is what makes "clear override → fall through to default" expressible on an immutable document.
- The Domain preference shape is forward-compatible: a persisted v1 metadata (pre-F-004) reads back through `UserMetadata.Reconstitute` with `SchemaVersion == 1` and all preferences unset, so the resolver treats it as "no overrides" until the next save. No v1→v2 migration is applied on read because the additive change makes the legacy payload's null-preference state the correct fall-through behaviour (LADR-015).

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — empty Clean Architecture domain skeleton (`Entities/`, `ValueObjects/`). | — |
| 2026-07-24 | Added the dependency-neutral versioned-document contract consumed by the SQLite JSON persistence adapter. | #59 |
| 2026-07-24 | Added the dual-identifier `User` model and version-1 serialization-free Domain metadata with invariant-preserving creation and reconstitution. | #60 |
| 2026-07-24 | Bumped metadata to schema version 2 with the F-004 typed preference fields (one nullable property per catalogue key) and added the immutable `WithPreferences` factory. | #68, #69 |
| 2026-07-24 | Clarified `WithPreferences` as full preference-snapshot replace (null clears), and pointed default literals at Host catalogue docs only. | #68, #69 |
| 2026-07-24 | Aligned string-preference wording with resolver blank-as-unset fall-through (null remains the only stored unset). | #68, #69 |
