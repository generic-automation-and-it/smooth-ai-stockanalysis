# smooth-ai-stockanalysis — Braindump Context Handoff

**Purpose:** onboard a fresh AI agent with the complete specification and decision context from a braindump session. This is summarised knowledge, not a transcript. Everything below is settled unless explicitly marked open or inferred.

**Status at handoff:** requirements gathering complete. No artifacts produced yet. Four deliverables pending (§14).

---

## 1. Project identity

| Item | Value |
|---|---|
| Org | `generic-automation-and-it` |
| Repo | `github.com/generic-automation-and-it/smooth-ai-stockanalysis` (exists, created via clickops) |
| Project board | `github.com/orgs/generic-automation-and-it/projects/2` (private, default GH feature board) |
| Owner timezone | CET (EU) |
| Repo visibility | Public |

The name `smooth-ai-stockbroker` appears in earlier discussion — superseded. `smooth-ai-stockanalysis` is final.

---

## 2. Business context

A **personal, single-user stock analysis tool** that surfaces short-term trading opportunities by email.

- **Decision support, not execution.** The human makes every buy/sell call. No broker integration in scope.
- **Horizon:** short term, days up to 2 weeks (configurable, this is the default).
- **Target:** ~10% gain over that window. Functions as a take-profit collar and as a hopeful KPI the AI agent evaluates against — not a hard screening threshold in Phase 1.
- **Delivery:** email only.
- **Markets:** US primary, Europe (incl. Nordics) where available.
- **Cost posture:** free tiers first; paid services added only on demonstrated ROI.

*(Inferred, needs owner confirmation: business objectives and success criteria were never stated explicitly. The dump was ~90% technical. Any BRD will need these articulated and flagged as inferred.)*

---

## 3. Scope boundaries

### Phase 1 — in scope
The analyse-and-recommend workflow, delivered as milestones. No big bang.

### Phase 2 — out of scope, documented as future
- Human replies by email stating what was bought/sold → position tracking → recommendations tailored to holdings. **First inbound channel** (everything in Phase 1 is outbound-only).
- Loss-collar notifications and buy/sell persistence (same capability cluster as above).
- Sell-side recommendation analysis: collar, potential loss, market-crash conditions.

### Future, out of scope
- **Dashboard UI** for the user.
- **Security:** Apple login with an EF Core RBAC backend. Nothing now (private home network), but architecture must not preclude it.
- **User-configurable market-cap limit** in a currency of the user's choosing, plus daily FX refresh updating translation multipliers when a rate moves >5% against the last *applied* multiplier. **Low priority.**

### Explicit non-goals
- Broker/trade execution (possible far-future phase).
- Authentication and authorization in Phase 1.

---

## 4. The core workflow — event-driven funnel

Catalyst-first, not valuation-first. Markets move on new information; valuation decides whether the move is worth acting on.

```
Daily news / market events
   → identify market-moving events
   → find affected stocks
   → rank by catalyst score
   → fundamental validation (elimination)
   → technical confirmation (timing)
   → analyst + social sentiment
   → AI ranks opportunities
   → email report
```

**Why not valuation-first:** sectors trade at structurally different multiples; low-P/E is riddled with value traps; high-growth names have high or meaningless P/E; opportunities come from new information, not static valuation.

### Stage caps (all configurable, defaults shown)

| Stage | Cap |
|---|---|
| Catalyst detection | Unlimited (provider output) |
| Fundamentals | 50 |
| Technicals | 20 |
| LLM reasoning | 10 |
| Published recommendations | 5 |

This makes LLM spend bounded and predictable regardless of universe size. It also means **tickers-per-cycle is an output of stage 1, not a config value**.

**Publish fewer than 5 — or none.** Quality over quantity; never pad to fill a quota.

---

## 5. Scoring model

Baseline weights, in appsettings:

| Category | Weight |
|---|---|
| News catalyst | 35% |
| Fundamentals | 30% |
| Technicals | 20% |
| Analyst sentiment | 10% |
| Social sentiment | 5% |

