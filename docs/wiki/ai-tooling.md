# AI Tooling

This project is built with AI coding agents and reviewed by one on every pull request. This page covers both sides: the shared agent configuration a contributor needs, and the automated review gate as delivered.

## Approach

The solution deliberately demonstrates an AI-agnostic approach to developer tooling. The goal was not to pick a favourite — it was to understand what each tool and its underlying models are genuinely best suited for across a real delivery.

| Tool | Primary Role |
|---|---|
| **Claude Code** (Anthropic) | Spec-driven generation, architectural reasoning, primary code authoring |
| **OpenAI Codex** | Code generation, pull request workflow automation, agentic task execution |
| **GitHub Copilot** (web agent) | In-editor assistance, agentic task execution, pull request participation |

All three share one source of truth. Rules, conventions and prompt templates live in `.agents/` and are surfaced to each tool through symlinks and path references (`.github/instructions`, Cursor and Copilot equivalents), so a convention is written once rather than per tool. Two kinds of context load automatically:

- **Rules** — `.agents/rules/**/*.instructions.md`, scoped per file via frontmatter, so backend rules attach when a C# file is opened. The Rule Categories table in the root `AGENTS.md` lists each one.
- **Contextual knowledge** — `AGENTS.md` and `*AGENTS.md` files, layered domain → sub-domain → feature → technology. The file nearest the code being changed is the most authoritative.

Run the setup script after cloning to recreate the symlink aliases:

```bash
# Mac / Linux
bash .agents/setup/scripts/agents-setup.sh

# Windows (PowerShell — run as Administrator)
.\.agents\setup\scripts\agents-setup.ps1
```

## The automated review gate

Two workflows, both independent of the [PR gate](ci.md).

### `PR Code Review Report`

`.github/workflows/pipeline-code-review-report.yml` is a **thin caller**. It holds no review logic — the reusable workflow, its scripts and its prompts are fetched from `generic-automation-and-it/smooth-ai-report-review` and execute there. Only the `/ai-review` *consumer* skill (`.agents/skills/ai-review/`) is vendored locally; that skill reads a posted review and helps apply or skip its findings.

It runs on every non-draft pull request, on a `/ai-review` comment, and on manual dispatch. Its job produces the required status check `review / open-code-review-report`.

The review is **consolidated**: one GitHub review per run, posted with a single `gh pr review` call, the verdict carried as the review state (`APPROVED` / `CHANGES_REQUESTED` / `COMMENTED`). Findings are sections of that one body, not scattered inline comments.

### `PR AI Analyse (Self-Fix)`

`.github/workflows/pipeline-ai-analyse.yml` follows a successful review report with a bounded, same-repository self-fix loop for **low and medium** findings only. Critical and high findings are left for a human, or for `/ai-review` run interactively. The cycle count is capped by `OPENCODE_ANALYSE_MAX_INCREMENTAL` (default 3).

### Diff chunking

Chunking is configured **upstream and is not tunable from this repository**. The effective behaviour:

| Behaviour | Value |
|---|---|
| Up to 10 changed files | reviewed as a single chunk |
| 15 or more changed files | grouped semantically by an LLM call, falling back to top-level directory grouping |
| Chunk diff budget | 100 KB — oversized groups split by descending a directory level, then by halving |
| Prompt diff budget | 200 KB per chunk |
| Chunks reviewed | in parallel, up to 10 at a time |
| Merge | chunk reviews concatenated, then one summariser call produces the holistic verdict |

Pull requests here have run 4–23 changed files, so both the single-chunk and the semantic-grouping paths are exercised in normal use.

**When a pull request is too large**, the failure is always visible — nothing is silently skipped:

- **More than 100 changed files** (`OPENCODE_REVIEW_REPORT_MAX_FILE_COUNT`) — the run posts a `CHANGES_REQUESTED` review saying so and reviews nothing. Split the pull request. This limit is the one chunking-adjacent knob this repository controls, and it is a GitHub Actions Variable rather than a workflow input.
- **A single file's diff over budget** — that file's diff is omitted from the prompt, and the model is told to read the file on demand and forbidden from raising a critical or high finding on it without doing so.
- **A chunk's model call failing or timing out** — the final review is forced to `CHANGES_REQUESTED` regardless of what the summariser concluded, and the gap is named in a coverage banner.
- **A review body over GitHub's 65,536-character limit** — the per-chunk detail section is dropped first, keeping the holistic summary; only then is the body truncated, with a warning banner.

### Supply chain

Both workflows follow the upstream default branch, and upstream code *executes* here on every pull request. This is a deliberate, recorded trade-off, and the mitigation levers are not the obvious ones — in particular, pinning to upstream's `v1` tag achieves nothing. See [LADR-021](../hlds/mvp/ladrs/021-live-upstream-call-for-ai-review-tooling.md).

### Configuration

The provider key and model variables the review pipelines require are inventoried — with their scope and their missing-credential failure symptoms — in [`.github/CI_AGENTS.md`](../../.github/CI_AGENTS.md). Provisioning them is a repository-owner action.

## Key findings

- Cross-model review (writing with one tool, reviewing with another) caught assumptions the authoring model would not have questioned, because they were its own.
- Claude Code performed strongest on spec-to-code generation when given structured HLDs and NFRs.
- Codex was well-suited to PR workflow automation and repetitive generation tasks.
- GitHub Copilot's web agent added value in PR participation, visible in the conversation history.

## Recommendations for teams adopting this approach

- Run AI reviews alongside linters and analyzers. They catch different classes of problem, and the AI review is the only one that can object to a *decision* rather than a pattern.
- Review with a different model than you author with.
- Invest in shared context files (a `.agents/` equivalent) early. Every tool benefits from one source of truth on conventions and domain knowledge.
- Decide consciously how you consume upstream agent tooling. Copying it and calling it live are different risks, and the second one is easy to acquire without noticing.

## Further reading

- [CI/CD](ci.md) — the PR gate, its steps, and the required status checks
- [Architecture](architecture.md) — solution structure and design decisions
- [Testing Strategy](testing.md) — test levels and infrastructure
