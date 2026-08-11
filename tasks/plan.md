# Implementation Plan: Windows 10/11 DirectInput-to-XInput Fork

## Overview

Build a maintained x360ce v4 fork that opens independently of controller, driver, runtime, or network health; converts generic DirectInput input through an observable mapping pipeline; and treats a virtual Xbox 360 controller as healthy only after a report is accepted by the bus. Work is split into small, independently verifiable slices, with P0 completed before P1/P2 expansion.

## Audit Findings

- The supplied workspace was legacy v3.2 and had no ViGEm architecture. The official v4 repository is now cloned under `x360ce-v4` and remains separate from the supplied files.
- Current VC++ checks scan uninstall display names with a `2015|2017|2019` regex. Windows on the reproduction machine has x86 and x64 v14.51.36247 installed with `Installed=1`; the x86 component is registered below `WOW6432Node`.
- `ViGEmClient.isVBusExists()` combines runtime detection, DLL extraction/loading, client allocation, bus connection, compatibility, and cached failures into one Boolean. A false runtime warning prevents a connection attempt to the externally installed, running bus.
- The bundled installer extracts an old driver and runs `devcon`, returns success without checking exit status, and can overwrite/remove installations it does not own.
- Before the WPF dispatcher runs, startup loads all settings, creates services/cloud state, checks app-data ACLs, and constructs a DirectInput COM object through a field initializer.
- DirectInput work later runs on a background thread, but enumeration has no deadline, the method named async is synchronous, failures are caught around only part of each device, and notification bursts are not debounced.
- Virtual target connection is tracked with local Boolean flags. Report submission is not surfaced as a health state.
- Exception files exist, but the ordinary rotating application log is not configured. Startup stages and dependency/device health are not recorded in a stable diagnostic format.

## Architecture Decisions

- Treat dependency discovery as data, not a Boolean. Runtime and virtual-bus probes return immutable result objects with source, version, stage, duration, and sanitized error data.
- Detect VC++ v14 from Microsoft’s component registry (`VC\\Runtimes\\{architecture}`), checking explicit 32-bit and 64-bit registry views and requiring `Installed=1` plus a valid v14-or-newer component version.
- Keep ViGEm support for compatible external installations, but remove automatic installation/overwrite behavior. Installation UI becomes guidance/troubleshooting; all mutations remain explicit and user initiated.
- Model ViGEm stages separately: package/device installed, service present, driver running, client DLL loaded, API connected, version compatible, target connected, report submitted.
- The UI dispatcher only constructs and paints the shell. Native enumeration, SetupAPI/service checks, network/update work, force-feedback discovery, and virtual target operations run on cancellable background operations with deadlines and per-device containment.
- Native calls that cannot be forcibly cancelled may outlive their deadline on a background worker, but they cannot block the UI or the rest of the device list. Repeated work is suppressed until the timed-out worker exits.
- Reuse the existing log writer with explicit rolling/retention settings, stable event names, a per-launch correlation ID, and allowlisted controller data (backend plus VID/PID only).
- Preserve .NET Framework 4.8 for the P0/P1 reliability milestone. Framework migration is a separate P2 decision after behavior is guarded by tests.

## Task List

### Phase 1: Dependency Detection (P0)

- [ ] Task 1: Add testable VC++ v14 component detection.
  - Acceptance: x86 and x64 `14.51.36247`, including x86 under `WOW6432Node`, report installed; `Installed=0`, missing keys, malformed versions, and access errors degrade without throwing.
  - Verification: focused MSTest cases fail before implementation and pass after; Visual Studio MSBuild succeeds.
  - Likely files: a runtime detector/result pair, the two runtime issue classes, `x360ceAppTest.cs`.

- [ ] Task 2: Introduce staged virtual-bus health detection.
  - Acceptance: installed, service-present, driver-running, API-connected, and version-incompatible are independently represented; a working external bus is healthy even if it was not installed by x360ce.
  - Verification: unit tests for status classification plus a read-only probe against the local running `ViGEmBus` service.
  - Dependencies: Task 1.

- [ ] Task 3: Replace Boolean issue/UI decisions with health results.
  - Acceptance: no Install action is shown for a working bus; missing or incompatible states show accurate guidance; probe exceptions never terminate startup.
  - Verification: focused tests for issue severity/text and manual status-panel check.
  - Dependencies: Task 2.

### Checkpoint: Dependency Detection

- [ ] v14.51 x86/x64 reproduction passes.
- [ ] External compatible ViGEmBus is recognized without mutation.
- [ ] Missing runtime or bus produces diagnostics, not process termination.

### Phase 2: Startup Isolation (P0)

- [ ] Task 4: Add rotating operational logging and startup stage timing.
  - Acceptance: each launch creates a bounded retained log with correlation ID, timestamps, stage durations, runtime source/version, ViGEm stages, and unhandled exceptions.
  - Verification: induced probe failure is discoverable from logs alone; retention test passes.

- [ ] Task 5: Make the pre-dispatcher path UI-only and non-device-aware.
  - Acceptance: no DirectInput object, SetupAPI/service probe, permission repair, network client, installer, or device initialization is created before the dispatcher and shell are available.
  - Verification: startup trace ordering test and manual cold launch with controllers disconnected.
  - Dependencies: Task 4.

