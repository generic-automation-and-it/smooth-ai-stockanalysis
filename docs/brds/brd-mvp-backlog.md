# Backlog Specification
## smooth-ai-stockanalysis — issues to be created

| | |
|---|---|
| **Purpose** | Complete specification of every milestone, Feature and Task to be created in GitHub. Written for an agent to execute. |
| **Target repo** | `generic-automation-and-it/smooth-ai-stockanalysis` |
| **Target board** | `github.com/orgs/generic-automation-and-it/smooth-ai-stockanalysiss/3` |
| **Source** | `docs/brd.md` (milestones, requirements), `docs/wiki/hld.md` (architecture), `docs/hlds/mvp/ladrs/` (decisions) |
| **Date** | July 2026 |

---

## 1. Instructions for the executing agent

### 1.1 Order of operations

1. **Create the 13 repo milestones** (§2). Milestones are a repository-level issue feature, not a project field — they must exist before issues can be assigned to them.
2. **Create all Feature issues.** Type `Feature`. Assign milestone and priority.
3. **Create all Task issues.** Type `Task`. Assign the same milestone as their parent.
4. **Link each Task to its parent Feature as a sub-issue.**
5. **Add every issue to project 3.**

### 1.2 Conventions

| Convention | Value |
|---|---|
| **Feature** | A vertical slice. Delivers something demonstrable on its own. |
| **Task** | A horizontal sub-issue of exactly one Feature. |
| **Bug** | Not used in this specification. |
| **ID scheme** | `F-nnn` and `T-nnn` are *specification* identifiers for cross-reference in this document only. Do not put them in issue titles. |
| **Milestone** | Every issue carries one, matching its parent Feature. |
| **Priority** | From the BRD requirement it covers. Critical / High / Medium / Low. |

### 1.3 Issue body template

**Features** — use the description and acceptance criteria given for each. Include a line referencing the BRD requirements covered.

**Tasks** — the one-line description given is the issue body. Expand only if the meaning would otherwise be lost.

### 1.4 Known unknowns

- Project 3 was verified before execution. It carries Milestone and default project fields; create Type and Priority if missing, tolerate what exists.
- Native issue types and sub-issue linking have partial CLI coverage. Some operations may require the GraphQL API. **Verify against current documentation before starting.**
- Creating issues is a side-effectful action in a real organisation. **Confirm with the owner before executing.**

### 1.5 Totals

| | Count |
|---|---|
| Milestones | 13 |
| Features | 40 |
| Tasks | 203 |
| **Total issues** | **243** |

---

## 2. Milestones

Create these first, in this order. Descriptions are the milestone description field.

| # | Title | Description |
|---|---|---|
| M1 | M1 — Foundation | A deployable, documented product skeleton with nothing yet to say. |
| M2 | M2 — First recommendation | One catalyst type, one provider, one email — end to end. |
| M3 | M3 — Catalyst coverage | See everything that moved today, and never look at it twice. |
| M4 | M4 — Tradeability filters | Only surface companies that could realistically be bought and sold. |
| M5 | M5 — Fundamental validation | Separate sound companies from speculative moves. |
| M6 | M6 — Sector context | Comparisons that mean something. |
| M7 | M7 — Timing confirmation | Is now a good moment to enter? |
| M8 | M8 — Corroboration | What does everyone else think? |
| M9 | M9 — Recommendation engine | The product. |
| M10 | M10 — Reporting and alerting | Output that can be acted on in under a minute. |
| M11 | M11 — Provider cost and ROI evaluation | Decide what is worth paying for, on evidence. |
| M12 | M12 — Continuous operation | It runs by itself. |
| M13 | M13 — Social sentiment *(optional)* | A confidence adjustment, nothing more. |

---

## 3. M1 — Foundation

*Release 1 · Proof of concept · Covers BR-38, BR-47, BR-49, BR-50*

---

### F-001 · Solution rename and structure alignment
**Milestone** M1 · **Priority** Critical

The repository was created from a template that is placeholder-named throughout. Rename it to the project and confirm the layer structure matches the intended architecture before anything is built on it.

*Acceptance:* solution builds and all tests pass under the new name, with no placeholder identifier remaining anywhere in the tree.

| ID | Task |
|---|---|
| T-001 | Rename the solution file and all project files to the project name |
| T-002 | Rename all namespaces and update using directives across the tree |
| T-003 | Confirm the four-layer structure and that dependencies point inward only |
| T-004 | Remove template sample code and features not relevant to this product |
| T-005 | Verify a clean build and full test run after the rename |

