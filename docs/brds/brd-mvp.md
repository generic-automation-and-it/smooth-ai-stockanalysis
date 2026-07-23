# Business Requirements Document
## smooth-ai-stockanalysis

| | |
|---|---|
| **Document** | Business Requirements Document (BRD) |
| **Product** | smooth-ai-stockanalysis |
| **Owner** | generic-automation-and-it |
| **Status** | Draft for review |
| **Date** | July 2026 |
| **Intended location** | `docs/brd.md` |

> **Scope of this document.** This BRD states *what the business needs and why*. It deliberately contains no technical design, architecture, or implementation detail — those live in the High Level Design and in per-milestone design documents.

> **Items marked 〔INFERRED〕** were not stated explicitly during requirements gathering and have been derived from context. They require owner confirmation before this document is baselined.

---

## 1. Executive summary

smooth-ai-stockanalysis is a personal decision-support tool that identifies short-term stock trading opportunities and delivers them to a single user by email.

The product does not trade. It observes what changed in the market today, narrows thousands of listed companies down to a small number of high-quality candidates, and explains its reasoning. Every buy and sell decision remains with the human.

The commercial premise is that a small number of well-chosen, low-cost data subscriptions combined with AI-assisted reasoning can produce genuinely useful trade candidates at a fraction of the cost of professional market terminals — and that a disciplined event-driven approach surfaces more actionable opportunities than screening the whole market on valuation every day.

---

## 2. Business objectives 〔INFERRED〕

Requirements gathering focused overwhelmingly on capability rather than business outcome. The following objectives are inferred from context and should be confirmed or replaced by the owner.

| # | Objective |
|---|---|
| BO-1 | Surface short-term trading opportunities the owner would not otherwise have found, without requiring daily manual market research. |
| BO-2 | Reduce the time between a market-moving event occurring and the owner being aware of it and its implications. |
| BO-3 | Provide reasoning transparent enough that the owner can accept or reject each recommendation on its merits, rather than trusting a score. |
| BO-4 | Operate at a monthly cost proportional to demonstrated value, starting at or near zero. |
| BO-5 | Establish a foundation that can later track actual positions, and later still serve additional users. |

---

## 3. Users and stakeholders

| Role | Description |
|---|---|
| **Primary user** | A single individual investor, based in Central European Time, trading US and European (including Nordic) equities. |
| **Product owner** | The same individual. Sets configuration, evaluates output quality, makes all trading decisions. |
| **Future users** | The system must be capable of serving multiple users without redesign, though only one user exists in Phase 1. |

There are no external customers, no regulatory reporting obligations, and no third-party consumers of the output.

---

## 4. Business context

### 4.1 The problem

Meaningful short-term opportunities arise when new information reaches the market — an earnings surprise, an analyst upgrade, insider buying, a contract award, a regulatory approval, a macroeconomic shift. Identifying these across thousands of listed companies, then assessing whether each is worth acting on, is more daily work than an individual investor can sustain.

### 4.2 The approach

The product starts from **what changed today**, not from static valuation.

Valuation-led screening is rejected as a starting point for stated reasons: different sectors trade at structurally different multiples; a low price-to-earnings ratio frequently signals a deteriorating business rather than a bargain; high-growth companies often have high or meaningless earnings multiples; and market-moving opportunities are driven by new information rather than by figures that were equally true last week.

Valuation therefore serves as a **validation step** — deciding whether a move is worth acting on — rather than as the initial filter.

### 4.3 The funnel

Each analysis cycle progressively narrows a large universe to a handful of candidates, applying cheaper and broader checks first and more expensive analysis only to survivors.

```
Market events today
   → companies affected
   → ranked by strength of catalyst
   → sound companies only          (fundamental validation)
   → favourable entry timing       (technical confirmation)
   → external corroboration        (analyst and sentiment signals)
   → AI-ranked opportunities
   → email report
```

---

## 5. Scope

### 5.1 In scope — Phase 1