- **Weights shift with the configured horizon.** The baseline above was designed for weeks-to-months; the short-term default (days–2 weeks) requires a different distribution.
- **The AI agent has free, unbounded latitude** to move weights. Not capped, not required to justify within a fixed band.
- Consequence (accepted, not resolved): the composite score is not reproducible from inputs alone. See §13.

---

## 6. Functional domains

Thirteen domains, priorities as stated by the owner. This is a complete multi-spec design; prioritisation into phases happens at milestone planning.

| # | Domain | Priority | Source |
|---|---|---|---|
| 1 | Market data (real-time/delayed prices, OHLCV, pre/post market, indices, sector performance) | Critical | Polygon.io |
| 2 | Company fundamentals (income statement, balance sheet, cash flow, ratios, valuation, growth, dividends) | Critical | Finnhub + FMP |
| 3 | Market news (breaking, company, macro, central bank, earnings headlines) | Critical | Benzinga |
| 4 | Earnings intelligence (dates, history, EPS/revenue estimates, surprises, guidance) | High | Finnhub |
| 5 | Analyst research (ratings, consensus, targets, up/downgrades, revisions) | High | Finnhub |
| 6 | Insider activity (buying, selling, SEC filings, executive transactions) | Medium | Finnhub |
| 7 | Technical indicators (RSI, MACD, SMA/EMA, Bollinger, ATR, VWAP, volume, breakout) | High | **Computed internally** |
| 8 | Market movers (gainers, losers, most active, gaps, high volume) | High | Polygon.io |
| 9 | Sentiment analysis | High | News sentiment now; Reddit optional Phase 2 |
| 10 | Event calendar (earnings, IPO, dividend, economic, splits) | Medium | Finnhub |
| 11 | **AI recommendation engine** | Critical | — |
| 12 | Alerting | Medium | — |
| 13 | Screening | High | Polygon / Finnhub / FMP |

**Domain 11 is the only domain that consumes the other twelve rather than fetching anything.** Everything else is ingestion. It is where the agentic layer lives.

**News article shape (fixed):** timestamp, publisher, related ticker symbols, categories, summary.

**Recommendation output shape:** ranked list, confidence score, supporting rationale, bull case, bear case, key risks, suggested holding horizon.

**Social sentiment** is an optional Phase 2 enrichment that influences *confidence only* — it never drives a recommendation.

---

## 7. Provider strategy

**Governing principle: buy proprietary data, compute deterministic values.**

- **Buy** what can't be reproduced: news, analyst ratings, fundamentals, earnings estimates, insider transactions.
- **Compute** what is formula-based: RSI, MACD, SMA/EMA, Bollinger Bands, ATR, momentum, breakout detection, composite scores.

Rationale for internal indicator calculation: Polygon already supplies high-quality OHLCV; indicators are deterministic; gives full parameter control, consistent maths, lower API cost, no vendor-specific implementation lock-in.

### AI provider
- **OpenAI SDK first.** Model name and provider base URL in appsettings; API key in env var.
- **Anthropic SDK** as the alternative if the OpenAI SDK proves inferior.
- Original NFR was per-task provider *and* model routing across both, for cost and reasoning optimisation. An intermediate recommendation naming "OpenAI" as the fixed AI reasoning layer was **explicitly corrected by the owner as a mistake** — both must be supported.
- OpenRouter / OpenCode Go considered as transport; LLM spend ceiling is enforced by the provider, with a no-credit HTTP status handled by the app.
- Open: if OpenRouter is the transport, the abstraction may collapse to one OpenAI-compatible client plus a model string (simpler, loses Anthropic-native features).

### Cost
POC runs on free tiers or a discounted startup subscription. **The BRD must include an analysis of high-gain paid tiers** so upgrades are ROI-driven.

Known free-tier constraints: Alpha Vantage 25 req/day, EODHD 20/day, FMP 250/day, Finnhub ~60/min, Twelve Data ~800/day. Free tiers generally mean delayed data and limited history.

---

## 8. Universe filters

All settings follow the pattern **user metadata JSON override → appsettings default**.

