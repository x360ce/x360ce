==== START OF INSTRUCTIONS FROM: developer.instructions.md ====

# Instructions from: developer.instructions.md

# Developer-Provided Context

Use only C# language features up to and including version 7.3; do not use any features introduced in C# 8.0 or later.

==== END OF INSTRUCTIONS FROM: developer.instructions.md ====

==== START OF INSTRUCTIONS FROM: repository-analysis.instructions.md ====

# Instructions from: repository-analysis.instructions.md

> Repository: x360ce/x360ce · generated from f0ec5a67 on 2026-08-25 by the `repository-analysis` skill. Regenerate rather than hand-edit.

# x360ce — Repository Analysis

## 1. Repository Overview

x360ce is a Windows XInput (Xbox 360 controller) emulator: it makes non-XInput game controllers appear to games as Xbox 360 pads. The repository holds **two independent, coexisting emulation strategies** plus a shared library and a cloud site:

- **v3 line** (`3.3.10.0`) — a native **DLL wrapper**. `xinput1_3.dll` is dropped next to the game executable; the game loads it instead of the system XInput DLL. The app is only a configurator and does not need to run while playing.
- **v4 line** (`4.17.50.0`, current) — a managed app that drives the **ViGEm virtual bus** driver. Nothing is copied into the game folder, but the app *is* the emulator and must stay running.

Both application projects set `AssemblyName` to `x360ce` and `RootNamespace` to `x360ce.App`, so they produce the same `x360ce.exe` in the same namespace — nothing can reference both.

Working branch context: `revert-to-4.17.0.0-reapply-bugfixes` is a revert-and-reapply effort whose own project management lives (mostly untracked) under `docs/plans/`.

## 2. Top-Level Structure

| Path | Contents |
|---|---|
| `App.v3/` | v3 configurator (WinForms, .NET Framework), embeds the native DLLs |
| `App.v4/` | v4 emulator app (ViGEm, DirectInput pipeline, UI) |
| `Engine/` | `x360ce.Engine.dll` — shared model, settings, cloud client, XInput shim |
| `Native/` | C++ sources: `x360ce`, `dinput8`, `InputHook`, `Common`, `ditool` |
| `MinHook/` | git submodule (TsudaKageyu/minhook) used by `InputHook` |
| `Web/` | ASP.NET Web Forms cloud site + ASMX settings service |
| `Data/` | SQL Server database project (`x360ce.Data.sqlproj`) — schema SSOT |
| `Tests/` | the single test project (SDK-style, MSTest) |
| `Documents/` | release pipeline scripts + signing manifest |
| `Resources/` | one file: `ZipFiles.ps1` |
| `scripts/ui/` | `Invoke-AppUiCapture.ps1` — screenshot evidence tooling |
| `docs/` | almost entirely git-ignored (`docs/.gitignore` ignores `plans/*`) |
| `.ai/` | two instruction files (see §8) |

## 3. Technology Stack & Key Dependencies

