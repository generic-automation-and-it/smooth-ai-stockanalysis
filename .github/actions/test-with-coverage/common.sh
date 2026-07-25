#!/usr/bin/env bash
# Shared helpers for per-level test execution and coverage merge.
# Sourced by run-level.sh and merge-coverage.sh — not executed directly.

set -euo pipefail

build_configuration="${BUILD_CONFIGURATION:-Release}"
artifacts_root="${ARTIFACTS_ROOT:-artifacts}"
timeout_seconds="${DEPENDENCY_TIMEOUT_SECONDS:-120}"
results_root="${artifacts_root}/testresults"
coverage_directory="${artifacts_root}/coverage"

# Product assemblies only — test/architecture projects must not dilute coverage.
coverage_collect="XPlat Code Coverage;Format=cobertura;Include=[SmoothAiStockAnalysis.Domain]*,[SmoothAiStockAnalysis.Application]*,[SmoothAiStockAnalysis.Infrastructure]*,[SmoothAiStockAnalysis.Host]*;ExcludeByFile=**/*.g.cs,**/obj/**,**/Migrations/*.cs,**/*ModelSnapshot.cs"

wiremock_health_url="http://127.0.0.1:19091/__admin/health"
aspire_pid=""
failures=()

# Pre-warming WireMock is an OPTIMISATION, never a requirement. AspireFixture probes the
# well-known endpoint first and starts its own AppHost when nothing answers, so a level that
# skips the pre-warm still works — it just pays container startup inside the test host.
# Default off: no integration test currently opts into AspireCollection, and provisioning a
# container for zero consumers would make L2 need a container runtime for no benefit.
prewarm_wiremock="${PREWARM_WIREMOCK:-0}"

is_prewarm_requested() {
  case "${prewarm_wiremock}" in
    1 | true | TRUE | yes | YES) return 0 ;;
    *) return 1 ;;
  esac
}

unit_projects=(
  "tests/SmoothAiStockAnalysis.Domain.UnitTest/SmoothAiStockAnalysis.Domain.UnitTest.csproj"
  "tests/SmoothAiStockAnalysis.Application.UnitTest/SmoothAiStockAnalysis.Application.UnitTest.csproj"
  "tests/SmoothAiStockAnalysis.Infrastructure.UnitTest/SmoothAiStockAnalysis.Infrastructure.UnitTest.csproj"
  "tests/SmoothAiStockAnalysis.Host.UnitTest/SmoothAiStockAnalysis.Host.UnitTest.csproj"
  "tests/SmoothAiStockAnalysis.Architecture.UnitTest/SmoothAiStockAnalysis.Architecture.UnitTest.csproj"
)

component_projects=(
  "tests/SmoothAiStockAnalysis.Application.ComponentTest/SmoothAiStockAnalysis.Application.ComponentTest.csproj"
  "tests/SmoothAiStockAnalysis.Infrastructure.ComponentTest/SmoothAiStockAnalysis.Infrastructure.ComponentTest.csproj"
)

integration_projects=(
  "tests/SmoothAiStockAnalysis.Host.IntegrationTest/SmoothAiStockAnalysis.Host.IntegrationTest.csproj"
)

# MUST return 0. Callers invoke this from the body of `if ! wait ...; then ... fi`, which is
# not a condition context, so under `set -e` a non-zero return here kills the script at the
# first failing project: the remaining `wait` calls never run, parallel failures are hidden,
# and `fail_if_any` never prints the summary. Recording a failure is not itself a failure —
# `fail_if_any` owns the exit code.
record_failure() {
  local message="$1"
  failures+=("${message}")
  echo "ERROR: ${message}"
}

fail_if_any() {
  local level_name=$1
  if [ "${#failures[@]}" -gt 0 ]; then
    printf '%s level failed:\n' "${level_name}"
    printf ' - %s\n' "${failures[@]}"
    exit 1
  fi
}

run_test_project() {
  local project_path=$1
  local level_results_directory=$2

  dotnet test "${project_path}" \
    --configuration "${build_configuration}" \
    --no-build \
    --results-directory "${level_results_directory}" \
    "--collect:${coverage_collect}"
}