---

### F-002 · Persistence and time foundation
**Milestone** M1 · **Priority** Critical

Replace the template's server database with a local file-based store, and establish time handling. Both carry technical uncertainty that must be resolved here rather than discovered later. See LADR-002.

*Acceptance:* the application starts against a local database file with no container runtime present, and stores and retrieves an instant correctly across a daylight-saving boundary.

| ID | Task |
|---|---|
| T-006 | Replace the PostgreSQL provider with SQLite |
| T-007 | Remove the orchestration and container-runtime dependency |
| T-008 | Configure write-ahead journaling and relaxed synchronous writes |
| T-009 | Establish one-transaction-per-cycle as the write pattern |
| T-010 | **Spike:** NodaTime value converters on SQLite — establish the storage representation |
| T-011 | Implement the chosen NodaTime conversions and cover with tests |
| T-012 | Add a timezone-aware window helper using a named zone, never a fixed offset |
| T-013 | Rework integration tests to run without containers |
| T-014 | Add the retention job skeleton for one-month pruning |

---

### F-003 · User identity and data isolation
**Milestone** M1 · **Priority** Critical

Establish user identity and enforced isolation from the first release, so that supporting further users later requires no restructuring. See LADR-010.

*Acceptance:* a query for user-owned data returns nothing for a different user, without the calling feature doing anything to make that true.

| ID | Task |
|---|---|
| T-015 | **Spike:** structured-document column support on SQLite — decide converter versus native mapping |
| T-016 | Create the user entity with a versioned metadata document |
| T-017 | Add the user reference to all user-owned entities |
| T-018 | Implement the global isolation filter at the data-access layer |
| T-019 | Implement an explicit current-user scope for background execution |
| T-020 | Implement an explicit system scope that bypasses the filter for shared ingestion |
| T-021 | Make uniqueness constraints composite on the user reference |
| T-022 | Seed the default user from deployment configuration |
| T-023 | Test isolation, including that shared reference data is *not* filtered |

---

### F-004 · Configuration and settings resolution
**Milestone** M1 · **Priority** High

One pattern for every tunable value: a user preference if set, otherwise an application default. Building it once here prevents twelve settings each being designed separately.

*Acceptance:* a value overridden in user metadata resolves to the override; unset, it resolves to the application default; invalid configuration fails at startup rather than mid-cycle.

| ID | Task |
|---|---|
| T-024 | Implement two-layer resolution: user preference over application default |
| T-025 | Define the settings catalogue with every tunable value and its default |
| T-026 | Validate configuration at startup and fail fast |
| T-027 | Read credentials from environment variables only |
| T-028 | Add placeholder values to committed configuration so shape is documented without secrets |

---

### F-005 · Documentation restructure and public README
**Milestone** M1 · **Priority** High

The repository is public. Make documentation visible and write for an external reader. See LADR-007.

*Acceptance:* a competent third party could install and run the product from the README alone.

| ID | Task |
|---|---|
| T-029 | Rename the hidden documentation folder to a visible one |
| T-030 | Fix all relative documentation links in the README and agent configuration |
| T-031 | Replace the template's architecture page with the project's own |
| T-032 | Write the human README: what it is, TL;DR, installation instructions |
| T-033 | Add the external provider list with links to create an account for each |
| T-034 | Add the disclaimer that this is a personal research tool, not financial advice |
| T-035 | Link the business requirements, high level design and decision records from the README |

---

### F-006 · CI pipeline and AI review gate
**Milestone** M1 · **Priority** High

Automated quality gates on every change, including AI-assisted code review.

*Acceptance:* a pull request runs build, tests and AI review, and a consolidated review is posted back to the pull request.

| ID | Task |
|---|---|
| T-036 | Port continuous integration workflows from the reference catalogue repository |
| T-037 | Run build and full test suite on every pull request |
| T-038 | Add formatting and analyzer gates |
| T-039 | Separate the test levels so unit, component and integration runs are distinguishable |
| T-040 | Wire the AI code review gate and configure diff chunking |
| T-041 | Provision review credentials as repository secrets |

---

### F-007 · Deployment to target hardware
**Milestone** M1 · **Priority** Critical

The product installs and runs unattended on the owner's own hardware, from a single automated release.

*Acceptance:* a release deploys to the device unattended and the health endpoint responds after restart of the device.

