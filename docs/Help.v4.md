# Xbox 360 Controller Emulator 4.x (uses ViGEmBus Virtual Gamepad Emulation Driver)

If you want `HELP` and have questions about installation or configuration, please go to:

- X360CE Home Page: <https://www.x360ce.com>
- NGEmu X360CE Forum: <https://www.ngemu.com/forums/x360ce.140/>
- Solutions and tutorials on Google: <https://www.google.com/search?q=x360ce>
- Solutions and tutorials on YouTube: <https://www.youtube.com/results?search_query=x360ce>
- ViGEm Homepage: <https://github.com/nefarius/ViGEmBus>
- HIDGuardian Homepage: <https://github.com/nefarius/HidGuardian>

## IMPORTANT Notes

1. There is no need to place `x360ce.exe` inside the game folder. You can keep a single copy at one place on your PC.

   For example: `C:\Program Files\x360ce\x360ce.exe`

2. **Do not close** `X360CE 4.x` **during the game**, just minimise it to reduce CPU use.
3. Make sure your game is set to use `XInput Devices`.

   For example: In "Tom Clancy's Ghost Recon Wildlands" you have to set:

   `OPTIONS > CONTROLLER > ENABLE CONTROLLER: ONLY GAMEPADS`

## Install and Use Instructions

1. Download latest `X360CE` (same file for 32-bit and 64-bit Windows).
2. Extract downloaded `ZIP` file and launch `x360ce.exe`.

## Installing ViGEmBus Virtual Gamepad Emulation Driver

`[Issues]` tab in `X360CE` will start blinking if `ViGEmBus Driver` is missing.

1. Select `[Issues]` tab and click on `[Install]` button to install `ViGEmBus Driver`.

## Adding DirectInput Device (Controller)

1. Connect your `DirectInput Device (controller)` to computer.
2. Select `[Controller 1]` tab and click on `[Add...]` button.
3. Select `controller` you want to add-map and click on `[OK]` button.
4. Enable `controller` by clicking on `[Enable # Mapped Device]` inside `[Controller 1]` tab.

## Configuring and Mapping Buttons and Axes

1. Select `[Controller 1]` tab → `[General]` tab.
2. Click on `[drop-down]` (drop-down menu with options will appear).
3. Map `button` or `axis` by selecting `[Record]` option and pressing `button` or moving `axis` on your `controller`.
4. Click `[Save All]` button (at top right corner of application) when done.
5. Minimise `X360CE` in order to reduce CPU use (program icon will be visible in tray).
6. Launch the game and see how it works.

## How to Install or Uninstall ViGEmBus Virtual Gamepad Emulation Driver

- Install: `[Options]` tab → `[Virtual Device]` tab → ViGEm Bus `[Install]` button.
- Uninstall: `[Options]` tab → `[Virtual Device]` tab → ViGEm Bus `[Uninstall]` button.

## How to Install or Uninstall HIDGuardian When Access to Keyboard and Mouse is Lost

**IMPORTANT !!! Please read before installing HIDGuardian !!!**

Purpose of `HIDGuardian` is to hide original controllers from games, so that only virtual controllers are visible. Install `HIDGuardian` only if original controller prevents virtual controller functioning properly in the game.

- Install: `[Options]` tab → `[HID Guardian]` tab → HID Guardian `[Install]` button.
- Uninstall: `[Options]` tab → `[HID Guardian]` tab → HID Guardian `[Uninstall]` button.

**DO NOT** attempt to remove `HIDGuardian` by simply deleting it from Windows OS `Device Manager`. This can result in **losing access** to your `Mouse` and `Keyboard` and you will be forced to follow Manual Uninstall Instructions below.

How to remove `HIDGuardian` if access to your Mouse and Keyboard is lost (GitHub):
<https://github.com/x360ce/x360ce/wiki/HID-Guardian>

## Problem: Application has failed to start because MSVCR100.dll was not found.

Reason: Microsoft Visual C++ 2010 Redistributable Package is missing.

Solution: Download and install Microsoft Visual C++ 2010 Redistributable Packages:

- (x86): <https://www.microsoft.com/en-us/download/details.aspx?id=5555>
- (x64): <https://www.microsoft.com/en-us/download/details.aspx?id=14632>

Note: You must install both packages on Windows 64-bit!

## Wheel doesn't work in the game, but it works inside x360ce Application.

Some games work only when controller is disguised as GamePad even if its Wheel. Try to:

1. Run `x360ce.exe`.
2. Select `[tab]` with your `Wheel Controller`.
3. Open `[Advanced]` tab page.
4. Set "Device Type" `[drop-down]` list value to: `[GamePad]`.
5. Click `[Save]` button.

## How to reduce wheel dead zone?

1. Run `x360ce.exe`.
2. Select `[tab]` with your `Wheel Controller`.
3. Open `[Advanced]` tab page.
4. Select `"Enabled (XInput, 80%)"` from `"AntiDeadZone"` `[drop-down]` to reduce dead zone by `80%`.
5. Click `[Save]` button.

Note: Some games have control issues when deadzone is reduced by `100%`.

## Gas and brake pedals are combined. How can I separate them?

Solution 1: If you have `Logitech wheel`:

1. Open `"Logitech Profiler"` Tool.
2. From menu open: Device → Game Controllers...
3. Select your controller and click `[Properties]` button.
4. Select `[Test]` tab and click `[Settings]` button.
5. Check `"[x] Combined (single axis - used for most games)"` option.
6. Click `[Close]` → `[OK]` → `[OK]` buttons.

Solution 2: If you can't separate pedals:

1. Open `X360CE`.
2. Set LEFT "Trigger" `[drop-down]` value to: Sliders → Half → `HSlider 1`.
3. Set RIGHT "Trigger" `[drop-down]` value to: Sliders → Inverted Half → `IHSlider 1`.
4. Test pedals.

