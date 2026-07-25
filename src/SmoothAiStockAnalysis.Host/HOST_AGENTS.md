# HOST_AGENTS.md

## TL;DR

ASP.NET Core composition root (Minimal API). Wires the application together and exposes endpoints — it holds no business logic.

## Non-Negotiables

- **Keep business logic out of Host.** Endpoints translate HTTP to a Mediator request and back; they contain no domain or orchestration logic.
- **One endpoint per use case** under `Endpoints/`; cross-cutting composition (DI, middleware, observability, problem-details) lives in `Configuration/`.
- **`Program` ends with `public partial class Program { }`** so integration tests can target it via `WebApplicationFactory<Program>`.
- **References Application, Domain, and Infrastructure** — it is the only project that composes all layers.

## Key Behaviors

- `Program.cs` is the composition root: after fail-fast `DefaultUser` validation it registers `AddInfrastructure`, eager F-004 `AddConfiguration`, `AddApplication`, and `IClock`. There are still **no mapped endpoints**, so any un-routed request returns `404` — that is what the Host integration smoke test asserts. Remaining composition (Serilog, OpenAPI/Scalar, health checks, endpoint mapping) lands with later features.
- Persistence enters through `AddInfrastructure(defaultUserUniqueIdentifier)`. That extension resolves the connection string only when EF creates its DbContext options, so configuration providers composed by `WebApplicationFactory` can select the isolated L2 database without replacing those options. The Host-validated GUID is registered as `DefaultUserSeedOptions` for the startup initializer.
- F-001 verified the solution dependency graph: `SmoothAiStockAnalysis.Domain` has no project references; `SmoothAiStockAnalysis.Application` references Domain; `SmoothAiStockAnalysis.Infrastructure` references Application and Domain to implement application contracts; and this Host references Application, Domain, and Infrastructure. No layer references Host and there are no cycles.

## Data-access scopes

- Host remains the composition root only: `AddInfrastructure(defaultUserUniqueIdentifier)` registers the scoped `IDataAccessScopeSetter` / `IDataAccessScope` / `ISystemDataAccessScope` and the DbContext that applies the global isolation filter.
- Host does not set a user scope itself. Background workers / future pipeline code set the scope deliberately after resolving the DI scope. No HTTP ambient user is assumed in Phase 1.

## Requirements

### Phase-1 default user configuration (T-022 / #66)

- Committed deployment configuration documents section `DefaultUser` with non-secret placeholder identity only (`DefaultUser:UniqueIdentifier`). Override at deploy time with `DefaultUser__UniqueIdentifier` (NFR-044, NFR-080). No credentials belong in this section (NFR-043).
- Host binds and validates the section at startup (same fail-fast family as the F-004 catalogue composition). Missing, empty, malformed, or `Guid.Empty` values throw before the host builds, and the exception message names `DefaultUser:UniqueIdentifier` (NFR-047). Validated identity is registered for Infrastructure seeding; Host does not write to the database.
- Phase 1 has **no authentication**. The configured GUID is a stable external identifier for the single seeded tenant (LADR-010), not a credential, session token, or authorization claim. Future sign-in adds a front door; it does not replace this seed contract.

### F-004 settings catalogue composition (T-024, T-025, T-026 / #68, #69, #70)

