#!/usr/bin/env bash
# Compatibility entry point: run all three levels then merge coverage.
# Prefer run-level.sh for a single level. CI invokes levels as separate steps.
#
# No EXIT/INT/TERM trap here on purpose: each run-level.sh runs in its own subshell
# and installs its own trap, so Aspire teardown is already owned one level down.
# Adding a trap here would only duplicate that, and would fire after the child has
# already cleaned up.

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

overall_failures=0

run_one() {
  local level=$1
  if ! bash "${script_dir}/run-level.sh" "${level}"; then
    overall_failures=$((overall_failures + 1))
  fi
}

# Always attempt every level so local full-suite runs still surface all failures.
run_one unit
run_one component
run_one integration

if ! bash "${script_dir}/merge-coverage.sh"; then
  overall_failures=$((overall_failures + 1))
fi

if [ "${overall_failures}" -gt 0 ]; then
  echo "Test with coverage failed (${overall_failures} phase(s))."
  exit 1
fi