# Run every project in a level; accumulate failures so parallel failures stay visible.
# Projects run concurrently (catalogue parallel-within-phase pattern). SQLite isolation
# uses Guid paths per process, so concurrent component/integration hosts are safe.
run_level_projects() {
  local level_results_directory=$1
  shift
  local projects=("$@")
  local pids=()
  local labels=()
  local project
  local pid
  local index
  local label

  mkdir -p "${level_results_directory}"

  for project in "${projects[@]}"; do
    label="$(basename "${project}" .csproj)"
    echo "→ ${label}"
    run_test_project "${project}" "${level_results_directory}" &
    pids+=("$!")
    labels+=("${label}")
  done

  for index in "${!pids[@]}"; do
    pid="${pids[$index]}"
    label="${labels[$index]}"
    if ! wait "${pid}"; then
      record_failure "${label} failed."
    fi
  done
}

check_aspire_alive() {
  [ -n "${aspire_pid}" ] && kill -0 "${aspire_pid}" 2>/dev/null
}

ensure_aspire_alive() {
  if ! check_aspire_alive; then
    record_failure "Aspire host exited before the action completed."
    return
  fi

  if ! curl -sf --max-time 2 "${wiremock_health_url}" > /dev/null 2>&1; then
    record_failure "WireMock health check failed; the Aspire-managed dependency is unhealthy."
  fi
}

wait_for_http() {
  local url="$1"
  local name="$2"
  local started_at
  local deadline
  local elapsed

  started_at=$(date +%s)
  deadline=$((started_at + timeout_seconds))

  echo "Waiting for ${name} at ${url}..."
  while ! curl -sf --max-time 2 "${url}" > /dev/null 2>&1; do
    if ! check_aspire_alive; then
      echo "ERROR: Aspire host exited while waiting for ${name}."
      return 1
    fi

    if [ "$(date +%s)" -ge "${deadline}" ]; then
      echo "ERROR: Timed out after ${timeout_seconds}s waiting for ${name}."
      echo "${name} health-probe diagnostics:"
      curl --verbose --max-time 2 "${url}" || true
      return 1
    fi

    sleep 2
  done

  elapsed=$(($(date +%s) - started_at))
  echo "${name} is healthy after ${elapsed}s."
}

# Last-resort sweep for a WireMock container Aspire failed to reap. Aspire names the resource
# `wiremock-<suffix>`, not `wiremock`, so this must match by prefix — the filter is anchored so
# an unrelated container such as `my-wiremock-dev` is left alone.
remove_wiremock_containers() {
  local container_ids
  container_ids=$(docker ps -aq --filter "name=^wiremock" 2>/dev/null || true)
  if [ -n "${container_ids}" ]; then
    # shellcheck disable=SC2086
    docker rm -f ${container_ids} > /dev/null 2>&1 || true
  fi
}

cleanup_aspire() {
  if [ -z "${aspire_pid}" ] || ! kill -0 "${aspire_pid}" 2>/dev/null; then
    remove_wiremock_containers
    return
  fi

  if ! kill "${aspire_pid}" 2>/dev/null; then
    echo "ERROR: Failed to terminate Aspire host ${aspire_pid}."
    remove_wiremock_containers
    return
  fi

  for _ in $(seq 1 15); do
    if ! kill -0 "${aspire_pid}" 2>/dev/null; then
      wait "${aspire_pid}" 2>/dev/null || true
      remove_wiremock_containers
      return
    fi
    sleep 1
  done

  echo "WARNING: Aspire host ${aspire_pid} is still running after the termination signal."
  if ! kill -KILL "${aspire_pid}" 2>/dev/null; then
    echo "ERROR: Failed to force-stop Aspire host ${aspire_pid}."
  fi
  wait "${aspire_pid}" 2>/dev/null || true
  remove_wiremock_containers
}

start_aspire_wiremock() {
  # Refuse a second start. Both `aspire_pid` and the trap live in this function, so a
  # second call would overwrite the pid and leak the first host — cleanup only ever
  # sees the latest one. One level starts Aspire today; this keeps that true.
  if [ -n "${aspire_pid}" ]; then
    echo "ERROR: Aspire host already started with PID ${aspire_pid}; refusing to start a second."
    return 1
  fi

  export ASPNETCORE_URLS="http://localhost:19888"
  export ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL="http://localhost:19889"
  export ASPIRE_ALLOW_UNSECURED_TRANSPORT="true"

  trap cleanup_aspire EXIT INT TERM

  dotnet run \
    --project tests/SmoothAiStockAnalysis.TestFramework.Aspire \
    --configuration "${build_configuration}" \
    --no-build \
    --no-launch-profile \
    -- \
    --no-dashboard &

  aspire_pid=$!
  echo "Aspire WireMock host started with PID ${aspire_pid}."

  if ! check_aspire_alive; then
    echo "ERROR: Aspire host exited before reaching the health probe."
    exit 1
  fi

  wait_for_http "${wiremock_health_url}" "WireMock" || exit 1
}