| Filter | Value | Notes |
|---|---|---|
| Market cap floor | EUR 1,000M base | Framed as a liquidity/manipulation guard, not a stability claim |
| Currency multipliers | USD 1.1, DKK 7.5 | Static. SEK, NOK, GBP **still to be added** |
| OTC / penny stocks | Excluded | |
| Min avg daily traded value | EUR 3M | |
| ADTV window | 20 sessions, **median**, **excluding trigger day** | |
| Participation cap | Position ≤ 10% of ADTV | Future-proofing; binds only once real position sizes exist (Phase 2) |
| Low turnover flag | <0.05% of free float/day | **Soft flag in evidence, never a hard filter** |

**Why median, excluding the trigger day:** the system fires on catalysts, and catalysts spike volume. A mean including the event day makes every thin stock look liquid precisely when it is being evaluated. Median over prior sessions describes a normal day — which is the day you sell on. This choice matters more than the threshold value.

**Why not a turnover-% floor:** mega-caps like Nestlé and Novo Nordisk turn over ~0.1–0.2% of shares daily because their floats are enormous. A percentage floor would exclude the most liquid stocks in Europe.

**Currency handling:** one EUR base threshold plus a static multiplier map — no FX provider, no extra failure mode. Listing currency becomes a required field on any ingested instrument; an unmapped currency should **skip** the ticker rather than defaulting to 1.0.

**Not covered:** bid-ask spread is the other half of exit cost and is rarely in free tiers. Belongs in the BRD's out-of-scope-but-relevant section.

### Sector-relative filters (own milestones)
- **Relative market cap within sector** — replaces an unimplementable "20% market share" requirement. Market share is not a reported field in any provider; it depends on defining the addressable market. Computed instead by ranking market cap within sector. Sector aggregates are **stored and reused** as shared reference data.
- **EBIT-derived metrics** in stage 2 elimination: EV/EBIT, operating margin, interest coverage, ROIC, earnings yield. Derived from income statements already being pulled — no new provider, no new cost. Only meaningful **sector-relative**, so shares the sector-aggregate plumbing above. Needs sector exclusions (appsettings): EBIT is meaningless for banks and insurers, REITs use FFO, and pre-profit growth names have negative EBIT.

---

## 9. Non-functional requirements

### Platform & architecture
- Latest .NET (.NET 10).
- **Clean/Onion architecture** at solution level; **vertical feature slices** in the Application layer (`Features/<Name>/`).
- Runs on **Raspberry Pi 4, 1GB RAM**, as a service. SD card boot (SSD upgrade later).
- **Must be runnable locally for debug.**
- Scalar for OpenAPI documentation (Swagger UI replacement). *Note: "Scalar for OpenAI documentation" in early notes was a typo for OpenAPI.*

### Persistence
- **EF Core + SQLite, on disk.** `journal_mode=WAL`, `synchronous=NORMAL`, one batched transaction per cycle, retention job pruning beyond 1 month.
- **Rejected:** in-memory SQLite with hourly dump to SD. An hourly full-DB dump rewrites the entire database (~4.8GB/day at 200MB) — worse SD wear than incremental WAL writes. The OS page cache already provides in-memory read speed for free, without consuming the .NET heap on a 1GB box. In-memory also forces a single long-lived connection, fighting EF Core's scoped `DbContext`.
- **NodaTime** for all time handling, stored **UTC**. ⚠️ NodaTime's first-class EF Core support is via Npgsql/PostgreSQL. On SQLite this requires custom value converters — **needs a scaffolding spike**.
- **1-month analysis history per company**, persisted and exposed to the AI. Priority is the event-trigger dedup check, not rich history tooling.
- **Event dedup:** hash of ticker + type + timestamp + headline, or provider ID where available. Two providers reporting the same upgrade must collapse to one. Checked at workflow start to prevent re-analysis.

### Multi-user readiness (day 1, in the Scaffolding milestone)
- User table with metadata; `UserId` on all user-owned entities; **EF Core global query filter** as the security boundary.
- Phase 1 seeds one user with one outbound email; nothing hardcodes single-user assumptions.
- **Shared (global, no UserId):** market data, fundamentals, news, computed indicators, sector aggregates. Fetching per-user would multiply API costs and defeat caching.
- **User-scoped:** watchlists, analysis history, recommendations and their metadata, alerts, notification preferences, scoring config.
- **No HttpContext** — everything runs in a background worker. Needs an explicit `ICurrentUser` scope set per user, plus a deliberate "system" context for shared ingestion that bypasses the filter.
- Uniqueness constraints become composite: `(UserId, …)`.
- Default user seeded; metadata configurable via appsettings.
- **User metadata stored as client-side JSON with a JSON schema-version column.** ⚠️ EF Core JSON column support on SQLite is thinner than on SQL Server/PostgreSQL — a value converter to a `TEXT` column is the safe path unless querying inside the metadata is needed. **Scaffolding spike.**