The complete analysis-and-recommendation workflow, delivered as a sequence of milestones. No single large release.

### 5.2 Out of scope — Phase 1

| Item | Rationale |
|---|---|
| Trade execution and broker integration | The human places every order. Possible far-future phase. |
| User authentication and access control | Single user on a private home network. |
| Position and portfolio tracking | Deferred to Phase 2. |
| Sell-side recommendations | Deferred to Phase 2. |
| A user interface of any kind | Email is the only interface in Phase 1. |
| Messaging channels other than email | See BR-31. |
| Multiple users in production | The system must be *capable* of it; it will not be exercised. |

### 5.3 Future phases

**Phase 2 — Position awareness**

The owner replies by email stating what was actually bought and sold. The system records these positions and tailors subsequent recommendations to holdings. This introduces the first inbound channel; everything in Phase 1 is outbound only.

Delivered together with:
- Loss-collar notifications — alerting when a held position moves adversely beyond a threshold.
- Sell-side recommendation analysis, considering profit collar, potential loss, and broad market-decline conditions.
- Measurement of actual outcomes against the target return, converting the target from an estimate into a tracked result.

**Phase 3 — Interface and access**

- A user dashboard.
- User authentication via Apple sign-in, with role-based access control.

**Future, low priority**

- Allow the user to express the minimum company size limit in a currency of their choosing, with a daily exchange-rate refresh that adjusts currency translations when a rate moves more than 5% away from the value currently in use.

---

## 6. Business requirements

### 6.1 Market awareness

| # | Requirement | Priority |
|---|---|---|
| BR-1 | Detect market-moving events daily, including earnings surprises, analyst rating changes, insider buying, significant company news, macroeconomic and central bank announcements, unusual trading volume, and large price gaps. | Critical |
| BR-2 | Identify which listed companies are affected by each detected event. | Critical |
| BR-3 | Rank detected events by the strength of the catalyst, so that limited analysis capacity is spent on the most significant. | Critical |
| BR-4 | Never analyse the same event twice. Each event is assessed once, and repeat notifications for an already-reported event are suppressed. | Critical |
| BR-5 | Retain one month of analysis history per company, so that prior findings inform later assessments. | High |
| BR-6 | Cover the United States market as primary, and European markets including the Nordics where data availability allows. | High |

### 6.2 Candidate quality

| # | Requirement | Priority |
|---|---|---|
| BR-7 | Exclude companies below a minimum size, expressed as a single base figure in euro with a fixed per-market currency conversion. Default: €1,000 million. | Critical |
| BR-8 | Exclude over-the-counter and penny stocks entirely. | Critical |
| BR-9 | Exclude companies whose shares cannot be traded in reasonable size without difficulty, measured by average daily traded value. Default minimum: €3 million. | Critical |
| BR-10 | Measure trading liquidity on a normal trading day rather than on the day a catalyst occurred, since catalysts inflate volume precisely when a company is being evaluated. | Critical |
| BR-11 | Limit any prospective position to a defined share of a company's daily traded value, so that exiting the position does not itself move the price. Default: 10%. | Medium |
| BR-12 | Flag — but never automatically exclude — companies whose shares change hands unusually rarely relative to their available float. | Medium |
| BR-13 | Where a company's listing currency has no defined conversion, exclude it rather than assume parity. | High |

> **Rationale for BR-7 and BR-9.** These are liquidity and manipulation guards, not claims about company stability. The €1,000 million threshold sits at the upper end of the small-capitalisation band — large enough that a position can be exited at a fair price, small enough that a meaningful short-term move remains plausible.

### 6.3 Company validation

