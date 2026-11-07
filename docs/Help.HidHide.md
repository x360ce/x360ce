# HID Hide

`HID Hide` hides original controllers from games, so that only virtual controllers are visible. Install it only if the original controller stops the virtual controller working properly in a game: the game sees two controllers, or every press arrives twice.

`HID Hide` is made by Nefarius, who also makes the virtual controller driver. It is a program of its own, installed and removed in Windows like any other. Its releases: <https://github.com/nefarius/HidHide/releases>.

## Installing and updating HID Hide

`[Options]` tab → `[HID Hide]` tab:

1. `[Install HID Hide]` asks GitHub for the latest release and downloads its setup.
2. `X360CE` checks that the setup is the file the release describes, by its size and, when GitHub publishes one, its SHA-256, and that Windows finds it signed by Nefarius Software Solutions e.U. A setup that fails a check is not run, and the box above the button says why.
3. Windows asks for administrator rights, then the setup opens. Follow it through to the end. The download is deleted afterwards.

Once `HID Hide` is installed, `[Check for Update]` asks GitHub for the latest release. When it is newer than the one installed, the button becomes `[Update HID Hide]`, which installs it the same way. Nothing is downloaded until a button is pressed.

The setup is made for 64-bit Windows 10 and 11. When the latest release has no setup for the computer's processor, the box says so. To download a setup yourself, open the link beside the buttons.

## Choosing what HID Hide hides

After installing, open `HID Hide` once with `[Open Configuration]`: it stays inactive until you do. There, add `x360ce.exe` to the application list, tick the controller under Devices, and make sure hiding is turned on.

`X360CE` can make those changes itself:

- `[Devices]` tab → a device's `[Hide]` box hides it from games, or shows it again. Hiding a device also adds `x360ce.exe` to the application list and turns hiding on.
- `[Devices]` tab → `[HID Hide...]` lists the gaming devices and the hidden ones, shows every device again, makes `HID Hide` hide exactly the devices whose `[Hide]` box is ticked, or opens `HID Hide`.
- `[Configure automatically]` on the `[HID Hide]` tab hides a device while it is mapped to a controller, and shows it again when `X360CE` closes.

Tick `[Hide]` before starting the game: a game already running keeps seeing the device until it is started again.

## The Issues tab

While a controller is mapped to a virtual one, the `[Issues]` tab says when `HID Hide` is missing, when hiding is off, when the controller is not hidden, or when `HID Hide` hides it from x360ce as well. While `HID Hide` is missing, its button installs it the same way `[Install HID Hide]` does; otherwise the button opens `HID Hide`'s own program.

## When the XBOX Synthetic Gamepad Bus fails

On some computers Microsoft's `XBOX Synthetic Gamepad Bus` device stops with Code 10 in `Device Manager` while `HID Hide` is installed. Its authors know of it and have not fixed it yet: <https://github.com/nefarius/HidHide/issues/223>. If that device fails, uninstall `HID Hide` in Windows `Settings` → `Apps`.

## HID Guardian

`HID Guardian` is the tool `HID Hide` replaced. Its author stopped it in 2023, and keyboards, mice and USB devices dropping out while it was installed were its known side effects. This version only removes it: `[Options]` tab → `[HID Guardian (obsolete)]` tab → `[Uninstall]`.

**DO NOT** remove `HID Guardian` by deleting it in Windows `Device Manager`. That can lose you your mouse and keyboard: [How to Uninstall HID Guardian When Access to Keyboard and Mouse is Lost](Help.HidGuardian.md#how-to-uninstall-hid-guardian-when-access-to-keyboard-and-mouse-is-lost).