### Scheduling
- **`BackgroundService` + `PeriodicTimer` + a persisted run-lock.** Temporal deferred (§10).
- **MVP:** 30-minute continuous cycles, skip if the previous cycle has not completed.
- **POC:** manual/API trigger. An HTTP endpoint runs one cycle on demand. A separate MVP milestone adds cyclic delivery.
- **Recommendation window:** CET 07:00–22:00, in appsettings. Must be stored as a **timezone ID + local times**, not a fixed UTC offset, or it drifts at every DST change.
- Each funnel stage should be a **discrete, idempotent, resumable step** with state in SQLite — this delivers skip-if-running and crash-resume from your own persistence, and makes a later Temporal swap non-destructive.

### Performance, resilience, caching
- API response < 500ms for cached requests.
- Automatic retry and rate-limit handling.
- Provider failover where possible.
- Cache static/fundamental data to reduce API usage. Fundamentals update quarterly → long TTL, a large reduction in calls on a 30-minute cycle.
- **Normalize responses from multiple providers into a common internal model.**
- Track API quotas and usage.
- **HybridCache** pattern (from builder-catalogue) — run **L1 in-memory only, no Redis**, on a 1GB Pi.

### Configuration & secrets
- Client IDs and URLs in appsettings; **secrets in environment variables** (dummy placeholders in appsettings).
- Email: POP3, credentials as env settings.
- Universal settings pattern: **user JSON override → appsettings default**. Applies to scoring weights, horizon, stage caps, market-cap floor, ADTV thresholds, the CET window.

### Observability
- Serilog + OpenTelemetry (from template).
- **Failure alerting by email.** No high-availability alerting required.

### Testing
- L0 (unit) / L1 (component) / L2 (integration) strategy, from builder-catalogue.
- Template ships xunit.v3, Shouldly, Bogus, Respawn.

### Notifications
- **Email only.** WhatsApp descoped (Business API requires Meta business verification and pre-approved outbound templates — too heavy for a personal project).
- Every notification carries a **TL;DR plus concise supporting evidence**.
- Evidence at model discretion, but the model must be instructed to keep it **concise, factual, mathematical and to the point**.
- **Ad-hoc alerts only.** No alert-volume ceiling needed — history persistence prevents repetition, and only high-potential recommendations are expected.

---

## 10. Decisions and rationale (LADR candidates)

The owner uses **LADR** = lightweight architectural decision record.

1. **Temporal deferred, not adopted.** Temporal Cloud has no free tier and starts at $100/month (greater of $100 or 5% of consumption). That exceeds the entire data-provider budget and contradicts free-tiers-first. The $1,000 signup credit buys ~10 months, then $1,200/year indefinitely; the startup programme requires being a funded company. Self-hosting needs its own Cassandra/PostgreSQL/MySQL cluster — not viable on a 1GB Pi. Recorded as a deferral with rationale, so the decision is visible rather than merely absent. Revisit if volume ever justifies the cost.
2. **On-disk SQLite over in-memory + periodic dump.** See §9.
3. **Technical indicators computed internally.** See §7.
4. **Event-driven funnel over valuation-first screening.** See §4.
5. **`docs/` not `.docs/`.** The template ships `.docs/{wiki,adr,nfr}`, but the repo is public and a dotfolder hides the BRD and HLD from humans. Rename wholesale — two doc trees is the worse outcome. `docs/` is also what GitHub Pages serves from.
6. **`smooth-ai-stockanalysis` is a one-time fork, no upstream tracking.** This is load-bearing: it means free divergence with zero future merge cost — drop Aspire, swap PostgreSQL for SQLite, restructure at will.
7. **Flatten `.agents` scoped rules to direct rules.** Path-scoping earns its complexity across multiple stacks; Phase 1 is backend-only .NET so a `rules-scoped/backend` set matches nearly every file — all indirection, no benefit, plus a silent-non-loading failure mode. Reintroduce when the Phase 2 dashboard adds frontend paths. Record as an ADR.
8. **WhatsApp descoped.** See §9.
9. **Real GitHub issues, not draft items.** Enables native issue types, sub-issues and repo milestones.
10. **ADTV median over trailing 20 sessions excluding the trigger day.** See §8.
11. **OpenCode is dev-time only.** Ruled out as a runtime dependency (would put a Node server on the Pi); it reappears legitimately as the CI review transport in `smooth-ai-report-review`. State this explicitly so it isn't re-litigated.

