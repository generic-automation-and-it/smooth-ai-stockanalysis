# AGENTS.md

This file provides guidance for AI coding agents working in the smooth-ai-stockanalysis repository.

## Product Overview

smooth-ai-stockanalysis is a self-hosted research service that identifies market catalysts, filters candidates through deterministic checks, uses AI to evaluate the strongest opportunities, and emails a small set of recommendations. It is a personal research tool, not financial advice.

**Tech stack:** .NET 10 · ASP.NET Core · Clean Architecture (Domain / Application / Infrastructure / Host) · NodaTime · EF Core + SQLite · Mediator (source-gen CQRS) · xunit.v3 · Aspire-managed WireMock

## Non-Negotiables

- `/ai-review execute` MUST make a final empty commit when any 🔴 Critical or 🟠 High finding is present — no exceptions. Commit message: `ci: /ai-review — processed review responses`. This applies whether those findings were fixed or skipped. Never omit this commit, never fold it into a fix commit. Only omit for medium/low-only reviews. This re-triggers the full review gate to re-verify critical/high findings.

## AI Context Files

`AGENTS.md` and `*AGENTS.md` are **AI-coder contextual knowledge documents**. Read them like `CLAUDE.md` (or your agent's equivalent standard context file): they are first-class, authoritative context — not optional reference material. Before changing code, treat any `AGENTS.md` / `*AGENTS.md` in scope as required reading.

These documents capture the **functional requirements and intent behind the code** — the "why", constraints, boundaries, and non-obvious behaviors that source code alone does not communicate. Use them to understand what the code is supposed to do before you change how it does it.

Contextual knowledge is layered, and applies at **multiple levels** — read every level that governs the code you touch (specific overrides general):

- **Domain** — the broad business/functional area.
- **Sub-domain** — a bounded slice within a domain.
- **Feature** — a specific capability or vertical slice.
- **Technology** — cross-cutting technical concerns (persistence, messaging, logging, etc.).

This root `AGENTS.md` is the top-level document; nested `*AGENTS.md` files inherit from it and add local context closest to the code. When working in a folder, the nearest `*AGENTS.md` is the most authoritative for that code.

Keep `*AGENTS.md` files synchronised with code and documentation changes. Functional `*AGENTS.md` files in feature folders are auto-loaded by the `load-agents-context` PostToolUse hook on the first Read/Edit in their directory tree — no manual registration required.

### Required Maintenance

- Every PR should create or update at least one `*AGENTS.md` file.
- Update the closest context file to the code you change. Prefer local context over adding more content to this root file.
- When domain model or structural shape changes, also update the relevant implementation or architecture context.

### Placement Rules

- Functional feature context belongs close to the feature code.
- Cross-cutting concerns belong under `docs/hlds/mvp/nfr/` or the nearest `*AGENTS.md`.
- Avoid creating duplicate context files that restate the same plan at multiple levels without adding new information.

## Implementation Docs

All planned work is tracked as worktasks under `.context/work-tasks/` (gitignored — local only). Use `/create worktask` to scaffold a new one from the template.

## AI Skills

First-party agent skills live under `.agents/skills/` and are registered in `.agents/skills/README.md`.

- **`ai-review`** — consumes a posted AI pull-request review, recommends per-finding fix/skip decisions, and routes processed results back to GitHub review threads or the PR description. It does not generate reviews; generation and autonomous low/medium remediation are delegated to `generic-automation-and-it/smooth-ai-report-review`.

## Repository Layout (Navigation)

| Layer | Path | Purpose |
|---|---|---|
| Domain | `src/SmoothAiStockAnalysis.Domain/` | Core entities and value objects; NodaTime is the sole allowed external value-semantics dependency |
| Application | `src/SmoothAiStockAnalysis.Application/` | Vertical-slice use cases via Mediator — `Features/<Name>/`, shared code in `Common/` |
| Infrastructure | `src/SmoothAiStockAnalysis.Infrastructure/` | EF Core + SQLite (`Persistence/`), HTTP clients (`Clients/`) |
| Host | `src/SmoothAiStockAnalysis.Host/` | ASP.NET Core Web API, Serilog, Scalar OpenAPI |

**Settings catalogue (F-004):** application defaults and two-layer resolution live under Host `Configuration/` + Application `Configuration/` (`ISettingsResolver`, `EffectiveSettings`). Authoritative agent context: [`HOST_AGENTS.md`](src/SmoothAiStockAnalysis.Host/HOST_AGENTS.md), [`CONFIGURATION_AGENTS.md`](src/SmoothAiStockAnalysis.Application/CONFIGURATION_AGENTS.md), Domain preference shape in [`DOMAIN_AGENTS.md`](src/SmoothAiStockAnalysis.Domain/DOMAIN_AGENTS.md).

**Conductor workspace scripts:** `.conductor/settings.toml` + `.conductor/scripts/` start the SmoothLlmImposter Docker container and wire `code-review-graph` on every teammate's workspace. Authoritative agent context: [`.conductor/AGENTS.md`](.conductor/AGENTS.md).

Detailed backend coding rules are maintained in `.agents/rules/backend/` and scoped per-file via frontmatter (see Rules section).

## Rules

All rules live under `.agents/rules/` as `*.instructions.md` files and are auto-loaded every session by Claude Code, Cursor, Copilot, and Codex via the symlinks/path-references documented in `.agents/AI_DEVELOPMENT_AGENTS.md`. Applicability is scoped **per-file** via frontmatter (`paths` for Claude, `globs`+`alwaysApply` for Cursor, `applyTo` for Copilot) — e.g. backend rules carry `**/*.cs` so they attach when a C# file is opened. Rules are organized into category subfolders for navigation; the folder is organizational only and does not change loading. One exception to "auto-loaded every session": prompt-scoped rules may be **deferred for Claude** and re-injected on demand by a `UserPromptSubmit` hook (e.g. `code-review-standards` loads only on review prompts via `.agents/hooks/code-review-standards-context.sh`; Cursor/Copilot still load it always). See `.agents/rules/meta/rules.instructions.md` ("Hook-deferred rules") for the file convention and `.agents/skills/manage-rule-system/SKILL.md` for the directory contract.

**14 rule files** in total — the table below lists every one. Future drift is detectable by comparing the count.

### Rule Categories

| Category | Folder | Contents |
|----------|--------|----------|
| _(cross-cutting)_ | `.agents/rules/` (flat) | `ai-workflow-rules`, `code-review-standards` (Claude: hook-deferred to review prompts), `project-overview`, `skill-secret-handling` (env-via-script guardrail for skills) |
| git | `.agents/rules/git/` | `git-policy`, `pr-standards` |
| meta | `.agents/rules/meta/` | `rules` (file convention), `knowledge-conventional-contexts-quality` (AGENTS.md quality) |
| backend (`**/*.cs`) | `.agents/rules/backend/` | `api-mediator-validation` (Minimal API + Mediator + FluentValidation fail-fast); `architecture-slices` (clean-architecture boundaries, vertical-slice Features); `backend-logging-conventions` (Information vs Debug levels); `external-api-clients` (Refit list vs singular client split, HybridCache adapter); `migrations` (`[ExcludeFromCodeCoverage]` requirement); `wiremock-stubbing` (Aspire-owned WireMock test dependency and shared admin client) |

## Build / Test Commands

```bash
dotnet build smooth-ai-stockanalysis.slnx -c Release       # build
dotnet run --project src/SmoothAiStockAnalysis.Host        # run the API

# Per-level test runs (same scripts CI uses; NFR-069).
# Requires bash (Linux/macOS/WSL or Git Bash on Windows). On Windows PowerShell:
#   bash .github/actions/test-with-coverage/run-level.sh unit
# Cross-platform without bash: dotnet test <project|slnx> (below).
bash .github/actions/test-with-coverage/run-level.sh unit
bash .github/actions/test-with-coverage/run-level.sh component
bash .github/actions/test-with-coverage/run-level.sh integration
bash .github/actions/test-with-coverage/merge-coverage.sh
# Container-free local default; only AspireCollection opt-in tests need a container runtime
dotnet test smooth-ai-stockanalysis.slnx
```

Target a single test project directly when needed (e.g. `dotnet test tests/SmoothAiStockAnalysis.Domain.UnitTest`); `ls tests/` lists them — no Trait annotations required. Architecture boundary tests live in `tests/SmoothAiStockAnalysis.Architecture.UnitTest` and run in the unit level.

## Test Framework

xunit.v3 · Shouldly · Bogus. Three tiers (the distinction is non-obvious and drives where a test belongs):

- **L0** unit — `*.UnitTest` plus `Architecture.UnitTest` (NetArchTest layer rules). No I/O, all in-process; must stay runnable on every CI matrix image (Linux + Windows) without a container runtime.
- **L1** component — `Application.ComponentTest` uses in-memory EF Core; `Infrastructure.ComponentTest` uses a real isolated SQLite file. No WireMock unless a test opts into Aspire.
- **L2** integration — `Host.IntegrationTest` full Host stack with an isolated local SQLite file; CI may pre-warm Aspire WireMock for this level only, when `PREWARM_WIREMOCK=1` is set or a test opts into `AspireCollection`.

Levels are separately runnable via `run-level.sh` (LADR-020). Shared fixtures, including isolated SQLite test-database support and the opt-in Aspire/WireMock fixture, live in `tests/SmoothAiStockAnalysis.TestFramework/`. The WireMock-only AppHost lives in `tests/SmoothAiStockAnalysis.TestFramework.Aspire/`. See `docs/wiki/testing.md`.

## Style and Dependencies

Authoritative stack and coding conventions for AI coders are in `.agents/rules/project-overview.instructions.md` and backend-specific rules under `.agents/rules/backend/` (scoped per-file via `**/*.cs` frontmatter).

## Architecture Decisions (NFRs)

Human-facing reviewer documentation lives in `docs/wiki/`. Detailed high-level designs, non-functional requirements, and lightweight architecture decision records live under `docs/hlds/`.

## CI/CD

PR gate — `.github/workflows/pr-gate.yml` (triggers: `pull_request` → `main`, `push` → `main`, `workflow_dispatch`): restore → `dotnet format whitespace --verify-no-changes` → build (Release, SDK analyzers + code style as errors) → **Secret scan** (gitleaks, PR commit range) → **Unit tests** → **Component tests** → **Integration tests** (no container runtime; WireMock pre-warm is opt-in via `PREWARM_WIREMOCK`) → merge coverage → upload per-level test-results + coverage artifacts. Scripts: `.github/actions/test-with-coverage/run-level.sh`. SQLite remains local and container-free outside the integration WireMock host. Authoritative agent context: [`.github/CI_AGENTS.md`](.github/CI_AGENTS.md). Full step list and local .NET tools: `docs/wiki/ci.md`.

**The gate has no `paths:` filter, and must not gain one.** Its job (`build-and-test`) is a required status check in the default-branch ruleset, alongside `review / open-code-review-report` and `CodeQL`. A workflow skipped by a path filter never creates its check run, so the requirement stays permanently unreported and the pull request becomes unmergeable — a path filter blocks merges rather than skipping work. The Secret scan step is the independent second reason: filtering means unscanned paths on a public repository.

AI review pipelines — `.github/workflows/pipeline-code-review-report.yml` is a thin caller that generates PR review reports through the reusable workflow in `generic-automation-and-it/smooth-ai-report-review`; `.github/workflows/pipeline-ai-analyse.yml` follows successful reports with a bounded, same-repository low/medium self-fix loop. Only the local `/ai-review` consumer skill is vendored. The generator and `ai-analyse` tooling stay upstream and are fetched at runtime.

Configuration the caller repository or organization must provide — one required provider secret (`OPENCODE_OPENAI_API_KEY`), four optional provider secrets (Gemini, Copilot, Anthropic, OpenRouter), one optional `workflow`-scoped push PAT (`OPENCODE_ANALYSE_GH_TOKEN`), six required `OPENCODE_REVIEW_REPORT_*` variables and two optional ones. `secrets: inherit` resolves against the caller repo **and** its organization, so an org-level secret satisfies the caller while remaining invisible to `gh secret list`. **Do not restate the inventory here** — the authoritative table, with each entry's scope and its missing-credential failure symptom, is in [`.github/CI_AGENTS.md`](.github/CI_AGENTS.md), maintained alongside the workflow files it documents. Provisioning was verified operational on 2026-07-25.

Both workflows deliberately follow upstream `main`, and upstream code *executes* here on every pull request. Note the trap before "fixing" it: upstream **does** publish a `v1` tag, but force-moves it to its default-branch head on every push, so `@v1` is `@main` renamed. The decision, the rejected alternatives and the two pinning levers are recorded in [LADR-021](docs/hlds/mvp/ladrs/021-live-upstream-call-for-ai-review-tooling.md).

## Git Constraints

This repository is hosted on **GitHub** at `https://github.com/generic-automation-and-it/smooth-ai-stockanalysis`.

- **CLI tool:** Use `gh` (GitHub CLI) for PR and repository operations.
- **PR template:** `.github/pull_request_template.md`
- **Code owners:** `.github/CODEOWNERS` — all files owned by `@generic-automation-and-it/smooth-ai-stockanalysis`

## Glossary

<!-- TODO: Add domain-specific terms and abbreviations as the project evolves. -->

| Term | Description |
|---|---|
| Catalyst | A market event that may justify evaluating an investment opportunity |
| Candidate | A company or instrument progressing through the analysis funnel |
| Analysis cycle | One complete, resumable run of catalyst detection, filtering, evaluation, and notification |

## Changelog

| Date | Change | Ref |
|---|---|---|
| 2026-07-31 | Added `.conductor/` workspace scripts (settings.toml + scripts/) following smooth-llm-imposter pattern; reference added to Repository Layout. | — |
| 2026-07-30 | Rule catalogue reconciled with `.github/instructions/` (14 rule files); `skill-secret-handling` row added. | #275 |
