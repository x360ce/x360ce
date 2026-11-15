# Developer-Provided Context

Use only C# language features up to and including version 7.3; do not use any features introduced in C# 8.0 or later.

## The engine and the interface are separate, and the engine never waits

The engine — `RefreshAllThread` in `App.v4/Common/DInput/DInputHelper.cs` and its six
`DInputHelper.Step*` files — reads hardware and produces controller state at **up to 1000
cycles a second**. The interface draws at about 10. They are separate on purpose, and the
separation is only real while the engine can complete a cycle without the interface's
permission.

**Rule: nothing on the engine path may wait for the interface thread, and nothing on it may
take a lock the interface thread also takes.** One blocked cycle at 1000 Hz is not a pause,
it is the whole budget.

This has been broken repeatedly, always the same way: a crash was fixed by adding a lock or
a marshalled call, and the rate fell from about 1000 Hz to single digits. Before changing
anything reachable from `RefreshAllThread`, or anything the engine calls, check every line
against this list:

- **No `lock` taken by both the engine and the interface.** `BindingListInvoked` holds
  `OneChangeAtTheTime` on the interface thread for the whole of each delivered notification.
  An engine-side `lock` on it blocks the engine for as long as the interface is drawing.
- **No synchronous marshalling.** `Control.Invoke`, `Task.RunSynchronously(uiScheduler)`,
  `Wait()`, `Result`, or anything else that blocks until the interface answers.
- **No work moved above the guard that used to skip it.** Hoisting a lookup out of an
  `if (hasForceFeedback)` or `if (mapped)` makes every device pay it on every cycle. A call
  that was cheap once a second is not cheap a thousand times a second.
- **No allocation or copy added per cycle.** `ItemsToArraySynchronized()` locks and copies the
  whole list. Calling it once per device per cycle is a cost; calling it for devices that do
  not need the answer is waste.
- **No exception thrown per cycle**, and no cost that grows with the number of devices.

### How to check it before saying it is done

1. `dotnet` cannot build this solution — use `Tests\Run-Tests.ps1`. Run
   `EngineFrequencyTest`, which measures the engine's rate with the interface idle and busy,
   over **both** the synchronous and the asynchronous marshalling paths. `UserDevices.Items`
   uses the asynchronous one (`SettingsManager.cs` sets `AsynchronousInvoke = true`), so a
   change that only slows that path must still fail a test.
2. Then run the program and read **HW Hz** in the status bar with a controller connected. It
   must sit near the polling rate chosen on the Options page. A drop to single or double
   digits means the engine is waiting on something. `EngineRateTest` does this and is tagged
   `ui-interactive`.

A measurement is not optional for a change on this path. Two of these regressions passed the
whole test suite and were caught by a person looking at the status bar.

## No crash passes unseen

A test run fails when Windows logged a crash or hang of x360ce, the test host or a program run
from the repository while it ran (`Tests/TestInfrastructure/CrashLog.cs`, `TestRun.Cleanup`), and
writes them to `Tests/TestResults/WindowsCrashes.txt`. A test that crashes a process on purpose
calls `CrashLog.Expect` first; a test that starts a program checks its exit code with
`CrashLog.AssertExitedCleanly` (`Ui.CloseApp` does), because a crash of a program a test starts can
reach no log at all. After running the real program by hand, read the Windows Application log for
its crashes too: a crash while closing shows nothing on screen.

## The HID Hide Raw Input workaround goes when HID Hide is fixed

HID Hide 1.5.230 and older refuse Windows' own Raw Input open of a hidden controller, and Windows then
leaves the controller out of Raw Input until it is plugged in again, even after it is shown. Three parts
of x360ce work around it, and each carries the tag `HidHide-RawInput-Workaround` in its doc comment:

1. `AppHelper.ShowHiddenForRawInputStart` and `HideAgainAfterRawInputStart`, called around
   `StartLoops` in `MainForm`.
2. `UserDevice.DirectInputTwin` and the stand-ins in `DeviceRouting` (`MappedStandsIn`,
   `RowStandIns`), read in `DInputHelper.Step2` and `Step3`.
3. `App.v4/Issues/RawInputMissingIssue.cs`.