### Document locations
- BRD → `docs/brd.md`
- HLD → `docs/wiki/hld.md`, or make it `docs/architecture.md` (the template reserves that slot; two architecture docs will drift)
- NFRs → `docs/nfr/` — the HLD summarises and links, does not duplicate
- LADRs → `docs/adr/`
- Root README must link BRD and HLD explicitly.
- Mermaid renders natively in GitHub markdown — no build step for C4 or sequence diagrams.

---

## 11. Source repositories

### `smooth-ai-stockanalysis` — the .NET base (one-time fork)
.NET 10 / ASP.NET Core reference implementation. Clean Architecture across Domain/Application/Infrastructure/Host. Minimal API endpoints. `martinothamar/Mediator` for source-generated CQRS. FluentValidation in a fail-fast pipeline. Serilog + OpenTelemetry. Scalar OpenAPI UI. xunit.v3 / Shouldly / Bogus / Respawn. `.agents/` drives Claude Code, Copilot, Cursor and Codex from one source of truth via symlinks. `.docs/{wiki,adr,nfr}`.

**Divergences required:** ships EF Core + **PostgreSQL** (Npgsql, provisioned via Aspire with a container runtime) — must become SQLite, with Aspire/Docker removed. Touches Respawn-based integration tests. Repo is **placeholder-named** (`smooth-ai-stockanalysis.slnx`, `src/SmoothAiStockAnalysis.*`) — a rename pass is a real task.

### `smooth-ai-report-review` — CI code-review gate
Chunked diffs through the OpenCode CLI as a provider-agnostic transport; posts one consolidated review with findings by priority. Provides `ai-review-report`, `ai-review`, `ai-analyse` skills. **Dev-time tooling only** — never touches the Pi or the runtime agentic layer.

### `builder-catalogue` — CI and `.agents` source
A .NET 10 case-study repo. Aspire used as both F5 orchestrator and integration-test dependency orchestrator. Onion + feature slices. **HybridCache** (L1 memory + L2 Redis). Scalar. L0/L1/L2 test strategy. `.agents/` shared across Claude Code, Codex and Copilot. GitHub Projects as the execution board.

⚠️ **Only the README was read.** `.github/` (CI workflows) and `.agents/rules-scoped/backend` (rules incl. API caching and structural organisation) were **not** retrieved — GitHub API rate limit. **Retry at output time.**

---

## 12. Scaffolding milestone scope

The repo already exists, so this is *update in place*, not *create from template*.

