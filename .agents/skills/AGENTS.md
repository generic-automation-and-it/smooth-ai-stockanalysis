# .agents/skills — AGENTS.md

## TL;DR

First-party AI agent skills. They legitimately run shell, `gh`/`git`, and template file operations, which the NVIDIA SkillSpector scan (`.github/workflows/skill-scan.yml`) rates HIGH for an untrusted third party — so the scan is **advisory / non-gating**: it reports every finding to the run summary and code scanning for human review and never blocks the PR.

## Non-Negotiables

- **Secrets go through the environment, never into text.** Any skill needing a secret MUST follow `.github/instructions/skill-secret-handling.instructions.md`: a script reads the value from a runtime environment variable; the value never appears in `SKILL.md`, prompts, agent YAML, README, or any committed file. No skill handles a real secret today.
- **Never quiet a SkillSpector finding by removing a skill's capability.** The scan is advisory, so there is nothing to "make green" — if the flagged behavior is the skill's actual job (shell out to `gh`, swap a symlink, refresh a template dir), keep it. Gutting a skill to reduce the report is a failure, not a fix.
- **Read the report on skill PRs.** Because nothing blocks, a genuinely new dangerous pattern (real exfiltration, hidden instructions, supply-chain) only gets caught by a human reading the run summary / code-scanning findings. Treat an unexpected new finding as a review blocker even though CI stays green.

## Architecture Decisions

### LADR-002 — Advisory scan, no baseline allowlist

- **Date:** 2026-07-23 · **Status:** Accepted (supersedes the LADR-001 baseline gate)
- **Context:** All skills here are first-party and several legitimately run shell/`gh`/`git`/template operations that SkillSpector pins at risk 100 (HIGH) by design. A blocking gate on that raw score fails every PR, so it previously required a per-finding allowlist (`skillspector-baseline.yml`) suppressing those inherent findings. With zero third-party skills, a raw-score gate carries no real signal, and the allowlist was pure maintenance overhead — every new inherent pattern needed a justified entry just to keep CI green.
- **Decision:** Remove the baseline allowlist and the pass/fail gate. The scan runs for **visibility only**: `skillspector-report.py` renders all findings to the run summary + SARIF, and no scan result (raw HIGH score, active finding, or even a scan error) fails the PR.
- **Consequences:** No allowlist to maintain and no gate to re-break on model/prompt drift. Trade-off: nothing hard-blocks, so novel dangerous patterns rely on a human reading the report (see Non-Negotiables) plus the optional LLM semantic scan. To restore a blocking gate later, reintroduce a decision step keyed on *new findings since a committed snapshot* rather than a per-finding allowlist.

## Key Behaviors

- `.github/scripts/skillspector-report.py` never emits a pass/fail decision — it only renders the summary and SARIF. The workflow has no enforce/gate step; SkillSpector's raw exit code (1 for HIGH, pinned at 100 for first-party skills) is ignored.
- Two scans run per CI invocation: a **static scan** (`--no-llm`, produces the summary + SARIF) and, when a key is configured, a **separate LLM semantic scan** rendered via `--advisory` as a clearly-labeled section. Neither blocks the PR.

## Changelog

> AI loading note: Skip this section during routine task execution. Use it only when updating this rule file.

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-06-21 | Initial version — documents the SkillSpector baseline gate contract and the secret-handling guardrail for skills. | #52 |
| 2026-06-21 | LADR-001: gate on the deterministic static scan; LLM semantic stage runs as a non-blocking advisory (policy A). Resolves the static-vs-LLM baseline mismatch that failed the gate on run 27907080342. | #52 |
| 2026-07-23 | LADR-002: removed the baseline allowlist and the pass/fail gate — the SkillSpector scan is now advisory / non-gating (reports for human review, never blocks). Deleted `skillspector-baseline.yml`. | #5 |
