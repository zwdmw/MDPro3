param(
    [string]$RuntimeRoot = "D:\game\MDPro3",
    [string]$ProjectRoot = "D:\game\MDPro3-src",
    [string]$CardLabRoot = "D:\Cards",
    [int[]]$TargetCardIds = @(100083, 99993, 100352, 100047),
    [string]$OutputRoot = "D:\game\MDPro3-src\Logs",
    [switch]$NoFail
)

$ErrorActionPreference = "Stop"

if (!(Test-Path -LiteralPath $RuntimeRoot -PathType Container)) {
    throw "Runtime root was not found: $RuntimeRoot"
}

if (!(Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    throw "Project root was not found: $ProjectRoot"
}

if (!(Test-Path -LiteralPath $CardLabRoot -PathType Container)) {
    throw "CardLab root was not found: $CardLabRoot"
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$jsonPath = Join-Path $OutputRoot "quest-expansion-resource-check-$stamp.json"
$markdownPath = Join-Path $OutputRoot "quest-expansion-resource-check-$stamp.md"
$pythonPath = Join-Path $OutputRoot "quest-expansion-resource-check-$stamp.py"

$python = @'
from __future__ import annotations

import json
import os
import sqlite3
import sys
import traceback
import zipfile
from pathlib import Path


def database_priority(path: Path) -> int:
    name = path.stem.lower()
    if name == "cards":
        return 0
    if name == "ifanime":
        return 100
    if name.startswith("ifzcg"):
        value = 0
        multiplier = 1
        found = False
        for ch in reversed(name):
            if ch < "0" or ch > "9":
                break
            found = True
            value += (ord(ch) - ord("0")) * multiplier
            multiplier *= 10
        return 300 + (value if found else 0)
    return 200


def sort_database_paths(paths: list[Path]) -> list[Path]:
    return sorted(paths, key=lambda p: (database_priority(p), p.stat().st_mtime, str(p).lower()))


def read_database(path: Path) -> dict[int, dict]:
    result: dict[int, dict] = {}
    con = sqlite3.connect(str(path))
    try:
        data_rows = con.execute(
            "select id, alias, setcode, type, level, race, attribute, atk, def from datas"
        ).fetchall()
        text_rows = {}
        try:
            text_rows = {
                int(row[0]): {
                    "name": row[1] or "",
                    "desc": row[2] or "",
                }
                for row in con.execute("select id, name, desc from texts").fetchall()
            }
        except sqlite3.Error:
            text_rows = {}
    finally:
        con.close()

    for row in data_rows:
        card_id = int(row[0])
        text = text_rows.get(card_id, {})
        result[card_id] = {
            "id": card_id,
            "alias": int(row[1] or 0),
            "setcode": int(row[2] or 0),
            "type": int(row[3] or 0),
            "level": int(row[4] or 0),
            "race": int(row[5] or 0),
            "attribute": int(row[6] or 0),
            "atk": int(row[7] or 0),
            "def": int(row[8] or 0),
            "name": text.get("name", ""),
            "desc_length": len(text.get("desc", "")),
            "database": str(path),
            "database_name": path.name,
        }
    return result


def summarize_buttons(report: dict) -> dict:
    return {
        "status": report.get("status"),
        "custom_script_loaded": report.get("custom_script_loaded"),
        "core_errors": report.get("core_errors", []),
        "missing_scripts": report.get("missing_scripts", []),
        "loaded_tail": report.get("loaded_scripts", [])[-5:],
    }


def run_ocg_script_check(cardlab_root: Path, runtime_root: Path, project_root: Path, database_paths: list[Path], card_id: int) -> dict:
    sys.path.insert(0, str(cardlab_root / "cardlab"))
    from cardlab.ocg_runner import OcgCoreRunner

    expansions = runtime_root / "Expansions"
    script_roots = [expansions / "script"]
    script_zips = []
    for candidate in [expansions / "script.zip", project_root / "Data" / "script.zip"]:
        if candidate.exists():
            script_zips.append(candidate)

    runner = OcgCoreRunner(
        database_paths=database_paths,
        script_roots=script_roots,
        script_zips=script_zips,
    )
    try:
        runner.load()
        report = runner.run_script_load_check(card_id)
        return summarize_buttons(report)
    finally:
        runner.close()


def main() -> int:
    runtime_root = Path(sys.argv[1])
    project_root = Path(sys.argv[2])
    cardlab_root = Path(sys.argv[3])
    json_path = Path(sys.argv[4])
    markdown_path = Path(sys.argv[5])
    target_ids = [int(item) for item in sys.argv[6].split(",") if item.strip()]

    expansions = runtime_root / "Expansions"
    script_dir = expansions / "script"
    script_zip = expansions / "script.zip"
    database_paths = sort_database_paths([p for p in expansions.glob("*.cdb") if p.is_file()])

    occurrences: dict[int, list[dict]] = {}
    final_rows: dict[int, dict] = {}
    database_errors = []

    for path in database_paths:
        try:
            rows = read_database(path)
        except Exception as exc:
            database_errors.append({"database": str(path), "error": str(exc)})
            continue
        for card_id, row in rows.items():
            occurrences.setdefault(card_id, []).append(row)
            final_rows[card_id] = row

    zip_entries: set[str] = set()
    zip_error = ""
    if script_zip.exists():
        try:
            with zipfile.ZipFile(script_zip) as zf:
                zip_entries = {item.filename.replace("\\", "/").lower() for item in zf.infolist()}
        except Exception as exc:
            zip_error = str(exc)
    else:
        zip_error = "script.zip missing"

    target_reports = []
    failures = []
    for card_id in target_ids:
        final = final_rows.get(card_id)
        loose_script = script_dir / f"c{card_id}.lua"
        zip_script_name = f"script/c{card_id}.lua"
        zip_has_script = zip_script_name.lower() in zip_entries
        script_check = None
        script_check_error = ""
        if loose_script.exists() or zip_has_script:
            try:
                script_check = run_ocg_script_check(cardlab_root, runtime_root, project_root, database_paths, card_id)
            except Exception as exc:
                script_check_error = str(exc)
                script_check = {
                    "status": "error",
                    "core_errors": [str(exc)],
                    "traceback": traceback.format_exc(limit=8),
                }

        report = {
            "id": card_id,
            "final": final,
            "occurrences": occurrences.get(card_id, []),
            "loose_script": str(loose_script),
            "loose_script_exists": loose_script.exists(),
            "zip_script": zip_script_name,
            "zip_script_exists": zip_has_script,
            "script_check": script_check,
            "script_check_error": script_check_error,
        }
        target_reports.append(report)

        if final is None:
            failures.append(f"{card_id}: missing card data")
        if not loose_script.exists() and not zip_has_script:
            failures.append(f"{card_id}: missing script in loose folder and script.zip")
        if script_check is not None and script_check.get("status") != "ok":
            failures.append(f"{card_id}: ocgcore script check failed")

    duplicate_count = sum(1 for rows in occurrences.values() if len(rows) > 1)
    duplicate_targets = {
        str(card_id): occurrences[card_id]
        for card_id in target_ids
        if len(occurrences.get(card_id, [])) > 1
    }

    result = {
        "runtime_root": str(runtime_root),
        "project_root": str(project_root),
        "cardlab_root": str(cardlab_root),
        "database_paths": [str(p) for p in database_paths],
        "database_errors": database_errors,
        "script_zip": str(script_zip),
        "script_zip_exists": script_zip.exists(),
        "script_zip_error": zip_error,
        "script_dir": str(script_dir),
        "target_ids": target_ids,
        "target_reports": target_reports,
        "duplicate_card_id_count": duplicate_count,
        "duplicate_targets": duplicate_targets,
        "failure_count": len(failures),
        "failures": failures,
    }

    json_path.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")

    lines = []
    lines.append("# Quest Expansion Resource Check")
    lines.append("")
    lines.append(f"- Runtime root: `{runtime_root}`")
    lines.append(f"- Databases: {len(database_paths)}")
    lines.append(f"- Duplicate card ids: {duplicate_count}")
    lines.append(f"- Failures: {len(failures)}")
    lines.append("")
    lines.append("## Database Load Order")
    lines.append("")
    for index, path in enumerate(database_paths, start=1):
        lines.append(f"{index}. `{path.name}` priority={database_priority(path)}")
    lines.append("")
    lines.append("## Target Cards")
    lines.append("")
    for item in target_reports:
        final = item["final"]
        if final:
            final_text = f"{final.get('name') or '<unnamed>'} type={final.get('type')} atk={final.get('atk')} def={final.get('def')} winner={final.get('database_name')}"
        else:
            final_text = "<missing>"
        script = "loose" if item["loose_script_exists"] else "-"
        script += " / zip" if item["zip_script_exists"] else " / -"
        check = item["script_check"] or {}
        lines.append(f"- `{item['id']}` {final_text}; scripts={script}; ocg={check.get('status', '<not-run>')}")
        if len(item["occurrences"]) > 1:
            winners = ", ".join(row["database_name"] for row in item["occurrences"])
            lines.append(f"  - duplicates: {winners}")
        if check.get("core_errors"):
            for error in check.get("core_errors", [])[:4]:
                lines.append(f"  - error: `{error}`")
    lines.append("")
    if failures:
        lines.append("## Failures")
        lines.append("")
        for failure in failures:
            lines.append(f"- `{failure}`")
    else:
        lines.append("## Result")
        lines.append("")
        lines.append("- All target cards have data, scripts, and passing ocgcore script-load checks.")
    lines.append("")
    lines.append(f"Full JSON: `{json_path}`")
    markdown_path.write_text("\n".join(lines), encoding="utf-8")

    print(f"Quest expansion resource check JSON: {json_path}")
    print(f"Quest expansion resource check report: {markdown_path}")
    if failures:
        print("Failures:")
        for failure in failures:
            print(f" - {failure}")
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
'@

Set-Content -LiteralPath $pythonPath -Value $python -Encoding UTF8

$targetArg = ($TargetCardIds -join ",")
& py -3.10 $pythonPath $RuntimeRoot $ProjectRoot $CardLabRoot $jsonPath $markdownPath $targetArg
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0 -and !$NoFail) {
    throw "Quest expansion resource check failed with exit code $exitCode. Report: $markdownPath"
}

Write-Host "Quest expansion resource check completed. Report: $markdownPath"
