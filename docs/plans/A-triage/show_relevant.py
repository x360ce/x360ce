#!/usr/bin/env python3
"""Print a commit's diff condensed to hunks touching files that exist in the restored tree.

Usage: python show_relevant.py <short_sha> [...]  (reads diffs/<short_sha>.txt)
For each existing (rename-mapped) file, prints its diff up to a cap; nonexistent
files are listed by name only. Keeps review reading focused on portable content.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
CAP = 120  # max printed lines per existing file


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


def show(sha):
    path = os.path.join(HERE, "diffs", sha + ".txt")
    lines = open(path, encoding="utf-8", errors="replace").read().splitlines()
    print("=" * 20, sha)
    print("\n".join(lines[:3]))
    cur_exists = False
    printed = 0
    skipped = []
    for line in lines:
        if line.startswith("diff --git "):
            hist = line.split(" b/")[-1]
            mapped = map_path(hist)
            cur_exists = bool(mapped) and os.path.exists(os.path.join(ROOT, mapped))
            printed = 0
            if cur_exists:
                print(f"--- EXISTS: {hist} -> {mapped}")
            else:
                skipped.append(hist)
        elif cur_exists:
            if printed < CAP:
                print(line)
            elif printed == CAP:
                print(f"    ... (capped at {CAP} lines)")
            printed += 1
    if skipped:
        print(f"[nonexistent files omitted: {len(skipped)}] " + "; ".join(skipped[:8]))


if __name__ == "__main__":
    for sha in sys.argv[1:]:
        show(sha)