| # | Requirement | Priority |
|---|---|---|
| BR-14 | Assess each candidate's financial soundness using income statement, balance sheet and cash flow information, key financial ratios, valuation measures, revenue and earnings growth, and dividend information. | Critical |
| BR-15 | Eliminate companies whose fundamentals indicate the market move is speculative rather than supported. | Critical |
| BR-16 | Assess operating profitability, capital efficiency and the ability to service debt, using operating-earnings-derived measures. | High |
| BR-17 | Compare each company against its own sector rather than against the market as a whole, since profitability and valuation norms differ materially between sectors. | High |
| BR-18 | Assess each company's scale relative to its sector peers, as an indicator of competitive position. | Medium |
| BR-19 | Exempt sectors where operating-earnings measures are not meaningful — including banks, insurers and property trusts — and companies that are not yet profitable, so that these are not wrongly eliminated. | High |

> **Note on BR-18.** An earlier requirement specified a minimum 20% market share. Market share is not a reported financial metric, is not available from any data provider, and depends on how the addressable market is defined. It has been replaced by relative scale within sector, which expresses the same commercial intent using obtainable information.

### 6.4 Timing

| # | Requirement | Priority |
|---|---|---|
| BR-20 | Assess whether the timing of an entry is favourable, using trend, momentum, relative volume, support and resistance, and breakout behaviour. | High |
| BR-21 | Derive all timing measures from price and volume information already obtained, rather than purchasing them separately. | High |

### 6.5 Corroboration

| # | Requirement | Priority |
|---|---|---|
| BR-22 | Incorporate professional analyst ratings, consensus recommendations, target prices, rating changes and estimate revisions. | High |
| BR-23 | Incorporate sentiment derived from financial news. | High |
| BR-24 | Incorporate social sentiment as an optional enrichment that adjusts confidence only. Social sentiment must never drive a recommendation. | Low |
| BR-25 | Incorporate insider buying and selling activity and regulatory filings. | Medium |
| BR-26 | Provide a forward calendar of earnings, dividends, initial public offerings, share splits and economic events. | Medium |

### 6.6 Recommendation

| # | Requirement | Priority |
|---|---|---|
| BR-27 | Produce a ranked list of opportunities, each with a confidence score, supporting rationale, a bull case, a bear case, key risks, and a suggested holding period. | Critical |
| BR-28 | Combine catalyst strength, fundamentals, timing, analyst sentiment and social sentiment into an overall assessment, using weightings the user can configure. | Critical |
| BR-29 | Adjust the default weightings according to the configured holding horizon, since a days-to-weeks horizon and a months-long horizon reward different evidence. | High |
| BR-30 | Permit the AI to depart from the configured weightings where the evidence warrants, and to explain why a company ranks highly rather than simply reporting a number. | High |

### 6.7 Delivery

| # | Requirement | Priority |
|---|---|---|
| BR-31 | Deliver recommendations by email. Email is the only supported channel. | Critical |
| BR-32 | Every notification must open with a short summary, followed by concise, factual, quantitative evidence. Length is constrained; the reader must be able to act on the summary alone. | Critical |
| BR-33 | Publish only genuine recommendations. If fewer than five qualify, publish fewer. If none qualify, publish nothing. Volume is never manufactured. | Critical |
| BR-34 | Publish no more than five recommendations per cycle. | High |
| BR-35 | Send alerts as events warrant rather than on a schedule. Repetition is prevented by BR-4. | Medium |
| BR-36 | Deliver recommendations within a configurable daily window. Default: 07:00–22:00 Central European Time, covering pre-market and open US trading hours as well as European trading hours. | High |
| BR-37 | Notify the owner by email when the system fails to complete a cycle. | Medium |

### 6.8 Operation and cost

| # | Requirement | Priority |
|---|---|---|
| BR-38 | Operate unattended on hardware the owner already owns, with no recurring hosting cost. | Critical |
| BR-39 | Run continuously at a configurable interval. Default: every 30 minutes, skipping a cycle if the previous one has not completed. | Critical |
| BR-40 | Support manual, on-demand execution of a single cycle, for evaluation and during the proof of concept. | Critical |
| BR-41 | Cap the number of companies carried into each successive stage of analysis, so that cost per cycle is bounded and predictable irrespective of how many events occur. Defaults: 50 validated, 20 timing-checked, 10 AI-analysed, 5 published. | Critical |
| BR-42 | Track consumption of each data subscription against its allowance. | High |
| BR-43 | Continue operating when a data provider is unavailable, using an alternative where one exists. | High |
| BR-44 | Constrain AI processing spend to a defined ceiling, and behave predictably when the ceiling is reached. | High |
| BR-45 | Reuse information that changes infrequently rather than repeatedly purchasing it. | High |

