#!/usr/bin/env python3
"""A.1 commit triage: classify 4.17.0.0..origin/master into risk buckets.

Spec: docs/plans/A-triage/design.md. Output: commits.json (same folder).
Stdlib only; deterministic. Safe to re-run any time.
"""
import argparse
import datetime
import fnmatch
import json
import os
import re
import subprocess
import sys

TAG_FROM = "4.17.0.0"
HEAD_REF = "origin/master"

BUCKET_NAMES = [
    "engine_data", "web_services", "engine", "web_other", "app_v4",
    "app_v3", "native", "build", "docs", "other",
]

_SETTINGS_PATTERNS = ["*Setting*.cs", "*Options*.cs", "*PadSetting*.cs",
                      "*UserGame*.cs", "*Preset*.cs"]
_HIGH_FILE_PATTERNS = ["IWebService.cs", "SearchParameter*.cs", "SearchResult*.cs",
                       "*Setting*.cs", "*Options*.cs", "*PadSetting*.cs"]
_VERSION_PATTERNS = ["*AssemblyInfo.cs", "Version.cs", "*.nuspec"]


def normalize_numstat_path(path):
    """Resolve git rename notation ('old => new', 'a/{b => c}/d') to the new path."""
    if "{" in path and " => " in path:
        path = re.sub(r"\{[^{}]*? => ([^{}]*?)\}", r"\1", path)
        path = path.replace("//", "/")
    elif " => " in path:
        path = path.split(" => ", 1)[1]
    return path


def classify_file(path):
    """Design §5 path-bucket rules, first match wins."""
    p = path.replace("\\", "/")
    parts = p.split("/")
    top = parts[0]
    name = parts[-1]
    nl = name.lower()
    if (p.startswith("x360ce.Engine/Data/")
            or (top == "x360ce.Engine" and ".edmx" in nl)
            or p == "x360ce.Engine/IWebService.cs"
            or top == "x360ce.Data"):
        return "engine_data"
    if p.startswith("x360ce.Web/WebServices/") or p.startswith("x360ce.Web/App_Code/"):
        return "web_services"
    if top == "x360ce.Engine":
        return "engine"
    if top == "x360ce.Web":
        return "web_other"
    if top in ("x360ce.App.4", "x360ce.App.v4"):
        return "app_v4"
    if top in ("x360ce.App", "x360ce.App.Beta", "x360ce.App.WPF"):
        return "app_v3"
    if top in ("x360ce", "MinHook", "x360ce.RemoteController", "Mobile"):
        return "native"
    if nl.endswith((".cpp", ".h", ".hpp", ".def", ".rc")) or ".vcxproj" in nl:
        return "native"
    if (nl.endswith((".sln", ".slnx", ".csproj", ".props", ".targets",
                     ".bat", ".cmd", ".ps1"))
            or nl in (".gitignore", ".gitattributes", ".editorconfig", ".gitmodules")):
        return "build"
    if (nl.endswith((".md", ".txt"))
            or nl.startswith(("readme", "license"))
            or any(seg.lower() == "documents" for seg in parts[:-1])
            or (len(parts) == 1 and nl.endswith((".png", ".jpg")))):
        return "docs"
    return "other"


def compute_buckets(files):
    """Aggregate per-file buckets into per-commit booleans + LOC + xaml overlay."""
    buckets = {b: False for b in BUCKET_NAMES}
    buckets_loc = {}
    app_ui_xaml = False
    for path, ins, dels in files:
        b = classify_file(path)
        buckets[b] = True
        loc = buckets_loc.setdefault(b, {"ins": 0, "del": 0})
        loc["ins"] += ins
        loc["del"] += dels
        if b in ("app_v3", "app_v4") and path.lower().endswith(".xaml"):
            app_ui_xaml = True
    return buckets, buckets_loc, app_ui_xaml


def _basename(path):
    return path.replace("\\", "/").rsplit("/", 1)[-1]


def _matches_any(name, patterns):
    return any(fnmatch.fnmatch(name, pat) for pat in patterns)


def compute_flags(files, buckets, parent_count):
    """Design §7 heuristics."""
    names = [_basename(p) for p, _, _ in files]
    return {
        "touches_data_model": buckets["engine_data"]
            or any(".edmx" in n.lower() for n in names),
        "touches_settings": any(_matches_any(n, _SETTINGS_PATTERNS) for n in names),
        "touches_webservice_api": buckets["web_services"]
            or any(n == "IWebService.cs" for n in names),
        "is_version_bump": bool(names)
            and all(_matches_any(n, _VERSION_PATTERNS) for n in names),
        "is_docs_only": bool(files)
            and all(classify_file(p) == "docs" for p, _, _ in files),
        "is_merge_no_changes": parent_count >= 2 and not files,
    }