| ID | Task |
|---|---|
| T-042 | Publish a self-contained build for the target processor architecture |
| T-043 | Create the service definition so it starts on boot and restarts on failure |
| T-044 | Build the deployment workflow producing a downloadable release artifact |
| T-045 | Document credential and configuration provisioning on the device |
| T-046 | Verify unattended restart and recovery after a device reboot |
| T-047 | Document the rollback procedure |

---

### F-008 · Agent rules and skills
**Milestone** M1 · **Priority** Medium

Port agent configuration from the reference catalogue and flatten it. See LADR-008.

*Acceptance:* rules load for every supported agent tool with no path-scoping indirection.

| ID | Task |
|---|---|
| T-048 | Port agent rules and skills from the reference catalogue repository |
| T-049 | Flatten path-scoped rules to direct rules |
| T-050 | Add rules for provider caching behaviour and structural organisation |
| T-051 | Verify configuration resolves across all supported agent tools |

---

### F-009 · API host and observability baseline
**Milestone** M1 · **Priority** High

The entry points and the operational contract. Modest by design: structured logs, tracing, and an email when a cycle fails.

*Acceptance:* interactive API documentation is reachable, the health endpoint responds, and an induced failure produces an email.

| ID | Task |
|---|---|
| T-052 | Stand up the API host with interactive documentation |
| T-053 | Add the health endpoint |
| T-054 | Configure structured logging |
| T-055 | Configure tracing |
| T-056 | Implement the failure notification sender |

---

## 4. M2 — First recommendation

*Release 1 · Proof of concept · Covers BR-1 (partial), BR-27 (partial), BR-31, BR-40*

---

### F-010 · Pipeline host, run lock and stage state
**Milestone** M2 · **Priority** Critical

The execution shell. Delivers skip-if-running and resume-after-crash from persisted state rather than an orchestration platform. See LADR-003.

*Acceptance:* a cycle killed mid-run resumes from its last completed stage; a second trigger during a running cycle is skipped, not queued.

| ID | Task |
|---|---|
| T-057 | Implement the background pipeline host with an interval timer, disabled by default |
| T-058 | Implement the persisted run lock with claim and release |
| T-059 | Implement per-stage state recording |
| T-060 | Implement resume-from-last-completed-stage on restart |
| T-061 | Add the manual trigger endpoint that runs one cycle on demand |
| T-062 | Test skip-if-running and crash resume |

---

### F-011 · Provider abstraction and first adapter
**Milestone** M2 · **Priority** Critical

Every provider is reached through one shape: retry, rate-limit handling, quota recording and normalisation to the internal model.

*Acceptance:* a feature receives normalised data without knowing which provider served it; a rate-limited response is retried and recorded.

| ID | Task |
|---|---|
| T-063 | Define the provider adapter contract |
| T-064 | Define the internal normalised model for market and company data |
| T-065 | Implement retry with backoff |
| T-066 | Implement rate-limit detection and handling |
| T-067 | Implement per-provider quota and usage recording |
| T-068 | Implement the first provider adapter |
| T-069 | Test adapters against recorded responses rather than live services |

---

### F-012 · Minimal end-to-end slice
**Milestone** M2 · **Priority** Critical

One catalyst type, one provider, a fixed transparent score, one email. Proves the chain before anything expensive is built on it.

*Acceptance:* the owner receives a real, correct email describing a real market event, produced unattended on the target hardware.

| ID | Task |
|---|---|
| T-070 | Detect a single category of market event from one provider |
| T-071 | Define the candidate model |
| T-072 | Apply a fixed, transparent scoring rule |
| T-073 | Compose the email: summary first, then evidence |
| T-074 | Implement the outbound email sender |
| T-075 | Verify end to end on the target hardware |

---

## 5. M3 — Catalyst coverage

*Release 2 · Covers BR-1 to BR-6*

---

### F-013 · Event ingestion across all catalyst types
**Milestone** M3 · **Priority** Critical

Full breadth of detection: earnings surprises, analyst rating changes, insider buying, company news, macroeconomic and central bank events, unusual volume, large price gaps.

*Acceptance:* a day's genuine market events across all categories are detected and normalised.

| ID | Task |
|---|---|
| T-076 | Define the event model with type, company, timestamp, source and payload |
| T-077 | Ingest earnings surprises |
| T-078 | Ingest analyst rating changes |
| T-079 | Ingest insider buying activity |
| T-080 | Ingest company and macroeconomic news |
| T-081 | Detect unusual trading volume |
| T-082 | Detect large price gaps |
| T-083 | Map affected companies to each event |

