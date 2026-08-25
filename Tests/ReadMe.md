# Tests

One test project covers the whole solution.

```powershell
.\Tests\Run-Tests.ps1              # everything except the interactive UI tests
.\Tests\Run-Tests.ps1 -Interactive # adds the tests that launch the applications
```

## Layout

```
Tests/
  x360ce.Tests.csproj          project name follows the repo convention: x360ce.{FolderName}
  Run-Tests.ps1                the only supported way to run the suite
  Common/
    AboutTabTest.cs            the About tab's contents
    AccessibilityTest.cs       names and descriptions on the mapping controls
    AgentInstructionsTest.cs   the AI instruction files against the tree they describe
    AppUiTest.cs               launch smoke for App.v3 and App.v4
    BuildOutputTest.cs         which build output each application embeds
    CrashReportTest.cs         crash reporting and Release symbols
    DocumentTest.cs            the shipped documents and the changelog heading
    EngineTest.cs              mapping value parser
    ErrorReportTest.cs         the body the error report window sends
    MemoryLeakTest.cs          disposal and the memory ceiling
    NavImageTest.cs            the navigation glyphs and their scaling
    OptionsLayoutTest.cs       controls on the Options page that paint over each other
    PadGeometryTest.cs         the redrawn controller picture
    StartupInputTest.cs        keys pressed before the window has finished building
    WpfSurfaceTest.cs          no XAML survives anywhere in the repository
  TestInfrastructure/
    Ui.cs                      the only place polling lives
    MemoryLeak.cs              weak-reference disposal checks
    TestRun.cs                 assembly setup: crash reports go to a temporary folder
```

## Why one project rather than several

A test tree should not be more fractured than the product it tests. A split is only
justified by a different target framework, a headless CI constraint, or scale — and none
apply: `App.v3`, `App.v4` and `Engine` all target .NET Framework 4.6.2. Unit, diagnostics
and UI tests are separated by `[TestCategory]`, not by project boundaries.

Tests that need a desktop session are tagged **`ui-interactive`** and excluded by default,
which is the one split that matters here — a headless agent can run everything else.

## Why the applications are launched, not referenced

`App.v3` and `App.v4` both produce `x360ce.exe` with the root namespace `x360ce.App`, so
one assembly cannot reference both. They are driven as processes through
`System.Windows.Automation`, which is in-box: no driver process, no extra package, and it
exercises the applications the way a user meets them.

## Why not `dotnet test`

`x360ce.Engine` has a `Microsoft.mshtml` COM reference that the .NET SDK cannot resolve, so
`dotnet test` fails at compile time while Visual Studio MSBuild succeeds. `Run-Tests.ps1`
builds with VS MSBuild and runs `vstest.console.exe`, passing the MSTest adapter path
explicitly because the package does not copy the adapter into a `net462` output folder.

## The crash report tests

A crash report is only worth sending if it names the line that threw. Field reports arrive
with stack traces like `DInputHelper.Step3.UpdateXiStates.cs, 117` — remove the symbols and
the same report becomes a guess. Three tests hold that in place:

- **`Crash_report_names_the_source_file_and_line`** drives the application's own reporting
  path, then reads the written report back and asserts it names this source file and a line
  within two of the throw. Reports are redirected to a temporary folder, so a test run never
  mixes into real reports.
- **`Crash_report_body_is_complete_enough_to_send`** builds the body the error report window
  would send and asserts it carries the exception, the message and the file. Nothing is
  mailed: a test must never post to real support.
- **`Release_builds_ship_symbols`** checks that every Release binary has its `.pdb` beside
  it. The first two tests run against this assembly and cannot prove what the shipped build
  does; this one can. Verified to fail when a `.pdb` is removed.

## Adding a test

Mirror the product path and append `Tests` to the file name, then declare what it covers:

```csharp
// @under-test: Engine/Maps/SettingsConverter.cs
// @area: mapping   @layer: unit
```

