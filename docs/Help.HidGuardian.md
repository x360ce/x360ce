# HID Guardian

**IMPORTANT !!! Please read before installing HID Guardian !!!**

Purpose of `HID Guardian` is to hide original controllers from games, so that only virtual controllers are visible. Install `HID Guardian` only if original controller prevents virtual controller functioning properly in the game.

- Install: not supported by this version. The installer is disabled for safety, because a misconfigured HID filter driver can lock out keyboard and mouse. Only uninstall is available.
- Uninstall order: the HID class filter is removed first and checked, and the driver is removed only after the filter is confirmed gone. If the filter cannot be removed, the driver is left in place, because a filter naming a missing driver is what stops keyboard and mouse from working.
- Recovery: if HID devices stop working, run `HidGuardian_Remove.ps1` from an administrative command prompt, in safe mode if needed. The script is extracted to `C:\Program Files\ViGEm HidGuardian` and clears every HID Guardian registry entry, including the class filter.
- Uninstall: `[Options]` tab → `[HID Guardian (obsolete)]` tab → `[Uninstall]` button.

**DO NOT** attempt to remove `HID Guardian` by simply deleting it from Windows OS `Device Manager`. This can result in **losing access** to your `Mouse` and `Keyboard` and you will be forced to follow Manual Uninstall Instructions below.

## How to Uninstall HID Guardian When Access to Keyboard and Mouse is Lost

<https://github.com/x360ce/x360ce/wiki/HID-Guardian>
