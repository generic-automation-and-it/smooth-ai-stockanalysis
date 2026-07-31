# AGENTS.md - Conductor Workspace Scripts

AI Context: shared Conductor repository scripts (`.conductor/settings.toml` + `.conductor/scripts/`) that
bring up the SmoothLlmImposter Docker container and wire `code-review-graph` on every teammate's workspace.
Updated: 2026-07-31

## TL;DR

`.conductor/settings.toml` is committed and shared, so `git pull` is enough to get it — unlike
`.conductor/settings.local.toml`, which is machine-local and never reaches teammates. It points Conductor's
`[scripts] setup` (runs once, on workspace creation) and `[scripts.run.restart-imposter]` (on-demand trigger)
at scripts under `.conductor/scripts/`, which start the imposter container and, on setup only, wire
`code-review-graph` into four AI-coding platforms.

## Non-Negotiables

- **The `docker run` invocation lives in exactly one place: `imposter-container.sh`.** `setup.sh` and
  `restart-imposter.sh` both call it; neither may embed its own copy of the provider `-e` flags.
- **`run_mode` must stay `nonconcurrent`** (`.conductor/settings.toml`). The container uses a fixed name
  (`smooth-llm-imposter`) and a fixed host port (`127.0.0.1:5080` by default via `PORT`); two workspaces
  running `setup` or `restart-imposter` at the same time would race the same `docker rm -f` / `docker run`.
- **Every `code-review-graph install --platform` call in `setup.sh` keeps `--no-instructions`.** Without it,
  `install` appends an MCP-tools section to `CLAUDE.md`, which in this repository is a committed symlink to
  root `AGENTS.md` — the append would land in the tracked context file every teammate's agent reads.
- **The `claude-code` platform additionally keeps `--no-skills --no-hooks`.** Its skills/hooks resolve through
  the committed `.claude -> .agents` symlink into `.agents/skills/` and `.agents/settings.json`; the other
  three platforms write under `$HOME` instead and don't need the flags.
- **Never hardcode `OPENCODE_API_KEY` / `OPENROUTER_API_KEY` here.** `imposter-container.sh` reads them from
  the workspace environment and fails fast (`:?`) if either is unset — that's Conductor's job to supply, not
  this script's.

## Architecture Decisions

| Decision | Rejected alternative | Why |
|---|---|---|
| Extract the container lifecycle into its own file (`imposter-container.sh`), called by both `setup.sh` and `restart-imposter.sh` | Inline the `docker run` separately in each of `[scripts] setup` and `[scripts.run.restart-imposter]` as TOML strings | Two copies of ~30 `-e` flags drift silently — the `opencode-go-anthropic` index-2 collision in the original personal script (silently dropping `opus-4-7`) is exactly this failure mode. |
| Commit `.conductor/settings.toml` + `.conductor/scripts/*.sh` | Keep the workspace script only in `.conductor/settings.local.toml` | `settings.local.toml` is machine-local; every teammate had to hand-paste the script into their own workspace to get it at all. |
| Keep `code-review-graph install --platform claude-code` (flag-gated) rather than excluding the platform entirely | Skip `claude-code` outright because default `install` mutates tracked files | `--no-instructions --no-skills --no-hooks` gets the same safety (only writes untracked `.mcp.json`) without losing the fourth platform's code-intelligence coverage. |

## Key Behaviors

- **Session forwarding opt-out** — The image default is `SessionForwarding: opencode-go` on both
  `opencode-go-*` providers, so matched routes stamp `session_id` / `x-opencode-session`. To stop OpenCode
  session token usage, uncomment the two `OPENCODE_GO_{ANTHROPIC,OPENAI}_SESSION_FORWARDING` exports,
  add both names to `--preserve-env`, and add the two `-e` flags to the `docker run`.