---

### F-014 · Event identity and de-duplication
**Milestone** M3 · **Priority** Critical

Never analyse the same event twice, and never send a repeat alert. One mechanism serves both.

*Acceptance:* the same event reported by two providers resolves to one; a full week of operation produces no duplicate analysis.

| ID | Task |
|---|---|
| T-084 | Derive a stable event identity from provider reference where available |
| T-085 | Derive a fingerprint from company, type, timestamp and headline where not |
| T-086 | Persist seen event identities |
| T-087 | Check identity before analysis and discard on match |
| T-088 | Test cross-provider collapse of the same underlying event |

---

### F-015 · Catalyst strength ranking
**Milestone** M3 · **Priority** Critical

Decides which fifty of several hundred proceed. Load-bearing.

*Acceptance:* events are ordered by strength and the ordering is explainable.

| ID | Task |
|---|---|
| T-089 | Define the catalyst strength model |
| T-090 | Assign configurable per-type weighting |
| T-091 | Rank and cap the surviving set |
| T-092 | Test ranking stability and explainability |

---

### F-016 · Analysis history
**Milestone** M3 · **Priority** High

One month of prior findings per company, retained and readable.

*Acceptance:* history older than the retention period is pruned; prior findings for a company can be retrieved.

| ID | Task |
|---|---|
| T-093 | Create the analysis history entity, scoped to a user |
| T-094 | Record findings at the end of each cycle |
| T-095 | Implement retention pruning at one month |
| T-096 | Provide retrieval of recent history for a company |

---

## 6. M4 — Tradeability filters

*Release 2 · Covers BR-7 to BR-13*

---

### F-017 · Instrument reference data and currency conversion
**Milestone** M4 · **Priority** Critical

Listing currency becomes a required field, and one euro-based threshold converts across markets through a static multiplier table.

*Acceptance:* a company listed in an unmapped currency is skipped, not assumed at parity.

| ID | Task |
|---|---|
| T-097 | Create the instrument entity with listing currency as a required field |
| T-098 | Implement the multiplier table with a euro base |
| T-099 | **Blocked:** obtain and configure multipliers for the remaining Nordic and UK markets |
| T-100 | Skip instruments whose currency has no mapping |
| T-101 | Test conversion and the unmapped-currency path |

---

### F-018 · Size and exclusion gates
**Milestone** M4 · **Priority** Critical

Minimum company size, and outright exclusion of over-the-counter and penny stocks.

*Acceptance:* every company reaching the next stage meets the configured size threshold in its own listing currency.

| ID | Task |
|---|---|
| T-102 | Apply the minimum size threshold using the resolved user or default value |
| T-103 | Exclude over-the-counter and penny stocks |
| T-104 | Test threshold resolution across the override and default paths |

---

### F-019 · Liquidity gates
**Milestone** M4 · **Priority** Critical

The measure that determines whether a position can be exited. Excluding the catalyst day is the decision that matters most. See LADR-012.

*Acceptance:* a thinly-traded company whose volume spiked on its catalyst day is correctly rejected.

| ID | Task |
|---|---|
| T-105 | Compute average daily traded value as a median across trailing sessions |
| T-106 | Exclude the catalyst day from the measurement window |
| T-107 | Apply the minimum traded value threshold |
| T-108 | Implement the participation cap setting |
| T-109 | Flag low turnover relative to float in the evidence without excluding |
| T-110 | Handle companies with insufficient price history |
| T-111 | Test the catalyst-day exclusion explicitly with a spiked-volume case |

---

## 7. M5 — Fundamental validation

*Release 2 · Covers BR-14, BR-15*

---

### F-020 · Fundamentals ingestion and long-lived caching
**Milestone** M5 · **Priority** Critical

Where caching stops being an optimisation and becomes what makes free allowances workable.

*Acceptance:* a second cycle within the cache lifetime makes no fundamentals request.

| ID | Task |
|---|---|
| T-112 | Ingest income statement, balance sheet and cash flow data |
| T-113 | Ingest financial ratios, valuation measures, growth and dividend information |
| T-114 | Add the second fundamentals provider adapter |
| T-115 | Set cache lifetimes matched to how often each data type actually changes |
| T-116 | Verify quota consumption falls materially with caching enabled |