- Update `.slnx`, projects, namespaces; strip the placeholder name.
- Swap PostgreSQL → SQLite; remove Aspire/container dependency.
- Rename `.docs/` → `docs/`; fix all relative links in README and `.agents/`.
- Replace the template's `architecture.md` with the project's own (don't edit around it).
- CI workflows sourced from `builder-catalogue/.github`.
- AI code-review gate from `smooth-ai-report-review`.
- CD producing a usable package deployed on the Pi.
- `.agents/` rules and skills informed by `builder-catalogue`, flattened from scoped to direct.
- **User/security schema** (multi-user-ready) — explicitly in this milestone, not deferred.
- **README for humans:** TL;DR, installation instructions, list of external providers with links to create accounts, and a **disclaimer** that this is a personal research tool and not financial advice.
- **Spikes:** NodaTime + SQLite value converters; EF Core JSON columns on SQLite.

**Provider list for the README:** Polygon.io, Finnhub, Financial Modeling Prep, Benzinga, Reddit API (optional), OpenAI, Anthropic. *(Temporal Cloud removed — deferred.)*

---

## 13. Unresolved tensions

Carry these into artifacts rather than silently resolving them.

1. **Free weight latitude vs. reproducibility.** Unbounded re-weighting means the composite score cannot be reconstructed from inputs — which sits against the stated goal of a transparent, reproducible system. Owner's direction: *do what is possible with current persisted information; document the gap in the BRD's out-of-scope-but-relevant section.* Implication: the notification evidence should carry the weighting actually applied, or it cannot explain the ranking.
2. **Horizon vs. baseline weights.** Resolved in principle (weights shift with configured horizon) but the short-term weight distribution has not been specified.
3. **Bid-ask spread** — unaddressed exit-cost factor, absent from free tiers. BRD out-of-scope-but-relevant.
4. **No Phase 1 feedback loop.** A 10% KPI exists with no mechanism to measure it, because position tracking is Phase 2. In Phase 1 the target is an estimate for the AI to reason against, not a measured outcome.
5. **OpenRouter vs. dual SDKs** — see §7.

---

## 14. Pending deliverables

None produced yet. All four were specified by the owner during the session.

### 14.1 `gh` CLI script
Populates project 2 with the backlog. One-shot (not idempotent). Owner may instead ask an agent to execute the calls directly.

- Org `generic-automation-and-it`, project number **2**, repo `smooth-ai-stockanalysis`.
- **Real issues**, not draft items.
- Native issue types: **Feature** = vertical slice (story), **Task** = horizontal sub-issue, **Bug** = bug.
- Features carry Tasks as **sub-issues**.
- **Milestones are a repo-level issue feature**, not a project field — create them in the repo, assign on issue creation. Projects display them only for real issues.
- Board is known to have Type, Priority and Milestone; the exact field list could not be verified (project is private). Write defensively: create-if-missing, tolerate what exists.
- ⚠️ Native issue types and sub-issue linking are recent GitHub features with partial `gh` CLI coverage — parts may need `gh api graphql`. **Verify against current docs before writing.**
- Scaffolding is the first milestone and may span several features.
- ⚠️ An agent cannot execute this without GitHub credentials; creating issues in the org is a side-effectful action requiring explicit owner authorisation.

### 14.2 BRD
- **Business requirements only — no technical how, why or what.**
- Split into **deliverable milestones. No big bang.**
- Must include: the paid-tier ROI analysis, the out-of-scope/future section (§3), the out-of-scope-but-relevant gaps (§13), and the disclaimer.
- Business objectives and success criteria will need to be **inferred and marked as such**.

### 14.3 HLD
- Structured around the BRD's milestones.
- Sections for high-level design, architecture, **LADRs** and NFRs.
- **C4 C1 + C2 models and high-level sequence diagrams, all in Mermaid.**
- **High level only. Zero code unless a concept can be explained in words.**
- Deeper per-milestone HLDs generated later.

### 14.4 Full structured summary
Complete and ordered, following the structure of the session.

---

## 15. Open items requiring owner input

| # | Item | Blocking |
|---|---|---|
| 1 | Project 2 field list — board is private and unreadable | No (write defensively) |
| 2 | `builder-catalogue` `.github/` and `.agents/rules-scoped/` never read (rate limit) — retry | No |
| 3 | SEK, NOK, GBP currency multipliers — agreed to add, no values given | No |
| 4 | Milestone breakdown 2–n — **delegated to the agent**, largest inferential step | No |
| 5 | Short-term weight distribution (weights shift with horizon, target values unset) | No |
| 6 | Issue granularity — owner asked for "a reasonable amount", agent's judgement | No |

---

## 16. Session conventions

Captured under the `ai-brain-dump` skill with switches `--oktoask` and `--oktowebsearch` enabled: sparse blocker-only questioning, web grounding permitted, no synthesis until explicitly requested.

Owner's working style, worth preserving: corrections are direct and authoritative (e.g. the "AI reasoning: OpenAI" entry was explicitly retracted); contradictions should be surfaced rather than smoothed over; recommendations are wanted with reasoning, not just options.
