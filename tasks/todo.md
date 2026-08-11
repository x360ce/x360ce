# Windows 10/11 DirectInput-to-XInput Fork

## P0

- [x] Test and implement VC++ v14 component detection.
- [x] Model ViGEm installed/service/running/client/compatibility stages.
- [x] Drive issue and options UI from staged dependency health.
- [x] Configure rotating operational logs and startup timings.
- [x] Remove device/dependency work from the pre-dispatcher path.
- [x] Add cancellable, deadline-bounded startup operations.
- [ ] Add and pass the 50-cold-launch smoke harness.

## P1

- [x] Debounce hot-plug notifications and serialize refreshes.
- [x] Isolate DirectInput/HID failures per device.
- [x] Isolate PID/force-feedback failures per device.
- [x] Require successful neutral report before target health is success.
- [x] Expose five-stage input-to-submit health per controller slot.
- [x] Add sanitized Copy diagnostics.

## P2

- [x] Profile and improve polling with before/after measurements.
- [x] Restore guarded force-feedback passthrough where feasible.
- [x] Remove dormant startup network/update/download dependencies.
- [x] Complete Windows 11 manifest and compatibility review.

## Verification Gates

- [x] VC++ v14.51.36247 x86 and x64 are recognized.
- [x] Working external ViGEmBus is recognized without reinstall.
- [ ] No ViGEmBus still permits UI and mapping work.
- [ ] Broken/offline installation guidance cannot freeze the UI.
- [x] Connected generic DirectInput controller does not delay UI readiness.
- [ ] Repeated hot-plug/unplug has no crash or hang.
- [ ] Malformed HID device does not break the device list.
- [ ] Virtual target accepts button and axis state.
- [ ] 50 consecutive cold launches remain responsive.