---

### F-021 · Fundamental elimination stage
**Milestone** M5 · **Priority** Critical

Separates sound companies from speculative moves, and applies the first stage cap.

*Acceptance:* the candidate list narrows to the configured limit and every elimination can be explained.

| ID | Task |
|---|---|
| T-117 | Implement elimination rules against fundamental measures |
| T-118 | Apply the configured stage cap |
| T-119 | Record the reason for each elimination |
| T-120 | Test elimination ordering and cap enforcement |

---

## 8. M6 — Sector context

*Release 2 · Covers BR-16 to BR-19*

---

### F-022 · Sector aggregates
**Milestone** M6 · **Priority** High

Computed once, read by every candidate in that sector. Shared reference data, not user-scoped.

*Acceptance:* aggregates are computed once per refresh period and reused across candidates.

| ID | Task |
|---|---|
| T-121 | Establish the sector classification source |
| T-122 | Compute and store sector aggregates as shared reference data |
| T-123 | Define the aggregate refresh cadence |
| T-124 | Confirm aggregates are excluded from user isolation filtering |

---

### F-023 · Sector-relative metrics and exemptions
**Milestone** M6 · **Priority** High

Operating profitability, capital efficiency, debt service and relative scale — all judged against the sector, with exemptions where the measures do not apply.

*Acceptance:* a bank, a property trust and a pre-profit growth company all pass through without being wrongly eliminated.

| ID | Task |
|---|---|
| T-125 | Derive operating-earnings measures from statements already held |
| T-126 | Compute enterprise value to operating earnings, operating margin and interest coverage |
| T-127 | Compute return on invested capital and earnings yield |
| T-128 | Rank each company's scale relative to its sector peers |
| T-129 | Implement configurable sector exemptions where the measures are meaningless |
| T-130 | Test the exemption path with a bank, a property trust and a loss-making company |

---

## 9. M7 — Timing confirmation

*Release 2 · Covers BR-20, BR-21*

---

### F-024 · Price history retention
**Milestone** M7 · **Priority** High

Enough depth to compute the longest-period indicator in use.

*Acceptance:* two hundred sessions of history are available for any assessable company.

| ID | Task |
|---|---|
| T-131 | Store open, high, low, close and volume history |
| T-132 | Retain sufficient depth for the longest indicator period |
| T-133 | Handle gaps, halts and thin sessions |
| T-134 | Document the approach to splits and dividends |

---

### F-025 · Indicator computation
**Milestone** M7 · **Priority** High

Computed internally, never purchased. Correctness is ours, so this carries the heaviest test weight in the codebase. See LADR-004.

*Acceptance:* every indicator matches a known reference series within tolerance.

| ID | Task |
|---|---|
| T-135 | Implement relative strength and momentum |
| T-136 | Implement simple and exponential moving averages |
| T-137 | Implement moving-average convergence divergence |
| T-138 | Implement Bollinger bands and average true range |
| T-139 | Implement volume-weighted average price and relative volume |
| T-140 | Implement breakout, support and resistance detection |
| T-141 | Validate every indicator against reference series |

---

### F-026 · Timing stage
**Milestone** M7 · **Priority** High

Applies the second stage cap on timing grounds.

*Acceptance:* the candidate list narrows to the configured limit and each timing judgement is explainable.

| ID | Task |
|---|---|
| T-142 | Evaluate trend position against moving averages |
| T-143 | Evaluate momentum, relative volume and breakout behaviour |
| T-144 | Apply the configured stage cap |
| T-145 | Record the reason for each timing judgement |

---

## 10. M8 — Corroboration

*Release 2 · Covers BR-22, BR-23, BR-25, BR-26*

---

### F-027 · Analyst signals
**Milestone** M8 · **Priority** High

*Acceptance:* each candidate carries current analyst positioning and recent changes to it.

| ID | Task |
|---|---|
| T-146 | Ingest analyst ratings and consensus recommendations |
| T-147 | Ingest target prices |
| T-148 | Ingest rating upgrades, downgrades and estimate revisions |
| T-149 | Attach analyst signals to candidates |

---

### F-028 · News sentiment
**Milestone** M8 · **Priority** High

*Acceptance:* each candidate carries a sentiment position derived from news, with the articles that produced it.

| ID | Task |
|---|---|
| T-150 | Ingest news sentiment scores where the provider supplies them |
| T-151 | Aggregate sentiment per company over a configurable window |
| T-152 | Retain article references so sentiment is traceable |

