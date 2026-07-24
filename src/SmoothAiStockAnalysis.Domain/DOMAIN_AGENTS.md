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
- `UserMetadata` implements `IVersionedDocument`. Its current document contract is version `2`, which adds the typed preference fields resolved by the F-004 two-layer resolver (NFR-045). All preference fields are nullable on the Domain model: a `null` preference is the explicit "unset" signal that the resolver maps to the application default; zero and empty are never sentinels.
- Domain metadata remains serialization-free. It does not carry `[JsonExtensionData]`, `JsonElement`, serializer options, EF annotations, or SQLite representations. Infrastructure owns the persistence document and translates it explicitly to and from the Domain model. The `WithPreferences` factory returns a new immutable instance, so Domain code never mutates a metadata object — Infrastructure owns the apply-state side of the document round-trip.

### F-004 settings catalogue preference shape

- `UserMetadata` carries one nullable typed property per catalogue key declared in `src/SmoothAiStockAnalysis.Host/Configuration/`. The catalogue keys, with their default values, are the canonical list:

  | Catalogue key | Domain property | Type | Default |
  |---|---|---|---|
  | `Analysis:CompanySizeFloor` | `CompanySizeFloor` | `decimal?` | 250,000,000 |
  | `Analysis:MinAverageDailyVolume` | `MinAverageDailyVolume` | `decimal?` | 100,000 |
  | `Analysis:MinDaysTraded` | `MinDaysTraded` | `int?` | 30 |
  | `Analysis:ScoringWeightEvent` | `ScoringWeightEvent` | `decimal?` | 0.50 |
  | `Analysis:ScoringWeightFundamental` | `ScoringWeightFundamental` | `decimal?` | 0.30 |
  | `Analysis:ScoringWeightSentiment` | `ScoringWeightSentiment` | `decimal?` | 0.20 |
  | `Analysis:HoldingHorizonDays` | `HoldingHorizonDays` | `int?` | 90 |
  | `CostCaps:Event` | `CostCapEvent` | `int?` | 50 |
  | `CostCaps:Fundamental` | `CostCapFundamental` | `int?` | 20 |
  | `CostCaps:Reasoning` | `CostCapReasoning` | `int?` | 10 |
  | `CostCaps:Delivery` | `CostCapDelivery` | `int?` | 5 |
  | `FxMultipliers:UsdEur` | `FxUsdEur` | `decimal?` | 0.92 |
  | `FxMultipliers:UsdGbp` | `FxUsdGbp` | `decimal?` | 0.79 |
  | `FxMultipliers:UsdJpy` | `FxUsdJpy` | `decimal?` | 150.0 |
  | `Cycle:Interval` | `CycleInterval` | `TimeSpan?` | `00:15:00` |
  | `Cycle:DeliveryWindowTimeZoneId` | `DeliveryWindowTimeZoneId` | `string?` | `Europe/Paris` |
  | `Cycle:DeliveryWindowStart` | `DeliveryWindowStart` | `string?` | `07:00` |
  | `Cycle:DeliveryWindowEnd` | `DeliveryWindowEnd` | `string?` | `22:00` |
  | `Provider:Reasoning` | `ProviderReasoning` | `string?` | `OpenAI` |
  | `Provider:ReasoningModel` | `ReasoningModel` | `string?` | `gpt-4o-mini` |
  | `Provider:MarketData` | `ProviderMarketData` | `string?` | `OpenAI` |
  | `Provider:MarketDataModel` | `MarketDataModel` | `string?` | `gpt-4o-mini` |

- The Domain preference shape is forward-compatible: a persisted v1 metadata (pre-F-004) reads back through `UserMetadata.Reconstitute` with `SchemaVersion == 1` and all preferences unset, so the resolver treats it as "no overrides" until the next save. No v1→v2 migration is applied on read because the additive change makes the legacy payload's null-preference state the correct fall-through behaviour (LADR-015).

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — empty Clean Architecture domain skeleton (`Entities/`, `ValueObjects/`). | — |
| 2026-07-24 | Added the dependency-neutral versioned-document contract consumed by the SQLite JSON persistence adapter. | #59 |
| 2026-07-24 | Added the dual-identifier `User` model and version-1 serialization-free Domain metadata with invariant-preserving creation and reconstitution. | #60 |
| 2026-07-24 | Bumped metadata to schema version 2 with the F-004 typed preference fields (one nullable property per catalogue key) and added the immutable `WithPreferences` factory. | #68, #69 |
