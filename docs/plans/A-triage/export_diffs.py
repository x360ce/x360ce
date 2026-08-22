#!/usr/bin/env python3
"""Export every commit diff from 4.17.0.0..origin/master into diffs/<short_sha>.txt.

Gives the review process a fast, locally searchable store of all 371 diffs:
    rg -l "SetProcessDPIAware" docs/plans/A-triage/diffs/
    rg -i "collection was modified" docs/plans/A-triage/diffs/
The folder is git-ignored (regenerable). Re-run any time; existing files are
overwritten. Stdlib only.
"""
import json
import os
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "diffs")


def main():
    doc = json.load(open(os.path.join(HERE, "commits.json"), encoding="utf-8"))
    os.makedirs(OUT, exist_ok=True)
    repo = subprocess.run(["git", "rev-parse", "--show-toplevel"],
                          capture_output=True, text=True, cwd=HERE).stdout.strip()
    for c in doc["commits"]:
        show = subprocess.run(
            ["git", "-C", repo, "-c", "core.quotePath=false", "show",
             "--format=commit %H%nDate: %ad%nSubject: %s%n", "--date=short", c["sha"]],
            capture_output=True, text=True, encoding="utf-8", errors="replace")
        with open(os.path.join(OUT, c["short_sha"] + ".txt"), "w",
                  encoding="utf-8", newline="\n") as f:
            f.write(show.stdout)
    print(f"Exported {doc['commit_count']} diffs to {OUT}")


if __name__ == "__main__":
    main()
