# Xbox 360 Controller Emulator 3.x

Version 3 replaces the game's XInput library. Version 4 creates a virtual controller instead
and does not need to sit beside the game — see [Help.v4](Help.v4.md).

## How To Use - Installation

Run this program from the game executable directory.

## Uninstallation

Delete `x360ce.*` files, `xbox360cemu.ini` (if used) and `dinput8.dll` (if used) from the game executable directory.

## Problem: Game can't detect your controller

Reason: the game executable can look for the XInput DLL by different names.

Solution: create a copy of `xinput1_3.dll` with one of the names below, until you find one that works:

- `xinput1_2.dll`
- `xinput1_1.dll`
- `xinput9_1_0.dll`

## Problem: Application has failed to start because MSVCR100.dll was not found.

Reason: Microsoft Visual C++ 2010 Redistributable Package is missing.

Solution: Download and install this package from Microsoft:

- Microsoft Visual C++ 2010 Redistributable Package (x86):
  <http://www.microsoft.com/downloads/details.aspx?FamilyID=a7b7a05e-6de6-4d3a-a423-37bf0912db84>
- Microsoft Visual C++ 2010 Redistributable Package (x64):
  <http://www.microsoft.com/downloads/details.aspx?familyID=bd512d9e-43c8-4655-81bf-9350143d5867>

Note: You must install both packages on Windows 64-bit!

## Wheel doesn't work in the game, but it works inside x360ce Application.

Some games work only when the controller is disguised as a GamePad, even if it is a wheel. Try to:

1. Run `x360ce.exe`.
2. Select the tab with your `Wheel Controller`.
3. Open the `[Advanced]` tab page.
4. Set the "Device Type" drop-down list value to: `GamePad`.
5. Click the `[Save]` button.
6. Close the x360ce application and run the game.

## How to reduce wheel dead zone?

1. Run `x360ce.exe`.
2. Select the tab with your `Wheel Controller`.
3. Open the `[Advanced]` tab page.
4. Select `"Enabled (XInput, 80%)"` from the `"AntiDeadZone"` drop-down to reduce the dead zone by `80%`.
5. Click the `[Save]` button.
6. Close the x360ce application and run the game.

Note: Some games have control issues when the dead zone is reduced by `100%`.

## Gas and brake pedals are combined. How can I separate them?

Solution 1: If you have a `Logitech wheel`:

1. Open the `"Logitech Profiler"` tool.
2. From the menu open: Device → Game Controllers...
3. Select your controller and click the `[Properties]` button.
4. Select the `[Test]` tab and click the `[Settings]` button.
5. Check the `"[x] Combined (single axis - used for most games)"` option.
6. Click `[Close]`, `[OK]`, `[OK]` buttons.

Solution 2: If you can't separate pedals:

1. Open Xbox 360 Controller Emulator.
2. Set LEFT "Trigger" value to `"HSlider 1"` (Sliders → Half → HSlider 1).
3. Set RIGHT "Trigger" value to `"IHSlider 1"` (Sliders → Inverted Half → IHSlider 1).
4. Test pedals.

## What are real life steering wheel degrees?

- 1080° (3.0 x 360°) - Heavy cars, trucks.
- 900° (2.5 x 360°) - Average road cars, sports cars.
- 720° (2.0 x 360°) - Drift cars. Multiple classes of Rally cars (group N).
- 540° (1.5 x 360°) - GT1 and 3 spec race cars, WRC Rally cars.
- 360° (1.0 x 360°) - Formula 1 cars.

## What do HookMasks do?

Many games will work without any of the below set, but it is worth knowing them:

- **LL** - Many XACT games (the Xbox 360 / PC cross-platform games) require HookLL to run properly. As they do not load the XInput runtime directly, the `dinput8` wrapper is required to redirect back to the local x360ce binary instead of loading the default `xinput1/9_x` binary.
- **COM** - Games that support both DirectInput and XInput may end up displaying both controllers in game, resulting in ghosted input or being able to start a two player game using a single control (SF4/SSF4 and DMC, for example). HookCOM allows the game to mask the controller's DirectInput capabilities, which Microsoft themselves advise on TechNet. HookCOM is the method required in most cases of games not working.
- **DI** - Allows the wrapper to trick some games that detect the controller GUID via DirectInput. The first Assassin's Creed is one such title.
- **PIDVID** - Works in conjunction with `FakePID=` and `FakeVID=`. Without these it defaults to the wired Xbox 360 Controller, which are `0x28E` and `0x45E` respectively.
- **NAME** - Allows x360ce to return a different OEM name than that of the controller installed. In Assassin's Creed, for instance, it changes the name of the XInput device to `Xbox 360 Controller`. While this is generally not required for functionality, the game Mini Ninjas is known to check for "Xbox 360 Controller" in the registry and will not work if the name is anything else.
- **SA** - Enables the hooking of SetupAPI. Only the Beat Hazard titles are known to require it so far, so it should almost never be needed.
- **WT** - Enables the hooking of WinVerifyTrust. This is required for games which use WVT for process integrity checking. Only Gears of War is known to use it at this point, so it should almost never be needed.
