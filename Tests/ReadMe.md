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
    EngineTest.cs              mapping value parser
    CrashReportTest.cs         crash reporting and Release symbols
    MemoryLeakTest.cs          disposal and the memory ceiling
    AppUiTest.cs               launch smoke for App.v3 and App.v4
  TestInfrastructure/
    Ui.cs                      the only place polling lives
    MemoryLeak.cs              weak-reference disposal checks
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

### Not yet covered

Proving that App.v4's own main form is released when it minimises to tray needs one of two
things: the option enabled plus a settings sandbox so a test run cannot disturb real user
settings, or an in-process reference to App.v4 — which conflicts with App.v3 because both
produce `x360ce.exe` in the namespace `x360ce.App`. `V4_memory_stays_within_a_sane_ceiling`
is the interim guard: it catches a footprint that doubles, not a single leaked form.
