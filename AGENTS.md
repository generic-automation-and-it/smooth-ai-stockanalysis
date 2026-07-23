# AGENTS.md

This file provides guidance for AI coding agents working in the smooth-ai-stockanalysis repository.

## Product Overview

smooth-ai-stockanalysis is a self-hosted research service that identifies market catalysts, filters candidates through deterministic checks, uses AI to evaluate the strongest opportunities, and emails a small set of recommendations. It is a personal research tool, not financial advice.

**Tech stack:** .NET 10 · ASP.NET Core · Clean Architecture (Domain / Application / Infrastructure / Host) · EF Core + SQLite · Mediator (source-gen CQRS) · xunit.v3 · Aspire-managed WireMock

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
| Domain | `src/SmoothAiStockAnalysis.Domain/` | Core entities, value objects — no external deps |
| Application | `src/SmoothAiStockAnalysis.Application/` | Vertical-slice use cases via Mediator — `Features/<Name>/`, shared code in `Common/` |
| Infrastructure | `src/SmoothAiStockAnalysis.Infrastructure/` | EF Core + SQLite (`Persistence/`), HTTP clients (`Clients/`) |
| Host | `src/SmoothAiStockAnalysis.Host/` | ASP.NET Core Web API, Serilog, Scalar OpenAPI |

Detailed backend coding rules are maintained in `.agents/rules/backend/` and scoped per-file via frontmatter (see Rules section).

## Rules

All rules live under `.agents/rules/` as `*.instructions.md` files and are auto-loaded every session by Claude Code, Cursor, Copilot, and Codex via the symlinks/path-references documented in `.agents/AI_DEVELOPMENT_AGENTS.md`. Applicability is scoped **per-file** via frontmatter (`paths` for Claude, `globs`+`alwaysApply` for Cursor, `applyTo` for Copilot) — e.g. backend rules carry `**/*.cs` so they attach when a C# file is opened. Rules are organized into category subfolders for navigation; the folder is organizational only and does not change loading. One exception to "auto-loaded every session": prompt-scoped rules may be **deferred for Claude** and re-injected on demand by a `UserPromptSubmit` hook (e.g. `code-review-standards` loads only on review prompts via `.agents/hooks/code-review-standards-context.sh`; Cursor/Copilot still load it always). See `.agents/rules/meta/rules.instructions.md` ("Hook-deferred rules") for the file convention and `.agents/skills/manage-rule-system/SKILL.md` for the directory contract.

### Rule Categories

| Category | Folder | Contents |
|----------|--------|----------|
| _(cross-cutting)_ | `.agents/rules/` (flat) | `ai-workflow-rules`, `code-review-standards` (Claude: hook-deferred to review prompts), `project-overview` |
| git | `.agents/rules/git/` | `git-policy`, `pr-standards` |
| meta | `.agents/rules/meta/` | `rules` (file convention), `knowledge-conventional-contexts-quality` (AGENTS.md quality) |
| backend (`**/*.cs`) | `.agents/rules/backend/` | `api-mediator-validation` (Minimal API + Mediator + FluentValidation fail-fast); `architecture-slices` (clean-architecture boundaries, vertical-slice Features); `backend-logging-conventions` (Information vs Debug levels); `external-api-clients` (Refit list vs singular client split, HybridCache adapter); `migrations` (`[ExcludeFromCodeCoverage]` requirement); `wiremock-stubbing` (Aspire-owned WireMock test dependency and shared admin client) |

## Build / Test Commands

```bash
dotnet build smooth-ai-stockanalysis.slnx                  # build
dotnet test  smooth-ai-stockanalysis.slnx                  # run all tests
dotnet run --project src/SmoothAiStockAnalysis.Host        # run the API
```

Target a single test project directly when needed (e.g. `dotnet test tests/SmoothAiStockAnalysis.Domain.UnitTest`); `ls tests/` lists them — no Trait annotations required.

## Test Framework

xunit.v3 · Shouldly · Bogus. Three tiers (the distinction is non-obvious and drives where a test belongs):

- **L0** `*.UnitTest` — no I/O, all in-process.
- **L1** component — `Application.ComponentTest` uses in-memory EF Core; `Infrastructure.ComponentTest` uses a real isolated SQLite file.
- **L2** `*.IntegrationTest` — full Host stack using an isolated local SQLite file.

Shared fixtures, including isolated SQLite test-database support and the opt-in Aspire/WireMock fixture, live in `tests/SmoothAiStockAnalysis.TestFramework/`. The WireMock-only AppHost lives in `tests/SmoothAiStockAnalysis.TestFramework.Aspire/`. See `docs/wiki/testing.md`.

## Style and Dependencies

Authoritative stack and coding conventions for AI coders are in `.agents/rules/project-overview.instructions.md` and backend-specific rules under `.agents/rules/backend/` (scoped per-file via `**/*.cs` frontmatter).

## Architecture Decisions (NFRs)

Human-facing reviewer documentation lives in `docs/wiki/`. Detailed high-level designs, non-functional requirements, and lightweight architecture decision records live under `docs/hlds/`.

## CI/CD

PR gate — `.github/workflows/pr-gate.yml` (triggers: `pull_request` → `main`, `push` → `main`, `workflow_dispatch`): restore → build (Release) → start WireMock through the Aspire AppHost → test with coverage via the local action `.github/actions/test-with-coverage` → publish + upload the coverage report. SQLite remains local and container-free. Full step list and local .NET tools: `docs/wiki/ci.md`.

AI review pipelines — `.github/workflows/pipeline-code-review-report.yml` is a thin caller that generates PR review reports through the reusable workflow in `generic-automation-and-it/smooth-ai-report-review`; `.github/workflows/pipeline-ai-analyse.yml` follows successful reports with a bounded, same-repository low/medium self-fix loop. Only the local `/ai-review` consumer skill is vendored. The generator and `ai-analyse` tooling stay upstream and are fetched at runtime.

The caller repository or organization must provide the following GitHub Actions configuration:

- **Required secret:** `OPENCODE_OPENAI_API_KEY`.
- **Optional provider secrets:** `OPENCODE_GEMINI_API_KEY`, `OPENCODE_COPILOT_API_KEY`, `OPENCODE_ANTHROPIC_API_KEY`, and `OPENCODE_OPENROUTER_API_KEY`.
- **Optional push secret:** `OPENCODE_ANALYSE_GH_TOKEN`, a PAT with `workflow` scope, only when self-fixes must push changes under `.github/workflows/**`.
- **Required variables:** `OPENCODE_REVIEW_REPORT_PROVIDER=OPENAI`, non-empty `OPENCODE_REVIEW_REPORT_OPENAI_URL=https://api.openai.com/v1`, `OPENCODE_REVIEW_REPORT_MODEL_PRIMARY`, `OPENCODE_REVIEW_REPORT_MODEL_SECONDARY`, `OPENCODE_REVIEW_REPORT_MODEL_ORCHESTRATOR`, and `OPENCODE_REVIEW_REPORT_DISABLE_CLAUDE_CODE=1`.
- **Optional variables:** `OPENCODE_ANALYSE_MAX_INCREMENTAL` (default `3`) and `SMOOTH_AI_REVIEW_TOOLS_REF` (upstream tooling ref override).

Both workflows currently follow upstream `main` because `smooth-ai-report-review` has no release tag. This keeps consumers current but is a supply-chain trade-off; pin the reusable call and tooling checkout to a reviewed tag or commit SHA when upstream publishes a stable release.

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