- [ ] Task 6: Add bounded background startup operations.
  - Acceptance: dependency checks and initialization accept cancellation, enforce deadlines, publish partial results, and cannot prevent the mapping UI from opening.
  - Verification: fake hanging operations time out while a dispatcher heartbeat remains responsive.
  - Dependencies: Task 5.

- [ ] Task 7: Add a repeatable cold-launch smoke harness.
  - Acceptance: a readiness signal is emitted after the main UI can process dispatcher work; the harness performs 50 launches, checks responsiveness, closes cleanly, and reports timings/failures.
  - Verification: harness succeeds on the clean local configuration.
  - Dependencies: Task 6.

### Checkpoint: P0 Startup

- [ ] UI opens with no controllers, no ViGEm, unavailable network, and simulated slow/failing probes.
- [ ] No device/dependency operation runs on the UI thread.
- [ ] 50-launch smoke run has zero Not Responding results.

### Phase 3: Device and Hot-Plug Reliability (P1)

- [ ] Task 8: Debounce and serialize device notifications.
  - Acceptance: notification bursts coalesce; connect/disconnect work is cancellable; shutdown cannot race a refresh.
  - Verification: deterministic burst and shutdown-race tests.

- [ ] Task 9: Isolate DirectInput/HID enumeration failures per class and per device.
  - Acceptance: one malformed device or backend exception is logged and skipped/disabled without losing other devices; all native queries have deadlines.
  - Verification: fake device catalog containing good, malformed, throwing, and hanging devices returns all good devices within budget.
  - Dependencies: Task 8.

- [ ] Task 10: Isolate PID/force-feedback failures per device.
  - Acceptance: an effect/PID exception disables force feedback only for that device and polling continues.
  - Verification: failing FFB test double leaves input state updates operational.
  - Dependencies: Task 9.

### Phase 4: Virtual Controller Health (P1)

- [ ] Task 11: Verify target connection with an initial neutral report.
  - Acceptance: target creation is not successful until connect and report submission both succeed; failures disconnect/contain the target and preserve mapping UI.
  - Verification: fake client covers connect success/report failure, version mismatch, disconnect race, and successful submit.

- [ ] Task 12: Expose end-to-end diagnostic health.
  - Acceptance: diagnostics separately show Physical input OK, Mapping OK, Virtual bus OK, Virtual target connected, and State submit OK for each slot.
  - Verification: state-transition tests and manual UI inspection.
  - Dependencies: Task 11.

- [ ] Task 13: Add Copy diagnostics.
  - Acceptance: one action copies version/OS, startup timings, dependency health, per-slot health, and sanitized VID/PID/backend data without paths, device serials, credentials, or settings payloads.
  - Verification: clipboard content test and manual copy check.
  - Dependencies: Tasks 4 and 12.

### Checkpoint: P1 Reliability

- [ ] Repeated hot-plug/unplug does not hang or crash.
- [ ] Bad HID and PID/FFB devices do not cause global failure.
- [ ] A virtual Xbox 360 target accepts button and axis state.

### Phase 5: P2 Maintenance Pass

- [ ] Task 14: Profile polling and remove measured bottlenecks.
  - Acceptance: before/after frequency and latency data exists; only changes exceeding run-to-run noise are retained.
- [ ] Task 15: Restore force-feedback passthrough where guarded hardware paths support it.
- [ ] Task 16: Remove dormant automatic update/cloud/download paths from startup and eliminate certificate-bypass code.
- [ ] Task 17: Perform Windows 11 packaging, signing-readiness, dependency, and dead-code review.

### Checkpoint: Release Candidate

- [ ] Full MSTest suite passes under the supported test runner.
- [ ] Visual Studio MSBuild succeeds with no new warnings.
- [ ] Acceptance matrix and hardware-dependent results are documented honestly.
- [ ] Human review completed before merge/release.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| A native HID/DirectInput call never returns | Worker leak or repeated resource use | Deadline outside the UI thread, single-flight suppression, cancellation for subsequent work, explicit diagnostic state |
| ViGEm is end-of-life | No future upstream fixes | Support compatible external v1.x installations without auto-update/install; document status; keep bus adapter boundary replaceable |
| Legacy WPF/net48 UI is tightly coupled to loaded settings | Large startup refactor risk | Move one startup stage at a time behind readiness states and keep each increment buildable |
| Hardware is unavailable in automated tests | False confidence | Pure detector/lifecycle adapters, simulated failures, local read-only probes, and clearly separated manual hardware results |
| Existing build warnings and mixed-framework test project obscure regressions | Weak signal | Use Visual Studio Framework MSBuild as the baseline; record pre-existing warnings; add focused deterministic tests |

## Authoritative Sources

- Microsoft VC++ v14 downloads and binary-compatibility guidance: https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170
- Microsoft component registry/version guidance: https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files?view=msvc-170
- ViGEm end-of-life statement: https://docs.nefarius.at/projects/ViGEm/End-of-Life/
- ViGEm installation and health guidance: https://docs.nefarius.at/projects/ViGEm/How-to-Install/

## Open Questions

- None required for P0. The P2 choice between retaining ViGEm as a compatibility backend and adopting a future successor will be revisited only when a supported successor is actually available.