## What are real life steering wheel degrees?

- 1080° (3.0 x 360°) - Heavy cars, trucks.
- 900° (2.5 x 360°) - Average road cars, sports cars.
- 720° (2.0 x 360°) - Drift cars. Multiple classes of Rally cars (group N).
- 540° (1.5 x 360°) - GT1 and 3 spec race cars, WRC Rally cars.
- 360° (1.0 x 360°) - Formula 1 cars.

## Expressions: working out a value from other controls

A mapping normally names one control. It can instead work one out, by starting the value with an equals sign. For example `=a1*abs(a1)` gives fine control near the centre of a stick and full speed at its edge, which is the response curve most games offer as an aim setting.

Values are scaled for you before the sum and fitted to whatever they drive afterwards, so you write plain numbers and never have to know a device's range. Going past the limit is safe: it simply reaches full travel.

## Controls you can read

A control is written as a letter and a number, the same way mappings are stored. Inside an expression the letter is always required, because a bare number means the number itself.

- `a1` Axis, -1 to 1. A stick or wheel that rests in the middle and moves both ways.
- `b1` Button, 0 or 1. 0 while released, 1 while held.
- `s1` Slider, 0 to 1. A throttle, pedal or dial that rests at one end.
- `x1` Half axis, 0 to 1. One half of an axis on its own, so the two halves can drive different things.
- `h1` Half slider, 0 to 1. One half of a slider on its own.
- `p1` D-pad, 0 to 1. The hat switch read as a direction rather than as separate buttons.
- `d1` D-pad button, 0 or 1. One direction of the hat switch.
- `now` Clock, counts up. Milliseconds since the program started. Divide by 1000 for seconds, or by 60000 for minutes. The only source that is not a control.

  Counting minutes: `=now/60000`

## Operators that combine two values

- `+` Add. `=a1+a2` - both controls move the same output.
- `-` Subtract. `=a1-a2` - one control opposes the other, as two pedals on one axis.
- `*` Multiply. `=a1*1.5` - makes a control travel further for the same movement.
- `/` Divide. `=a1/2` - makes a control travel less, for finer control.
- `%` Remainder. `=a1%0.25` - what is left after dividing, useful for repeating steps.
- `^` Power. `=a1^2` - raises to a power. `2^3^2` is `2^9`, and `-2^2` is `-4`.

## Operators that act on one value

- `-` Negate. `=-a1` - reverses the direction of a control.

Brackets group a part of the sum, as in `=(a1+a2)*0.5`, and a comma separates the values a function takes, as in `=min(a1,a2)`.

## Functions

- `abs` (1) - size of a value, ignoring its direction.
- `sign` (1) - direction alone: -1, 0 or 1.
- `sqrt` (1) - square root.
- `exp` (1) - the number e raised to this power.
- `floor` (1) - rounds down to a whole number.
- `ceil` (1) - rounds up to a whole number.
- `round` (1) - rounds to a whole number. A half goes to the even neighbour, so `round(2.5)` is 2 and `round(3.5)` is 4.
- `sin` (1) - sine. Angles are in degrees, so `sin(90)` is 1.
- `cos` (1) - cosine. Angles are in degrees.
- `tan` (1) - tangent. Angles are in degrees.
- `asin` (1) - the angle, in degrees, whose sine is this value.
- `acos` (1) - the angle, in degrees, whose cosine is this value.
- `atan` (1) - the angle, in degrees, whose tangent is this value.
- `min` (2) - the smaller of two values.
- `max` (2) - the larger of two values.
- `pow` (2) - the first value raised to the power of the second, the same as `^`.
- `log` (2) - logarithm of the first value in the base given by the second.
- `clamp` (3) - holds a value between a low and a high limit.
- `deadzone` (2) - Ignores the first part of a movement, then stretches what is left over the full travel.
- `antideadzone` (2) - Lifts any movement above a floor, so a game that ignores small values still notices. Nothing is lifted at rest.
- `curve` (2) - Bends the middle of the travel and leaves both ends alone, the same as the Sensitivity setting.

## Turning a tuned row into a formula

The dead zone, anti dead zone and sensitivity on a row are replaced by its formula, not applied on top of it, so what you write is what the game receives. Nothing is lost when you switch: the box is filled with the formula that produces exactly what those settings were already doing. Hover the fx button to see that formula before you switch.

## Buttons and logic

A button is 0 or 1, so ordinary arithmetic already does the work of and, or and not. There is nothing extra to learn.

- `=b1*b2` - and, true only while both are held.
- `=max(b1,b2)` - or, true while either is held.
- `=1-b1` - not, true while it is released.
- `=abs(b1-b2)` - either one but not both.
- `=a1*b1` - full speed only while the button is held.
- `=a1*(0.5+b1*0.5)` - half speed until the button is held.

## Examples

- `=a1*abs(a1)` - fine control near the centre, full speed at the edge.
- `=sign(a1)*sqrt(abs(a1))` - quick to respond, gentler at the edge.
- `=a1*1.5` - more sensitive everywhere.
- `=a1*0.5` - less sensitive, for aiming through a scope.
- `=a1*(0.5+a2*0.5)` - walk slowly, run when the trigger is held.
- `=a1-0.05` - correct a stick that drifts off centre.
- `=max(a1,0)` - one pedal axis split into the accelerator.
- `=-min(a1,0)` - the same axis, its braking half.
- `=a1-a2` - separate accelerator and brake onto one axis.

## Things worth knowing

- Anything that is not a real number, such as dividing by zero, becomes 0.
- A decimal point is always a dot, whatever language Windows is set to.
- Older versions of this program ignore expressions, so a configuration using one loses that mapping when it is opened in them.
