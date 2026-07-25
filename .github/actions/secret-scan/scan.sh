#!/usr/bin/env bash
# Secret scanner for the PR gate (WT-10-03, NFR-043 verification clause).
#
# Installs gitleaks by pinned SHA-256 checksum (so the scanner is reviewable
# and the step needs no secrets — fork PRs are scanned identically) and scans
# the PR commit range, not the full repository history. A key committed and
# removed within the same PR is still caught; history before the PR's merge
# base is not scanned on every run — that blind spot is documented in
# .github/CI_AGENTS.md and closed by WT-10-04's always-runs gate.
#
# Ruleset: the binary's built-in default is replaced the moment a config file
# is passed, so we also download the upstream default config at the SAME pinned
# tag, checksum-verify it, and have .gitleaks.toml extend it by path. That
# keeps the full upstream ruleset while making the repo allowlist the only
# reviewable suppression surface.
#
# Inputs (env):
#   GITLEAKS_VERSION       — pinned release tag, e.g. "8.27.2".
#   GITLEAKS_SHA256        — official sha256 of gitleaks_<ver>_linux_x64.tar.gz.
#   GITLEAKS_CONFIG_SHA256 — sha256 of the upstream default gitleaks.toml at tag v<ver>.
#   SCAN_LOG_OPTS          — git log opts. Required. The pr-gate workflow always
#                            sets it: a SHA range for pull_request / push, or
#                            "" for workflow_dispatch (full history scan).
#                            Unset is a fail-fast gate error (see below).
#   GITLEAKS_CONFIG        — repo allowlist path (default ".gitleaks.toml").
#
# Exit codes: 0 clean, 1 leaks found, 2 install/scan error.

set -euo pipefail

version="${GITLEAKS_VERSION:?GITLEAKS_VERSION must be set}"
binary_sha="${GITLEAKS_SHA256:?GITLEAKS_SHA256 must be set}"
config_sha="${GITLEAKS_CONFIG_SHA256:?GITLEAKS_CONFIG_SHA256 must be set}"
repo_allowlist="${GITLEAKS_CONFIG:-.gitleaks.toml}"
# SCAN_LOG_OPTS resolution (contract):
#   The pr-gate workflow always sets SCAN_LOG_OPTS — to a SHA range for
#   pull_request / push, or to "" for workflow_dispatch (full-history scan).
#   External callers must set it explicitly; the previous "unset →
#   origin/main..HEAD" fallback is removed because the workflow is the only
#   caller and always sets it.
if [ -z "${SCAN_LOG_OPTS+x}" ]; then
  echo "::error::SCAN_LOG_OPTS is not set. The pr-gate workflow must always set it (possibly to empty for workflow_dispatch full-history scans)."
  exit 2
fi
log_opts="${SCAN_LOG_OPTS}"

install_dir="${HOME}/.local/bin"
mkdir -p "${install_dir}"
binary="${install_dir}/gitleaks"
default_config="${install_dir}/gitleaks-default.toml"

# --- install gitleaks binary (pin checksum) ---
# `scan.sh` is invoked once per gate run, so a within-job cache check would
# always miss and is omitted. Archive integrity is verified against the
# pinned SHA below; that is sufficient.
url="https://github.com/gitleaks/gitleaks/releases/download/v${version}/gitleaks_${version}_linux_x64.tar.gz"
archive="/tmp/gitleaks.tar.gz"
echo "::group::Install gitleaks ${version}"
curl --fail --silent --show-error --location --output "${archive}" "${url}"
actual_sha="$(sha256sum "${archive}" | awk '{print $1}')"
if [ "${actual_sha}" != "${binary_sha}" ]; then
  echo "::error::gitleaks binary checksum mismatch: expected ${binary_sha}, got ${actual_sha}"
  exit 2
fi
tar --extract --gzip --file "${archive}" --directory /tmp gitleaks
mv /tmp/gitleaks "${binary}"
chmod +x "${binary}"
echo "::endgroup::"

# --- fetch upstream default config at the SAME pinned tag (pin checksum) ---
if [ -f "${default_config}" ] && sha256sum "${default_config}" 2>/dev/null | grep -q "${config_sha}"; then
  : # cache hit