- **Solution:** `x360ce.slnx` (XML solution format — there is no `.sln`). Its five *platforms* are build profiles, not CPU targets: `DLL_x86_v3`, `DLL_x64_v3`, `APP_x86_v3`, `APP_x64_v3`, `APP_Any_v4`. Each project maps solution platform to project platform and disables itself outside its own profiles.
- **Managed:** `App.v3`, `App.v4`, `Engine`, `Web` are **legacy non-SDK** MSBuild projects on `<TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>`. Only `Tests/x360ce.Tests.csproj` is SDK-style (`Microsoft.NET.Sdk`, `net462`).
- **Native:** all five `Native/**/*.vcxproj` pin `<PlatformToolset>v141</PlatformToolset>` (VS 2017 C++ build tools) in **every** configuration, with `<WindowsTargetPlatformVersion>10.0.26100.0</WindowsTargetPlatformVersion>`. Each project states the toolset once and nothing overrides it: `Build_All.cmd` passes no `/p:PlatformToolset` and reads no `TOOLSET` variable, and `Documents\App_0_Release.ps1` (`Assert-Toolsets`) checks the toolset the configuration asks for is installed *before* the destructive clean step runs.
- **No shared build props.** There is no `Directory.Build.props`/`.targets` and no `.props`/`.targets` of any kind, and `LangVersion` is set nowhere (see §9).
- **Dependencies:** no `packages.config` anywhere; the only `PackageReference`s in the repo are the three in `Tests/x360ce.Tests.csproj` (`Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`, `MSTest.TestFramework` — read versions there). Everything else is a GAC/framework `<Reference>` or a **checked-in DLL by `HintPath`**: SharpDX under `App.v4/Resources/SharpDX/` and `App.v3/Resources/SharpDX/`, with `Engine` reaching across into `..\App.v4\Resources\SharpDX\`. ViGEm's native client ships as `App.v4/Resources/ViGEmClient/{x86,x64}/ViGEmClient.dll`.
- **Project graph:** `App.v3 → Engine`, `App.v4 → Engine`, `Web → Engine`, `Tests → Engine` **and** `App.v4` (App.v3 is exercised as a launched process instead).

## 4. Projects & Responsibilities

**`Native/x360ce`** → the emulator DLL. `TargetName` renames the output to **`xinput1_3.dll`**; `x360ce.def` exports the full XInput surface, the undocumented ordinals, and a private `Reset @256` used everywhere as the "this is our DLL" probe. Links `dxguid/dinput8/psapi/wintrust/Shlwapi`, project-references `Common` and `InputHook`. Config parsing is `Native/x360ce/Config.cpp` (`x360ce.ini`, searched in the exe folder then `%ALLUSERSPROFILE%\X360CE`).

**`Native/InputHook`** (static lib) — MinHook detours so the game believes an XInput pad is attached and does not also see the physical device. Detours are a bitmask (`HOOK_LL/COM/DI/PIDVID/NAME/SA/WT/STOP` in `Native/InputHook/InputHook.h`), resolved per game from `x360ce.gdb` keyed by process file name, falling back to `[InputHook] HookMask`. One file per bit (`HookCOM.cpp` = the WMI "is this XInput" probe, `HookDI.cpp` = DirectInput8, `HookLL.cpp` = LoadLibrary, `HookSA.cpp`, `HookWT.cpp`).

**`Native/dinput8`** — thin proxy DLL for the system `dinput8.dll`; each export forwards through `DirectInputModuleManager` and first calls `LoadEmulator()` (`Native/dinput8/dllmain.cpp`), which probes the module folder for `xinput1_4/1_3/1_2/1_1/9_1_0.dll` and identifies ours by the exported `Reset`. Exists only to force the emulator to load in games that never load an XInput DLL by name.

**`Native/ditool`** — 48-line console diagnostic dumping DirectInput devices to `ditool.txt`. **`Native/Common`** — shared static lib (`IniFile`, `Logger`, `Utils`, `WindowsVersion`).

**`App.v3`** — configurator only. It embeds **both bitnesses of both native DLLs** plus `x360ce.Engine.dll` as managed resources and writes the matching DLL into the game folder via `AppHelper.WriteFile` (`App.v3/Common/AppHelper.cs`), elevating if the write is denied; while testing it calls the DLL's `Reset` export to force an INI re-read.

**`App.v4`** — the v4 emulator. Creates up to four Xbox 360 targets on the ViGEm bus and feeds them reports. Managed wrapper: `App.v4/ViGEm/Client/ViGEmClient.cs`; `App.v4/ViGEm/HidGuardianHelper.cs` hides the physical device so the game does not see both. Bus install/uninstall requires elevation (`Program.RunElevated(AdminCommand.InstallViGEmBus)`). `App.v4/Service/RemoteService.cs` is an optional UDP receiver for controller state from another machine.

**`Engine`** — mapping model and settings conversion (`Engine/Maps/SettingsConverter.cs`, `Engine/Data/PadSetting.cs`), an EDMX data layer (`Engine/Data/x360ceModel.edmx`), cloud client (`Engine/Common/WebServiceClient.cs`), a scanner that reads XInput import masks out of game binaries (`Engine/Common/XInputMaskScanner.cs`), a vendored JocysCom class library, and a hand-maintained SharpDX.XInput shim. `Engine/SharpDX.XInput/Controller.x360ce.All.cs` is the runtime loader both apps use — `LoadLibrary` + `GetProcAddress` delegates, with `Reset` as the emulator-detection probe.

**`Web`** — outside the emulation path: Web Forms pages (`Web/Default.aspx`, `.ascx` controls) over the frozen SOAP surface `Web/WebServices/x360ce.asmx.cs` (partials `.v3.cs` = save/delete settings, `.v4.cs` = users, games, vendors, presets, search), an ASP.NET Membership area under `Web/Security/`, and the shipped game list `Web/Files/x360ce_Games.xml`.

## 5. Runtime Architecture

**v3 request path.** Game calls `XInputGetState` → lands in `Native/x360ce/x360ce.cpp` → `ControllerManager::DeviceInitialize` returns a controller → the controller reads a **DirectInput** device and maps it to an Xbox 360 report per `x360ce.ini`. Pads flagged `PassThrough` are forwarded to the real system XInput DLL, loaded by name from the system directory (`Native/x360ce/XInputModuleManager.h`). `InputHook` runs alongside to suppress the physical device from the game's own enumeration.

**v4 request path.** A background thread (`RefreshAllThread`), gated by a `HiResTimer` in `App.v4/Common/DInput/DInputHelper.cs`, runs six ordered steps living in sibling partial files `DInputHelper.Step1.UpdateDevices.cs` … `Step6.RetrieveXiStates.cs`: enumerate devices → poll DirectInput states → apply the per-pad map to build an XInput state → combine pads → `SendReport` to the ViGEm target (rumble returns as `Xbox360FeedbackReceivedEventArgs`) → read the state back through XInput for the UI.

**Native ↔ managed link.** Native output paths are split by bitness as `bin\$(Configuration)\` (Win32) and `bin64\$(Configuration)\` (x64); the managed side hardcodes those folder names. Two MSBuild targets, **duplicated in both application projects**, do the embedding — `PopulateEmbeddedFiles` and `StageGeneratedResources` (see `App.v3/x360ce.App.v3.csproj` ~lines 490–560). `StageGeneratedResources` runs `AfterTargets="ResolveReferences"`, copies each `GeneratedResource` into `$(IntermediateOutputPath)Embedded\`, re-adds it as an `EmbeddedResource` with `LogicalName` `$(RootNamespace).Resources.<name>`, and errors with a `MissingResourceHint` when the file is absent. App.v3 embeds `dinput8.dll` as `dinput.dll`/`dinput_x86.dll`/`dinput_x64.dll`, `xinput1_3.dll` as `xinput.dll`/`xinput_x86.dll`/`xinput_x64.dll`, plus `x360ce.Engine.dll` from `@(ReferencePath)`. App.v4 routes only `x360ce.Engine.dll` through that target; its other payloads are statically listed checked-in files (ViGEmClient, SharpDX, `Resources/DXTweak/DXTweak2.exe`, `Resources/*.zip`, `Resources/*.xml.gz`).

## 6. Developer Workflows

**There is no CI** — no `.github/`, `azure-pipelines.yml`, or `appveyor.yml`. Every step is manual on a Windows machine with Visual Studio.

**Build.** `Build_All.cmd` at the repo root is the supported entry point (`Build_All.cmd` = Release, `Build_All.cmd Debug`). It locates MSBuild via `vswhere` (`-latest -find MSBuild\**\Bin\MSBuild.exe`), then builds `x360ce.slnx` once per solution platform in dependency order — `DLL_x86_v3 → DLL_x64_v3 → APP_x86_v3 → APP_x64_v3 → APP_Any_v4` — stopping at the first failure with exit code 1. DLL platforms come first because both native bitnesses are embedded into the apps. It deliberately passes **no** platform toolset; an in-file comment states the `.vcxproj` is the single source of truth so the script and the release script cannot diverge. Single-project builds target the solution with a platform: `msbuild x360ce.slnx -p:Platform=APP_x64_v3`. `dotnet build` is not a supported path for the non-SDK projects.

Supporting pieces: `MinHook_Update.cmd` (`git submodule update --init MinHook`); `Native/x360ce/genrev.cmd`, a `PreBuildEvent` writing `svnrev.h` from `git rev-list --count HEAD` + last commit date + dirty flag; `App.v3` has a `PreBuildEvent` copying native `*.dll`/`*.pdb` into `$(TargetDir)` for Debug; `App.v4` has a `PreBuildEvent` copying `x360ce.Engine.dll` into `App.v4/Resources/`; `Engine`'s `PostBuildEvent` is empty. `App.v4/Documents/Install_BuildTools.ps1` verifies the v141 component (`Microsoft.VisualStudio.Component.VC.v141.x86.x64`), falling back to winget `Microsoft.VisualStudio.2022.BuildTools` — but it still references `x360ce.sln` and platforms `Win32`/`x64`, none of which exist in the current tree.

**UI evidence.** `scripts/ui/Invoke-AppUiCapture.ps1` drives an already-running `x360ce.exe`: it selects tabs by posting `TCM_SETCURFOCUS` to native `SysTabControl32` children and captures with `PrintWindow(PW_RENDERFULLCONTENT)`, so it neither steals focus nor needs the window unoccluded (`-NoResize -Capture pad1.png`, `-SelectTabs 0,6 …`). Output goes to the git-ignored `scripts/ui/captures/`. Each invocation needs a fresh PowerShell process — `Add-Type` types cannot be redefined in-session.

**Release.** Three numbered scripts in `Documents/`, run in order. `App_0_Release.ps1` goes from a clean tree to signed zips, reading everything project-specific from the declarative `App_1_Sign_and_Zip.json` (solution, configuration, and the ordered `Library → Engine → App` stages — files embedded into an application must be signed *before* the application embedding them is compiled, so the App stage sets `BuildProjectReferences=false` to avoid overwriting the signed engine). Flags: `-NoClean`, `-SkipSign`, `-WhatIf`; requires `SIGN_MODULE_PATH` pointing at the USB-token signing module. `App_1_Sign_and_Zip.ps1` performs Sign/Zip/Copy per file, skipping already-trusted signatures and delegating compression to `Resources/ZipFiles.ps1`. `App_2_VirusTotal.ps1` is the publish gate: SHA-256 lookup of every shipped file (`VIRUSTOTAL_API_KEY`, `-Upload`, `-ListOnly`, `-UpdateBaseline`), exiting non-zero on an unknown file or a detection outside the baseline. Output lands in `Documents/Files.v3/` and `Files.v4/`. `Solution_Cleanup.ps1` self-elevates and removes `bin`/`obj`, IIS Express config, and user-specific solution files.

## 7. Testing

One test project covers the whole solution: `Tests/x360ce.Tests.csproj` (SDK-style, `net462`, MSTest + `Microsoft.NET.Test.Sdk`, project references to `Engine` and `App.v4`). Root `.runsettings` disables app domains. Runs deposit `Deploy_*` folders under `TestResults/`.

`.\Tests\Run-Tests.ps1` is documented in `Tests/ReadMe.md` as the **only** supported way to run the suite. It resolves `MSBuild.exe` and `vstest.console.exe` via `vswhere -latest -prerelease`, runs `msbuild Tests\x360ce.Tests.csproj -t:restore,build`, then invokes `vstest.console.exe` on `Tests\bin\<Config>\net462\x360ce.Tests.dll`, propagating vstest's exit code. It passes `/TestAdapterPath` explicitly to `%USERPROFILE%\.nuget\packages\mstest.testadapter\<version>\buildTransitive\net462` — version parsed out of the csproj — because the package does not copy the adapter into a `net462` output folder. Switches: `-Interactive` (adds tests tagged `ui-interactive`, which launch the applications and need a desktop session; without it the script appends `/TestCaseFilter:TestCategory!=ui-interactive`) and `-Configuration Debug|Release`.

Two traps: **`dotnet test` fails at compile time** because `x360ce.Engine` carries a `Microsoft.mshtml` COM reference the .NET SDK cannot resolve; and the project sets `Optimize=true` in *every* configuration, with a test asserting it, because the memory-leak checks are meaningless in an unoptimised build.

## 8. Documentation & Data

Tracked prose is small — **14 `.md` files repo-wide**, five of them the end-user pages under `docs/`. `README.MD` is the front door (download links for both lines, system requirements, troubleshooting); the pages a reader is sent on to are `docs/`. `SECURITY.md` is four lines pointing at support@x360ce.com.

`.ai/` holds exactly two files: `developer.instructions.md` (151 bytes — the C# ≤ 7.3 rule and nothing else) and `repository-analysis.instructions.md` (~20 KB, the nine sections you are reading). Root `CLAUDE.md` is a one-line `@AGENTS.md`; `AGENTS.md` is those two files concatenated, each wrapped in `==== START OF INSTRUCTIONS FROM: <file> ====` markers. There are no `.claude/` or `.agents/` copies to keep in step. **Open `.ai/repository-analysis.instructions.md` first** — it is the densest orientation document in the tree.

⚠ **`AGENTS.md` is generated, not authored.** It is the two `.ai/*.instructions.md` files copied in verbatim, so an edit made there is lost the next time it is written — change the `.ai/` file and regenerate. The two therefore cannot disagree with each other, but both are prose and can fall behind the code they describe: when a document and a build script disagree, the script is right (§9.14).

`docs/` holds the end-user documentation as Markdown, laid out as wiki pages: `.order` names the pages in display order (`Home` first), page assets sit in a dot-prefixed folder beside the page (`.HowToBuild/`), and image links are relative. `docs/.gitignore` contains `plans/*`, so `docs/plans/` is **untracked** — where a working copy has it, its `README.md` is the entry point for the 4.17.0.0 revert-and-reapply effort (`A-triage/` with `commits.json` and `REVIEW.md`, then `B-db`, `C-tests`, `D-cherry-pick`, `E-dataporter`), but nothing in the repository puts it there, so do not plan around it.

The help pages each application shows are those same `docs/*.md` files, embedded as links (`docs\Help.v4.md` and `docs\Help.HidGuardian.md` into v4, `docs\Help.v3.md` into v3) and turned into rich text by `Engine/Common/MarkdownRtf.cs` when the page is opened — so a document is written once and there is no converted copy to keep in step. `App.v3/Documents/` and `App.v4/Documents/` keep the rest (`ChangeLog.txt`, `License.txt`), and `Native/Support/` carries `ReadMe.RTF`, `changelog.txt`, the `x360ce.gdb` game database and `usb-detection.pdf`.

`Data/x360ce.Data.sqlproj` is the declared schema source of truth: `dbo/Tables` (25 scripts — `x360ce_*` product tables plus `aspnet_*` membership), stored procedures, functions, views, UDTs, pre/post-deployment scripts, `Permissions.sql`, and `Change Scripts/Backup/` (`Backup-Data.ps1`, `Restore-Data.ps1`, `x360ce-DataPorter.json`).

## 9. Constraints That Matter When Changing Code

1. **C# 7.3 only.** `.ai/developer.instructions.md` states "Use only C# language features up to and including version 7.3". This is a **documented convention, not an enforced build property** — `LangVersion` is set in no project file, `.editorconfig`, or props file, and there is no `Directory.Build.props`/`.targets`. The non-SDK projects get 7.3 as MSBuild's default for .NET Framework; the SDK-style `Tests` project defaults to `latest`, so newer syntax will compile there and violate the convention silently.
2. **Legacy non-SDK projects.** `App.v3`, `App.v4`, `Engine`, `Web` are old-format `.csproj` on .NET Framework 4.6.2 — new files must be added to the `<Compile>` item list explicitly; `dotnet build`/`dotnet test` are not supported paths.
3. **Build order is load-bearing.** Native DLLs are embedded into the apps, so `DLL_*` platforms must build before `APP_*`. `StageGeneratedResources` hard-errors when an expected native binary is missing, and it hardcodes the `bin\`/`bin64\` output folder names.
4. **Both apps produce `x360ce.exe` in namespace `x360ce.App`** — they can never reference each other, and shared code must go through `Engine`.
5. **v141 toolset required.** All native projects pin `PlatformToolset v141`; the `.vcxproj` is the declared single source for it — do not add a toolset override to `Build_All.cmd` or the release script.
6. **`Reset @256` is a public contract.** `Native/dinput8`, `App.v3`, and `Engine/SharpDX.XInput/Controller.x360ce.All.cs` all use the exported `Reset` to detect the emulator and to force an INI re-read. Removing or renaming it breaks emulator discovery across three components.
7. **Duplicated MSBuild targets.** `PopulateEmbeddedFiles` / `StageGeneratedResources` exist in both app `.csproj` files with different item sets; a change to one does not propagate.
8. **Vendored binaries, not packages.** SharpDX, ViGEmClient, and the JocysCom library are checked in and referenced by path (`Engine` reaches into `..\App.v4\Resources\SharpDX\`). `MinHook/` is a submodule and must be initialised.
9. **`Microsoft.mshtml` COM reference in `Engine`** blocks any SDK-based build/test of the graph — use `Tests\Run-Tests.ps1`.
10. **`Optimize=true` in every test configuration** is asserted by a test; do not "fix" the Debug configuration.
11. **Signing order.** Anything embedded into an application must be signed before that application is compiled — the `Library → Engine → App` stage order in `Documents/App_1_Sign_and_Zip.json` encodes this, and `App_2_VirusTotal.ps1` gates publication.
12. **Elevation is intrinsic.** Writing the DLL into a game folder (v3), and installing or uninstalling the ViGEm bus (v4), require administrator rights; both apps re-launch themselves elevated. HID Guardian is **uninstall-only** in a release build — `AdminCommand.InstallHidGuardian` and the Install button exist only under `#if DEBUG`, because a HID filter left registered without its driver can cost the user keyboard and mouse.
13. **`docs/plans/*` is git-ignored** — work-in-progress notes there are not shared through the repository.
14. **Trust the build scripts over this prose.** `Build_All.cmd`, `Tests/Run-Tests.ps1`, `Documents/App_0_Release.ps1` and the `.vcxproj`/`.csproj` files are the executable truth; this document — and `AGENTS.md`, which is a copy of it — only describes them. When they disagree, correct the document (see §8).

==== END OF INSTRUCTIONS FROM: repository-analysis.instructions.md ====
