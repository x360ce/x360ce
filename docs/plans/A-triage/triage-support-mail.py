#!/usr/bin/env python3
"""Classify a batch of support crash reports against the known-issue catalog.

    python triage-support-mail.py <batch.json> [--catalog support-issues.json]

Batch files come from the n8n workflow "Support - Ingest Mail" and hold user mail,
so keep them in the git-ignored .tmp/ folder - never in the repository.

Each report is sorted into one of four buckets:

  dismiss   matches a fixed issue, reported by a version older than the fix.
            Expected straggler while the new version spreads. No action.
  REGRESSED matches a fixed issue but reported by a version at or above the fix.
            The fix did not work. This is the bucket to read first.
  known     matches an open or needs-check issue. Adds to its report count.
  NEW       matches nothing in the catalog. Needs a human decision.
"""
import json, re, io, sys, collections

def field(body, label):
    m = re.search(r"^\s*" + re.escape(label) + r":\s*\n\s*(.+?)\s*$", body, re.M)
    return m.group(1).strip() if m else ""

def exception_type(body):
    m = re.search(r"\b(System\.[A-Za-z.]*Exception|SharpDX\.[A-Za-z.]*Exception|"
                  r"Nefarius\.[A-Za-z.]*Exception|[A-Za-z0-9_.]+Exception)\b", body)
    return m.group(1) if m else ""

def app_frame(body):
    for line in body.splitlines():
        s = line.strip()
        if s.startswith("at ") and "x360ce" in s:
            sig = re.sub(r"\s+", " ", s[3:])
            return re.sub(r"\(.*", "", sig).strip().replace(" .", ".")
    return ""

def version_tuple(text):
    m = re.search(r"(\d+)\.(\d+)\.(\d+)\.(\d+)", text or "")
    return tuple(int(x) for x in m.groups()) if m else None

def main():
    batch_path = sys.argv[1] if len(sys.argv) > 1 else sys.exit(__doc__)
    cat_path = "support-issues.json"
    if "--catalog" in sys.argv:
        cat_path = sys.argv[sys.argv.index("--catalog") + 1]

    catalog = json.load(io.open(cat_path, encoding="utf-8"))
    by_sig = {(i["match"]["exception"], i["match"]["frame"]): i for i in catalog["issues"]}
    items = json.load(io.open(batch_path, encoding="utf-8"))

    buckets = collections.defaultdict(list)
    new_sigs = collections.Counter()

    for it in items:
        b = it.get("BodyText") or ""
        sig = (exception_type(b), app_frame(b))
        ver = version_tuple(field(b, "Name"))
        issue = by_sig.get(sig)
        if issue is None:
            buckets["NEW"].append((it.get("Uid"), sig, ver))
            new_sigs[sig] += 1
            continue
        if issue.get("status") == "fixed":
            fixed = version_tuple(issue.get("fixedInVersion") or "")
            if ver and fixed and ver >= fixed:
                buckets["REGRESSED"].append((it.get("Uid"), issue["id"], ver))
            else:
                buckets["dismiss"].append((it.get("Uid"), issue["id"], ver))
        else:
            buckets["known"].append((it.get("Uid"), issue["id"], ver))

    total = len(items)
    print(f"batch: {batch_path}  reports: {total}\n")
    for name in ("REGRESSED", "NEW", "known", "dismiss"):
        rows = buckets[name]
        print(f"{name:10} {len(rows):4}  ({len(rows) * 100 // max(total,1)}%)")

    if buckets["REGRESSED"]:
        print("\nREGRESSED - a shipped fix did not hold:")
        for uid, iid, ver in buckets["REGRESSED"][:20]:
            print(f"  uid={uid} {iid} version={'.'.join(map(str,ver)) if ver else '?'}")

    if new_sigs:
        print("\nNEW signatures, most frequent first:")
        for (exc, frame), n in new_sigs.most_common(15):
            print(f"  [{n:3}] {exc or '(none)'}\n        {frame or '(no app frame)'}")

    counts = collections.Counter(iid for _, iid, _ in buckets["known"])
    if counts:
        print("\nKnown open issues in this batch:")
        for iid, n in counts.most_common():
            issue = next(i for i in catalog["issues"] if i["id"] == iid)
            print(f"  [{n:3}] {iid} {issue['status']:12} {issue['title']}")

if __name__ == "__main__":
    main()