### 6.9 Configuration and future readiness

| # | Requirement | Priority |
|---|---|---|
| BR-46 | Allow the user to override any default — company size floor, liquidity thresholds, scoring weightings, holding horizon, stage limits, delivery window — with a personal setting. | High |
| BR-47 | Associate all user-specific information with an identified user from first release, so that supporting additional users later requires no restructuring. | Critical |
| BR-48 | Treat market information, company financials, news and sector data as shared, so that adding users does not multiply data subscription costs. | High |
| BR-49 | Publish the product publicly with documentation sufficient for a competent third party to install and operate it, including a list of every external data provider required and how to obtain access. | High |
| BR-50 | Display a clear disclaimer that the product is a personal research tool and not financial advice. | Critical |

---

## 7. Delivery milestones

Delivered incrementally. Each milestone is independently valuable and independently demonstrable.

〔INFERRED〕 *The milestone breakdown below was delegated to be proposed rather than specified. Sequence and grouping are open to revision; the constraint honoured throughout is that no milestone is a "big bang".*

### Release 1 — Proof of concept

Manually triggered. Free data subscriptions only. Goal: prove the concept end to end and produce a first real recommendation.

---

#### M1 — Foundation

| | |
|---|---|
| **Goal** | A deployable, documented product skeleton with nothing yet to say. |
| **Business value** | Everything downstream depends on it; delivering it separately prevents foundation work hiding inside feature work. |
| **Covers** | BR-38, BR-47, BR-49, BR-50 |

**Delivered**
- The product installs and runs unattended on the owner's own hardware, and also runs locally for evaluation.
- Automated quality gates run on every change, including AI-assisted review.
- A single user exists, with personal settings, and all user-owned information is associated with them.
- Public documentation: what the product is, how to install it, which external providers are required and how to obtain access to each, and the disclaimer.

**Done when** the product deploys unattended to the owner's hardware from a single automated release, and a third party could follow the documentation to install it.

---

#### M2 — First recommendation

| | |
|---|---|
| **Goal** | One catalyst type, one data provider, one email — end to end. |
| **Business value** | Proves scheduling, storage, analysis and delivery work together before any expensive capability is built on top. |
| **Covers** | BR-1 (partial), BR-27 (partial), BR-31, BR-40 |

**Delivered**
- A single category of market event is detected from one provider.
- Affected companies are identified and scored by a fixed, transparent rule.
- One email is delivered containing a summary and its supporting evidence.
- Triggered manually, on demand.

**Done when** the owner receives a real, correct email describing a real market event, produced unattended on the target hardware.

---

### Release 2 — Analysis depth

Each milestone adds one stage of the funnel. The product produces output throughout; the output gets better.

---

#### M3 — Catalyst coverage

| | |
|---|---|
| **Goal** | See everything that moved today, and never look at it twice. |
| **Covers** | BR-1, BR-2, BR-3, BR-4, BR-5, BR-6 |

**Delivered**
- Full breadth of event detection: earnings surprises, analyst rating changes, insider buying, company news, macroeconomic events, unusual volume, large price gaps.
- Events ranked by catalyst strength.
- Repeat detection of the same event, including by different providers, resolves to a single event.
- One month of analysis history retained per company.

**Done when** a day's genuine market events are detected and ranked with no duplicates across a full week of operation.

---

#### M4 — Tradeability filters

| | |
|---|---|
| **Goal** | Only surface companies the owner could realistically buy and sell. |
| **Covers** | BR-7 to BR-13 |