The fix proposed to HID Hide is pull request 226 (https://github.com/nefarius/HidHide/pull/226). Check
it when a release branch is made and whenever you change any tagged code:

1. `GET https://api.github.com/repos/nefarius/HidHide/pulls/226`: note `merged` and `merge_commit_sha`.
   An open pull request has a `merge_commit_sha` too, from GitHub's test merge; use it only when `merged`
   is true.
2. If merged, `GET https://api.github.com/repos/nefarius/HidHide/releases/latest`, then
   `GET https://api.github.com/repos/nefarius/HidHide/compare/{tag_name}...{merge_commit_sha}`.
   A `status` of `behind` or `identical` means that release contains the fix.
3. If it was closed without merging, read its last comments and the newer HID Hide release notes: the
   fix may have landed another way. If it has not, the workaround stays.

When a released HID Hide contains the fix, remove all three parts with their tests, in one change:
search for the tag, delete what carries it, build, and let the compiler find the remaining callers. Then
remove this section and the matching line in `docs/TODO.md`, add one changelog line, and make
`HidHideSetupClient.Outdated` report any HID Hide older than the fixed release, so users with an older
copy are told to update.

## Changelog lines are short

`App.v4/Documents/ChangeLog.txt` is read by people who are not technical. A line names what
the reader gets or what stopped hurting, and nothing else, the way the oldest entries at the
end of the file do: "Fixed: Recording.", "Added: Help tab added.", "Changed: Supports
hot-plugging USB devices."

- One short sentence, under about twelve words. "Off by default." may follow as a second.
- No cause, no mechanism, no reasoning, no reporter. Those belong in the commit message and
  the pull request.
- A feature added and reshaped in the same unreleased version gets one Added line for its
  final state, never a Changed line about an earlier shape.
- Read the end of the file before writing a line, and match it.

## Versioning

Versions are `Major.Minor.Patch.0`; the fourth number is always 0. Each application line is
versioned on its own: v4 in `App.v4/Properties/AssemblyInfo.cs`, v3 in
`App.v3/Properties/AssemblyInfo.cs`. Within a release the patch number rises by one for every
changelog line the release carries, so the changelog, the assembly version and the version line
in `README.MD` are written together.

## The AI skill in `skills/x360ce/`

`skills/x360ce/` is the skill the program installs for AI agents, kept where skill sites and installers find it in
the repository. Only `SKILL.md` is written by hand. Every v4 build runs `x360ce.exe -Skill=skills`, which writes
the program's version into `SKILL.md` and both programs' help into `references/help-v3.md` and `help-v4.md`; each
program's `x360ce.exe /ExportUi` writes its `references/ui-tree-v3.*` or `ui-tree-v4.*`. The v4 program carries all
of them (`App.v4/Mcp/McpTools.cs` points the shared `AiSkill` at them) and removes from the folder any file it does not write, so a new reference goes
into `McpTools.SkillReferences` first. Change the help in `docs/Help.v3.md` or `docs/Help.v4.md`, never in `references/`. `Tests/Common/SkillTest.cs` checks the folder against what the program
installs.

## Plans live in `docs/plans/`

Design notes, requirements and to-do lists for unshipped work live in
`docs/plans/{Letter}-{name}/`, which `docs/.gitignore` keeps out of the repository. It is the
only plan folder: a skill that writes to `docs/superpowers/` has its output moved into
`docs/plans/` straight away. A plan is deleted when its work ships. What a user needs goes into
`docs/Help.*.md`, what a maintainer needs goes into a doc-comment beside the code or into
`Tests/ReadMe.md`, and what is still open goes into `docs/TODO.md` as one line. The code and its
tests are the record of what was built.

## The documents live in `docs/`, and the wiki is made from them

`docs/` is the only place a user document is written. Both programs show its `Help.*.md` as their
help, every v4 build copies the help into `skills/x360ce/references/`, and
`.github/workflows/publish-wiki.yml` writes the GitHub wiki from the folder with
`.github/scripts/Publish-Wiki.ps1` each time `docs/` changes on `master`. Never edit the wiki: the
next publish overwrites it. Link between documents by paths relative to `docs/`, such as
`[Help.v3](Help.v3.md#what-do-hookmasks-do)` or `![...](.attachments/picture.png)`; the publish and
`MarkdownRtf.ResolveLinks` make them into addresses where there is no folder to be relative to. An
old wiki page name that links elsewhere may still use goes into `docs/.moved`, and a page's place in
the wiki's sidebar into `docs/.order`. `Tests/Common/PublishWikiScriptTest.cs` publishes the folder
and fails on a link to any page, heading or file it does not hold.

