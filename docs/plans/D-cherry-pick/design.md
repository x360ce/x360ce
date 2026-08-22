# Sub-project D — Cherry-pick PR (design, stub)

**Date:** 2026-05-16
**Status:** **POSTPONED** — design will be fleshed out after sub-projects B and C are complete. This file exists as a placeholder so the A→B→C→D structure is consistent.
**Parent:** `docs/plans/README.md`
**Depends on:** B (test DB), C (test harness — at least C-M1 green).

## Why this is a stub

Per user direction 2026-05-16:
> "Skip this part. Start analysing first commit and report me if it contains bugfix worth applying it to current code. I need to slowly trust you until I allow you commits to current branch. Also I think I told you that analysing commits is the last part, creating x360ce_Tests database and creating Tests projects is the task which must be done first."

So:
- **D is the LAST sub-project, not the first.**
- **D does not need a cherry-pick automation script or a path-rewrite tool.** Earlier brainstorming proposed both; the user rejected both.

## Reframed approach (to expand later)

When B and C are done and the user gives the go:

1. **Pick one commit at a time.** Start with the oldest non-SKIP/non-HIGH commit from `docs/plans/A-triage/commits.json` (sorted oldest first by author date).
2. **Read its diff.** Apply the historical→current rename map mentally (small enough to fit in head):

   | Historical (at 4.17.0.0) | Current |
   | --- | --- |
   | `x360ce.Engine/` | `Engine/` |
   | `x360ce.App/` | `App.v3/` (3.x-line WinForms app) |
   | `x360ce.App.Beta/` | `App.v4/` (4.x app — verified via `git log --follow` on `App.v4/Global.cs`) |
   | `x360ce.Web/` | `Web/` |
   | `x360ce.Data/` | `Data/` |
   | `x360ce/` | `Native/x360ce/` (plus `Native/InputHook/`, `Native/dinput8/`, ...) |
   | `MinHook/` | `MinHook/` |
   | `x360ce.App.WPF/`, `Mobile/`, `x360ce.RemoteController/` | deleted on this branch (`6b05fb2f` Cleanup solution) — commits touching only these are Skip |

3. **Report to the user:** "Commit `<sha>` ('<subject>'). Verdict: this fixes <X>. Worth applying? If yes, here's the translation to current code paths: <diff>."
4. **User approves or rejects.** Approval is per-commit; no batched permission.
5. **If approved, apply manually** — edit the current code to match the translated diff. Run relevant tests from sub-project C. If green, stage. If broken, undo and report.
6. **Repeat until 10 approved fixes accumulate**, then open a single PR with 10 individual commits (each with a `(cherry picked from commit <sha>)` trailer for traceability).

## Trust model

The user is explicit: trust is earned per-commit. The first fix proves I can:
- Read an old diff correctly.
- Identify whether it's a bugfix (not a feature / refactor / cosmetic change).
- Translate it to current code without breaking anything.
- Validate it via the test harness from sub-project C.

Only after several successes does the user authorise actual commits to the working branch. Until then, every proposed change is a diff in the chat for review.

## Out of scope for D
- Bulk cherry-pick scripts.
- Automated path rewriting.
- LLM verdict generation (that's sub-project A.2 — runs first, feeds D's input).
- Any change to live `x360ce` database or to settings/data-model contracts (the global constraints in `docs/plans/README.md` apply).

## Open until B + C are done
- Exact filter expression on `commits.json` (likely: `is_bugfix=true ∧ breaks_*=false ∧ confidence∈{med,high}`, oldest-first).
- PR-size threshold (10 fixes is the target but could be 5 if early picks reveal larger churn).
- Test-run scoping (per-fix: only the test project covering the touched code; final: full suite).

## Next step

Do nothing in D until B + C are complete. Then the user signals to begin; the design above is updated with concrete steps and the writing-plans pass produces `plan.md`.
