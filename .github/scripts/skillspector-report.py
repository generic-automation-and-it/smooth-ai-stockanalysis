#!/usr/bin/env python3
"""Render a SkillSpector JSON report into a GitHub job summary (stdout) and SARIF.

SkillSpector emits one --format per run, and its native SARIF is minimal (no
risk score, category, or confidence). The JSON report carries everything, so we
scan once with --format json and derive everything here:

  - Markdown summary -> stdout (redirect into $GITHUB_STEP_SUMMARY)
  - SARIF 2.1.0      -> <sarif_out> (for GitHub code scanning, when enabled)

This scan is ADVISORY / NON-GATING. These are FIRST-PARTY agent skills that
legitimately run shell, gh/git, and template file operations; SkillSpector
(correctly, for an untrusted third-party skill) rates those HIGH and pins its risk
score at 100, so a raw `risk > 50` gate would block every PR forever, and the
inherent findings cannot be "fixed" without gutting the skill. Rather than maintain
a per-finding allowlist to suppress them, the scan runs for visibility only: every
finding is reported to the job summary and to code scanning for human review, and
the PR is never blocked. Novel-pattern safety comes from that human review plus the
optional LLM semantic scan, not from an automated gate.

Both the static and the LLM report are rendered the same way (as informational
findings tables); pass the LLM report via --advisory to render it as a separate,
clearly-labeled section.

Usage:
  skillspector-report.py <report.json> <out.sarif> [path_prefix] [--advisory FILE]

`path_prefix` (default ".agents/skills") is prepended to each finding's file so SARIF
locations resolve from the repo root. Missing/empty JSON (e.g. SkillSpector errored
before writing) is handled gracefully: a note is emitted and an empty SARIF is written.
Because the scan is advisory, an incomplete scan is reported but never fails the run.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

# SkillSpector severity -> (SARIF level, GitHub security-severity numeric)
LEVEL = {"CRITICAL": "error", "HIGH": "error", "MEDIUM": "warning", "LOW": "note"}
SECSEV = {"CRITICAL": "9.5", "HIGH": "8.0", "MEDIUM": "5.0", "LOW": "2.0"}
SEV_ORDER = {"CRITICAL": 0, "HIGH": 1, "MEDIUM": 2, "LOW": 3}
EMOJI = {"CRITICAL": "🟥", "HIGH": "🟧", "MEDIUM": "🟨", "LOW": "🟦"}


def empty_sarif() -> dict:
    return {
        "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
        "version": "2.1.0",
        "runs": [
            {
                "tool": {
                    "driver": {
                        "name": "SkillSpector",
                        "informationUri": "https://github.com/NVIDIA/SkillSpector",
                        "rules": [],
                    }
                },
                "results": [],
            }
        ],
    }


def build_sarif(report: dict, prefix: str) -> dict:
    issues = report.get("issues") or []
    version = (report.get("metadata") or {}).get("skillspector_version")
    rules: dict[str, dict] = {}
    results = []
    for it in issues:
        rid = it.get("id") or "UNKNOWN"
        sev = (it.get("severity") or "MEDIUM").upper()
        if rid not in rules:
            rules[rid] = {
                "id": rid,
                "name": (it.get("category") or rid).replace(" ", ""),
                "shortDescription": {"text": it.get("category") or rid},
                "fullDescription": {"text": it.get("explanation") or it.get("pattern") or rid},
                "helpUri": "https://github.com/NVIDIA/SkillSpector",
                "properties": {
                    "security-severity": SECSEV.get(sev, "5.0"),
                    "tags": [t for t in [it.get("category")] if t],
                },
            }
        loc = it.get("location") or {}
        f = loc.get("file")
        uri = f"{prefix.rstrip('/')}/{f}" if f else (f or "")
        region = {}
        if loc.get("start_line"):
            region["startLine"] = loc["start_line"]
        if loc.get("end_line"):
            region["endLine"] = loc["end_line"]
        physical = {"artifactLocation": {"uri": uri}}
        if region:
            physical["region"] = region
        results.append(
            {
                "ruleId": rid,
                "level": LEVEL.get(sev, "warning"),
                "message": {"text": f"[{sev}] {it.get('pattern') or it.get('category') or rid}: {it.get('explanation') or ''}".strip()},
                "locations": [{"physicalLocation": physical}],
            }
        )
    driver = {
        "name": "SkillSpector",
        "informationUri": "https://github.com/NVIDIA/SkillSpector",
        "rules": list(rules.values()),
    }
    if version:
        driver["version"] = version
    return {
        "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
        "version": "2.1.0",
        "runs": [{"tool": {"driver": driver}, "results": results}],
    }


def _skill(it: dict) -> str:
    """The owning skill = first path segment of the finding's file, relative to the
    scanned skills dir (e.g. 'ai-template-sync/scripts/sync.sh' -> 'ai-template-sync').
    Files directly under the scan root (e.g. 'README.md') have no owning skill."""
    f = (it.get("location") or {}).get("file") or ""
    head = f.split("/", 1)[0]
    return head if head and "/" in f else "(root)"


def _issue_row(it: dict) -> str:
    loc = it.get("location") or {}
    where = loc.get("file", "?")
    if loc.get("start_line"):
        where += f":{loc['start_line']}"
    conf = it.get("confidence")
    conf_s = f"{conf:.0%}" if isinstance(conf, (int, float)) else ""
    return (
        f"| {(it.get('severity') or '?').upper()} | {it.get('id','')} | "
        f"`{_skill(it)}` | {it.get('category','')} | {it.get('pattern','')} | `{where}` | {conf_s} |"
    )


def _table(out: list[str], items: list[dict]) -> None:
    out.append("| Sev | ID | Skill | Category | Pattern | Location | Conf |")
    out.append("|-----|----|-------|----------|---------|----------|------|")
    for it in sorted(items, key=lambda i: SEV_ORDER.get((i.get("severity") or "").upper(), 9)):
        out.append(_issue_row(it))
    out.append("")


def build_summary(report: dict) -> str:
    ra = report.get("risk_assessment") or {}
    score = ra.get("score")
    sev = (ra.get("severity") or "?").upper()
    issues = report.get("issues") or []
    meta = report.get("metadata") or {}
    llm = "LLM semantic + static" if meta.get("llm_available") else "static-only"

    counts: dict[str, int] = {}
    for it in issues:
        s = (it.get("severity") or "?").upper()
        counts[s] = counts.get(s, 0) + 1
    breakdown = ", ".join(
        f"{counts[s]} {s}" for s in sorted(counts, key=lambda x: SEV_ORDER.get(x, 9))
    ) or "none"

    out = ["## 🛡️ SkillSpector scan — advisory, NON-GATING", ""]
    out.append(
        "Informational only. These are first-party skills that legitimately run shell/`gh`/`git`/"
        "template operations, so SkillSpector rates them HIGH by design. This scan reports findings "
        "for human review and **never blocks the PR**; review anything unexpected below."
    )
    out.append("")
    badge = EMOJI.get(sev, "⬜")
    out.append(f"{badge} **SkillSpector raw score: {score}/100 — {sev}** (expected-HIGH for first-party skills; informational)")
    out.append("")
    out.append(f"**{len(issues)} findings** ({breakdown}) · analysis: {llm}")
    out.append("")

    if issues:
        out.append("### Findings (review, non-gating)")
        out.append("")
        _table(out, issues)
    else:
        out.append("### ✅ No findings")
        out.append("")

    return "\n".join(out)


def build_advisory(report: dict) -> str:
    """Render the LLM semantic report as a separate advisory section. The whole scan is
    non-gating; this section exists to keep the LLM findings visually distinct from the
    static ones because they are nondeterministic across model/prompt drift."""
    issues = report.get("issues") or []
    meta = report.get("metadata") or {}
    model = meta.get("model") or meta.get("llm_model")
    model_s = f" (model: `{model}`)" if model else ""

    out = [f"### 🔬 LLM semantic analysis — advisory{model_s}", ""]
    out.append(
        "Informational only. Semantic findings are nondeterministic across model/prompt "
        "changes, so they are reported for human review and never block the PR."
    )
    out.append("")
    if not issues:
        out.append("No semantic findings.")
        out.append("")
        return "\n".join(out)

    out.append(f"**{len(issues)} semantic finding(s).**")
    out.append("")
    _table(out, issues)
    return "\n".join(out)


def main() -> int:
    ap = argparse.ArgumentParser(description="Render SkillSpector JSON -> advisory job summary + SARIF.")
    ap.add_argument("report_json")
    ap.add_argument("sarif_out")
    ap.add_argument("path_prefix", nargs="?", default=".agents/skills")
    ap.add_argument("--advisory", help="Path to a secondary (LLM) report rendered as a separate advisory section.")
    args = ap.parse_args()

    p = Path(args.report_json)
    if not p.exists() or p.stat().st_size == 0:
        Path(args.sarif_out).write_text(json.dumps(empty_sarif()))
        print("## 🛡️ SkillSpector scan — advisory, NON-GATING\n\n⚠️ No JSON report produced; the scan likely errored before completing. See the scan step log. (Advisory scan — the PR is not blocked.)\n")
        return 0

    report = json.loads(p.read_text())

    Path(args.sarif_out).write_text(json.dumps(build_sarif(report, args.path_prefix)))
    print(build_summary(report))

    # Advisory LLM section. Rendered only when a non-empty report is given; a parse
    # error is noted but, since the whole scan is non-gating, never affects the run.
    if args.advisory:
        ap_path = Path(args.advisory)
        if ap_path.exists() and ap_path.stat().st_size > 0:
            try:
                print(build_advisory(json.loads(ap_path.read_text())))
            except json.JSONDecodeError as exc:
                print(f"\n### 🔬 LLM semantic analysis — advisory\n\n> ⚠️ Advisory report could not be parsed ({exc}); skipped.\n")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
