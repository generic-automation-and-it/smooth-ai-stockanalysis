#!/usr/bin/env bash

set -euo pipefail

build_configuration="${BUILD_CONFIGURATION:-Release}"
artifacts_root="${ARTIFACTS_ROOT:-artifacts}"
timeout_seconds="${DEPENDENCY_TIMEOUT_SECONDS:-120}"
results_directory="${artifacts_root}/testresults"
coverage_directory="${artifacts_root}/coverage"
coverage_collect="XPlat Code Coverage;Format=cobertura;Include=[SmoothAiStockAnalysis.*]*;ExcludeByFile=**/*.g.cs,**/obj/**,**/Migrations/*.cs,**/*ModelSnapshot.cs"
wiremock_health_url="http://127.0.0.1:19091/__admin/health"
aspire_pid=""
failures=()

cleanup() {
  if [ -z "${aspire_pid}" ] || ! kill -0 "${aspire_pid}" 2>/dev/null; then
    docker rm -f wiremock >/dev/null 2>&1 || true
    return
  fi

  if ! kill "${aspire_pid}" 2>/dev/null; then
    echo "ERROR: Failed to terminate Aspire host ${aspire_pid}."
    docker rm -f wiremock >/dev/null 2>&1 || true
    return
  fi

  for _ in $(seq 1 15); do
    if ! kill -0 "${aspire_pid}" 2>/dev/null; then
      wait "${aspire_pid}" 2>/dev/null || true
      docker rm -f wiremock >/dev/null 2>&1 || true
      return
    fi
    sleep 1
  done

  echo "WARNING: Aspire host ${aspire_pid} is still running after the termination signal."
  if ! kill -KILL "${aspire_pid}" 2>/dev/null; then
    echo "ERROR: Failed to force-stop Aspire host ${aspire_pid}."
  fi
  wait "${aspire_pid}" 2>/dev/null || true
  docker rm -f wiremock >/dev/null 2>&1 || true
}

trap cleanup EXIT INT TERM

check_aspire_alive() {
  kill -0 "${aspire_pid}" 2>/dev/null
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
  local url=$1
  local name=$2
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

run_test_project() {
  local project_path=$1

  dotnet test "${project_path}" \
    --configuration "${build_configuration}" \
    --no-build \
    --results-directory "${results_directory}" \
    "--collect:${coverage_collect}"
}

record_failure() {
  local message=$1
  failures+=("${message}")
  echo "ERROR: ${message}"
}

export ASPNETCORE_URLS="http://localhost:19888"
export ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL="http://localhost:19889"
export ASPIRE_ALLOW_UNSECURED_TRANSPORT="true"

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

dotnet tool restore || exit 1

rm -rf "${results_directory}" "${coverage_directory}"
mkdir -p "${results_directory}" "${coverage_directory}"

echo "Phase 1 — integration tests..."
run_test_project tests/SmoothAiStockAnalysis.Host.IntegrationTest/SmoothAiStockAnalysis.Host.IntegrationTest.csproj \
  || record_failure "Host integration tests failed."
ensure_aspire_alive

echo "Phase 2 — component tests..."
run_test_project tests/SmoothAiStockAnalysis.Application.ComponentTest/SmoothAiStockAnalysis.Application.ComponentTest.csproj \
  || record_failure "Application component tests failed."
run_test_project tests/SmoothAiStockAnalysis.Infrastructure.ComponentTest/SmoothAiStockAnalysis.Infrastructure.ComponentTest.csproj \
  || record_failure "Infrastructure component tests failed."
ensure_aspire_alive

echo "Phase 3 — unit tests..."
run_test_project tests/SmoothAiStockAnalysis.Domain.UnitTest/SmoothAiStockAnalysis.Domain.UnitTest.csproj \
  || record_failure "Domain unit tests failed."
run_test_project tests/SmoothAiStockAnalysis.Application.UnitTest/SmoothAiStockAnalysis.Application.UnitTest.csproj \
  || record_failure "Application unit tests failed."
run_test_project tests/SmoothAiStockAnalysis.Infrastructure.UnitTest/SmoothAiStockAnalysis.Infrastructure.UnitTest.csproj \
  || record_failure "Infrastructure unit tests failed."
run_test_project tests/SmoothAiStockAnalysis.Host.UnitTest/SmoothAiStockAnalysis.Host.UnitTest.csproj \
  || record_failure "Host unit tests failed."
ensure_aspire_alive

dotnet tool run reportgenerator \
  "-reports:${results_directory}/**/coverage.cobertura.xml" \
  "-targetdir:${coverage_directory}" \
  "-reporttypes:HtmlInline_AzurePipelines;Cobertura;TextSummary;MarkdownSummaryGithub" \
  || record_failure "Coverage report generation failed."

if [ "${#failures[@]}" -gt 0 ]; then
  printf 'Test with coverage failed:\n'
  printf ' - %s\n' "${failures[@]}"
  exit 1
fi
