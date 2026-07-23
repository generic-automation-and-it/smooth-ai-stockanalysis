# smooth-ai-stockanalysis

[![Skill Security Scan](https://github.com/generic-automation-and-it/smooth-ai-stockanalysis/actions/workflows/skill-scan.yml/badge.svg)](https://github.com/generic-automation-and-it/smooth-ai-stockanalysis/actions/workflows/skill-scan.yml)

> AI agent skills in `.agents/skills/` are scanned by [NVIDIA SkillSpector](https://github.com/NVIDIA/SkillSpector) on pull requests that touch `.agents/skills/**` (or the workflow itself), once the PR is marked ready for review. A green badge means the latest scan found no HIGH/CRITICAL findings; results are published to the repository's **Security → Code scanning** tab.

> A self-hosted personal research service that detects market catalysts, evaluates investment candidates, and emails a small set of AI-assisted recommendations.

---

## Tech Stack

### AI Toolchain

| Component | Technology |
|---|---|
| Agent scaffold | `.agents/` — single source of truth for all AI tools |
| Coding agents | Claude Code · GitHub Copilot · Cursor · OpenAI Codex |
| Skills | Executable multi-file workflows in `.agents/skills/` |
| Rules | Per-file coding standards in `.agents/rules/` |
| Prompts & roles | Reusable prompt templates and multi-agent role instructions |
| Hooks | `PostToolUse` / `UserPromptSubmit` automation via `.agents/hooks/` |

### Application

| Component | Technology |
|---|---|
| Framework | ASP.NET Core (.NET 10) |
| Architecture | Clean Architecture — `Domain` / `Application` / `Infrastructure` / `Host` |
| API style | Minimal API endpoints (`src/SmoothAiStockAnalysis.Host`) |
| Mediator | [`martinothamar/Mediator`](https://github.com/martinothamar/Mediator) — source-gen CQRS dispatch |
| Validation | FluentValidation in a fail-fast Mediator pipeline |
| Persistence | EF Core + PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`) |
| Observability | Serilog + OpenTelemetry, Scalar OpenAPI UI |
| Testing | xunit.v3 · Shouldly · Bogus · Respawn |

---

## Getting Started

### Prerequisites

- **.NET 10 SDK**
- A container runtime — Docker Desktop, Rancher Desktop, Colima, or Podman (for PostgreSQL via Aspire)

### One-time AI-agent setup

The repository drives four AI coding agents from a single `.agents/` directory via symlinks (`.claude`, `.codex`, `.cursor` → `.agents`, and `CLAUDE.md`/`GEMINI.md` → `AGENTS.md`). Run the setup script once after cloning so the agents can discover skills, hooks, and rules:

```bash
# Mac/Linux
bash .agents/setup/scripts/agents-setup.sh
```

```powershell
# Windows (requires admin; enable Developer Mode for symlink support)
powershell -ExecutionPolicy Bypass -File .agents/setup/scripts/agents-setup.ps1
```

> On Windows, enable Developer Mode (**Settings → System → For developers → Developer Mode**) so symlinks resolve.

### Build & Test

```bash
dotnet restore smooth-ai-stockanalysis.slnx
dotnet build   smooth-ai-stockanalysis.slnx --configuration Release
dotnet test    smooth-ai-stockanalysis.slnx
```

Target a single test project directly when iterating, e.g. `dotnet test tests/SmoothAiStockAnalysis.Domain.UnitTest`.

### Run locally

```bash
dotnet run --project src/SmoothAiStockAnalysis.Host      # start the API
```

Once the stack is up:

| Interface | URL |
|---|---|
| Scalar API Docs | `/scalar/v1` on the Host |
| OpenAPI schema | `/openapi/v1.json` on the Host |

---

## Solution Structure

```
.agents/                         # All AI tooling — single source of truth
  hooks/                         # PostToolUse / UserPromptSubmit automation
  prompts/                       # Reusable prompt templates
  roles/                         # Multi-agent role instructions (PO, Architect, QA, …)
  rules/                         # Per-file coding standards (auto-loaded by agents)
  skills/                        # Executable multi-file workflows
  setup/                         # One-time symlink / config setup scripts
  settings.json                  # Tool permissions, compile/test commands

src/
  SmoothAiStockAnalysis.Domain/          # Entities, value objects, invariants — no external deps
  SmoothAiStockAnalysis.Application/     # Vertical-slice use cases (Features/<Name>/) + Mediator handlers
  SmoothAiStockAnalysis.Infrastructure/  # EF Core + PostgreSQL persistence, HTTP clients
  SmoothAiStockAnalysis.Host/            # Minimal API composition, middleware, observability

tests/
  SmoothAiStockAnalysis.*.UnitTest/          # L0 — no I/O, in-process
  SmoothAiStockAnalysis.*.ComponentTest/     # L1 — in-memory EF Core / real isolated DB + Respawn
  SmoothAiStockAnalysis.*.IntegrationTest/   # L2 — full stack, real PostgreSQL
  SmoothAiStockAnalysis.TestFramework/       # Shared fixtures
  SmoothAiStockAnalysis.TestFramework.Aspire/# Aspire dependency host (PostgreSQL + WireMock)
```

---

## Documentation

| Topic | Location |
|---|---|
| AI agent context & coding rules | [`AGENTS.md`](AGENTS.md) · [`.agents/`](.agents/) |
| Architecture & design | [`docs/hlds/mvp/readme.md`](docs/hlds/mvp/readme.md) |
| Testing strategy | [`docs/wiki/testing.md`](docs/wiki/testing.md) |
| CI/CD pipeline | [`docs/wiki/ci.md`](docs/wiki/ci.md) |
| Architecture decisions & NFRs | [`docs/hlds/mvp/ladrs/`](docs/hlds/mvp/ladrs/) · [`docs/hlds/mvp/nfr/`](docs/hlds/mvp/nfr/) |

---

## Contributing

- Work on a branch off `main`: `<type>/<ticket>-short-description` (e.g. `feat/1234-add-user-export`).
- Commits and PR titles follow [Conventional Commits](https://www.conventionalcommits.org). See [`.agents/rules/git/`](.agents/rules/git/).
- Every PR should create or update at least one `*AGENTS.md` context file.