**Delivered**
- Minimum company size, with per-market currency conversion.
- Over-the-counter and penny stocks excluded.
- Liquidity threshold applied, measured on normal trading days rather than catalyst days.
- Low-turnover companies flagged rather than excluded.

**Done when** every company reaching the next stage meets the size and liquidity thresholds, and the owner agrees the survivors are genuinely tradeable.

---

#### M5 — Fundamental validation

| | |
|---|---|
| **Goal** | Separate sound companies from speculative moves. |
| **Covers** | BR-14, BR-15 |

**Delivered**
- Financial statements, ratios, valuation measures, growth and dividend information for each candidate.
- Elimination of companies whose fundamentals do not support the move.

**Done when** the candidate list narrows to the configured limit on the basis of financial soundness, and eliminations are explainable.

---

#### M6 — Sector context

| | |
|---|---|
| **Goal** | Comparisons that mean something. |
| **Covers** | BR-16, BR-17, BR-18, BR-19 |

**Delivered**
- Sector aggregates computed once and reused.
- Each company assessed relative to its sector rather than the whole market.
- Operating profitability, capital efficiency and debt-service measures.
- Relative scale within sector as a competitive-position indicator.
- Sector exemptions where these measures do not apply.

**Done when** a bank, a property trust and a pre-profit growth company all pass through without being wrongly eliminated.

---

#### M7 — Timing confirmation

| | |
|---|---|
| **Goal** | Is now a good moment to enter? |
| **Covers** | BR-20, BR-21 |

**Delivered**
- Trend, momentum, relative volume, support and resistance, breakout detection.
- All derived from information already obtained, purchased separately from no one.

**Done when** the candidate list narrows to the configured limit on timing grounds and each judgement can be explained.

---

#### M8 — Corroboration

| | |
|---|---|
| **Goal** | What does everyone else think? |
| **Covers** | BR-22, BR-23, BR-25, BR-26 |

**Delivered**
- Analyst ratings, consensus, targets, changes and revisions.
- News-derived sentiment.
- Insider activity and regulatory filings.
- Forward event calendar.

*Social sentiment (BR-24) is explicitly excluded from this milestone and deferred — see M13.*

**Done when** each candidate carries external corroboration alongside the system's own assessment.

---

#### M9 — Recommendation engine

| | |
|---|---|
| **Goal** | The product. |
| **Covers** | BR-27 to BR-30, BR-41, BR-44 |

**Delivered**
- All signals combined into a ranked assessment using configurable weightings.
- Weightings adjust to the configured holding horizon.
- The AI may depart from configured weightings where evidence warrants.
- Each recommendation carries confidence, rationale, bull case, bear case, key risks and suggested holding period.
- Stage limits enforced throughout, bounding cost per cycle.
- AI spend ceiling enforced.

**Done when** the owner reviews a week of output and judges the reasoning sound enough to act on.

---

### Release 3 — Product

---

#### M10 — Reporting and alerting

| | |
|---|---|
| **Goal** | Output the owner can act on in under a minute. |
| **Covers** | BR-31 to BR-37 |

**Delivered**
- Report format: summary first, then concise factual quantitative evidence.
- Publish fewer than five, or none, where warranted.
- Ad-hoc alerts, de-duplicated.
- Delivery constrained to the configured daily window.
- Failure notification.

**Done when** the owner can decide on each recommendation from the summary alone, and has received a failure notification from a deliberately induced failure.

---

#### M11 — Provider cost and ROI evaluation

| | |
|---|---|
| **Goal** | Decide what is worth paying for, on evidence. |
| **Covers** | BR-42, BR-45, §8 of this document |

**Delivered**
- Measured record of where free subscription limits constrained output during Releases 1 and 2.
- Assessment of each paid upgrade against the value it would add.
- A decision on which subscriptions to purchase before continuous operation.

**Done when** the owner has a costed, evidence-based recommendation for the monthly data budget.

---

#### M12 — Continuous operation