---

### F-029 · Insider activity and event calendar
**Milestone** M8 · **Priority** Medium

*Acceptance:* each candidate carries recent insider activity and any imminent scheduled event.

| ID | Task |
|---|---|
| T-153 | Ingest insider buying and selling and regulatory filings |
| T-154 | Ingest the earnings and dividend calendar |
| T-155 | Ingest the economic, listing and share-split calendar |
| T-156 | Flag candidates with an imminent scheduled event |

---

## 11. M9 — Recommendation engine

*Release 2 · Covers BR-27 to BR-30, BR-41, BR-44*

---

### F-030 · Deterministic baseline scoring
**Milestone** M9 · **Priority** Critical

Weighted scoring applied before the AI sees anything. The baseline is retained even when the AI departs from it.

*Acceptance:* the same inputs produce the same baseline score every time.

| ID | Task |
|---|---|
| T-157 | Implement the configurable weighting model across all five signal categories |
| T-158 | **Blocked:** obtain and configure the weighting distribution for the short holding horizon |
| T-159 | Adjust weightings according to the configured horizon |
| T-160 | Compute and persist the baseline composite score |
| T-161 | Test determinism and horizon adjustment |

---

### F-031 · AI reasoning adapter
**Milestone** M9 · **Priority** Critical

One interface, provider and model resolved from configuration, spend bounded. See LADR-013.

*Acceptance:* switching provider and model is a configuration change; reaching the spend ceiling halts cleanly and notifies.

| ID | Task |
|---|---|
| T-162 | Define the reasoning abstraction at the application boundary |
| T-163 | Implement the first provider adapter |
| T-164 | Resolve model identifier and endpoint from configuration, credentials from environment |
| T-165 | Implement the spend ceiling and halt behaviour |
| T-166 | Handle exhausted-credit responses explicitly |
| T-167 | **Decide:** routing gateway versus dual provider SDKs |

---

### F-032 · Recommendation generation
**Milestone** M9 · **Priority** Critical

The product. At most ten candidates, one call, a fixed output contract.

*Acceptance:* the owner reviews a week of output and judges the reasoning sound enough to act on.

| ID | Task |
|---|---|
| T-168 | Assemble context: candidates, all signals, and recent analysis history |
| T-169 | Apply the configured stage cap before the reasoning call |
| T-170 | Define and enforce the output contract: confidence, rationale, bull case, bear case, risks, horizon |
| T-171 | Capture the weighting the model actually applied, alongside the baseline |
| T-172 | Persist recommendations scoped to the user |
| T-173 | Handle malformed or incomplete model output without failing the cycle |

---

## 12. M10 — Reporting and alerting

*Release 3 · Covers BR-31 to BR-37*

---

### F-033 · Report composition
**Milestone** M10 · **Priority** Critical

Summary first, then concise factual quantitative evidence. Publishing nothing is a valid outcome.

*Acceptance:* the owner can decide on each recommendation from the summary alone.

| ID | Task |
|---|---|
| T-174 | Compose the summary section |
| T-175 | Compose the evidence section, constrained to be concise, factual and quantitative |
| T-176 | Disclose the weighting actually applied so the evidence explains the ranking |
| T-177 | Enforce the publication cap and the below-threshold discard |
| T-178 | Publish nothing when nothing qualifies |

---

### F-034 · Delivery window and ad-hoc alerts
**Milestone** M10 · **Priority** High

*Acceptance:* no delivery occurs outside the configured window, and the window does not drift across a daylight-saving transition.

| ID | Task |
|---|---|
| T-179 | Implement the delivery window using a named timezone and local times |
| T-180 | Suppress delivery outside the window |
| T-181 | Implement ad-hoc alerting reusing event de-duplication to prevent repeats |
| T-182 | Test window behaviour across a daylight-saving boundary |

---

### F-035 · Failure notification
**Milestone** M10 · **Priority** Medium

*Acceptance:* a deliberately induced failure produces an email to the owner.

| ID | Task |
|---|---|
| T-183 | Notify the owner by email when a cycle fails to complete |
| T-184 | Include enough context to diagnose without device access |
| T-185 | Verify with an induced failure |

---

## 13. M11 — Provider cost and ROI evaluation

*Release 3 · Covers BR-42, BR-45*

---

### F-036 · Quota telemetry and constraint reporting
**Milestone** M11 · **Priority** High

