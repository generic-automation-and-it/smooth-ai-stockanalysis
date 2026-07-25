#!/usr/bin/env bash
# Run exactly one test level: unit | component | integration
# Usage:
#   BUILD_CONFIGURATION=Release bash .github/actions/test-with-coverage/run-level.sh unit
# Unit and component levels do not start WireMock. Integration starts Aspire WireMock.

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
mkdir -p "${level_results_directory}"

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
    echo "Level — integration (Aspire WireMock + isolated SQLite)..."
    start_aspire_wiremock
    run_level_projects "${level_results_directory}" "${integration_projects[@]}"
    ensure_aspire_alive
    fail_if_any "Integration"
    ;;
  *)
    echo "Unknown level '${level}'. Expected unit, component, or integration." >&2
    exit 2
    ;;
esac

echo "Level ${level} passed."
