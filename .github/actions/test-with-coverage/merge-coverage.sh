#!/usr/bin/env bash
# Merge per-level cobertura outputs into artifacts/coverage/ for the PR summary.

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
source "${script_dir}/common.sh"

rm -rf "${coverage_directory}"
mkdir -p "${coverage_directory}"

coverage_count=$(find "${results_root}" -type f -name 'coverage.cobertura.xml' 2>/dev/null | wc -l | tr -d '[:space:]')
if [ "${coverage_count}" -eq 0 ]; then
  echo "WARNING: no cobertura files found under ${results_root}; skipping reportgenerator."
  exit 0
fi

dotnet tool restore

echo "Merging ${coverage_count} cobertura file(s)..."
dotnet tool run reportgenerator \
  "-reports:${results_root}/**/coverage.cobertura.xml" \
  "-targetdir:${coverage_directory}" \
  "-reporttypes:HtmlInline_AzurePipelines;Cobertura;TextSummary;MarkdownSummaryGithub"
