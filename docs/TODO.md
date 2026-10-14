# TODO and Known issues

## Requested features

- Record controller actions over time, so a player can see their actions per minute.
- Hide the real controller from games without leaving the program. Today HID Hide does it from its own window.
- A controller button combination that turns emulation on and off, for when the keyboard is out of reach.
- Install and update through winget.
- Publish a nightly build as a GitHub pre-release, made by a GitHub Actions workflow.
- Publish `docs/` as a website (MkDocs Material on GitHub Pages).

## Known issues

- An inverted D-Pad button (`d-N`, `IPOVButton`) is read from settings but the engine never applies it. No menu offers it either.
- After Repair, Remove Leftover Pads, a reorder or a virtual controller made again, a game's steady rumble stays off until the game sends it again.
- A crash inside Microsoft's XInput driver ends the program.
- A removed stale virtual controller record keeps its device open until the program closes, one handle per record.
- A pad whose force feedback keeps failing on one motor plays neither motor.

## Settings to finish

A setting that cannot work is greyed out or read-only, with the reason. A setting nothing reads or saves may be removed. Settings only x360ce 3 uses are kept, so its settings in the cloud database stay as they are. This version writes no `x360ce.ini`; anything it must keep goes in its own settings files.

Force feedback:

1. When the same device is on two controller tabs, one tab's settings drive it, the spring included: the lowest tab with `[Enable]` ticked, or else the lowest tab. The spring's note says another tab drives it, but not which. Choose the rule, then name the tab on the page.
2. A pad that refuses an effect type could be retried with the matching "2" type. Needs a pad that refuses.
3. Grey out what a device cannot use: everything but `[Pass Through]` for a device without force feedback, and the right motor's strength and direction and `[Swap Motors]` for a one-motor device. The right motor's period still counts, because the two motors make one effect.
4. Grey out the Test motor sliders when nothing they send reaches a device. Where they go depends on the emulation type.

x360ce 3 settings:

5. `[Axis to D-Pad]` could work for virtual controllers. x360ce 3 turned the device's X and Y axes into the D-pad and stopped sending both thumbsticks while it was on. Its X looks reversed (`Native/x360ce/Controller.cpp`).
6. `Library` emulation makes this version do nothing for a game: it loads the library only to show states. Decide whether `[Games]` should still offer it. The architecture issue still checks `Library` games.
7. `[Pass Through ID]`, `[Combine this controller]` and `[Combine Into]` have no column in the settings, so they are lost on reload and not kept for x360ce 3 either. Add the columns or remove them.
8. `[Games]` tab: `[Hook Mask]`, `[XInput Files]`, `[DInput File]`, fake VID and PID, `[Timeout]`, `[XInput Path]` and the DInput file name are x360ce 3 only. Grey them out with the reason. `[Other Options]` holds them beside `[Emulation]`, which works, so it needs a new layout.
9. `[Options]` tab: `[Enable Logging]`, `[Enable Console]`, `[Debug Mode]`, `[Version]` and `[Include [Products]]` were `x360ce.ini` settings. Nothing reads or saves them. Remove them; their two groups need a new layout.
10. Remove the code left from `x360ce.ini`: `SettingsManager.SetPadSetting`, whose only caller is the unused `NewDeviceForm`, and the close-time check of `x360ce.tmp` against `x360ce.ini`.

Other settings:

11. Nothing reads `[Load Settings from Cloud]` or the hidden Remote controllers group. Make them work or remove them.
12. `[Auto Map]` is greyed out. Build it, so a device is mapped when a game starts (the call to `MainForm.AutoConfigure` in `DInputHelper.Step1.UpdateDevices.cs` is commented out), or remove it. `[Exclude Supplemental Devices]` and `[Exclude Virtual Devices]` are read only by Auto Map's code, so they go with it; until then, grey them out with the reason.
13. A plugin cannot say that one of its settings needs another. Add that to the plugin contract, so the Test Controller can grey out what does nothing.
