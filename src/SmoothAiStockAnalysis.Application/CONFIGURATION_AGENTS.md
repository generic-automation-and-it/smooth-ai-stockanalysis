# APPLICATION/CONFIGURATION_AGENTS.md

## TL;DR

The F-004 settings catalogue façade, the immutable `EffectiveSettings` snapshot, and the `ISettingsResolver` that turns *user preference or application default* into the one view feature code consumes (NFR-045, HLD §7.2).

## Non-Negotiables

- **`ISettingsResolver` is the only sanctioned way for feature code to read effective settings** (NFR-045). Ad-hoc `if (pref)` branching in features is prohibited and would defeat the single-pattern design that lets the Phase 3 dashboard edit user metadata directly.
- **Override semantics are nullable-based for typed values**: a `null` preference is the explicit "unset" signal that falls through to the application default. Numeric zero and `TimeSpan.Zero` are valid business overrides, never sentinels.
- **String overrides treat blank as unset at resolve time**: `SettingsResolver` uses `string.IsNullOrWhiteSpace` for provider names/models and delivery-window strings, so whitespace-only overrides fall through to the catalogue default. Domain still stores the raw string if written; clearing an override is done by persisting `null` via a full `WithPreferences` snapshot replace.
- **`UserMetadata.WithPreferences` is a full snapshot replace** (see Domain AGENTS): omitted/`null` arguments unset those fields on the new instance rather than preserving prior values.
- **`EffectiveSettings` is an immutable DTO snapshot**, not an `IOptions` wrapper — feature code receives a stable value at the start of a unit of work and reads it without surprise.
- **No credentials in the catalogue**: `IApplicationDefaults` carries non-secret tunables only (NFR-043/044). The `Provider` section exposes provider names and model identifiers; secrets arrive in worktask 03 (T-027 / #71) via environment variables.

## Key Behaviors

- The merge in `SettingsResolver.Resolve(IApplicationDefaults, UserMetadata)` is a **pure function**: no I/O, no DI, no `IOptions`. It exists so unit tests can exercise the merge without faking the resolver's collaborators.
- The orchestration `SettingsResolver.ResolveForUserAsync(userId, ct)` calls `IUserMetadataProvider.GetForUserAsync` (a port implemented in Infrastructure) and forwards the metadata to the pure merge. A non-positive `userId` throws immediately so background work never accidentally resolves a "default" user (LADR-010, NFR-041).
- The `DeliveryWindow` override is composed from three string properties (`DeliveryWindowTimeZoneId`, `DeliveryWindowStart`, `DeliveryWindowEnd`); a partial override (any of the three set, others unset) substitutes the supplied values and falls through to the catalogue default for the missing ones. Malformed `HH:mm` strings and unknown TZDB IANA zones throw `ArgumentException` so an invalid override fails the cycle rather than silently producing a 24/7 window.
- `IApplicationDefaults.GetDefaultDeliveryWindow()` returns the `DeliveryWindow` materialised once when Host composes the catalogue façade (eager NFR-047 validation). The resolver returns that same instance on the default path and builds a new window only when the user supplies a delivery-window override.
- The resolver is registered as **Scoped** so it shares the per-unit-of-work lifetime with `IUserMetadataProvider` and the underlying DbContext (LADR-017). Singleton would fail DI scope validation.

## Catalogue Contract

The façade mirrors the five Host `Configuration/` sections:

| Property | Type | Notes |
|---|---|---|
| `Analysis` | `AnalysisDefaults` | Company size floor, liquidity thresholds, scoring weightings, holding horizon. |
| `CostCaps` | `CostCaps` | Per-cycle stage caps; defaults follow NFR-025 (50/20/10/5). |
| `FxMultipliers` | `FxMultipliers` | Static USD→target multipliers; refresh deferred (NFR-050). |
| `Cycle` | `CycleDefaults` | Interval, delivery-window TZ, start, end. |
| `Provider` | `ProviderDefaults` | Non-secret provider and model selection (NFR-021, NFR-043/044). |
| `GetDefaultDeliveryWindow()` | `DeliveryWindow` | Returns the catalogue's default `DeliveryWindow` for the override-fall-through path. |

`EffectiveSettings` mirrors the same five shapes plus the resolved `DeliveryWindow`, so feature code can pick the field it needs without navigating a separate config object.

## Test References

- **L0:** `Application.UnitTest/EffectiveSettingsTests.cs` exercises the pure merge across every typed shape: empty metadata resolves to defaults; full override replaces every value; partial override falls through per field; zero/`TimeSpan.Zero` overrides are honoured; `DeliveryWindow` override substitutes supplied strings and falls through for the unspecified TZ; malformed `HH:mm` and unknown TZDB zones throw; blank string provider overrides fall through; null decimal and null int overrides fall through; `ResolveForUserAsync` rejects non-positive ids and merges metadata loaded from the port.
- **L0:** `Domain.UnitTest/UserMetadataTests.cs` covers v2 metadata creation, all-preferences-null, `WithPreferences` full-replace/clear semantics, and `Reconstitute` accepting any positive schema version (so legacy v1 documents read back as v1 with no preferences).

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-07-24 | Added the F-004 settings catalogue façade (`IApplicationDefaults`), the immutable `EffectiveSettings` DTO, and the `ISettingsResolver` (pure merge function + scoped orchestrator) wired through `AddApplication`. | #68, #69 |
| 2026-07-24 | Clarified blank-string fall-through, zero-override semantics, full-replace `WithPreferences`, and eager default-window composition. | #68, #69 |
