# x360ce Workspace Architecture & Engineering Rules

## 1. Core Constraints & Language
- **C# <= 7.3 Only**: Target is .NET Framework 4.6.2. Never use C# 8.0+ features (nullable reference types, pattern matching enhancements, switch expressions, index/range operators, async streams).
- **Legacy Non-SDK MSBuild**: `App.v3`, `App.v4`, `Engine`, and `Web` are non-SDK `.csproj`. New files must be explicitly declared in `<Compile>`. Note: `dotnet build` and `dotnet test` are not supported.
- **Native Toolset**: `Native/` projects pin `<PlatformToolset>v141</PlatformToolset>` (VS 2017 C++). Do not override.
- **Public Export Contract**: `Reset @256` in native DLLs is a load-bearing probe contract across `Native/dinput8`, `App.v3`, and `Engine`.

## 2. 1000 Hz Engine Isolation (`DInputHelper` / `RefreshAllThread`)
The polling engine (`RefreshAllThread` in `App.v4/Common/DInput/DInputHelper.cs` and partials `Step1`..`Step6`) produces state at up to **1000 Hz** (1 ms cycle). The UI thread renders at ~10 Hz.
- **Zero UI-Engine Shared Locks**: Nothing on the engine thread may wait for UI or acquire locks held by UI (e.g. `BindingListInvoked`, `OneChangeAtTheTime`).
- **Zero Synchronous UI Marshalling**: Never use `Control.Invoke`, `Task.RunSynchronously`, `Wait()`, or `.Result` from the engine path.
- **Zero Allocations or Copies per Cycle**: Avoid calling `ItemsToArraySyncronized()` or allocating objects per polling cycle.
- **Zero Exceptions per Cycle**: Never use exceptions for flow control on the hot path.
- **Guard Preservation**: Never hoist lookups out of `if (mapped)` or `if (hasForceFeedback)` guards.

## 3. Architecture, Build & Test Standards
- **Build Workflow**: Run `Build_All.cmd` (Release) or `Build_All.cmd Debug`. MSBuild builds platforms in order: `DLL_x86_v3 -> DLL_x64_v3 -> APP_x86_v3 -> APP_x64_v3 -> APP_Any_v4`.
- **Testing**: Run `Tests\Run-Tests.ps1`. Requires `vstest.console.exe`. Keep `Optimize=true` (asserted by memory-leak check tests). Always run `EngineFrequencyTest` to verify that 1000 Hz polling rate is preserved.
- **Dual Architecture**: v3 line (`xinput1_3.dll` native wrapper) and v4 line (ViGEm virtual bus emulator). Both compile to `x360ce.exe` under namespace `x360ce.App`. Shared code lives in `Engine`.
- **Dependencies**: SharpDX, ViGEmClient, and JocysCom library are vendored and referenced by path. MinHook is a submodule.
- **Elevation**: Required for ViGEm bus installation and writing native DLLs into game folders.
<!-- Full historical analysis preserved in .ai/repository-analysis.instructions.md -->