- `AddConfiguration(this IServiceCollection, IConfiguration)` **eagerly** binds five Host-owned section options (`Analysis`, `CostCaps`, `FxMultipliers`, `Cycle`, `Provider`) through each section's validating `FromConfiguration`, composes the `IApplicationDefaults` façade immediately, and registers **only** that instance as a singleton. It does **not** also register unvalidated `IOptions<section>` bindings for the catalogue — one validated path, no double-bind divergence (NFR-047). Composition fails fast if any section fails its range rules, if `Cycle:Interval` is missing/malformed/non-positive, or if the default delivery window (`Cycle:DeliveryWindowTimeZoneId` / `Start` / `End`) cannot form a valid `DeliveryWindow` — exception messages name the configuration path and do not echo invalid payload values (LADR-018). `AddApplication()` then registers the scoped `ISettingsResolver` over the façade. The composition root calls `AddConfiguration` once, after `DefaultUser` validation, before `AddApplication`.
- The Host-owned section options live in `Configuration/` and follow the `DefaultUser` options style: a `FromConfiguration(IConfiguration)` static bind, a `SectionName` const, and a `ToDefaults()` conversion that produces the corresponding Application `IApplicationDefaults` fragment. Catalogue sections (except the separate `DefaultUser` path) inherit `CatalogueSectionOptions`, an abstract base that owns the LADR-018 message helpers as **protected instance methods** so each section remains a real type with its own rules rather than a static-helper bag or extension-method dump. Defaults are documented in XML docs beside each property and in the table below (NFR-049).
- The catalogue exposes only non-secret tunables. The `Provider` section carries provider names and model identifiers (NFR-021) and has no API-key or token property; actual credentials arrive in worktask 03 (T-027 / #71) via environment variables.
- There is no standalone `DeliveryWindow` Host options class. The cycle section owns the window strings; `ApplicationDefaults` materialises the default `DeliveryWindow` once at composition; the resolver produces the effective per-user window (HLD §7.2 unification, NFR-045).
- `IApplicationDefaults` captures bound values at composition time. Deploy-time changes require a process restart (NFR-046 is satisfied by configuration/metadata, not by hot-reload of the singleton façade).
- Every catalogue section validates its own values inside `XxxOptions.FromConfiguration` after `Bind()`, so a broken section fails at the same composition call that binds it. Validation ranges:
  - `Analysis` — `CompanySizeFloor`, `MinAverageDailyVolume`, `MinDaysTraded`, `HoldingHorizonDays` must be strictly positive; `ScoringWeightEvent/Fundamental/Sentiment` must be in `[0, 1]`. The sum of weights is **not** enforced (NFR-046 keeps weights freely configurable; product may revisit).
  - `CostCaps` — `Event`, `Fundamental`, `Reasoning`, `Delivery` must each be strictly positive (NFR-025; a zero or negative cap would silently disable a stage, which the worktask-02 contract forbids).
  - `FxMultipliers` — `UsdEur`, `UsdGbp`, `UsdJpy` must each be strictly positive (NFR-050; zero or negative would invert the size-floor conversion).
  - `Provider` — every property is non-blank. The section is an allow-list of non-secret knobs only (NFR-021, NFR-043/044); property names are checked for credential-shape in the L0 test below.
  - `Cycle:Interval` — must be present, parseable as `hh:mm:ss`, and strictly positive (rejects blank/whitespace, malformed, and non-positive values). `FromConfiguration` caches the parsed `TimeSpan`; `ToDefaults` reuses that value (or parses once when the section was built outside the bind path). The delivery-window `TimeZoneId` / `Start` / `End` are validated when `ApplicationDefaults` is composed (TZDB zone lookup and `HH:mm` parse); this also fires inside `AddConfiguration`.
  - Every validation message names the `Section:Key` path and never echoes the offending value (LADR-018 / NFR-047). The composition root calls `AddConfiguration` exactly once, before `AddApplication` and before the host builds, so the failure throws before any background work starts.
  - Modern Host composition keeps a **single validated path**: bind + validate in `FromConfiguration`, compose `ApplicationDefaults` from those instances, register `IApplicationDefaults` only. Prefer this over mixing eager validation with a second unvalidated `services.Configure<section>()` bind (or over `IValidateOptions` + `ValidateOnStart` unless a live `IOptions<section>` consumer is required). `ApplicationDefaults` construction is **internal** (Host tests reach it via `InternalsVisibleTo`); feature code consumes `IApplicationDefaults` only.
- The L0 `AddConfiguration*` tests in `CatalogueOptionsTests` exercise the same `AddConfiguration(this IServiceCollection, IConfiguration)` extension that `Program.cs` calls, so the composition-time fail-fast is proved directly — including that catalogue section `IOptions<T>` bindings are not registered. A separate L2 host-factory test is unnecessary because `Program.cs`'s only call to validate-and-bind configuration is `AddConfiguration(builder.Configuration)`, which the L0 tests already cover.

### Provider credentials — env-only bind with committed placeholders (T-027, T-028 / #71, #72)

- The Host owns a separate `Credentials` section for provider API keys and other secret material (NFR-043). `CredentialsOptions` is **not** part of the `IApplicationDefaults` catalogue façade (credentials never enter the two-layer resolver). It binds from the same configuration sources as every other section, but the committed `appsettings.json` carries **placeholder tokens only**; real values arrive at deploy time from environment variables or, for local development, from `dotnet user-secrets` (the Host project declares `<UserSecretsId>smooth-ai-stockanalysis-host</UserSecretsId>`). ASP.NET Core's default configuration sources override JSON with env vars, so the deploy-time value wins without any code change (NFR-080).
- `AddConfiguration` binds `CredentialsOptions` and calls `Validate(ProviderOptions)` so each provider's credential is checked only when that provider is selected in the non-secret `Provider` section (validate-when-enabled). The committed placeholder token `{{CREDENTIALS__OPENAI__APIKEY}}` is treated as "not configured" and fails startup validation (NFR-047). Error messages name the configuration path and the environment variable; they never echo the bound value (LADR-018).
- `CredentialsOptions` is registered as a singleton so future Infrastructure clients can resolve it directly. It is intentionally not exposed via `IOptions<CredentialsOptions>` — the single validated path pattern applies to credentials too (NFR-047).
- The hierarchical env-var separator is `__` (double underscore), which ASP.NET Core maps to the `:` section separator on every platform.

| Configuration path | Environment variable | Purpose | Placeholder token |
|---|---|---|---|
| `Credentials:OpenAi:ApiKey` | `CREDENTIALS__OPENAI__APIKEY` | OpenAI API key (required when `Provider:Reasoning` or `Provider:MarketData` is `OpenAI`) | `{{CREDENTIALS__OPENAI__APIKEY}}` |

- Extending to additional providers (Anthropic, SMTP, etc.) adds a new property on `CredentialsOptions`, a matching env-var constant, a placeholder token, a row in this table, a case in `Validate`, and a committed placeholder in `appsettings.json`. The validate-when-enabled pattern is the same: the credential is checked only when the provider is selected.
- The L0 `CredentialsOptionsTests` prove bind, validate-when-enabled, placeholder rejection, blank rejection, non-OpenAI provider bypass, property allow-list, and no-echo of the bound value. The L0 `CommittedConfigurationGuardTests` scan the committed `appsettings.json` for the placeholder on every known credential key and for secret-shaped literals (`sk-`, `ghp_`, `sk-ant-`, `AKIA`, `AIza`, `github_pat_`) as defense-in-depth (NFR-007 verification). The guard walks up from the test output directory to the repository root (`smooth-ai-stockanalysis.slnx`) and reads the file from disk, so it runs on every `dotnet test` invocation and catches accidental secret commits before the PR opens.
- Local development story: `dotnet user-secrets set "Credentials:OpenAi:ApiKey" "sk-..."` or `export CREDENTIALS__OPENAI__APIKEY=sk-...` in the shell. `launchSettings.json` does **not** carry credentials (it is committed); it only sets `ASPNETCORE_ENVIRONMENT`.

## Catalogue Defaults Table

| Section key | Type | Default | NFR / source |
|---|---|---|---|
| `Analysis:CompanySizeFloor` | `decimal` | 250,000,000 | Sized floor for "small-cap" cutoff. |
| `Analysis:MinAverageDailyVolume` | `decimal` | 100,000 | Liquidity floor (LADR-012 median). |
| `Analysis:MinDaysTraded` | `int` | 30 | Minimum trading days. |
| `Analysis:ScoringWeightEvent` | `decimal` | 0.50 | Event-driven funnel weight (LADR-005). |
| `Analysis:ScoringWeightFundamental` | `decimal` | 0.30 | Fundamental weight. |
| `Analysis:ScoringWeightSentiment` | `decimal` | 0.20 | Sentiment weight. |
| `Analysis:HoldingHorizonDays` | `int` | 90 | Default holding horizon. |
| `CostCaps:Event` | `int` | 50 | NFR-025 first stage. |
| `CostCaps:Fundamental` | `int` | 20 | NFR-025 second stage. |
| `CostCaps:Reasoning` | `int` | 10 | NFR-025 / NFR-026 reasoning ceiling. |
| `CostCaps:Delivery` | `int` | 5 | NFR-025 delivery stage. |
| `FxMultipliers:UsdEur` | `decimal` | 0.92 | NFR-050 placeholder; refresh deferred. |
| `FxMultipliers:UsdGbp` | `decimal` | 0.79 | NFR-050 placeholder. |
| `FxMultipliers:UsdJpy` | `decimal` | 150.0 | NFR-050 placeholder. |
| `Cycle:Interval` | `TimeSpan` | `00:15:00` | Cycle scheduling. |
| `Cycle:DeliveryWindowTimeZoneId` | `string` | `Europe/Paris` | NFR-052 named zone. |
| `Cycle:DeliveryWindowStart` | `string` (`HH:mm`) | `07:00` | Delivery window inclusive start. |
| `Cycle:DeliveryWindowEnd` | `string` (`HH:mm`) | `22:00` | Delivery window exclusive end. |
| `Provider:Reasoning` | `string` | `OpenAI` | NFR-021 provider selection. |
| `Provider:ReasoningModel` | `string` | `gpt-4o-mini` | Model identifier. |
| `Provider:MarketData` | `string` | `OpenAI` | Provider selection. |
| `Provider:MarketDataModel` | `string` | `gpt-4o-mini` | Model identifier. |
| `Credentials:OpenAi:ApiKey` | `string` | `{{CREDENTIALS__OPENAI__APIKEY}}` | NFR-043/044 placeholder; real value from env or user-secrets. |

## Test References

- **L0:** `Host.UnitTest/CatalogueOptionsTests.cs` proves each of the five Host-owned section options binds its defaults from an empty `IConfiguration`, that a configured value overrides a default, that `Cycle:Interval` and default delivery-window composition reject malformed values with the configuration key named and without echoing the bad payload (NFR-047), that the default window is materialised once, and that the `Provider` options property set is an allow-list of non-secret knobs only (NFR-043/044). Range-failure theories cover non-positive numerics, out-of-unit-interval scoring weights, non-positive cost caps, non-positive FX multipliers, blank provider/model knobs, blank / malformed / non-positive cycle intervals. The `AddConfigurationRejects*`, `AddConfigurationAcceptsCommittedDefaults`, and `AddConfigurationRegistersOnlyTheValidatedApplicationDefaultsFacade` tests prove the same composition-time fail-fast path that `Program.cs` invokes and that no unvalidated catalogue `IOptions<section>` bindings are registered. `AddConfigurationRejectsMissingOpenAiApiKeyWhenOpenAiIsEnabled`, `AddConfigurationRejectsPlaceholderOpenAiApiKey`, and `AddConfigurationAcceptsNonOpenAiProviderWithoutOpenAiCredential` cover the credentials fail-fast in the same composition pass.
- **L0:** `Host.UnitTest/CredentialsOptionsTests.cs` proves `CredentialsOptions` bind from configuration, validate-when-enabled for OpenAI, placeholder-token rejection, blank rejection, non-OpenAI provider bypass, no-echo of the bound value, and that the credential property set is an allow-list (NFR-043/044).
- **L0:** `Host.UnitTest/CommittedConfigurationGuardTests.cs` scans the committed `appsettings.json` for the placeholder token on every known credential key and for secret-shaped literals (`sk-`, `ghp_`, `sk-ant-`, `AKIA`, `AIza`, `github_pat_`) as defense-in-depth (NFR-007 verification). The guard walks up from the test output directory to the repository root (`smooth-ai-stockanalysis.slnx`) and reads the file from disk.
- **L0:** `Host.UnitTest/DefaultUserOptionsTests.cs` proves fail-fast bind/validation of `DefaultUser:UniqueIdentifier` (missing, empty, malformed, `Guid.Empty`) names the configuration key.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — minimal runnable Host (`Program.cs`, `appsettings(.Development).json`, `Properties/launchSettings.json`) with empty `Configuration/`, `Endpoints/`, `HealthChecks/`, `Workers/`. | — |
| 2026-07-23 | Renamed solution/layers to SmoothAiStockAnalysis and verified the inward dependency graph. | #5 |
| 2026-07-24 | Registered Infrastructure without eagerly reading its connection string, preserving L2 configuration overrides. | #252 |
| 2026-07-24 | Documented Host composition of explicit data-access scopes and the global isolation filter (no ambient user). | #62, #63, #64 |
| 2026-07-24 | Documented Phase-1 default-user configuration keys, fail-fast validation, and identity-vs-auth boundary for seed work. | #66, #67, #7 |
| 2026-07-24 | Added the F-004 settings catalogue composition (`AddConfiguration` + five section options + `IApplicationDefaults` façade); folded the previous standalone `DeliveryWindow` Host options class into the `Cycle` section. | #68, #69 |
| 2026-07-24 | Made catalogue composition eager (interval + default delivery window fail at Host build) and tightened Provider allow-list / no-echo validation messages. | #68, #69 |
| 2026-07-24 | Updated Key Behaviors to describe the real Program composition (Infrastructure + catalogue + Application) while endpoints remain unmapped. | #68, #69 |
| 2026-07-24 | Added range validation for the `Analysis` / `CostCaps` / `FxMultipliers` / `Provider` sections and moved `Cycle:Interval` validation into `FromConfiguration`; documented per-section rules and the L0 composition-time test set. | #70 |
| 2026-07-24 | Removed unused catalogue `Configure<section>` double-bind so Host exposes only the validated `IApplicationDefaults` singleton; `ApplicationDefaults` now takes the validated options instances directly. | #70 |
| 2026-07-24 | Extracted `CatalogueSectionOptions` base for shared LADR-018 validation helpers; tightened `ApplicationDefaults` ctor and cycle interval parse to internal/single-parse; documented blank/malformed/non-positive `Cycle:Interval` failure modes. | #70 |
| 2026-07-24 | Added `Credentials` section with placeholder-token commitment (NFR-044), env-only bind + validate-when-enabled for `Credentials:OpenAi:ApiKey` (NFR-043/047), and L0 guard that scans committed `appsettings.json` for secret-shaped literals (NFR-007 verification). Credentials remain outside `IApplicationDefaults` and register as a separate singleton. Closes Feature #8. | #71, #72, #8 |