| | |
|---|---|
| **Goal** | It runs by itself. |
| **Covers** | BR-39, BR-43, BR-45 |

**Delivered**
- Continuous cycles at the configured interval, skipping if the previous cycle is still running.
- Paid subscriptions in place per M11.
- Continued operation when a provider is unavailable.
- Reuse of infrequently-changing information to control cost.

**Done when** the system runs unattended for two weeks, delivering within its window, with no manual intervention.

---

#### M13 — Social sentiment *(optional)*

| | |
|---|---|
| **Goal** | A confidence adjustment, nothing more. |
| **Covers** | BR-24 |

**Delivered**
- Social sentiment aggregated and used solely to adjust confidence.

**Done when** social sentiment visibly adjusts confidence without ever changing which companies are recommended.

> Deliberately last and deliberately optional. Lowest-weight signal, lowest reliability, and the only one that can be omitted entirely without affecting the product's value.

---

### Milestone summary

| Release | Milestone | Trigger | Subscriptions |
|---|---|---|---|
| 1 | M1 Foundation | — | None |
| 1 | M2 First recommendation | Manual | Free |
| 2 | M3 Catalyst coverage | Manual | Free |
| 2 | M4 Tradeability filters | Manual | Free |
| 2 | M5 Fundamental validation | Manual | Free |
| 2 | M6 Sector context | Manual | Free |
| 2 | M7 Timing confirmation | Manual | Free |
| 2 | M8 Corroboration | Manual | Free |
| 2 | M9 Recommendation engine | Manual | Free + AI |
| 3 | M10 Reporting and alerting | Manual | Free + AI |
| 3 | M11 Cost and ROI evaluation | — | — |
| 3 | M12 Continuous operation | Automatic | Paid as decided |
| 3 | M13 Social sentiment *(optional)* | Automatic | Optional |

---

## 8. Data subscription strategy and cost

### 8.1 Principle

**Purchase what cannot be reproduced; compute what can.**

Purchase: news, analyst ratings, company financials, earnings estimates, insider transactions.
Compute: all timing and momentum measures, and all composite scoring.

This reduces both cost and dependence on any single supplier.

### 8.2 Commercial posture

Begin on free subscriptions. Add paid subscriptions only where a measured limitation is blocking value, and only in ascending order of cost. Discounted startup subscriptions to be taken where offered.

### 8.3 Free subscription limits

Verified July 2026. Figures change; re-confirm before purchase.

| Provider | Free allowance | Principal limitation |
|---|---|---|
| Finnhub | ~60 calls/minute | International coverage requires a paid plan |
| Financial Modeling Prep | 250 calls/day | Limited history |
| Twelve Data | ~800 calls/day | — |
| Polygon.io *(now Massive)* | 5 calls/minute | End-of-day and 15-minute delayed data only |
| Alpha Vantage | 25 calls/day | Severely limiting for any multi-company scan |
| EODHD | 20 calls/day | Severely limiting |
| Benzinga | None | Paid only |

> **Supplier change.** Polygon.io has rebranded to Massive.com and its pricing was in transition at the time of writing. Confirm current terms before relying on any figure above.

### 8.4 Paid upgrade analysis

Ordered by return on cost. This ordering is the recommended purchase sequence.

**First — Finnhub Premium. Approximately $12–100 per month.**

The highest-value upgrade by a wide margin. Finnhub's free allowance is unusually generous and already covers company financials, analyst ratings, earnings and insider activity for the US market. However, **international coverage requires a paid plan** — and European and Nordic coverage is a stated business requirement (BR-6) that the free plan cannot satisfy at any usage level. This is the only upgrade that unlocks a requirement rather than merely relaxing a limit. Low cost, direct requirement coverage, single supplier serving four capability areas.

**Second — Polygon/Massive paid tier. Price to be confirmed following rebrand.**

