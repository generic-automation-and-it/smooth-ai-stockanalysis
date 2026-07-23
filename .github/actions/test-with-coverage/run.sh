#!/usr/bin/env bash

set -u

build_configuration="${BUILD_CONFIGURATION:-Release}"
artifacts_root="${ARTIFACTS_ROOT:-artifacts}"
results_directory="${artifacts_root}/testresults"
coverage_directory="${artifacts_root}/coverage"
coverage_collect="XPlat Code Coverage;Format=cobertura;Include=[SmoothAiStockAnalysis.*]*;ExcludeByFile=**/*.g.cs,**/obj/**,**/Migrations/*.cs,**/*ModelSnapshot.cs"
failures=()

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

dotnet tool restore || exit 1

rm -rf "${results_directory}" "${coverage_directory}"
mkdir -p "${results_directory}" "${coverage_directory}"

echo "Phase 1 — integration tests..."
run_test_project tests/SmoothAiStockAnalysis.Host.IntegrationTest/SmoothAiStockAnalysis.Host.IntegrationTest.csproj \
  || record_failure "Host integration tests failed."

echo "Phase 2 — component tests..."
run_test_project tests/SmoothAiStockAnalysis.Application.ComponentTest/SmoothAiStockAnalysis.Application.ComponentTest.csproj \
  || record_failure "Application component tests failed."
run_test_project tests/SmoothAiStockAnalysis.Infrastructure.ComponentTest/SmoothAiStockAnalysis.Infrastructure.ComponentTest.csproj \
  || record_failure "Infrastructure component tests failed."

echo "Phase 3 — unit tests..."
run_test_project tests/SmoothAiStockAnalysis.Domain.UnitTest/SmoothAiStockAnalysis.Domain.UnitTest.csproj \
  || record_failure "Domain unit tests failed."
run_test_project tests/SmoothAiStockAnalysis.Application.UnitTest/SmoothAiStockAnalysis.Application.UnitTest.csproj \
  || record_failure "Application unit tests failed."
run_test_project tests/SmoothAiStockAnalysis.Infrastructure.UnitTest/SmoothAiStockAnalysis.Infrastructure.UnitTest.csproj \
  || record_failure "Infrastructure unit tests failed."
run_test_project tests/SmoothAiStockAnalysis.Host.UnitTest/SmoothAiStockAnalysis.Host.UnitTest.csproj \
  || record_failure "Host unit tests failed."

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
