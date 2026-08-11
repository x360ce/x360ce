# Windows 10/11 DirectInput-to-XInput Fork

## P0

- [x] Test and implement VC++ v14 component detection.
- [ ] Model ViGEm installed/service/running/client/compatibility stages.
- [ ] Drive issue and options UI from staged dependency health.
- [ ] Configure rotating operational logs and startup timings.
- [ ] Remove device/dependency work from the pre-dispatcher path.
- [ ] Add cancellable, deadline-bounded startup operations.
- [ ] Add and pass the 50-cold-launch smoke harness.

## P1

- [ ] Debounce hot-plug notifications and serialize refreshes.
- [ ] Isolate DirectInput/HID failures per device.
- [ ] Isolate PID/force-feedback failures per device.
- [ ] Require successful neutral report before target health is success.
- [ ] Expose five-stage input-to-submit health per controller slot.
- [ ] Add sanitized Copy diagnostics.

## P2

- [ ] Profile and improve polling with before/after measurements.
- [ ] Restore guarded force-feedback passthrough where feasible.
- [ ] Remove dormant startup network/update/download dependencies.
- [ ] Complete Windows 11 packaging and compatibility review.

## Verification Gates

- [x] VC++ v14.51.36247 x86 and x64 are recognized.
- [ ] Working external ViGEmBus is recognized without reinstall.
- [ ] No ViGEmBus still permits UI and mapping work.
- [ ] Broken/offline installation guidance cannot freeze the UI.
- [ ] Connected generic DirectInput controller does not delay UI readiness.
- [ ] Repeated hot-plug/unplug has no crash or hang.
- [ ] Malformed HID device does not break the device list.
- [ ] Virtual target accepts button and axis state.
- [ ] 50 consecutive cold launches remain responsive.
