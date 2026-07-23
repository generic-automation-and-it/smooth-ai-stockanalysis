# .agents/skills — AGENTS.md

## TL;DR

First-party AI agent skills. They legitimately run shell, `gh`/`git`, and
template file operations, so changes to skill instructions and executable helpers
require the same security review as other privileged automation.

## Non-Negotiables

- **Secrets go through the environment, never into text.** Any skill needing a secret MUST follow `.github/instructions/skill-secret-handling.instructions.md`: a script reads the value from a runtime environment variable; the value never appears in `SKILL.md`, prompts, agent YAML, README, or any committed file. No skill handles a real secret today.
- **Review executable helpers as privileged automation.** Validate fixed paths,
  argument handling, destructive operations, external calls, and secret boundaries.
- **Keep vendored mirrors aligned with upstream.** Record any required local delta
  explicitly so it can be reapplied or retired during the next sync.

## Key Behaviors

- The vendored `ai-review` consumer keeps its human-in-the-loop boundary:
  analyse mode always stops for explicit fix/skip decisions and never starts
  execute mode on its own.
- No specialized automated skill-security scanner is configured. Skill changes
  rely on normal pull-request review and the safeguards documented here.

## Changelog

> AI loading note: Skip this section during routine task execution. Use it only when updating this rule file.

| Date | Change | Ref |
|:-----|:-------|:----|
| 2026-06-21 | Initial version — documents the SkillSpector baseline gate contract and the secret-handling guardrail for skills. | #52 |
| 2026-06-21 | LADR-001: gate on the deterministic static scan; LLM semantic stage runs as a non-blocking advisory (policy A). Resolves the static-vs-LLM baseline mismatch that failed the gate on run 27907080342. | #52 |
| 2026-07-23 | Recorded the `ai-review` consumer integration and its explicit human-in-the-loop boundary. | #248 |
| 2026-07-23 | Removed the SkillSpector workflow, baseline, and report renderer; retained manual security and secret-handling guidance. | #248 |
| 2026-07-23 | Vendored the `git-commit-review-push` supporting skill from `smooth-ai-report-review` and registered it in local skill indexes/settings. No local behavioral delta from upstream. | |