No new components. Consumes the usage data accumulated since M2 to produce evidence.

*Acceptance:* a report shows, per provider, consumption against allowance and every occasion a limit constrained output.

| ID | Task |
|---|---|
| T-186 | Report consumption against allowance per provider |
| T-187 | Record every occasion a provider limit constrained the result |
| T-188 | Produce the constraint summary covering Releases 1 and 2 |

---

### F-037 · Subscription decision
**Milestone** M11 · **Priority** High

*Acceptance:* the owner has a costed, evidence-based recommendation for the monthly data budget.

| ID | Task |
|---|---|
| T-189 | Assess each candidate paid upgrade against measured constraint evidence |
| T-190 | Re-verify current provider pricing, including any rebranded supplier |
| T-191 | Record the subscription decision as a decision record |

---

## 14. M12 — Continuous operation

*Release 3 · Covers BR-39, BR-43, BR-45*

---

### F-038 · Scheduled operation
**Milestone** M12 · **Priority** Critical

Promotes the manual trigger to a scheduled one. Architecturally small, because everything it needs was built earlier for other reasons.

*Acceptance:* the system runs unattended for two weeks, delivering within its window, with no manual intervention.

| ID | Task |
|---|---|
| T-192 | Enable the interval timer at the configured cadence |
| T-193 | Verify skip-if-running under real overlapping conditions |
| T-194 | Confirm cycles honour the delivery window |
| T-195 | Run and document a two-week unattended soak |

---

### F-039 · Provider failover
**Milestone** M12 · **Priority** High

*Acceptance:* with the primary provider unavailable, cycles complete using the fallback and the substitution is recorded.

| ID | Task |
|---|---|
| T-196 | Define fallback ordering per data category in configuration |
| T-197 | Fail over on exhausted allowance or persistent error |
| T-198 | Record which provider served each result |
| T-199 | Test failover with the primary provider unavailable |

---

## 15. M13 — Social sentiment *(optional)*

*Release 3 · Covers BR-24 · Deliberately last and removable*

---

### F-040 · Social sentiment as a confidence adjustment
**Milestone** M13 · **Priority** Low

The lowest-weight, least reliable signal. It adjusts confidence and never changes which companies are recommended.

*Acceptance:* social sentiment visibly adjusts confidence without ever altering the selected set.

| ID | Task |
|---|---|
| T-200 | Ingest social sentiment for candidate companies |
| T-201 | Aggregate mention frequency and sentiment per company |
| T-202 | Apply as a confidence modifier only |
| T-203 | Test that selection is unchanged when social sentiment is removed |

---

## 16. Blocked and decision tasks

These carry a dependency on owner input or an explicit decision. Surface them when created.

| ID | Task | Status |
|---|---|---|
| T-099 | Nordic and UK currency multipliers | Blocked — owner to supply values |
| T-158 | Short-horizon weighting distribution | Blocked — owner to supply values |
| T-167 | Routing gateway versus dual provider SDKs | Blocked — decision required before M9 |
| T-010 | NodaTime on SQLite — spike | Blocked — spike outcome shapes the schema |
| T-015 | Structured-document columns on SQLite — spike | Blocked — spike outcome shapes user metadata storage |
| T-036 | Port CI workflows | Blocked — source repository content not yet reviewed |
| T-048 | Port agent rules | Resolved 2026-07-30 (WT-12-01, #279) |
| T-190 | Re-verify provider pricing | In progress — supplier rebrand |

---

## 17. Coverage check

Every BRD requirement maps to at least one Feature.

| BRD requirements | Covered by |
|---|---|
| BR-1 to BR-6 | F-012, F-013, F-014, F-015, F-016 |
| BR-7 to BR-13 | F-017, F-018, F-019 |
| BR-14, BR-15 | F-020, F-021 |
| BR-16 to BR-19 | F-022, F-023 |
| BR-20, BR-21 | F-024, F-025, F-026 |
| BR-22, BR-23, BR-25, BR-26 | F-027, F-028, F-029 |
| BR-24 | F-040 |
| BR-27 to BR-30 | F-030, F-031, F-032 |
| BR-31 to BR-37 | F-012, F-033, F-034, F-035 |
| BR-38 to BR-45 | F-002, F-007, F-010, F-011, F-020, F-031, F-036, F-038, F-039 |
| BR-46 to BR-50 | F-003, F-004, F-005 |