The free allowance of 5 calls per minute is the binding constraint on how many companies can be assessed per cycle, and delayed pricing weakens timing judgements. Whether this matters depends on measured experience during Release 2. For a holding period of days to two weeks, 15-minute-delayed pricing may prove entirely adequate — in which case the upgrade should be deferred indefinitely. **Decide on evidence from M11, not in advance.**

**Third — Benzinga Essential. Approximately $166 per month on annual billing, $197 monthly.**

The most expensive single line item by an order of magnitude, at roughly €1,800–2,100 per year. It also feeds the highest-weighted input in the scoring model, so the theoretical case is strong: fastest proprietary news, sentiment indicators, calendars.

The commercial case is nonetheless weak for a personal tool. Benzinga's speed advantage is calibrated to intraday desk trading measured in seconds. For a holding period of days to two weeks, arriving at a story minutes rather than milliseconds after it breaks is immaterial. Free news from Finnhub, supplemented by news sentiment from Alpha Vantage, plausibly covers the same requirement at zero cost with a delay the business model does not care about.

**Recommendation: do not purchase before M11.** Run Releases 1 and 2 on free news sources and measure specifically whether news latency or coverage — as opposed to news *quality* — actually caused a missed opportunity. Purchase only if that evidence exists. This single decision is the difference between a data budget of roughly €150 per year and roughly €2,200 per year.

**Excluded — managed workflow orchestration.**

Evaluated and rejected on cost. The candidate service carries a minimum of $100 per month with no free tier — exceeding the entire remaining data budget for infrastructure the product does not currently need. Revisit only if scale ever justifies it.

### 8.5 Indicative annual cost

| Scenario | Approximate annual cost |
|---|---|
| Proof of concept — free subscriptions only | €0 plus AI usage |
| Minimum viable — Finnhub Premium only | €150–1,200 |
| Full stack including Benzinga | €2,000–3,500 |

AI processing cost is separate, controlled by a spend ceiling (BR-44), and bounded by design: no more than ten companies reach AI analysis per cycle regardless of market activity (BR-41).

---

## 9. Success criteria

| # | Criterion | Measure |
|---|---|---|
| SC-1 | The system surfaces opportunities the owner would not otherwise have found. | Owner judgement, reviewed after each release. |
| SC-2 | Recommendations are actionable from the summary alone. | Owner can decide within one minute of opening the email. |
| SC-3 | Output is trustworthy enough to act on. | Owner acts on at least one recommendation without independent re-research. |
| SC-4 | The system does not generate noise. | Cycles producing no recommendation are common and unremarkable. |
| SC-5 | Cost remains proportionate to value. | Monthly spend stays within the ceiling set at M11. |
| SC-6 | The system runs unattended. | Two consecutive weeks with no manual intervention. |
| SC-7 〔INFERRED〕 | Recommendations achieve approximately 10% return within days to two weeks. | **Not measurable in Phase 1** — see §10. |

### On the 10% target

The target return of approximately 10% over a holding period of days to two weeks is, in Phase 1, **an estimate for the AI to reason towards** — a statement of what constitutes an opportunity worth surfacing. It is not a measured outcome, because the system does not know what the owner actually bought.

It becomes a measurable success criterion in Phase 2, when position tracking exists.

---

## 10. Known gaps

Acknowledged, deliberately unresolved, recorded so they are not mistaken for oversights.

| # | Gap | Consequence |
|---|---|---|
| GAP-1 | **No outcome feedback in Phase 1.** The system cannot learn whether its recommendations were correct, because it does not know which were acted upon. | Quality is assessed by owner judgement alone. Resolved in Phase 2. |
| GAP-2 | **Transaction spread is not assessed.** The gap between buying and selling price is a real cost of entering and exiting, and is not available from free data sources. | Exit cost is understated. Liquidity thresholds (BR-9) partially compensate. |
| GAP-3 | **Scores are not fully reproducible.** BR-30 permits the AI to depart from configured weightings, so an identical set of inputs may not produce an identical score. | Accepted trade-off for reasoning quality. Mitigation: reported evidence should convey the weighting actually applied, or it cannot explain the ranking. |
| GAP-4 | **Currency conversions are fixed.** Rates drift. | Immaterial for a coarse size threshold; revisited in the low-priority future item in §5.3. |
| GAP-5 | **Currency conversions are defined for only two markets.** | Nordic and UK listings cannot currently be assessed against the size threshold. Values required before M4. |
| GAP-6 | **Short-horizon weightings undefined.** BR-29 requires weightings to shift with the holding horizon; the values for the short-horizon default are not yet set. | Required before M9. |

