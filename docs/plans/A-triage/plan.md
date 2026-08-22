# Sub-project A.1 — Commit Triage Heuristics (implementation plan)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce `docs/plans/A-triage/commits.json` — a deterministic, risk-classified inventory of all 371 commits in `4.17.0.0..origin/master` — via a stdlib-only Python script.

**Architecture:** One script, `triage_commits.py`, split internally into pure classification functions (path → bucket, files → flags, flags → risk) and a thin git-plumbing layer (`git log --numstat --parents`). Pure functions are unit-tested with synthetic paths; the git layer is validated by invariant checks on the real repo output. A.2 (`dispatch_verdicts.py`) is **out of scope** — gated on sub-projects B + C being green.

**Tech Stack:** Python 3.10+ stdlib only (`subprocess`, `json`, `fnmatch`, `re`, `datetime`, `unittest`). Git CLI.

**Spec:** `docs/plans/A-triage/design.md` (§4 JSON schema, §5 bucket rules, §6 risk rubric, §7 heuristics, §11 failure modes).

## Global Constraints

- Triage produces **data only** — no product code is touched.
- Output file is `docs/plans/A-triage/commits.json`, committed to git as a record (design §13 default).
- Deterministic: two runs against the same `origin/master` produce byte-identical output except `generated_at`.
- Bucket evaluation is per-file, first-match-wins, in design §5 order. Risk rubric is first-match-wins in design §6 order.
- Historical paths (as of tag `4.17.0.0`) are what get classified — no remapping to the current folder layout (that is sub-project D's concern).

---

## File structure (what gets created)

```
docs/plans/A-triage/
├── design.md                 # exists (spec)
├── plan.md                   # this file
├── triage_commits.py         # A.1 implementation
├── test_triage_commits.py    # unittest suite for the pure functions
└── commits.json              # generated output (committed)
```

---

## Task 1: Pure classification functions + unit tests

**Files:**
- Create: `docs/plans/A-triage/triage_commits.py` (classification half)
- Test: `docs/plans/A-triage/test_triage_commits.py`

**Interfaces (produces, consumed by Task 2):**
- `classify_file(path: str) -> str` — returns one of the 10 bucket names.
- `normalize_numstat_path(path: str) -> str` — resolves git rename notation to the post-rename path.
- `compute_buckets(files: list[tuple[str, int, int]]) -> tuple[dict, dict, bool]` — returns `(buckets, buckets_loc, app_ui_xaml)`; input tuples are `(path, insertions, deletions)`.
- `compute_flags(files, buckets, parent_count: int) -> dict` — returns the six design-§7 flags.
- `compute_risk(buckets, flags, files) -> tuple[str, str]` — returns `(risk_level, risk_reason)`.

- [ ] **Step 1: Write the failing tests**

`docs/plans/A-triage/test_triage_commits.py`:

```python
"""Unit tests for the pure classification half of triage_commits.py."""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from triage_commits import (
    classify_file, normalize_numstat_path,
    compute_buckets, compute_flags, compute_risk,
)


class TestClassifyFile(unittest.TestCase):
    def test_engine_data(self):
        self.assertEqual(classify_file("x360ce.Engine/Data/UserSetting.cs"), "engine_data")
        self.assertEqual(classify_file("x360ce.Engine/x360ceModel.edmx"), "engine_data")
        self.assertEqual(classify_file("x360ce.Engine/IWebService.cs"), "engine_data")
        self.assertEqual(classify_file("x360ce.Data/Tables/dbo.Products.sql"), "engine_data")

    def test_web_services(self):
        self.assertEqual(classify_file("x360ce.Web/WebServices/x360ce.asmx.cs"), "web_services")
        self.assertEqual(classify_file("x360ce.Web/App_Code/Helper.cs"), "web_services")

    def test_engine_and_web_other(self):
        self.assertEqual(classify_file("x360ce.Engine/Engine.cs"), "engine")
        self.assertEqual(classify_file("x360ce.Web/Default.aspx.cs"), "web_other")

    def test_app_buckets(self):
        self.assertEqual(classify_file("x360ce.App.4/MainWindow.xaml.cs"), "app_v4")
        self.assertEqual(classify_file("x360ce.App/MainForm.cs"), "app_v3")
        self.assertEqual(classify_file("x360ce.App.Beta/Program.cs"), "app_v3")
        self.assertEqual(classify_file("x360ce.App.WPF/App.xaml"), "app_v3")

    def test_native(self):
        self.assertEqual(classify_file("x360ce/x360ce/Config.cpp"), "native")
        self.assertEqual(classify_file("MinHook/src/hook.c"), "native")
        self.assertEqual(classify_file("x360ce.RemoteController/main.cpp"), "native")
        self.assertEqual(classify_file("SomeDir/thing.vcxproj.filters"), "native")

    def test_build(self):
        self.assertEqual(classify_file("x360ce.sln"), "build")
        self.assertEqual(classify_file("Build_All.cmd"), "build")
        self.assertEqual(classify_file(".gitignore"), "build")

    def test_docs(self):
        self.assertEqual(classify_file("README.MD"), "docs")
        self.assertEqual(classify_file("Documents/Help.txt"), "docs")
        self.assertEqual(classify_file("banner.png"), "docs")

    def test_other_fallback(self):
        self.assertEqual(classify_file("SomeDir/data.bin"), "other")

    def test_first_match_wins(self):
        # csproj under x360ce.Engine is engine (rule 3) before build (rule 8)
        self.assertEqual(classify_file("x360ce.Engine/x360ce.Engine.csproj"), "engine")
        # ps1 under Documents is build (rule 8) before docs (rule 9)
        self.assertEqual(classify_file("Documents/Install.ps1"), "build")
        # nested image is NOT docs (docs images are root-level only) -> other
        self.assertEqual(classify_file("SomeDir/img.png"), "other")


class TestNormalizeNumstatPath(unittest.TestCase):
    def test_plain(self):
        self.assertEqual(normalize_numstat_path("a/b/c.cs"), "a/b/c.cs")

    def test_whole_rename(self):
        self.assertEqual(normalize_numstat_path("old.cs => new.cs"), "new.cs")

    def test_brace_rename(self):
        self.assertEqual(
            normalize_numstat_path("x360ce.App/{Forms => Controls}/Pad.cs"),
            "x360ce.App/Controls/Pad.cs")

    def test_brace_rename_empty_side(self):
        self.assertEqual(
            normalize_numstat_path("x360ce.App/{ => Sub}/Pad.cs"),
            "x360ce.App/Sub/Pad.cs")


class TestComputeBuckets(unittest.TestCase):
    def test_aggregation_and_loc(self):
        files = [("x360ce.App/MainForm.cs", 10, 2), ("README.MD", 3, 1)]
        buckets, loc, xaml = compute_buckets(files)
        self.assertTrue(buckets["app_v3"])
        self.assertTrue(buckets["docs"])
        self.assertFalse(buckets["engine"])
        self.assertEqual(loc["app_v3"], {"ins": 10, "del": 2})
        self.assertEqual(loc["docs"], {"ins": 3, "del": 1})
        self.assertFalse(xaml)

    def test_app_ui_xaml_overlay(self):
        files = [("x360ce.App.4/MainWindow.xaml", 5, 5)]
        _, _, xaml = compute_buckets(files)
        self.assertTrue(xaml)

    def test_xaml_outside_app_not_overlay(self):
        files = [("x360ce.Engine/Themes/Generic.xaml", 5, 5)]
        _, _, xaml = compute_buckets(files)
        self.assertFalse(xaml)


class TestComputeFlags(unittest.TestCase):
    def _flags(self, paths, parents=1):
        files = [(p, 1, 1) for p in paths]
        buckets, _, _ = compute_buckets(files)
        return compute_flags(files, buckets, parents)

    def test_version_bump(self):
        f = self._flags(["x360ce.App/Properties/AssemblyInfo.cs", "Version.cs"])
        self.assertTrue(f["is_version_bump"])
        f = self._flags(["x360ce.App/Properties/AssemblyInfo.cs", "x360ce.App/MainForm.cs"])
        self.assertFalse(f["is_version_bump"])

    def test_docs_only(self):
        self.assertTrue(self._flags(["README.MD", "Documents/a.txt"])["is_docs_only"])
        self.assertFalse(self._flags(["README.MD", "x360ce.sln"])["is_docs_only"])

    def test_merge_no_changes(self):
        f = compute_flags([], compute_buckets([])[0], 2)
        self.assertTrue(f["is_merge_no_changes"])
        f = compute_flags([], compute_buckets([])[0], 1)
        self.assertFalse(f["is_merge_no_changes"])

    def test_touches_data_model(self):
        self.assertTrue(self._flags(["x360ce.Engine/Data/UserSetting.cs"])["touches_data_model"])
        self.assertTrue(self._flags(["SomeDir/Model.edmx.diagram"])["touches_data_model"])
        self.assertFalse(self._flags(["x360ce.App/MainForm.cs"])["touches_data_model"])

    def test_touches_settings(self):
        for p in ["x360ce.App/SettingsManager.cs", "x360ce.Engine/JocysCom/Options.cs",
                  "x360ce.Engine/Data/PadSetting.cs", "x360ce.App/UserGameControl.cs",
                  "x360ce.App/PresetForm.cs"]:
            self.assertTrue(self._flags([p])["touches_settings"], p)
        self.assertFalse(self._flags(["x360ce.App/MainForm.cs"])["touches_settings"])

    def test_touches_webservice_api(self):
        self.assertTrue(self._flags(["x360ce.Web/WebServices/x360ce.asmx.cs"])["touches_webservice_api"])
        self.assertTrue(self._flags(["x360ce.Engine/IWebService.cs"])["touches_webservice_api"])
        self.assertFalse(self._flags(["x360ce.Web/Default.aspx"])["touches_webservice_api"])


class TestComputeRisk(unittest.TestCase):
    def _risk(self, paths, parents=1):
        files = [(p, 1, 1) for p in paths]
        buckets, _, _ = compute_buckets(files)
        flags = compute_flags(files, buckets, parents)
        return compute_risk(buckets, flags, files)

    def test_skip_wins_over_high(self):
        # docs-only always SKIPs even though nothing HIGH is present
        level, _ = self._risk(["README.MD"])
        self.assertEqual(level, "SKIP")

    def test_high_engine_data(self):
        level, reason = self._risk(["x360ce.Engine/Data/UserSetting.cs"])
        self.assertEqual(level, "HIGH")
        self.assertIn("engine_data", reason)

    def test_high_by_filename_in_low_bucket(self):
        # *Setting*.cs inside app bucket still HIGH per design §6 row 2
        level, _ = self._risk(["x360ce.App/SettingsDatabaseForm.cs"])
        self.assertEqual(level, "HIGH")

    def test_medium(self):
        level, _ = self._risk(["x360ce.Engine/Common.cs"])
        self.assertEqual(level, "MEDIUM")

    def test_low(self):
        level, _ = self._risk(["x360ce.App/MainForm.cs", "x360ce.sln"])
        self.assertEqual(level, "LOW")

    def test_skip_merge_no_changes(self):
        level, _ = self._risk([], parents=2)
        self.assertEqual(level, "SKIP")


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run tests to verify they fail**

Run (from repo root):
```powershell
python docs/plans/A-triage/test_triage_commits.py
```
Expected: `ModuleNotFoundError: No module named 'triage_commits'` (or ImportError on names).

- [ ] **Step 3: Write the classification half of `triage_commits.py`**

```python
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run:
```powershell
python docs/plans/A-triage/test_triage_commits.py
```
Expected: `OK` with all tests passing.

- [ ] **Step 5: Commit** *(user performs or approves — per project rule, no unrequested commits)*

```bash
git add docs/plans/A-triage/triage_commits.py docs/plans/A-triage/test_triage_commits.py docs/plans/A-triage/plan.md
git commit -m "Add A.1 triage classification functions with tests."
```

---

## Task 2: Git plumbing, JSON emission, real run

**Files:**
- Modify: `docs/plans/A-triage/triage_commits.py` (append the git half + `main`)
- Create (generated): `docs/plans/A-triage/commits.json`

**Interfaces:**
- Consumes: all Task 1 functions.
- Produces: `commits.json` matching design §3/§4 exactly; `python triage_commits.py [--repo <path>]` CLI.

- [ ] **Step 1: Append the git-plumbing half to `triage_commits.py`**

```python
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
```

- [ ] **Step 2: Re-run unit tests (regression)**

Run:
```powershell
python docs/plans/A-triage/test_triage_commits.py
```
Expected: `OK`.

- [ ] **Step 3: Run the triage against the real repo**

Run (from x360ce repo root):
```powershell
python docs/plans/A-triage/triage_commits.py
```
Expected: `Wrote ... 371 commits, risk {...}` on stdout; no WARN lines (or investigate each).

- [ ] **Step 4: Validate invariants on the output**

Run:
```powershell
python - <<'PY'
import json
d = json.load(open("docs/plans/A-triage/commits.json", encoding="utf-8"))
assert d["commit_count"] == 371 == len(d["commits"])
assert sum(d["summary_by_risk"].values()) == 371
assert all(c["risk_level"] in ("HIGH", "MEDIUM", "LOW", "SKIP", "UNKNOWN")
           for c in d["commits"])
assert all(c["verdict"] is None for c in d["commits"])
dates = [c["date"] for c in d["commits"]]
assert dates == sorted(dates, reverse=True)
print("invariants OK")
PY
```
Expected: `invariants OK`. (On Windows PowerShell, save the block as a temp `.py` and run it instead of the heredoc.)

- [ ] **Step 5: Determinism check**

Run the script twice; diff the two outputs ignoring `generated_at`. Expected: identical.

- [ ] **Step 6: Commit** *(user performs or approves)*

```bash
git add docs/plans/A-triage/triage_commits.py docs/plans/A-triage/commits.json
git commit -m "Add A.1 triage git plumbing and generated commits.json."
```

---

## Out of scope (unchanged from design)

- A.2 sub-agent verdicts (`dispatch_verdicts.py`) — gated on B + C green.
- Path remapping to current layout — sub-project D.
- Any cherry-picking or code changes.
