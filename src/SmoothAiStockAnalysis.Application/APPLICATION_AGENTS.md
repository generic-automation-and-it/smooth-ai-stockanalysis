# APPLICATION_AGENTS.md

## TL;DR

Vertical-slice use cases dispatched via the Mediator source generator — one folder per feature under `Features/`, never a horizontal `Commands/`/`Queries/` split.

## Non-Negotiables

- **No `Commands/` or `Queries/` folders.** Each use case lives in `Features/<FeatureName>/` with its request, response, validator, and handler colocated.
- **No domain logic here.** Application orchestrates Domain and infrastructure abstractions; business rules live in Domain.
- **Define infrastructure contracts as interfaces here.** Persistence store contracts go in `Common/Persistence/`, external client contracts in `Common/Clients/`. Application depends on `IFoo`, never on an Infrastructure concrete type.
- **Use `Mediator` (martinothamar), not `MediatR`.** Register handlers as `Scoped` (`MediatorOptions.ServiceLifetime = ServiceLifetime.Scoped`) so they can consume scoped collaborators such as `DbContext`; the generator defaults to Singleton, which fails DI scope validation.
- **FluentValidation runs fail-fast** in a Mediator pipeline behavior under `Common/Pipelines/`, before the handler.
- **References Domain only** — never Infrastructure or Host.

## Slice shape (`Features/<Name>/<UseCase>.cs`)

```text
Features/
  <FeatureName>/
    <UseCase>.cs
      ├ Request    : IRequest<Response>
      ├ Response
      ├ Validator  : AbstractValidator<Request>
      └ Handler    : IRequestHandler<Request, Response>
```

## Packages to add when implementing

`Mediator.Abstractions` + `Mediator.SourceGenerator`, `FluentValidation.DependencyInjectionExtensions`, and (for upstream clients) `Refit` — all declared centrally in `Directory.Packages.props`.

## Data-access scopes

- Contracts live in `Common/Persistence/`: `DataAccessScope` / `DataAccessScopeKind`, `IDataAccessScopeSetter`, `IDataAccessScope`, and `ISystemDataAccessScope`.
- Feature and background code set the scope deliberately through `IDataAccessScopeSetter` before touching owned data. There is no ambient user.
- Shared ingestion takes `ISystemDataAccessScope` (a separate interface) so the isolation bypass is auditable. Ordinary features do not depend on it.
- Infrastructure implements the contracts and enforces isolation with a global EF Core query filter (LADR-017). Application never references EF Core.

## F-004 settings resolution

- The settings catalogue façade (`IApplicationDefaults`) and the `ISettingsResolver` live in `Configuration/`. The resolver is the only sanctioned way for feature code to read effective settings; ad-hoc `if (pref)` branching is prohibited (NFR-045, HLD §7.2).
- The `IUserMetadataProvider` port (in `Common/Configuration/`) loads a user's `UserMetadata` for the resolver; the Infrastructure implementation respects the current `IDataAccessScope` and fails closed if no scope is set. `ResolveForUserAsync` rejects non-positive user ids and never invents an ambient user (LADR-010).
- `AddApplication` registers the resolver as **Scoped** so it shares the per-unit-of-work lifetime with the metadata port and the underlying DbContext. See [CONFIGURATION_AGENTS.md](CONFIGURATION_AGENTS.md) for the full contract.

## Changelog

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-05-30 | Created — empty vertical-slice skeleton (`Features/`, `Common/{Clients,Exceptions,Models,Persistence,Pipelines}/`, `Extensions/`). | — |
| 2026-07-24 | Added explicit data-access scope contracts for user isolation (`DataAccessScope`, setter/reader, system-scope interface). | #62, #63, #64 |
| 2026-07-24 | Added the F-004 settings catalogue façade, the immutable `EffectiveSettings` DTO, the `ISettingsResolver` (pure merge + scoped orchestrator), the `IUserMetadataProvider` port, and the `AddApplication` extension. | #68, #69 |
