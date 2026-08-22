# Plans — Reapply Bug Fixes from 4.17.0.0..master

**Context:** This branch (`revert-to-4.17.0.0-reapply-bugfixes`) was reverted to the very old `4.17.0.0` tag (Nov 2020) because the intervening UI/WPF rewrite (~371 commits) left the application in an unworkable state. The goal of this work is to cherry-pick **bug fixes only** from those 371 commits while preserving full backwards compatibility:

- **Settings file format** — millions of users have existing `Settings.xml` / `Settings.xml.gz` files; they must continue to load.
- **WebService API** — the SOAP/ASMX endpoints in `Web/WebServices/x360ce.asmx[.v3/.v4].cs` are consumed by deployed clients; the wire contract is frozen.
- **Data model** — `Data/x360ce.Data.sqlproj` is the canonical schema source of truth; Engine EDMX entities (`Engine/Data/x360ceModel.edmx`) shape is frozen.

Any commit that changes any of the above is excluded. UI changes are tolerated only if isolated and clearly bug-fix in intent.

## Decomposition (A → E → B → C → D)

The work is decomposed into five sub-projects, each with its own design doc and implementation plan:

| # | Sub-project | Purpose | Where code lives | Design |
|---|---|---|---|---|
| **A** | Commit Triage | Enumerate the 371 commits, auto-classify by path-bucket + risk, then (gated on C) sub-agent verdict per commit | x360ce | [A-triage/design.md](A-triage/design.md) |
| **E** | DataPorter Enhancement | Add DAT (BCP native binary) and JSON data formats to DataPorter; upgrade per-table schema scripting to include indexes/FKs/triggers/constraints. Makes DataPorter a complete per-table backup tool, retiring the legacy `Data/Change Scripts/Backup/` | **`D:\Projects\Jocys.com\Sql\DataPorter\`** (separate repo) | [E-dataporter/design.md](E-dataporter/design.md) |
| **B** | Test DB Scaffolding | PowerShell scripts that build/deploy/refresh/drop `x360ce_Tests` on local SQL Server Developer Edition. Schema via SqlPackage from sqlproj. After E lands: uses DataPorter for synthetic seed AND for x360ce_Tests backup/restore. Hard guardrail allow-list prevents writing to live `x360ce` | x360ce | [B-db/design.md](B-db/design.md) |
| **C** | Test Harness | Engine.Tests + Web.Tests + App.v4.Tests + App.v3.Tests (qa-tester framework). Baselines current behaviour AND gates cherry-picks. Web.Tests depends on B; the others don't | x360ce | [C-tests/design.md](C-tests/design.md) |
| **D** | First 10-fix PR | Read each candidate commit individually, mentally translate via small rename map to current paths, propose change to user, apply manually with test validation. **No cherry-pick automation, no path-rewrite scripts.** Trust earned per-commit | x360ce | [D-cherry-pick/design.md](D-cherry-pick/design.md) (stub — postponed until B + C are complete) |

## Ordering principle

- **A.1** (heuristic triage): non-destructive, can be done any time.
- **E** (DataPorter enhancement): in a separate repo. Can be implemented in parallel with B-current. B can ship using DataPorter-current (CSV-only) and be revised after E lands; or B can wait for E.
- **B**: prerequisite for any DB-touching test. After E: B uses DataPorter for seed AND backup/restore.
- **C-M1** (Engine.Tests): non-destructive AND teaches us the code. No DB dependency — can run in parallel with B / E.
- **C-M2** (Web.Tests): gated on B being complete (needs `x360ce_Tests` to exist).
- **C-M3 / C-M4** (App tests): no DB dependency, can run after B + C-M1.
- **A.2** (sub-agent verdicts on all 371 commits): gated on C being green.
- **D** (cherry-pick PR): gated on C being green for the relevant surface:
  - D may pick Engine-only fixes after **C-M1**.
  - D may pick Web-touching fixes after **C-M2** (and therefore B).
  - D may pick App-touching fixes after **C-M3** / **C-M4** as appropriate.

## Status

- [x] A design — drafted, user-reviewed
- [x] B design — drafted, awaiting review (revisions pending after E lands)
- [x] C design — drafted, awaiting review
- [x] D design — stub (postponed)
- [x] E design — drafted, awaiting review
- [x] B plan — written (interim version; revise after E)
- [ ] E plan — writing-plans next
- [ ] B revision — revise after E lands (DataPorter-based seed; delete `Data/Change Scripts/Backup/`)
- [ ] C plan — writing-plans
- [x] A.1 plan — written ([A-triage/plan.md](A-triage/plan.md))
- [x] A.1 implemented — `triage_commits.py` + unit tests green; `commits.json` generated (371 commits: 111 HIGH / 107 MEDIUM / 137 LOW / 16 SKIP)
- [ ] A.2 review — all 371 rows DECIDED at triage level via [A-triage/REVIEW.md](A-triage/REVIEW.md), but NOT yet fully verified: a per-hunk audit (audit_hunks.py) found 104 Skip-decided commits with 1107 hunks that would apply to surviving logic files and still require reading (queue: audit-applicable-hunks.txt). The moved-file audit (same-name relocations) and the RENAME-LINEAGE audit (per-commit git rename detection chained into per-file lineages, lineage-aliases.json: 15 files gained 17 renamed descendant paths such as PadControl.cs->PadUserControl.cs, DebugForm.cs->DebugWindow.xaml.cs) are both complete across all 371 commits: 6 commits / 22 hunks + 71 commits / 67 lineage hunks, ALL read - zero missed fixes; two WPF-era disposal hygiene near-misses noted (3dc942eb MacrosControl unsubscribe, 87108b77 PadControlImager null-outs) with no defect in the restored tree where those objects are app-lifetime. (371/371 triaged; decisions come only from actual diffs, never subjects — three merge commits were mis-SKIPped by the numstat heuristic and are now reclassified). Second fix batch applied 2026-08-22: test-device POV crash (84550a9b), GetVendors entity query (bbf81063), secure sign-up passwords (2ef1fb5b, net462-adapted) - version 4.17.25.0. Third pass finished the table: one further fix found (ce39f274 Downloader retry-timer, applied locally, awaiting user approval); upstream 9116247f is noted as having REVERTED the PR #1341 password fix (net462 build break), so this branch is ahead of master there. 14 fixes total at that point. FOURTH PASS (2026-08-22) closed the verification debt: the whole 1107-hunk applicable-hunk queue (audit-applicable-hunks.txt, 104 commits) was read end to end, and every rewrite-era commit with "fix" in its subject was re-read for the BUG INTENT and checked against the restored implementation (a scan showed only 9 of them touch framework-independent code at all - the rest are WPF bindings, converters, styles and load/unload plumbing, or defects the rewrite introduced itself). That produced 7 more fixes (batch at 4.17.33.0): a94d4b28 Microsoft XInput lookup (only xinput1_3.dll was searched, absent on clean Windows 8+, and FileVersionInfo then threw), 1497b94a intent ported to the WinForms IssuesUserControl (worker thread updated grid and status controls), 28e95885 missing-Logs-folder and double-ZipStorer.Close crashes, 40d137ab SocketServer keep-alive timer restart and integer-division aspect ratio, 563d26de [field: NonSerialized] on PropertyChanged - plus the already-pending ce39f274 Downloader timer. Deliberate non-ports recorded: 085f2ed2 CollectionsHelper.Synchronize rewrite (no demonstrable defect in the old algorithm), 3988aa20 DevicesNeedUpdating on InputLost/Unplugged (our tree refreshes on WM_DEVICECHANGE; stale-state path not demonstrable), f5204f2f Interlocked.Increment (single writer thread), null-Assembly.GetEntryAssembly guards (never null in either exe). Apply-candidates are now exhausted at the hunk level - future work is the Postponed set (81ee19e2+11c47449 crisp-DPI relayout, c407e6b8, 08087a38 which needs the rewrite-era PadSetting.Load)
- [x] First fixes APPLIED on branch (2026-08-22, per user direction, ahead of the C gate): 5 ported commits (1088022a, 2c6a14fe, 545fed1d, d4a3c79a, f295e72e-Step5-hunk) + branch-only fixes: HID Guardian uninstall-only; VC++ 2015-2022 runtime detection; **DPI-unaware locked via `app.manifest` in App.v4 AND App.v3** (asInvoker + `dpiAware=false`, wired via `<ApplicationManifest>`) so Windows scales the whole UI at high DPI, layout keeps its designed proportions, and the hosted WPF content can no longer flip the process DPI-aware mid-run (which caused a tiny unscaled UI and an intermittent empty title); AutoScale Font declarations added to all App.v4 WinForms user controls + MainForm (inert at locked 96 DPI; correct for the VS designer and any future crisp-DPI work). App.v4 verified by screenshots at 150% (window 1230x1170 physical, all labels/buttons/gaps correct, Logitech F710 detected and mapped); App.v3 builds via `msbuild x360ce.slnx -p:Platform=APP_x64_v3` (or per-csproj with `-p:SolutionDir=` supplied) and works end-to-end: the empty Controller tab body and the failing "Create" fix in the DLL-architecture warning shared one root cause — `EngineHelper.GetResourcePath` matched `.x64.` folder-style resource names while the embedded DLLs are named `xinput_x64.dll` underscore-style, so the arch-specific resource was never selected and Create always re-extracted the generic 32-bit DLL; additionally `DllFileIssue` compared the assembly architecture (MSIL for Any CPU builds) against the native DLL's, which can never match. Fixed both (GetResourcePath now resolves underscore-style names — also fixes `dinput.dll` siblings; DllFileIssue normalizes MSIL to the real process architecture). Verified end-to-end: Create now writes an x64 `xinput1_3.dll`, the app loads it (status bar `xinput1_3.dll 3.4.1.1357`), the New Device wizard for the Logitech F710 (Cordless RumblePad 2) completes with internet settings, and the mapping saves to `x360ce.ini` (`PAD1=IG_...`). The 347427e9 conditional-DPI port was tested and reverted (WPF-era design, wrong for the WinForms line); crisp-DPI via 81ee19e2 + 11c47449 relayouts stays queued
- [ ] D plan — written after C is green

## Key constraints baked into all sub-projects

- **`Data/x360ce.Data.sqlproj` is the canonical schema SSOT.** Any drift discovered against live is a sqlproj defect to fix; do not work around in test scripts. DataPorter does *not* compete with this — DataPorter handles per-table backup/restore + seed; sqlproj handles forward schema deploy.
- **Live `x360ce` is never written to.** All B scripts use an allow-list guardrail (`^x360ce_Tests(_\w+)?$`). The C harness re-verifies at `[AssemblyInitialize]`.
- **No real captured data in git.** Fixtures are synthetic; user-supplied real fixtures (if any) go into `Engine.Tests/Fixtures/Real/` which is initially empty.
- **Settings XML wire format is frozen.** `[XmlType("Setting")]` on `UserSetting` is a hard contract for v3.x clients still in the wild.
- **net462 reality.** EventPipe / `Microsoft.Diagnostics.NETCore.Client` don't work on .NET Framework. Use ETW (`wpr.exe`) + BenchmarkDotNet for perf instead. Microsoft.Testing.Platform is not available — VSTest only.
- **One tool per concern.** SqlPackage = schema deploy (DACPAC). DataPorter = per-table backup/restore + seed (CSV/DAT/JSON after E). No overlap. `BackupAndRestoreData.ps1` is retired by E.
