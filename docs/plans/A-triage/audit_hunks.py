#!/usr/bin/env python3
"""Verification audits over the exported diffs (run export_diffs.py first).

Mode "applicable" (default): for every commit, extract each hunk touching a
LOGIC file that exists in the restored tree (after rename mapping) and whose
pre-image (context + removed lines, whitespace-normalized) matches the file's
current content — i.e. the change could actually land here. Writes
audit-applicable-hunks.txt with the full hunks, flagging hunks whose changed
lines are only using/namespace/comment/brace churn as noise.

Mode "moved": catches fixes made after upstream moved a file. For every touched
path that does NOT exist in the restored tree, finds surviving files with the
same basename and tests each hunk's pre-image against them. Writes
audit-moved-files.txt.

Both outputs are regenerable reading queues, not decisions: every listed hunk
must be read; a decision changes only after reading.
"""
import json
import os
import re
import sys
from collections import defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
NOISE = re.compile(r'^\s*(using\s+[\w.=\s]+;|namespace\s|#region|#endregion|//|///|\{|\}|\s*)$')


def map_path(p):
    rules = [("x360ce.App.Beta/", "App.v4/"), ("x360ce.App.4/", "App.v4/"),
             ("x360ce.App.WPF/", None), ("Mobile/", None), ("x360ce.RemoteController/", None),
             ("x360ce.App/", "App.v3/"), ("x360ce.Engine/", "Engine/"),
             ("x360ce.Web/", "Web/"), ("x360ce.Data/", "Data/"),
             ("x360ce/", "Native/x360ce/"), ("MinHook/", "MinHook/")]
    for old, new in rules:
        if p.startswith(old):
            return None if new is None else new + p[len(old):]
    return p


def is_logic_name(name):
    ln = name.lower()
    if not ln.endswith((".cs", ".cpp", ".h", ".sql", ".ps1")):
        return False
    return not (ln.endswith(".designer.cs") or ln.endswith(".g.cs"))


def norm(text):
    return "\n".join(l.strip() for l in text.splitlines() if l.strip())


def iter_hunks(sha):
    """Yield (historical_path, hunk_lines) for every hunk in the commit's diff."""
    lines = open(os.path.join(HERE, "diffs", sha + ".txt"),
                 encoding="utf-8", errors="replace").read().splitlines()
    hist = None
    hunk = []
    for line in lines:
        if line.startswith("diff --git "):
            if hist and hunk:
                yield hist, hunk
            hunk = []
            hist = line.split(" b/")[-1].strip()
        elif line.startswith("@@"):
            if hist and hunk:
                yield hist, hunk
            hunk = []
        elif hist is not None and line[:1] in (" ", "+", "-"):
            hunk.append(line)
    if hist and hunk:
        yield hist, hunk


def hunk_parts(hunk):
    pre = norm("\n".join(l[1:] for l in hunk if l.startswith((" ", "-"))))
    changed = [l for l in hunk if l.startswith(("+", "-")) and l[1:].strip()]
    substance = [l for l in changed if not NOISE.match(l[1:])]
    return pre, changed, substance


def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "applicable"
    commits = json.load(open(os.path.join(HERE, "commits.json"), encoding="utf-8"))["commits"]
    cache = {}

    def content(path):
        if path not in cache:
            try:
                cache[path] = norm(open(os.path.join(ROOT, path), "rb").read()
                                   .decode("utf-8", errors="replace"))
            except OSError:
                cache[path] = ""
        return cache[path]

    if mode == "moved":
        index = defaultdict(list)
        for root, dirs, files in os.walk(ROOT):
            if any(seg in root for seg in (".git", os.sep + "bin", os.sep + "obj", "diffs", "packages")):
                continue
            for f in files:
                if is_logic_name(f):
                    rel = os.path.relpath(os.path.join(root, f), ROOT).replace("\\", "/")
                    index[f.lower()].append(rel)
        out_path = os.path.join(HERE, "audit-moved-files.txt")
        total = 0
        with open(out_path, "w", encoding="utf-8", newline="\n") as out:
            out.write(__doc__ + "\n")
            for c in commits:
                hits = []
                for hist, hunk in iter_hunks(c["sha"][:8]):
                    m = map_path(hist)
                    base = os.path.basename(hist).lower()
                    if (m is None or not os.path.exists(os.path.join(ROOT, m))) and is_logic_name(base):
                        pre, changed, _ = hunk_parts(hunk)
                        if not (pre and changed):
                            continue
                        for cand in index.get(base, []):
                            if pre in content(cand):
                                hits.append((hist, cand, hunk))
                                break
                if hits:
                    total += len(hits)
                    out.write(f"######## {c['short_sha']} {c['date']} \"{c['subject'][:70]}\" moved-file hunks={len(hits)}\n")
                    for hist, cand, h in hits:
                        out.write(f"---- historical: {hist}\n---- matches our: {cand}\n")
                        out.write("\n".join(h) + "\n")
                    out.write("\n")
        print(f"moved-file applicable hunks: {total} -> {out_path}")
        return

    out_path = os.path.join(HERE, "audit-applicable-hunks.txt")
    total = 0
    with open(out_path, "w", encoding="utf-8", newline="\n") as out:
        out.write(__doc__ + "\n")
        for c in commits:
            subs = []
            noise = 0
            for hist, hunk in iter_hunks(c["sha"][:8]):
                m = map_path(hist)
                if not m or not os.path.exists(os.path.join(ROOT, m)) or not is_logic_name(os.path.basename(m)):
                    continue
                pre, changed, substance = hunk_parts(hunk)
                if not (pre and changed) or pre not in content(m):
                    continue
                if substance:
                    subs.append((m, hunk))
                else:
                    noise += 1
            if subs:
                total += len(subs)
                out.write(f"######## {c['short_sha']} {c['date']} \"{c['subject'][:70]}\" substance-hunks={len(subs)} (noise-only skipped: {noise})\n")
                for m, h in subs:
                    out.write(f"---- {m}\n")
                    out.write("\n".join(h) + "\n")
                out.write("\n")
    print(f"substance hunks: {total} -> {out_path}")


if __name__ == "__main__":
    main()