`@under-test` makes reverse coverage a single search:
`rg "@under-test:.*SettingsConverter" Tests/`.

No `Thread.Sleep` in a test body — polling belongs in `Ui.WaitFor`. Tests that launch an
application must be tagged `ui-interactive` and must clean up their process.

## The memory tests

A window that closes but is still referenced keeps its whole control tree, its images and
its event subscriptions alive. The process then holds hundreds of megabytes while doing
nothing but polling devices. App.v4 measures around 200 MB private once settled, so there
is not much headroom before that becomes a complaint.

**Optimisation is mandatory, and this is the trap.** In an unoptimised build the compiler
keeps locals rooted for the debugger even after they are set to null, so a weak reference
never dies and *every* disposal test passes regardless of the truth. The project sets
`Optimize=true` in all configurations, and `Disposal_tests_run_against_an_optimised_build`
fails the suite if that is ever undone. The 5.x `MemoryLeakHelper` carries the same warning.

`MemoryLeak.CreateUseAndRelease` builds the object inside a non-inlined helper so it never
occupies a local of the calling frame — a local would root it for the whole method and the
result would be meaningless. It then collects fully, including the large object heap, and
polls until the weak reference dies or the timeout expires.

Two tests validate the helper itself, one positive and one negative, so the suite cannot
pass simply because the helper never fails. Two more document the defect class behind the
"Fixing Unloading/Disposing" work: subscribing to a publisher that outlives the control
keeps that control alive, and detaching the handler is what actually frees it. Disposing
alone does not.

### Where App.v4's memory went

Measured, so nobody re-litigates it from intuition.

| | Before | After |
|---|---|---|
| App.v4, window open | 198 MB | **85 MB** |
| App.v4, in tray | 164 MB | **85 MB** |
| App.v3 for reference | 47 MB | 47 MB |

The cost was WPF, not the windows. Its managed heap was only 24 MB; the rest was the WPF
runtime, committed the first time any WPF element was created and never returned. Three
teardown routes were measured before the fix was chosen:

| Approach | Result |
|---|---|
| Dispose every control, then the form, then force compacting collections | returns **1.2 MB** |
| Run WPF on its own thread and shut that Dispatcher down | returns **33 MB**, unreachable from `ElementHost`, which must share the interface thread |
| Retarget to .NET 8 | same architecture, about 10 percent leaner, and it drops Windows 7 and 8.1 |
| **Remove WPF** | **the whole charge, about 90 MB** |

The cost was also fixed rather than proportional: a test application with one WPF island and
the same application with sixty both cost about 90 MB. That is why partial removal was worth
nothing and only the last control mattered.

App.v4 now loads **no WPF modules at all**, confirmed by reading the module list of the running
process. `WpfSurfaceTest` fails if any XAML reappears anywhere in the repository, and
`V4_memory_stays_within_a_sane_ceiling` is set to 120 MB, below the 198 MB the WPF build used.

The remaining gap to App.v3 is not WPF. App.v4 carries features v3 does not, including the
cloud client and a larger interface.

### Not yet covered

Proving that App.v4's own main form is released when it minimises to tray needs one of two
things: the option enabled plus a settings sandbox so a test run cannot disturb real user
settings, or an in-process reference to App.v4 — which conflicts with App.v3 because both
produce `x360ce.exe` in the namespace `x360ce.App`.

`V4_does_not_grow_across_minimize_restore_cycles` is the guard that matters here, and it is
the stronger of the two: it drives the real application through minimise and restore and
asserts that private bytes, GDI handles and USER handles all hold steady. Handle
counts are the sharper signal — an undisposed control costs a window and a device context
long before it costs measurable memory. Currently all three are flat to the megabyte and the
handle across six cycles. `V4_memory_stays_within_a_sane_ceiling` covers the other direction: the
absolute figure, which is what a return to hardware rendering would move.