def compute_risk(buckets, flags, files):
    """Design §6 rubric, first match wins. Returns (risk_level, risk_reason)."""
    if flags["is_merge_no_changes"]:
        return "SKIP", "Merge commit with no changes"
    if flags["is_version_bump"]:
        return "SKIP", "Version bump only"
    if flags["is_docs_only"]:
        return "SKIP", "Documentation only"
    if buckets["engine_data"]:
        return "HIGH", "Touches engine_data bucket (data model / EDMX / IWebService)"
    if buckets["web_services"]:
        return "HIGH", "Touches web_services bucket (SOAP API surface)"
    high_hit = next((_basename(p) for p, _, _ in files
                     if _matches_any(_basename(p), _HIGH_FILE_PATTERNS)), None)
    if high_hit:
        return "HIGH", f"Touches sensitive filename: {high_hit}"
    if buckets["engine"]:
        return "MEDIUM", "Touches engine bucket (shared engine code)"
    if buckets["web_other"]:
        return "MEDIUM", "Touches web_other bucket (web app, non-API)"
    return "LOW", "Only app/native/build/other buckets touched"


def _git(repo, *args):
    result = subprocess.run(
        ["git", "-C", repo, "-c", "core.quotePath=false", *args],
        capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode != 0:
        sys.exit(f"git {' '.join(args)} failed:\n{result.stderr.strip()}")
    return result.stdout


def parse_git_log(repo):
    """Return raw commit dicts for TAG_FROM..HEAD_REF via one git log call."""
    fmt = "%x01%H%x02%P%x02%ad%x02%an%x02%s"
    out = _git(repo, "log", f"{TAG_FROM}..{HEAD_REF}",
               "--numstat", "--date=short", f"--pretty=format:{fmt}")
    commits = []
    for record in out.split("\x01"):
        if not record.strip():
            continue
        header, _, body = record.partition("\n")
        sha, parents, date, author, subject = header.split("\x02")
        files = []
        parse_error = False
        for line in body.splitlines():
            if not line.strip():
                continue
            try:
                ins_s, del_s, path = line.split("\t", 2)
                ins = 0 if ins_s == "-" else int(ins_s)
                dels = 0 if del_s == "-" else int(del_s)
                files.append((normalize_numstat_path(path), ins, dels))
            except ValueError:
                print(f"WARN: numstat parse error in {sha[:8]}: {line!r}",
                      file=sys.stderr)
                parse_error = True
        commits.append({
            "sha": sha, "parents": parents.split() if parents else [],
            "date": date, "author": author, "subject": subject,
            "files": files, "parse_error": parse_error,
        })
    # git log --numstat emits nothing for merge commits, which made every merge
    # look like "no changes". Use the combined diff (what the merge itself
    # introduced, e.g. conflict resolutions) for merges instead.
    for c in commits:
        if len(c["parents"]) >= 2 and not c["files"]:
            out = _git(repo, "show", "--numstat", "--format=", c["sha"])
            for line in out.splitlines():
                if not line.strip():
                    continue
                try:
                    ins_s, del_s, path = line.split("\t", 2)
                    ins = 0 if ins_s == "-" else int(ins_s)
                    dels = 0 if del_s == "-" else int(del_s)
                    c["files"].append((normalize_numstat_path(path), ins, dels))
                except ValueError:
                    print(f"WARN: merge numstat parse error in {c['sha'][:8]}: {line!r}",
                          file=sys.stderr)
                    c["parse_error"] = True
    return commits


def build_row(raw):
    files = raw["files"]
    if raw["parse_error"]:
        buckets = {b: False for b in BUCKET_NAMES}
        row_flags, risk, reason = {}, "UNKNOWN", "numstat parse error"
        buckets_loc, app_ui_xaml = {}, False
    else:
        buckets, buckets_loc, app_ui_xaml = compute_buckets(files)
        row_flags = compute_flags(files, buckets, len(raw["parents"]))
        risk, reason = compute_risk(buckets, row_flags, files)
    return {
        "sha": raw["sha"],
        "short_sha": raw["sha"][:8],
        "date": raw["date"],
        "author": raw["author"],
        "subject": raw["subject"],
        "is_merge": len(raw["parents"]) >= 2,
        "files_touched": len(files),
        "insertions": sum(f[1] for f in files),
        "deletions": sum(f[2] for f in files),
        "buckets": dict(buckets, app_ui_xaml=app_ui_xaml),
        "buckets_loc": buckets_loc,
        "flags": row_flags,
        "risk_level": risk,
        "risk_reason": reason,
        "verdict": None,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", help="repo root override")
    args = parser.parse_args()
    repo = args.repo or subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True,
        cwd=os.path.dirname(os.path.abspath(__file__))).stdout.strip()
    rows = [build_row(r) for r in parse_git_log(repo)]
    rows.sort(key=lambda r: r["sha"])
    rows.sort(key=lambda r: r["date"], reverse=True)
    summary = {"HIGH": 0, "MEDIUM": 0, "LOW": 0, "SKIP": 0}
    for r in rows:
        summary[r["risk_level"]] = summary.get(r["risk_level"], 0) + 1
    doc = {
        "tag_from": TAG_FROM,
        "tag_from_sha": _git(repo, "rev-parse", f"{TAG_FROM}^{{commit}}").strip(),
        "head_sha": _git(repo, "rev-parse", HEAD_REF).strip(),
        "generated_at": datetime.datetime.now().astimezone()
            .isoformat(timespec="seconds"),
        "commit_count": len(rows),
        "summary_by_risk": summary,
        "verdicts_complete": False,
        "commits": rows,
    }
    out_path = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                            "commits.json")
    with open(out_path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, indent=2, ensure_ascii=False)
        f.write("\n")
    print(f"Wrote {out_path}: {len(rows)} commits, risk {summary}")


if __name__ == "__main__":
    main()