- **Enabling this in a new workspace.** Nothing to configure beyond secrets: once
  `.conductor/settings.toml` is on the branch a workspace is created from, Conductor runs its `setup` script
  automatically. The only prerequisite is that the workspace has `OPENCODE_API_KEY` and `OPENROUTER_API_KEY`
  set as environment variables (Conductor workspace/environment settings, not committed anywhere) — without
  them `imposter-container.sh` exits immediately with a `:?` message naming the missing variable.
- **Running the trigger.** `restart-imposter` shows up as a named, on-demand run script (icon
  `refresh-cw`) in Conductor — run it any time to recreate the container without recreating the workspace:
  after pulling a new image tag, rotating either API key, or recovering from a crash-looped container. It
  skips the `code-review-graph` step entirely (only `setup.sh` runs that).
- **Precedence gotcha.** Conductor resolves settings per-value across layers — if two layers set the same
  value, Conductor uses the highest layer that applies. A teammate who already has a personal
  `settings.local.toml` with its own `setup` value will keep running that instead of this file's `setup.sh`,
  silently, until they delete or reconcile it — verify which one actually ran (check for the
  code-review-graph install log lines, only present via this file's `setup.sh`) rather than assuming.
- **Cloud-sandbox assumptions, not verified on local macOS.** `imposter-container.sh` starts `dockerd`
  directly with `sudo nohup` when `docker info` fails, matching the Amazon Linux 2023 cloud sandbox lifecycle
  (no systemd as PID 1). It has only been run in that cloud sandbox. A local Mac workspace normally has
  Docker Desktop already running its own daemon. `[scripts] setup` has no `available_in` gate — unlike
  `[scripts.run.*]` — so it runs unconditionally on every workspace, local or cloud. Local users who hit
  failures should override `setup` via a personal `settings.local.toml`.
- **Idempotent recreate, not incremental update.** Every run (`setup` or `restart-imposter`) does
  `docker rm -f` then `docker run -d` unconditionally — there's no update-in-place path.
- **Current imposter model mappings** (single source of truth: the `-e` flags in `imposter-container.sh`):
  `claude-sonnet-4-6`/`claude-opus-4-6`/`claude-opus-4-8` → OpenCode Go `qwen3.6-plus`/`qwen3.7-plus`/`qwen3.7-max`;
  `claude-haiku-*` → OpenRouter Anthropic `inclusionai/ling-3.0-flash:free`; `gpt-5.4`/`gpt-5.5` route via
  `opencode-go-openai-chat` (`OpenAiUpstreamApi: chat_completions`) → OpenCode Go `kimi-k2.7-code`/`glm-5.2`,
  and `gpt-5.6-luna` routes via `opencode-go-openai-responses` (`OpenAiUpstreamApi: responses`) → OpenCode Go
  `grok-4.5`.
- **The MCP servers `code-review-graph install` configures all invoke `uvx code-review-graph serve`**,
  regardless of platform. If a workspace's snapshot doesn't have `uv` on `PATH`, the generated MCP configs
  are written successfully but the servers themselves cannot start.
- **Project-scoped MCP configs are hidden via `.git/info/exclude`, not `.gitignore`.** `setup.sh` seeds
  `.mcp.json` (from `claude-code`) and `opencode.jsonc` (from `opencode`) into `.git/info/exclude` so they
  never show up in a workspace diff. `code-review-graph install` still appends `.code-review-graph/` to the
  tracked `.gitignore` directly (all platforms) — that one line is expected to show up as a diff in every
  workspace.

## Migration Plans

Any teammate's pre-existing `.conductor/settings.local.toml` that duplicates this shared script's container
logic should be deleted once its behavior is confirmed equivalent — per the precedence gotcha above, a
lingering local file silently masks every update made here.

## Changelog

| Date | Change | Ref |
| :---- | :---- | :---- |
| 2026-07-31 | Initial version. Added `.conductor/settings.toml` + `.conductor/scripts/{setup,restart-imposter,imposter-container}.sh`, following the pattern established in `smooth-llm-imposter`. | — |
