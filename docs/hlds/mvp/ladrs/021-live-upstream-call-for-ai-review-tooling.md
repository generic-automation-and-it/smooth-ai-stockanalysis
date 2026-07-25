# LADR-021: Live upstream call for the AI review tooling

**Status:** Accepted
**Date:** July 2026

## Context

This repository consumes code from outside itself in three materially different ways, and until now only two of them had been written down. Conflating them is what makes the third look like a violation of [LADR-006](006-one-time-fork-of-template.md).

| Source | Consumption model | Failure mode if it goes wrong |
|---|---|---|
| The development template | One-time fork, no tracking (LADR-006) | A missed upstream improvement. Costs effort, never correctness. |
| `builder-catalogue` | One-time copy, then deliberately diverged and hardened | Same. The copy is ours; upstream cannot change it. |
| `smooth-ai-report-review` | **Live call to a moving ref** — the code is fetched and *executed* on every pull request | A change upstream runs here, unreviewed, in a job holding `pull-requests: write`, `issues: write` and `secrets: inherit`. |

LADR-006 rejected upstream *tracking* because tracking turns every structural divergence into a recurring merge conflict. That reasoning is about **merge cost on copied source**. It does not transfer to the third row, where nothing is copied and there is no merge to pay for — the trade-off there is supply-chain risk, which LADR-006 never weighed.

`.github/workflows/pipeline-code-review-report.yml` calls the reusable workflow at `@main` and passes `tools_ref: main`; `.github/workflows/pipeline-ai-analyse.yml` checks the tooling out at `vars.SMOOTH_AI_REVIEW_TOOLS_REF || 'main'`. Both therefore follow the upstream default branch.

The obvious mitigation — "pin to the published tag" — was investigated and **does not exist**. Upstream has one tag, `v1`, and zero GitHub Releases. `v1` is a *floating major tag*: upstream's `update-major-tag.yml` force-updates it to the default branch head on every push to `main` (`gh api --method PATCH .../git/refs/tags/v1 -F force=true`). Measured 2026-07-25:

```
v1   -> fc2c2037b6032aad252cb13a3355093dd6aad81b
main -> fc2c2037b6032aad252cb13a3355093dd6aad81b
```

Pinning to `@v1` would be pinning to `main` under another name. Only a commit SHA is an actual pin.

## Decision

**Track upstream `main` for the AI review tooling, deliberately and with the risk named.** This is not an extension of LADR-006's "no upstream tracking" being broken; it is a different decision about a different kind of artifact, taken on supply-chain grounds rather than merge-cost grounds.

## Alternatives weighed

**Pin the reusable workflow and tooling to a commit SHA.** The only genuine pin available. Rejected for now: the review tooling is under active development and is the component most likely to need an upstream fix mid-flight (three of the last four pull requests exercised it heavily). A SHA pin freezes bug fixes as effectively as it freezes risk, and nothing in this repository would signal that the pin had gone stale. The lever is kept ready rather than pulled — see *Revisit when*.

**Pin to the `v1` tag.** Rejected on evidence, not preference: `v1` is force-moved to `main` on every upstream push, so it offers the *appearance* of a pin with none of the effect. Recording this matters more than the decision itself — a future reader will reach for `@v1` precisely because it looks like the responsible choice.

**Vendor the generator locally.** Would convert the third row into the second and remove the live-execution risk entirely. Rejected: it also removes the safer of the two upstream code paths. The callee executes review scripts *from the pull-request branch* when the consuming repository vendors `.agents/skills/ai-review-report/`, and falls back to a side-checkout of pinned upstream tooling when it does not. This repository vendors only the `/ai-review` **consumer** skill, so it takes the side-checkout path. Vendoring the generator would mean every contributor's branch content is bash-executed with repository secrets before merge — strictly worse than the risk it was meant to remove.

**Wait for an upstream release before adopting the gate at all.** Rejected: F-006's acceptance requires a working AI review on pull requests in M1, and upstream shows no sign of cutting an immutable release.

## Consequences

- **Accepted risk, stated plainly.** Upstream `main` executes here on every pull request in a job with `pull-requests: write`, `issues: write` and `secrets: inherit` — which resolves the caller organisation's provider API keys. An upstream compromise or a careless upstream commit reaches this repository with no review step in between.
- The blast radius is bounded by the caller: `permissions:` in `pipeline-code-review-report.yml` caps the `GITHUB_TOKEN` the callee receives, and the gate holds no `contents: write`. It cannot push to `main`.
- **Two levers, and they are not equivalent.** `vars.SMOOTH_AI_REVIEW_TOOLS_REF` outranks the workflow's own `tools_ref` input in the callee's resolution order, so setting that one repository or organisation Variable to a commit SHA pins the *executed scripts* for **both** pipelines with no file change — the fastest response to an upstream incident. It does **not** pin the reusable workflow YAML itself, whose inline `run:` blocks also execute; that requires editing `uses: …@main` to a SHA. A complete pin needs both.
- This decision does not reopen LADR-006. Nothing is being merged from upstream; no divergence becomes expensive.

## Revisit when

Any one of:

- Upstream publishes an **immutable** release (a GitHub Release, or a tag that `update-major-tag.yml` does not force-move) — then pin `uses:` to it and drop this trade-off.
- Upstream begins accepting external contributors, or its commit review posture changes.
- A security incident, unexplained behaviour change, or unexpected review output is observed — set `SMOOTH_AI_REVIEW_TOOLS_REF` to the last known-good SHA first, investigate second.