else
  echo "::group::Fetch upstream gitleaks default config v${version}"
  curl --fail --silent --show-error --location \
    --output "${default_config}" \
    "https://raw.githubusercontent.com/gitleaks/gitleaks/v${version}/config/gitleaks.toml"
  actual_cfg_sha="$(sha256sum "${default_config}" | awk '{print $1}')"
  if [ "${actual_cfg_sha}" != "${config_sha}" ]; then
    echo "::error::gitleaks default config checksum mismatch: expected ${config_sha}, got ${actual_cfg_sha}"
    exit 2
  fi
  echo "::endgroup::"
fi

# --- rewrite the [extend] path in a runtime copy of the repo allowlist so it
#     points at the absolute fetched default config. The committed .gitleaks.toml
#     carries a placeholder path so the file stays portable and reviewable. ---
#     Scoped to the [extend] table only: a future [[rules]] block with its own
#     `path = "…"` line must not be silently rewritten by this pass.
runtime_allowlist="${HOME}/.local/share/gitleaks-runtime.toml"
mkdir -p "$(dirname "${runtime_allowlist}")"
awk -v path="${default_config}" '
  /^\[extend\]/ { in_extend=1; print; next }
  /^\[/          { in_extend=0; print; next }
  in_extend && /^[[:space:]]*path[[:space:]]*=/ {
    sub(/^[[:space:]]*path[[:space:]]*=[[:space:]]*".*"[[:space:]]*$/,
        "path = \"" path "\"")
  }
  { print }
' "${repo_allowlist}" > "${runtime_allowlist}"

echo "::group::gitleaks ${version}"
"${binary}" version
echo "config: ${runtime_allowlist} (extends ${default_config})"
if [ -n "${log_opts}" ]; then
  echo "log-opts: ${log_opts}"
else
  echo "log-opts: <empty — full history scan>"
fi
echo "::endgroup::"

mkdir -p artifacts/secret-scan

# On a fork pull_request the base SHA may not be reachable from the local
# history (actions/checkout fetches the head + origin/main, not the base).
# Fetch the base ref so --log-opts "base..head" resolves. Failures are
# swallowed so a transient network blip does not block the scan; gitleaks
# then fails closed with an unresolvable base.sha and a clear error.
# GITHUB_BASE_REF is set by the pull_request event.
if [ -n "${GITHUB_BASE_REF:-}" ]; then
  git fetch --no-tags --quiet origin "${GITHUB_BASE_REF}" 2>/dev/null || true
fi

# `gitleaks detect` is the legacy alias and has been deprecated since
# gitleaks v8.19.0; `gitleaks git` is the recommended subcommand. The alias
# still works on 8.27.2 (pinned above) but should be migrated on the next
# gitleaks bump — see WT-10-04 follow-up.
#
# --no-banner keeps the log readable; -v prints each finding inline so a red
# step shows what was caught before the JSON report is uploaded. --redact keeps
# secret material out of the log.
set +e
if [ -n "${log_opts}" ]; then
  "${binary}" detect \
    --source . \
    --config "${runtime_allowlist}" \
    --log-opts "${log_opts}" \
    --no-banner \
    --verbose \
    --redact \
    --report-format json \
    --report-path artifacts/secret-scan/report.json
else
  "${binary}" detect \
    --source . \
    --config "${runtime_allowlist}" \
    --no-banner \
    --verbose \
    --redact \
    --report-format json \
    --report-path artifacts/secret-scan/report.json
fi
status=$?
set -e

case "${status}" in
  0)
    if [ -n "${log_opts}" ]; then
      echo "gitleaks: no leaks found in range ${log_opts}."
    else
      echo "gitleaks: no leaks found (full history scan)."
    fi
    ;;
  1)
    if [ -n "${log_opts}" ]; then
      echo "::error::gitleaks detected secrets in the commit range ${log_opts}."
    else
      echo "::error::gitleaks detected secrets (full history scan)."
    fi
    echo "::error::See artifacts/secret-scan/report.json. Do NOT push a real key to test this step."
    echo "::error::If a finding is a known-safe placeholder, add it to .gitleaks.toml WITH a comment."
    ;;
  *)
    echo "::error::gitleaks failed (exit ${status}) — treat as a gate failure."
    ;;
esac

exit "${status}"
