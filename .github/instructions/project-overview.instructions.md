---
description: 'smooth-ai-stockanalysis backend tech stack, architecture, and commands for AI coding tasks'
globs: "**"
paths:
  - "**"
applyTo: '**'
alwaysApply: true
---
# Product Overview

Updated: 2026-05-09

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | ASP.NET Core (.NET 10) |
| Architecture | Clean Architecture (`Domain` / `Application` / `Infrastructure` / `Host`) |
| API style | Minimal API endpoints in `src/SmoothAiStockAnalysis.Host` |
| Mediator | [`martinothamar/Mediator`](https://github.com/martinothamar/Mediator) (in-process request/response dispatch with pipeline support) |
| Messaging durability options | Message Queue / Message Streaming can be introduced when durability, retries, or asynchronous decoupling are required |
| Validation | FluentValidation in Mediator pipeline (fail fast) |
| Persistence | EF Core + SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) |
| Logging/Observability | Serilog + OpenTelemetry |
| Testing | xunit.v3 + Shouldly + Bogus + isolated SQLite files + Aspire-managed WireMock |

## Commands

```bash
dotnet build smooth-ai-stockanalysis.slnx
dotnet test smooth-ai-stockanalysis.slnx

# Targeted test projects
dotnet test tests/SmoothAiStockAnalysis.Domain.UnitTest
dotnet test tests/SmoothAiStockAnalysis.Application.UnitTest
dotnet test tests/SmoothAiStockAnalysis.Infrastructure.UnitTest
dotnet test tests/SmoothAiStockAnalysis.Host.UnitTest
dotnet test tests/SmoothAiStockAnalysis.Application.ComponentTest
dotnet test tests/SmoothAiStockAnalysis.Infrastructure.ComponentTest
dotnet test tests/SmoothAiStockAnalysis.Host.IntegrationTest
```

## Solution Structure

```
src/
  SmoothAiStockAnalysis.Domain/          # Entities, value objects, invariants
  SmoothAiStockAnalysis.Application/     # Feature slices + Mediator handlers/pipelines
  SmoothAiStockAnalysis.Infrastructure/  # EF Core persistence + external integrations
  SmoothAiStockAnalysis.Host/            # Minimal API composition, middleware, observability

tests/
  SmoothAiStockAnalysis.*.UnitTest/
  SmoothAiStockAnalysis.*.ComponentTest/
  SmoothAiStockAnalysis.*.IntegrationTest/
  SmoothAiStockAnalysis.TestFramework/
  SmoothAiStockAnalysis.TestFramework.Aspire/
```

## AI Coder Rules (Summary)

- Keep business logic out of `Host`; route requests into Application via Mediator.
- In Application, organize by `Features/<FeatureName>/` (no global `Commands/` or `Queries/` folders).
- Use `Mediator` (martinothamar) — not `MediatR`.
- Add a FluentValidation validator for each request model and enforce validation in a fail-fast Mediator pipeline.
- If a use case suggests durable/asynchronous processing, explicitly prompt whether to introduce Message Queue or Message Streaming before generating that integration.
- Update the closest `*AGENTS.md` context file in each PR.

## Changelog

> AI loading note: Skip this section during routine task execution. Use it only when updating this rule file.

| Date | Change |
|:-----|:-------|
| 2026-05-30 | Initial version. |
| 2026-07-23 | Switched persistence from EF Core + PostgreSQL to EF Core + SQLite; dropped Respawn in favour of isolated SQLite test files. | #6 |
| 2026-07-23 | Restored Aspire as the WireMock-only external test dependency host. | #252 |
