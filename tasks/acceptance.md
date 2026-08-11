# Windows 10/11 Reliability Acceptance Record

Date: 2026-08-11  
Branch: `feature/windows11-controller-reliability`

## Verified in this Windows 11 environment

| Acceptance item | Result | Evidence |
| --- | --- | --- |
| Release build | Pass | `x360ce.App.Beta/bin/Release/net48/x360ce.exe` built with Visual Studio MSBuild. |
| VC++ v14.51.36247 x86 installed | Pass | Component-registry detector returned Installed=1 and parsed the v14 version through the 32-bit registry view. |
| VC++ v14.51.36247 x64 installed | Pass | Component-registry detector returned Installed=1 and parsed the v14 version through the 64-bit registry view. |
| External compatible ViGEmBus | Pass | Existing third-party installation detected as installed, service present/running, driver 1.16.112.0, client connection successful. No install action was offered. |
| Generic DirectInput controller connected before launch | Pass | Initial interactive smoke reached the mapping window with the controller connected. |
| Polling path | Pass | Measured approximately 961 Hz with the requested 1000 Hz setting. |
| Focused automated tests | Pass | 15/15 dependency, startup deadline, diagnostics, logging and controller-health tests. |

## Requires a dedicated VM or hardware test bench

These checks deliberately remain open. The local ViGEm installation and HID stack
must not be removed or damaged to simulate failures, and the visible cold-launch
loop must not run on an interactive desktop.

| Acceptance item | Status |
| --- | --- |
| No ViGEmBus: application opens and mapping stays usable | Not run; staged missing-bus behavior is covered by automated tests. |
| Broken/offline installation guidance | Not run end-to-end; the app no longer downloads or executes an installer and only opens official guidance on explicit user action. |
| Repeated controller hot-plug/unplug | Not run on a hardware rig. |
| Malformed/unsupported HID device | Not run with a fault-injection device. Per-device exception isolation is implemented. |
| Virtual target receives mapped button/axis state | Not re-run after final health-state changes. |
| 50 consecutive cold launches | Not run. `tools/ColdLaunchSmoke.ps1` refuses to start unless `-AllowDesktopWindows` is explicitly supplied in a dedicated VM/CI desktop. |

## Known upstream test-suite limitation

The focused suite passes. The complete cross-target test project currently reports
15 pre-existing infrastructure/UI failures under .NET 8 because legacy .NET
Framework assemblies and WPF resources are unavailable; 22 other tests pass. This
does not affect the successful .NET Framework 4.8 Release build, but should be
repaired before using the full suite as a release gate.
