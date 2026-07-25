#!/usr/bin/env bash
# Run exactly one test level: unit | component | integration
# Usage:
#   BUILD_CONFIGURATION=Release bash .github/actions/test-with-coverage/run-level.sh unit
# No level starts WireMock by default. Integration pre-warms it only when PREWARM_WIREMOCK=1;
# otherwise a test that needs it starts it through AspireFixture.

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "${script_dir}/common.sh"

level="${1:-}"
if [ -z "${level}" ]; then
  echo "Usage: $0 <unit|component|integration>" >&2
  exit 2
fi

level_results_directory="${results_root}/${level}"

case "${level}" in
  unit)
    echo "Level — unit (no container runtime required)..."
    run_level_projects "${level_results_directory}" "${unit_projects[@]}"
    fail_if_any "Unit"
    ;;
  component)
    echo "Level — component (isolated SQLite / in-memory EF; no WireMock)..."
    run_level_projects "${level_results_directory}" "${component_projects[@]}"
    fail_if_any "Component"
    ;;
  integration)
    if is_prewarm_requested; then
      echo "Level — integration (pre-warmed Aspire WireMock + isolated SQLite)..."
      start_aspire_wiremock
      run_level_projects "${level_results_directory}" "${integration_projects[@]}"
      ensure_aspire_alive
    else
      echo "Level — integration (isolated SQLite; no container runtime)..."
      echo "  WireMock is not pre-warmed. Tests that opt into AspireCollection start it themselves."
      echo "  Set PREWARM_WIREMOCK=1 to start it once up front and skip that per-run cost."
      run_level_projects "${level_results_directory}" "${integration_projects[@]}"
    fi
    fail_if_any "Integration"
    ;;
  *)
    echo "Unknown level '${level}'. Expected unit, component, or integration." >&2
    exit 2
    ;;
esac

echo "Level ${level} passed."