---

## 11. Assumptions

| # | Assumption |
|---|---|
| A-1 | The owner reads email daily and can act on recommendations within the trading day. |
| A-2 | Delayed rather than real-time pricing is adequate for a days-to-weeks holding period. |
| A-3 | Free subscription allowances are sufficient to prove the concept, if not to operate continuously. |
| A-4 | The owner's own hardware remains available and connected. |
| A-5 | A single user is sufficient for the foreseeable future. |
| A-6 | Data providers remain available on substantially their current commercial terms. |

---

## 12. Constraints

| # | Constraint |
|---|---|
| C-1 | Operates on the owner's existing home hardware. No recurring hosting cost. |
| C-2 | Runs on a private home network with no external access. |
| C-3 | Free subscriptions first; paid subscriptions only on demonstrated value. |
| C-4 | Email is the only delivery channel. Messaging platforms requiring business verification and message-template approval are excluded as disproportionate. |
| C-5 | Published publicly, so all documentation and disclaimers must suit an external reader. |
| C-6 | The system never places a trade. |

---

## 13. Risks

| # | Risk | Impact | Mitigation |
|---|---|---|---|
| R-1 | Free subscription limits prove too restrictive to prove the concept. | Release 2 stalls. | Manual triggering rather than continuous cycles during the proof of concept; M11 decision point. |
| R-2 | Recommendation quality is insufficient to act on. | Product fails its purpose. | Owner reviews output at every milestone rather than at the end. |
| R-3 | A data provider changes terms, pricing, or withdraws. | Capability lost. | Requirement BR-43; no single provider covers more than one capability area alone. Already realised once during requirements gathering, via a supplier rebrand. |
| R-4 | The system generates plausible but unsound recommendations. | Financial loss. | The human decides every trade (C-6); reasoning is published with every recommendation (BR-27); disclaimer (BR-50). |
| R-5 | The owner over-trusts the output. | Financial loss. | Bull case, bear case and key risks are mandatory in every recommendation, not optional. |
| R-6 | Ongoing cost exceeds value delivered. | Product abandoned. | Free-first posture; explicit ROI decision point at M11. |

---

## 14. Disclaimer

smooth-ai-stockanalysis is a personal research and decision-support tool. It is not financial advice, investment advice, or a recommendation to buy or sell any security.

Its output is generated automatically from third-party data and AI-assisted analysis, and may be incomplete, delayed, or wrong. No representation is made as to accuracy or fitness for any purpose. The software places no trades and takes no positions.

Anyone using this software does so entirely at their own risk and remains solely responsible for their own investment decisions. Consult a qualified, licensed financial adviser before making investment decisions.

---

## 15. Glossary

| Term | Meaning |
|---|---|
| **Catalyst** | A new piece of information capable of moving a share price — earnings surprise, rating change, news event, insider transaction, unusual volume. |
| **Confidence score** | The system's own assessment of how strong the case for a recommendation is. |
| **Cycle** | One complete pass of the analysis funnel, from event detection to publication. |
| **Fundamentals** | A company's reported financial position and performance. |
| **Holding horizon** | How long a position is expected to be held. Default: days to two weeks. |
| **Liquidity** | How readily shares can be bought or sold without moving the price. |
| **MVP** | Minimum viable product — the point at which the system runs continuously and unattended. |
| **POC** | Proof of concept — manually triggered, free subscriptions, proving the approach works. |
| **Universe** | The full set of listed companies the system may consider. |
