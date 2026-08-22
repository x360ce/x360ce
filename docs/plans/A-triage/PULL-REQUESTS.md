# Open Pull Request Review

Review of all 10 open pull requests on <https://github.com/x360ce/x360ce/pulls>,
judged for the `revert-to-4.17.0.0-reapply-bugfixes` branch.

Same rules as the commit review (`REVIEW.md`): the decision comes from the
actual diff, never the title, and a change is only worth taking if the defect it
fixes exists in **this** tree. Nine of the ten PRs target the WPF rewrite
(`x360ce.App.Beta` as it exists on master), so most of the value is in the
*intent*, not the patch.

## Summary

| PR | Author | Title | What we borrow | Apps |
|---|---|---|---|---|
| [863](https://github.com/x360ce/x360ce/pull/863) | myrho72 | Tencent Gaming Buddy | Nothing — rejected on safety grounds | — |
| [959](https://github.com/x360ce/x360ce/pull/959) | Itaxh1 | V3.x (NEED FOR SPEED PAYBACK) | Nothing (dead CodePlex link is incidental) | — |
| [1473](https://github.com/x360ce/x360ce/pull/1473) | OmarIskandarani-KSO | Guide button / HidHide | Guide button reaches the virtual controller | v4 |
| [1527](https://github.com/x360ce/x360ce/pull/1527) | MoriartyMe | Update app.config | Nothing | — |
| [1539](https://github.com/x360ce/x360ce/pull/1539) | ThreeDeeJay | Build via GitHub Actions | Nothing (see security notes) | — |
| [1576](https://github.com/x360ce/x360ce/pull/1576) | Glitchtest51 | Update C++ Issue Helper | VC++ runtime detected by registry version | v4 |
| [1596](https://github.com/x360ce/x360ce/pull/1596) | mNandhu | Cross-thread crash in GetActiveControl | Its analysis exposed an infinite loop in our copy | v4 |
| [1604](https://github.com/x360ce/x360ce/pull/1604) | ykondury | Windows 11 controller reliability | 7 defects: ViGEm churn, worker thread, slot handling | v4 |
| [1605](https://github.com/x360ce/x360ce/pull/1605) | Rislantrs | Stability, MSVC v143, DragonRise | Crash-reporter null dereference; axis name lookup | v3 + v4 |
| [1606](https://github.com/x360ce/x360ce/pull/1606) | lucasn-tech | Fix Beta VC++ runtime compatibility | Same fix already shipped in 4.17.25.0 | — |

---

## PR 863 — Tencent Gaming Buddy

Adds one `[AppMarket.exe]` block with `HookMask = 0x00000002` to the game
database. Applies to `Native/Support/x360ce.gdb` in this tree (upstream path
`x360ce/Support/x360ce.gdb`), which `InputHook::ReadGameDatabase` reads at run
time. Data only, no build impact, no format change.

**Optional accept.** It is a compatibility entry rather than a defect fix, so it
is outside the "fixes only" rule; take it only if game-database entries count as
in scope.

## PR 959 — V3.x (NEED FOR SPEED PAYBACK)

The head is the repository's own abandoned `v3.x` branch, so the "diff" is the
whole divergence between that branch and master: 28 files, mostly re-adding
`JocysCom/Win32/*` helpers, `x360ce.sln`, and a v120 to v140 platform-toolset
bump. Nothing in it mentions Need For Speed Payback or any game database.
Everything it carries is years behind this tree.

**Skip.** One incidental observation: it replaces the dead CodePlex Visual Leak
Detector link with the GitHub one. This tree still has `https://vld.codeplex.com`
in `App.v3/Issues/LeakDetectorIssue.cs` (twice) — CodePlex shut down in 2021, so
that link is dead. Trivial, cosmetic, take it or leave it.

## PR 1473 — Guide button / Update Help Page HidHide

Mixed bag. Most of it is unusable: committed `.idea` project folders, an
`x360ce.zip` binary, `packages.config` churn, `.edmx` regeneration, and .NET
version comment edits. Inside that is one line that matters:

```csharp
// x360ce.App.Beta/Common/DInput/DInputHelper.Step5.VirtualDevices.cs
report.SetButtonState(Xbox360Buttons.Guide, n.Buttons.HasFlag(GamepadButtonFlags.Guide));
```

**Verified against this tree.** `App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs`
`FeedDevice()` sets A, B, X, Y, Start, Back, both thumbs, both shoulders and all
four D-Pad directions — and never Guide. `Xbox360Buttons.Guide = 0x0400` is
already declared in `App.v4/ViGEm/Client/Targets/Xbox360/Xbox360Report.cs`. So a
mapped Guide button reaches the XInput state but is dropped on the way to the
virtual (ViGEm) controller, and games never see it. This completes the guide
button work already applied in 4.17.25.0.

**Partial accept — one line.** Reject the rest, including the new
`ButtonGuideDeadZone` setting: it adds an INI key ("Guide DeadZone") and joins
the pad-setting checksum, which changes the settings format we are holding
frozen.

## PR 1527 — Update app.config

Adds `<system.windows.forms jitDebugging="true" />` to the v3 app config (plus a
Russian comment). That switch tells WinForms to skip its unhandled-exception
dialog and hand the crash to a JIT debugger. On a developer machine it is
convenient; on a user machine it replaces the app's error report with a Windows
Error Reporting dialog.

**Skip.** It is a debugging preference, not a fix, and it makes end-user crash
handling worse.

## PR 1539 — Compile and upload binaries via GitHub Actions

The intent is one new file, `.github/workflows/build.yml`: build on push, upload
an artifact, and publish a rolling `latest` pre-release. The other 47 files are
collateral — the author's fork carried a different snapshot of the JocysCom
shared library, so the PR silently reverts `Helper.cs`, `SqlHelper.cs`,
`HardwareControl.cs`, `LogFileWriter.cs`, `Attributes.cs`, `SimpleServiceBase.cs`
and friends by hundreds of lines each.

**Skip.** The shared-library revert is exactly the kind of churn this restore is
undoing. The workflow file is also written for the master layout
(`x360ce.App.Beta/bin/Release/net48`), which does not exist here. If CI is wanted
on this branch it should be written for this branch, not imported.

## PR 1576 — Update C++ Issue Helper

Replaces the display-name regex in `CppX64RuntimeInstallIssue` /
`CppX86RuntimeInstallIssue` with a version check against Microsoft's documented
component key:

```
HKLM\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\{x86|x64}
  Installed, Major, Minor, Bld, RBld
```

**This is a better fix than the one already on this branch.** The regex approach
matches Add/Remove Programs display names, so it depends on Microsoft's yearly
label and on the user's Windows display language. Our current version widened the
pattern to `(2015|2017|2019|2022|v14)`, which works today and will go stale the
next time the label changes. The registry key is the documented detection method,
is language independent, and cannot go stale — all v14x runtimes are backwards
compatible and register there. It also fixes upstream issues #1575 and #1572
(false "not installed" reports).

**Adopt, adapted.** Keep `IssueHelper.IsInstalled` for other issue checks and
change only the two VC++ checks. Fix two things while porting: the x86 file's
`MoreInfo` URL has a typo (`vc-redists`), and the x64 file drops a `using` it no
longer needs. Keep our "2015-2022" wording in the user-facing message.

## PR 1596 — Fix cross-thread crash in GetActiveControl

Wraps `FocusManager.GetFocusedElement` in `Invoke` inside the **WPF**
`ControlsHelper.WPF.GetActiveControl`, because an unobserved task exception is
delivered on the finalizer thread, chains into the exception logger, and WPF
throws when a non-UI thread touches a `DispatcherObject`.

**The patch does not apply here** — this tree has no `GetActiveControl` in
`ControlsHelper.WPF.cs`; the equivalent is the WinForms
`MainForm.GetActiveControl`, and WinForms does not enforce thread affinity on
property reads, so there is no cross-thread crash.

**But reviewing it exposed a worse defect in our copy.** `App.v4/MainForm.cs:1730`:

```csharp
var container = control as ContainerControl;
while (container != null)
{
    control = container.ActiveControl;
    if (control != null)
    {
        activePath += string.Format("/{0}", control.Name);
        activeControl = control;
        container = control as ContainerControl;   // only updated inside the if
    }
}
```

When `container.ActiveControl` is `null` — no focused child, which happens when
the window is minimised to tray, before first activation, or during shutdown —
`container` is never reassigned and the loop spins forever. The caller is
`LogHelper_Current_WritingException`, so the app hangs at 100% CPU on one core
precisely while it is trying to report an error. Upstream fixed this in passing
during the WPF migration (`4cf3509c`, later `29be9292`) with `if (control == null)
break;`. My per-hunk pass read that hunk and filed it as migration noise; that
was a miss, and this PR review is what surfaced it.

**Apply the two-line guard** to `MainForm.GetActiveControl`. v3 has no equivalent
method.

## PR 1604 — Feature/windows11 controller reliability

50 files, roughly 2,500 changed lines: deletes `Downloader` and `UpdateWindow`,
adds a diagnostics subsystem (`OperationalLog`, `DiagnosticReport`,
`StartupStageRunner`), ViGEmBus health probes, a task list under `tasks/`, and a
PowerShell smoke test. It rewrites `Program.cs`, `TrayManager`, `ViGEmClient` and
the device pipeline.

**The patch is not mergeable here, but underneath the redesign it repairs seven
defects that this tree shares — and they are the most severe ones found in any of
the ten PRs.** All seven were verified against the restored code.

### 1. ViGEmBus client is allocated and destroyed on every poll tick

`UpdateVirtualDevices` runs once per DirectInput update — 990 Hz on the test
machine. `App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs:31`:

```csharp
if (!ViGEmClient.isVBusExists(true))    // allocates + vigem_connect when Current is disposed
    return;
var isVirtual = game != null && ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
if (!isVirtual)
{
    ViGEmClient.DisposeCurrent();       // vigem_disconnect + vigem_free, leaves Current non-null
    return;
}
```

`DisposeCurrent` never clears `Current`, so on the next tick
`isVBusExists`'s guard (`Current != null && !Disposing && !IsDisposed`) fails on
`IsDisposed` and builds a brand-new client: `vigem_alloc` + `vigem_connect`, then
`UnplugAllControllers` + `vigem_disconnect` + `vigem_free` again. For any game
**not** in Virtual mode, x360ce opens and closes a kernel driver handle up to a
thousand times a second for as long as it runs. Machines without ViGEmBus escape
it only because the 5-second `PendingError` cache short-circuits the retry.

PR 1604 fixes this with a `virtualModeActive` flag so the dispose happens once on
leaving virtual mode, and clears `Current` in `DisposeCurrent`.

### 2. The controller worker thread dies on any unhandled exception

`DInputHelper.ThreadAction` calls `RefreshAll(manager, detector)` with no
try/catch inside the loop, and `RefreshAll` itself only guards some of its steps.
One escaped exception ends the thread: device polling, mapping and virtual
feeding all stop, `detector.Dispose()` and `manager.Dispose()` never run, and the
window stays responsive so nothing looks wrong. This is the most likely cause of
"the controller just stopped working until I restarted x360ce".

### 3. `FeedDevice` sends to the virtual target with no error handling

`ViGEmClient.Current.Targets[i - 1].SendReport(report)` is unprotected. When the
bus drops a target — driver update, bus reset, a target unplugged underneath —
the exception escapes into `RefreshAll` and, with defect 2, kills the worker. The
PR catches it, marks the slot failed and unplugs so the next tick re-plugs.

### 4. `PlugIn` leaves placeholder controllers connected when it fails

To place a virtual pad at slot *n*, `PlugIn` connects slots 1..n-1 first, then
disconnects them again — but the cleanup sits at the end of the `try`, so if
`t[userIndex - 1].Connect()` throws, the placeholders stay plugged in. The user
gets phantom virtual controllers. The PR moves the cleanup into `finally`.

### 5. `UnPlug` reports success after a failed disconnect

It catches the exception, logs it, and still returns `true` with
`connected[i - 1]` left `true`, so the tracking array disagrees with the driver.

### 6. No bounds checks on the slot index

`UnPlug`, `PlugIn` and `IsControllerConnected` all index `connected[i - 1]`
without validating `i`, so a bad slot throws `IndexOutOfRangeException` instead
of returning false.

### 7. `StopDInputService` joins the worker with no timeout

`_Thread.Join()` is called from the UI thread (`MainForm.cs:966`, `:1547`,
`SettingsGridUserControl.cs:192`). If the worker is wedged in a native
DirectInput or ViGEm call — the exact Windows 11 scenario this PR is named for —
the app hangs. The PR uses a 2-second join and logs the timeout.

Related, same file: `isVBusExists(bool createIfMissing = false)` never reads its
parameter, so the "just checking" callers
(`CheckInstallVirtualDriver`, `CheckUnInstallVirtualDriver`, `DisableFeeding`,
`VirtualDeviceDriverIssue`) all allocate and connect a client as a side effect of
asking a question. PR 1604 deletes the parameter.

Also worth adopting for the same reason we removed the HID Guardian installer:
the PR makes `CheckUnInstallVirtualDriver` a no-op, because ViGEmBus is shared
with other tools (DS4Windows and friends) and x360ce should not remove it.

### What to reject

- `app.manifest` switching to `dpiAware=true/pm` and `PerMonitorV2`. This branch
  deliberately locks the process DPI-unaware; per-monitor awareness is what
  produced the tiny unscaled UI fixed in 4.17.25.0.
- The Step2 "state leak" fix — resetting `newState`/`newUpdates` at the top of the
  per-device loop. It repairs a bug the rewrite created by hoisting those
  variables out of the loop; this tree still declares them inside it.
- The Step1 enumeration hardening: it targets the 2024 rewritten Step1, and our
  `UpdateDiDevices` already wraps the whole scan in try/catch.
- The diagnostics subsystem, health model, `ViGEmBusHealth`/probe classes,
  `tasks/*.md`, `ColdLaunchSmoke.ps1`, and the removal of `Downloader` /
  `UpdateWindow`.
- Its VC++ detector — same conclusion as PR 1576 (registry component key), which
  independently raises confidence in that change, but 1576's version is a third
  of the size.
- Per-device force-feedback disable after repeated HID PID failures: a sensible
  resilience idea, but new behaviour rather than a defect fix.

## PR 1605 — Cross-thread crashes, MSVC v143, PadList null safety, DragonRise preset

The broadest of the recent PRs. Most of it is WPF (XAML `ScrollViewer` wrapping,
window sizing, `PadListControl` null guards, `ErrorsHelper` dispatcher checks) or
new behaviour (auto-configure controllers at startup, `Environment.Exit(0)` on
close, 250 Hz default polling, a DragonRise device preset). Two items are real
defects that **this tree shares**:

1. **`Program.cs` `AddExceptionMessage` reads the wrong exception.** Verified at
   `App.v4/Program.cs:302`:

   ```csharp
   foreach (var key in ex.Data.Keys)
       m += string.Format("{0}: {1}\r\n", key, ex1.Data[key]);   // ex1, not ex
   ```

   `ex1` is `ex as ConfigurationErrorsException`, so it is `null` for every
   exception that is not a configuration error. Any exception carrying `Data`
   entries — and the logger adds `ActiveControlPath` to every one — throws a
   `NullReferenceException` inside the crash reporter. Together with the hang in
   PR 1596 above, the error-reporting path has two ways to fail while reporting.

2. **`AutoMapHelper.GetAxisValue` searches for axis names among buttons.**
   Verified at `App.v4/Common/AutoMapHelper.cs:206-208`: the by-name lookup
   filters on `x.Type == ObjectGuid.Button || x.Type == ObjectGuid.Key`, so an
   axis or slider can never match. Every name hint passed to that method
   ("L2", "Wheel axis", …) is dead, and auto-map silently falls back to matching
   by axis type. Not a crash — it just makes auto-configure worse than intended.

A third item is plausible but riskier: `GetButtonValue` and `GetAxisValue` honour
`removeIfFound` only on the by-name path, so an object found by index or by type
stays in the pool and can be assigned to two mappings. PR 1605 removes it on all
paths. That is probably correct, but it changes generated presets, so it needs a
controller test before it goes in — not a blind apply. Do **not** copy their
`GetButtonValue` return change (`o.DiIndex` without `+ 1`); that belongs to the
rewrite's index semantics and would shift every generated button by one here.

Skip the DragonRise preset (device-specific feature), `AutoConfigure` (changes
what happens to a user's mappings on startup), and `Environment.Exit(0)` (skips
the save path on close).

## PR 1606 — Fix Beta VC++ runtime compatibility

Widens the same regex to `(2015|2017|2019|2022|v14)` for x64 and x86, and adds a
`System.Resources.Extensions` package reference.

**Already on this branch** — the identical regex change shipped in 4.17.25.0,
where we also updated the user-facing label to "2015-2022" (this PR leaves it
saying 2015-2019). Superseded by PR 1576's registry check, which removes the
regex entirely. The package reference belongs to the master `.csproj` layout and
does not apply here.

---

## Recommended integration

Fixes, in priority order:

1. ViGEmBus client allocated and freed on every poll tick outside virtual mode
   (PR 1604) — up to a thousand driver connect/disconnect cycles per second.
2. Controller worker thread dies on any unhandled exception (PR 1604) — input
   silently stops while the window stays responsive.
3. `FeedDevice` sends to the virtual target with no error handling (PR 1604).
4. `MainForm.GetActiveControl` infinite loop (found via PR 1596) — app hangs
   while reporting an error.
5. `Program.AddExceptionMessage` `ex1.Data` null dereference (PR 1605) — crash
   reporter crashes.
6. `PlugIn` leaves placeholder virtual controllers connected on failure, `UnPlug`
   reports success after a failed disconnect, and neither validates the slot
   index (PR 1604).
7. `StopDInputService` joins the worker thread with no timeout from the UI thread
   (PR 1604).
8. Guide button not forwarded to the virtual controller (PR 1473) — one line.
9. VC++ runtime detection by registry component key instead of display-name
   regex (PR 1576) — replaces our own weaker fix.
10. `AutoMapHelper.GetAxisValue` name lookup filtered to button types (PR 1605).

Needs a controller test before deciding: `removeIfFound` on the by-type and
by-index paths in `AutoMapHelper` (PR 1605).

Optional, non-fix: dead CodePlex link in `App.v3/Issues/LeakDetectorIssue.cs`
(observed in PR 959).

## Implementation status

Applied on the branch (v4 `4.17.46.0`, v3 `3.3.6.3`):

| Fix | From | Apps |
|---|---|---|
| Virtual bus client no longer allocated and freed on every update | PR 1604, ykondury | v4 |
| Controller update thread survives an unhandled error | PR 1604, ykondury | v4 |
| `FeedDevice` handles a failed report and replugs the target | PR 1604, ykondury | v4 |
| `PlugIn` cleans up placeholder targets in `finally` | PR 1604, ykondury | v4 |
| `UnPlug` reports failure and clears the slot state | PR 1604, ykondury | v4 |
| Slot index validated in `PlugIn`, `UnPlug`, `IsControllerConnected` | PR 1604, ykondury | v4 |
| `StopDInputService` joins with a two second timeout | PR 1604, ykondury | v4 |
| `GetActiveControl` cannot loop forever | PR 1596, mNandhu | v4 |
| `AddExceptionMessage` reads the right exception | PR 1605, Rislantrs | v3 + v4 |
| Guide button reaches the virtual controller | PR 1473, OmarIskandarani-KSO | v4 |
| VC++ runtime detected by registry version | PR 1576, Glitchtest51 | v4 |
| Microsoft XInput lookup (logged for v3, shipped in 4.17.33.0) | — | v3 |

Driver work, own design, prompted by PR 1604:

- `UninstallViGEmBus` refuses to remove a bus whose version differs from the
  embedded package, removes only exact hardware ids, and returns the state
  actually reached. The UI confirms first and reports what it measures.
- `UninstallHidGuardian` removes the HID class filter first, verifies it is gone,
  and only then removes the driver. If the filter cannot be removed the driver
  stays, because a filter naming a missing driver is what locks out input.
- `InstallHidGuardian` restored under `#if DEBUG` only, installing the driver
  first and adding the class filter second.
- `RunDevCon` refuses wildcards in any driver command and checks the exit code.
- `GetHidGuardianRemoveScript` extracts the bundled registry recovery script, and
  the help page documents the removal order and the recovery path.

Deliberately not implemented: `AutoMapHelper` name lookup and `removeIfFound`
(PR 1605) — see that section for the interaction that makes them unsafe without
a test; driver package deletion with `pnputil`; and everything listed under
"What to reject" for PR 1604.

## Security review

Nothing is merged from a pull request. Every borrowed change is a source
fragment that was read line by line and re-implemented against this tree. No
branch merges, no binaries, no project or solution files, no new NuGet packages,
no new network endpoints, and no new elevation paths.

What each accepted change touches:

- **PR 1604 (ViGEm and worker thread)** — process-local object lifetime only:
  disposing a client once instead of every tick, catching exceptions, bounds
  checking an array index, adding a join timeout. It *reduces* privileged
  activity, because today the app opens and closes a kernel driver handle at
  polling frequency. No new API surface.
- **PR 1596 (loop guard)** — two lines inside a diagnostic helper.
- **PR 1605 (`ex.Data`)** — worth stating explicitly, because it changes what
  ends up in an error report. Today the loop throws before writing anything;
  once fixed, the values in `Exception.Data` are written into the report the
  user can choose to send. Checked what this tree puts there: `ActiveControlPath`
  (a control-name path), `FFInfo` (force-feedback step trace), and SharpDX error
  descriptors. No credentials, paths to user files, or personal data.
- **PR 1605 (axis name lookup)** — controller mapping logic only.
- **PR 1473 (Guide button)** — one call that sets a button flag on the virtual
  pad. The `ButtonGuideDeadZone` setting is **not** taken: it changes the INI
  format and the pad-setting checksum. The PR's committed `x360ce.zip` binary and
  `.idea` folders are **not** taken — never import binaries from a pull request.
- **PR 1576 (VC++ detection)** — replaces an enumeration of the Windows
  uninstall registry keys with four read-only DWORD reads under
  `HKLM\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes`. This is the narrower
  surface: the helper it replaces, `IssueHelper.IsInstalled`, can execute an
  `UninstallString` command read out of the registry when called with
  `uninstall: true` (we never do, but the new code cannot). The download URL
  moves from `aka.ms/vs/16/...` to `aka.ms/vs/17/...` — same Microsoft-controlled
  HTTPS redirect, same trust model as today.

Rejected partly or wholly for security reasons:

- **PR 863** — adds a `HookMask` entry that enables x360ce's API hooking inside
  Tencent Gaming Buddy. Injecting into a third-party app that ships its own
  anti-cheat can crash it or get users banned, and it would be on by default for
  anyone with that app installed. Not worth a compatibility entry.
- **PR 1527** — `jitDebugging="true"` hands unhandled WinForms exceptions to a
  JIT debugger instead of the application's own error dialog.
- **PR 1539** — the workflow declares no `permissions:` block, so it inherits the
  repository default; it publishes a GitHub release from a job that also runs on
  `pull_request`; it pins `microsoft/setup-msbuild@v1.0.2` by mutable tag rather
  than commit SHA; and it force-overwrites a rolling `latest` pre-release
  (`gh release upload latest --clobber`) with an unsigned build. For a program
  that hooks into other processes, publishing unsigned automatic builds under a
  fixed download URL is a supply-chain decision, not a convenience. If CI is
  wanted later, write it here with least-privilege permissions, no publishing
  from PR triggers, SHA-pinned actions, and signing.
- **PR 959** — carries the maintainer's code-signing script with a hard-coded
  path to a `.pfx`. Nothing to borrow; a reminder not to copy signing config
  between branches.

Pre-existing weakness, unrelated to any PR, worth its own work item:
`IssueHelper.DownloadAndInstall` downloads an installer over HTTPS and launches
it without checking the Authenticode signature — while this tree already has
`App.v4/Common/CertificateHelper.cs` (`IsSignedAndTrusted`) and `WinVerify.cs`
sitting unused for exactly that purpose.

## Release follow-up (do this when the version ships)

The borrowed fixes credit their authors in the changelog. When a release goes
out containing them, close the loop with the contributors:

1. Comment on each pull request below, thanking the author by name, naming the
   release that contains their fix, and linking the commit.
2. Close the pull request, explaining that the change could not be merged as-is
   because this branch is the restored 4.17.x WinForms line, not master.

| PR | Author | Credit in |
|---|---|---|
| [1473](https://github.com/x360ce/x360ce/pull/1473) | OmarIskandarani-KSO | Guide button on virtual controller |
| [1576](https://github.com/x360ce/x360ce/pull/1576) | Glitchtest51 | VC++ runtime detection by registry version |
| [1596](https://github.com/x360ce/x360ce/pull/1596) | mNandhu | Hang while reporting an error |
| [1604](https://github.com/x360ce/x360ce/pull/1604) | ykondury | ViGEm client churn, worker thread, slot handling |
| [1605](https://github.com/x360ce/x360ce/pull/1605) | Rislantrs | Crash reporter, axis name lookup |
| [1606](https://github.com/x360ce/x360ce/pull/1606) | lucasn-tech | VC++ detection — same fix, already shipped in 4.17.25.0 |

PRs 863, 959, 1527 and 1539 are not being used. If they are closed, say why
rather than closing them silently.
