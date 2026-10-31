# Xbox 360 Controller Emulator 4.x (uses ViGEmBus Virtual Gamepad Emulation Driver)

If you want `HELP` and have questions about installation or configuration, please go to:

- X360CE Home Page: <https://www.x360ce.com>
- NGEmu X360CE Forum: <https://www.ngemu.com/forums/x360ce.140/>
- Solutions and tutorials on Google: <https://www.google.com/search?q=x360ce>
- Solutions and tutorials on YouTube: <https://www.youtube.com/results?search_query=x360ce>
- ViGEm Homepage: <https://github.com/nefarius/ViGEmBus>
- HidHide Homepage: <https://github.com/nefarius/HidHide>

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
5. The `[Enabled]` box on a device's row in the list leaves the device out of the game without removing it. The box in front of a device on the `[Devices]` page leaves it out of every game, and greys its rows' `[Enabled]` box. An unticked device is not read, not held and sent no force feedback, so another program can use it. A Pass through that names the XInput place shown for it sends it nothing either. x360ce no longer hides it or asks for it to be hidden. A device you hid in HID Hide stays hidden until you untick it there. The tab's pages no longer show it live, and a centering spring `[Auto]` run on it stops.

An Xbox controller works in x360ce but not in the game? Windows gives DirectInput no background access to Xbox controllers, so through DirectInput x360ce reads one only while its own window is in front. For an Xbox One controller, map its `Raw Input` row instead: see DirectInput and Raw Input below. `[Add...]` asks when you pick an Xbox controller's `DirectInput` row, and for one already mapped the `[Issues]` tab offers `[Use Raw Input]`, which moves it with its settings. An Xbox controller is an XInput device already, so most games read it directly without x360ce.

A wheel and its pedals work in x360ce but not in a Steam game? Steam Input can take a wheel over, and then the game does not see what x360ce provides. In the Steam library open the game's `Properties` → `Controller` and choose `Disable Steam Input`.

## DirectInput and Raw Input

x360ce reads a controller in two ways and lists it once for each, so a controller can appear twice on the `[Devices]` page and in the list `[Add...]` opens. Both lists show the DirectInput rows first, tinted blue, then the Raw Input rows, teal, then plugin devices, violet. The `[Source]` column says how each row is read.

- `Raw Input` reads the controller while a game has the focus. Choose it for an Xbox One controller.
- Vibration and wheel forces reach the controller from either row. Raw Input only reads, so a `Raw Input` row's go through the same controller's `DirectInput` side. An Xbox controller's DirectInput side takes none: `[Pass Through]` on the `[Force Feedback]` page reaches its motors from either row.

Both rows of a controller read every button and axis into the same place, so a mapping made on one works on the other. Adding one to a controller tab where the other is mapped starts it with the other's settings. Map one of them: if both are ticked in the tab's list, both drive the controller, so untick the `[Enabled]` box of the one you do not use.

Every press, hat tap and stick flick reaches the controller, even one shorter than the time between two reads of the device. A worn button whose contacts chatter can then send extra presses; for it, turn off `[Read Every Change]` on the `[Options]` tab.

A few devices do not tell Windows what their controls are, so Windows does not list them as game controllers and they have no `DirectInput` row. x360ce reads the ones it knows through Raw Input, without the maker's software: the Logitech G13's stick is the X and Y axes and its keys are `Button 1` to `Button 40`.

## Plugin devices

A plugin adds a device x360ce cannot read by itself, or one that exists only in code; its rows say `Plugin` in the `Source` column. The Test Controller, a pretend controller that moves by itself for trying a mapping without hardware, is a plugin inside x360ce: `[Devices]` tab → `[Add Plugin Device]` adds one. So is the Remote MCP Controller, whose controls a program or an AI assistant sets over MCP, through x360ce's own AI assistant access or an MCP server of its own that its tab opens. A plugin device has a tab of its own on the controller page it is mapped to, with a page for each part: the plugin's own control, `[Properties]`, its settings in a grid like Visual Studio's Properties window, and `[MCP Server]` for a device with an MCP server of its own. Other plugins load from the `Plugins` folder of the x360ce data folder, each from a folder of its own; `[Options]` tab → `[Plugins]` lists every plugin found and why any did not load. Writing a plugin, one `.cs` file: [Help.Plugins](Help.Plugins.md).

## One device on several controller tabs

A device can drive up to four emulated controllers at once.

1. Select another `[Controller #]` tab, click `[Add...]` and choose a device that is already on another tab.
2. `[Move here]` takes it off every other tab. A tab left with no device is switched off.
3. `[Keep there too]` puts it on this tab as well. This tab starts with the settings of the lowest-numbered tab it is on, force feedback included; after that each tab keeps its own. Do the same on more tabs, up to `[Controller 4]`.
4. `[Cancel]` changes nothing.
5. On each tab, `[Force Feedback]` → `[Enable]` says whether that tab's game rumbles the device. On for one tab: that tab's rumble. On for several: the strongest of each motor.
6. The lowest enabled controller tab the device is on shapes its force: effect type, strengths, motors and centering spring, whatever its own `[Enable]` says. On the other tabs those settings are greyed out; `[Enable]`, `[Pass Through]` and the Test sliders stay live.
7. While the device is on another tab too, the `[Force Feedback]` page title names the other tabs, for example `Shared with Controller 2` or `Shared with Controllers 1, 3`, and the `[Force Source]` box says which tab shapes the force and which tabs' force the device plays. A controller read through Raw Input on one tab and DirectInput on another counts as on both.

## Configuring and Mapping Buttons and Axes

1. Select `[Controller 1]` tab → `[General]` tab.
2. Click on `[drop-down]` (drop-down menu with options will appear).
3. Map `button` or `axis` by selecting `[Record]` option and pressing `button` or moving `axis` on your `controller`.

   A switch, stick or pedal recorded onto a button presses it where you moved it to, and the status bar says which way that is. If the button is held the wrong way round, click the box and choose `[Invert]`. How far the control must move is the button's press point on the `[Buttons]` tab; on the way back the button lets go a little before that point, so a switch that wobbles does not flicker.

   If a stick rests off centre, leave it at rest, click its box and choose `[Recentre]`. Where it rests now then reads as the middle; the box shows the formula that does it, such as `=a1-0.05`.
4. Or use the `[Input]` column on the left: it lights every button, axis, slider and POV of your device as you use it, with its reading, before anything is mapped. Click a mapping box, then click a chip to put it there, or pick the chip up and drop it on a box; the boxes that can take it turn green while you hold it.
5. Click `[Save All]` button (at top right corner of application) when done.
6. Minimise `X360CE` in order to reduce CPU use (program icon will be visible in tray).
7. Launch the game and see how it works.

Buttons are numbered from 1 in the mapping boxes, the `[Input]` column and the `[Direct Input]` page's list of pressed buttons. Windows names them from 0, and the `[Direct Input]` page's list of the device's controls shows those names: its `[Maps as]` column says what a mapping box calls each one, so the button Windows calls `Button 0` is `Button 1` in a box.

## D-Pad buttons that report as a stick

Some pads report their D-Pad as a stick: their X and Y axes jump to the ends while a direction is held. `[Controller 1]` tab → `[D-Pad]` tab → `[Axis To D-Pad]` → `[Enabled]` turns those axes into the D-Pad: right and left from X, down and up from Y, two at once on a diagonal. The stick's own mappings stay as they are, so clear the left `[Stick Axis X]` and `[Stick Axis Y]` boxes on the `[General]` tab if the game should not see the stick move as well.

- `[Dead Zone]` is how far an axis must move from the centre before a direction is pressed. `100 %` presses close to the end, where such a pad puts the axis even when it falls a little short. For a real stick used as a D-Pad, try `50 %`.
- `[Offset]` moves the centre towards the right and the bottom, for a device that does not rest in the middle.
- The chart beside them is the X and Y square: the shaded sides are where a direction is pressed, the dot is the device's reading, and a side it presses is lit.

## A mouse as a stick

A mouse can move a stick: to steer, fly, aim or turn a camera. Map the mouse's axes in the stick's boxes on the `[General]` tab as for any device: `Axis 1` is across, `Axis 2` is up and down. Then `[Controller 1]` tab → `[Mouse]` tab sets how the mouse moves each stick. It works for any control that reports how far it moved rather than where it is, such as a trackball or a mouse wheel.

`[Style]`:

- `Position`: the stick follows how far the mouse has moved, and stays there. For steering and flying. This is how a mouse moved a stick before the page existed, and the page starts with it.
- `Speed`: the stick follows how fast the mouse moves, and returns to the centre when it stops. For a camera or aiming.
- `Screen`: the stick follows where the pointer is on the screen, measured from the middle of the screen. For a game that shows a pointer, as a mouse yoke.

The other settings:

- `[Sensitivity across]` and `[Sensitivity up and down]`, in percent. At `100`, the whole stick is 2,048 counts of movement in `Position`, 4,096 counts a second in `Speed`, and half the screen's height in `Screen`. A mouse with a high DPI counts more for the same movement, so it needs less.
- `[Smoothing]`, in milliseconds: how long the stick takes to follow a change in `Speed` and `Screen`, which evens out the mouse's uneven reports. `30` is barely felt.
- `[Back to centre]`, in milliseconds: how long the stick takes to go back to the centre. In `Position`, `0` leaves it where the mouse left it, and a value makes a stick that centres itself. In `Speed`, it slows the return after the mouse stops.
- `[Active]`: always, while a button is held, or turned on and off by a button. A stick turned on by a button starts from the centre each time, and `Screen` takes the pointer's place at that moment as the centre. Let go, the stick returns to the centre, or stays where it was if you choose that.
- `[Button]`: the mouse's button that `[Active]` uses: button 1 is the left button, 2 the right, 3 the middle.

The stick's own `[Dead Zone]`, `[Anti-Dead Zone]` and `[Sensitivity]` on the stick's tab shape the result as for any device.

For example, to steer a car as in GTA, with a camera when not steering: on the left stick, `Position`, active while button 1 is held, then centre; on the right stick, `Speed`, always.

## Checking for a newer version

The program can look for a newer release and install it, but only when asked to.

1. `[Options]` tab → `[Update]` → `[Check now]` looks now, downloads the newest release, checks its size, hash, digital signature and version, and asks before restarting if a game is using the emulated controllers. Each step is written on the page.
2. `[Check for updates on startup]` is off by default. When on, the program looks once a day, at a random moment within the first hour after it starts, and shows "version X available" at the top of the window. Nothing is installed until you click `[Check now]`.
3. The look is one request to `api.github.com` for the list of the newest releases. It sends the program's name and version, and at start-up the tag of the list last seen, and nothing else about you or the computer.

## Turning emulation on and off

The emulated controllers can be switched off without closing the program, so a game that reads both a wheel and a real gamepad can be swapped between them.

1. `[Options]` tab → `[General]` → `[Enable XInput]` turns them on or off. The right half of each controller tab light turns grey while they are off.
2. The same switch is in the tray menu: right-click the `X360CE` icon in the notification area.
3. `[Options]` tab → `[General]` → `[Hotkeys]`: tick `[Emulation]` to turn the hotkey on. The field beside it holds `Ctrl + Alt + X`; click it and press other keys to change them. The keys need `Ctrl`, `Alt` or `Shift` in them, `Backspace` clears the field, and red means another program already holds those keys.
4. Tick `[Controller]` below it to do the same with buttons held together on a controller, `Back + Start` to begin with. To change them, click its field, hold two buttons or more on a controller mapped to the current game, then let go. They work while the emulated controllers are off too: x360ce goes on reading the mapped devices. The game also sees the buttons while the controllers are on.
5. Pressing the hotkey or the buttons shows a short note at the top of the screen, over the game, saying whether the emulated controllers are on or off. Untick `[Show a note on the screen when pressed]` to have the tray notification instead, which Windows hides while a full-screen game runs.
6. The same kind of note names a device mapped to the current game when it disconnects or connects again, so a pad that drops out mid-game is not mistaken for a game that stopped answering. Untick `[Show a note when a device disconnects]` to turn it off.

## Saving and loading a preset as a file

1. `[Controller #]` tab → `[Save Preset...]` writes the controller's settings to an `XML` file.
2. `[Load Preset...]` → `[Open File...]` loads such a file onto the selected controller.
3. `[Copy Preset]` and `[Paste Preset]` carry the same settings through the clipboard.

## What the light on each controller tab means

The light answers two questions at once, so it is drawn in two halves. The **left half is your
device**, the **right half is the emulated controller** a game reads. Both green means it is
working.

Each tab owns one of the four XInput places, and the place never moves: `[Controller 1]`
is XInput 1. Three things can be in it - the controller this program makes, a real
controller you plugged in, or nothing.

| Half | Colour | Meaning |
| --- | --- | --- |
| Left (your device) | Green | A device mapped here is connected. |
| Left | Amber | A device is mapped here but is not connected. |
| Left | Grey | Nothing is mapped to this controller. |
| Right (emulated controller) | Green | The controller for this tab is in this tab's place. Nothing to do. |
| Right | Amber | Something else holds this tab's place, so no controller was made for it. |
| Right | Orange | The controller for this tab exists, in a different place, so a game reads it as a different player. Put them in the order you want on the `[Devices]` page. |
| Right | Red | No controller was made and nothing reaches a game. |
| Right | Grey | Nothing is set up for this controller. |
| Right | Blue | `[Show XInput State]` is off, so nothing was checked. |

The right half is mixed rather than picked from that list: the more that is wrong at once,
the further it moves towards red. A tab with two faults therefore looks worse than a tab
with one, so the tab most in need of attention is the one that stands out.

Blue is not a fault. `[Show XInput State]` only decides where the numbers shown on screen
come from - read back from the emulated controller, or worked out from your own device.
The emulated controller works either way, so with that setting off the program cannot say
whether one exists and says so rather than guessing.

Hover over a tab to read the same thing in words, including anything the virtual bus
reported about that controller.

When something else holds a tab's place, the words say what it is: a real controller,
another of this program's own virtual controllers, or a virtual controller this program did
not make, such as one from DS4Windows or one left behind, which the `[Devices]` page lists
as Leftover. Until its place is free, the `[Devices]` page lists the tab's own controller as
`Virtual N (waiting)`. `[Auto-Order]` on the `[Devices]` page moves a real controller or another of
this program's own virtual controllers out of the way; a Leftover has to be removed with
`[Remove Leftover Pads]` on the same page instead. While a tab waits, its controller picture
stays grey: the controller in its place is not the tab's own.

The program notes the place each of its own controllers lands in, and checks that note against
the product number XInput reports for each place. A note on a place that holds another kind of
controller is moved to where its controller is, or, when that cannot be told, the lists say
`Virtual (place not known)`.

## How to Install or Uninstall ViGEmBus Virtual Gamepad Emulation Driver

- Install: `[Options]` tab → `[Virtual Device]` tab → ViGEm Bus `[Install]` button.
- Uninstall: `[Options]` tab → `[Virtual Device]` tab → ViGEm Bus `[Uninstall]` button.
- Repair: when games do not detect the controllers although the driver is installed, the `[Issues]` tab shows "Virtual driver is installed but not working" and says why. Click its `[Repair]` button and allow the Administrator prompt. On Windows 10 and later the driver's own setup opens: choose `Repair` there. Once the driver is installed, the ViGEm Bus button on the `[Virtual Device]` tab also reads `[Repair]`. When the driver refused only a controller's vibration, the row reads "Virtual controller has no vibration" instead; `[Repair]` may restore it.
- If the message comes back after a repair, close and reopen `X360CE` and click `[Repair]` again. Restart Windows if it still comes back.

## Hiding the real controller from games (HID Hide)

Duplicated controller, or the game sees two controllers? A game that sees both the real controller and the emulated one gets every press twice. `HID Hide` hides the real one: `[Options]` tab → `[HID Hide]` tab shows whether it is installed, `[Download HID Hide...]` opens its download page and `[Open Configuration]` opens its own program. There, add `x360ce.exe` to the application list, tick the controller under Devices, and make sure hiding is turned on.

`X360CE` can make those changes itself once `HID Hide` is installed:

- `[Devices]` tab → a device's `[Hide]` box hides it from games, or shows it again. Hiding a device also adds `x360ce.exe` to the application list and turns hiding on.
- `[Devices]` tab → `[HID Hide...]` lists the gaming devices and the hidden ones, shows every device again, makes `HID Hide` hide exactly the devices whose `[Hide]` box is ticked, or opens `HID Hide`.
- `[Options]` tab → `[HID Hide]` tab → `[Configure automatically]` hides a device while it is mapped to a controller, and shows it again when `X360CE` closes.

Tick `[Hide]` before starting the game: a game already running keeps seeing the device until it is started again.

While a controller is mapped to a virtual one, the `[Issues]` tab says when `HID Hide` is missing, when hiding is off, when the controller is not hidden, or when `HID Hide` hides it from x360ce as well. Its button opens the download page or `HID Hide`'s own program.

`HID Guardian` is the tool `HID Hide` replaced. Its author stopped it in 2023, and keyboards, mice and USB devices dropping out while it was installed were its known side effects. This version only removes it: `[Options]` tab → `[HID Guardian (obsolete)]` tab → `[Uninstall]`.

**DO NOT** attempt to remove `HIDGuardian` by simply deleting it from Windows OS `Device Manager`. This can result in **losing access** to your `Mouse` and `Keyboard` and you will be forced to follow Manual Uninstall Instructions below.

How to remove `HIDGuardian` if access to your Mouse and Keyboard is lost:
[How to Uninstall HID Guardian When Access to Keyboard and Mouse is Lost](Help.HidGuardian.md#how-to-uninstall-hid-guardian-when-access-to-keyboard-and-mouse-is-lost)

## Games with anti-cheat

With the usual emulation, `Virtual`, `X360CE 4.x` puts nothing into the game or its folder: the game sees an Xbox 360 controller made by the `ViGEmBus` driver. The `Library` emulation, a game's `[Emulation]` setting on the `[Games]` tab, writes XInput files into the game's folder instead, and anti-cheat and copy protection such as Denuvo can refuse to run with them. Keep such games on `Virtual`.

Some anti-cheat systems refuse virtual controllers too, and some refuse to start a game while a program that makes them is running. If a game with anti-cheat ignores the emulated controller, or will not start while `X360CE` runs, that game does not allow virtual controllers, and `X360CE` does not try to get around it. Before playing online, check the game's rules: some ban controller remapping programs.

## Recording a problem for a bug report

Some problems a screenshot cannot show: a stick that drifts, vibration on the wrong motor, a game that will not start, a window that stops answering. Record the problem and attach the file to your issue on GitHub:

1. Open the `[Issues]` tab. Under `Record for a bug report`, keep every box ticked, unless you are asked for less.
2. Press `[Start Recording]`.
3. Make the problem happen: start the game, move the controller, or wait for the freeze.
4. Press `[Stop Recording]`, then `[Open Folder]`.
5. Open the file and read it. Then drag it into your issue on GitHub.

The file is plain text, made to be posted. It leaves out your user name, your PC's name, folders, web addresses and keys. Devices are written as `D1`, `D2` and so on, with their product name and their vendor and product ID. Controllers are `C1` to `C4`. The keys and buttons of keyboards and mice are never recorded. A recording stops by itself after 30 minutes or 20 MB. It is saved as it runs, so it holds everything up to the moment the program froze or was closed.

What each box adds:

- `Settings and devices`: the options, the game's settings, each controller's mapping, and every device.
- `Device input`: what each device reports as it changes: buttons, axes, sliders and hats, a device going offline, and each failed read.
- `Controller output`: what each controller sends to the game, whether its virtual controller is fed, and what XInput reads back from its place.
- `Force feedback`: the vibration each game asks for, the force sent to each device, and whether each device is held exclusively.
- `Engine timing`: how fast the engine runs each second, each device call that takes long, and a call that does not return.
- `Window and errors`: whether the window answers, which program is in front, the window objects and handles `X360CE` holds, and its errors.

The layers side by side show where a problem starts. A value already wrong in the device input comes from the device or its driver. A value right there but wrong in the controller output comes from the settings. A device call that does not return names the driver that holds everything up.

If `X360CE` closes by itself or freezes completely, a memory dump shows more: see [Help.MemoryDump](Help.MemoryDump.md).

## Problem: VCRUNTIME140.dll or MSVCP140.dll was not found.

Reason: Microsoft Visual C++ v14 Redistributable is missing.

Solution: `[Issues]` tab → `[Download and Install]`, or install the latest Microsoft Visual C++ v14 Redistributable (Visual Studio 2017–2026) from Microsoft:

- (x86): <https://aka.ms/vc14/vc_redist.x86.exe>
- (x64): <https://aka.ms/vc14/vc_redist.x64.exe>

Note: You must install both packages on Windows 64-bit!

`[Download and Install]` asks for administrator permission, runs Microsoft's installer and waits for it. The `[Issues]` tab then says how it went: Windows needs a restart, the installation was cancelled, another installation is running, or the same or a newer version is already registered. In that last case remove every Microsoft Visual C++ v14 Redistributable in Windows Settings → Apps, restart Windows and install again.

If the installer fails, see Microsoft's troubleshooting guide: <https://learn.microsoft.com/cpp/windows/troubleshoot-vc-redistributable-installation-issues>

## Wheel doesn't work in the game, but it works inside x360ce Application.

With the usual emulation, `Virtual`, a game reads an Xbox 360 gamepad whatever the device behind it, so a wheel needs no disguise. Check that both halves of the light on the wheel's controller tab are green, as described in "What the light on each controller tab means", and choose an Xbox or XInput controller in the game's own settings.

`[Device Sub Type]` on the `[Advanced]` page told a game what kind of device it read, through x360ce 3's XInput library. This version does not write the `x360ce.ini` file that library reads, so the page is greyed out. Its settings are kept, so x360ce 3's settings in the cloud stay as they are.

## How to reduce wheel dead zone?

1. Select the `[tab]` with your wheel.
2. Open the `[Left Thumb]` page. Steering is usually mapped to Left Thumb Axis X on the `[General]` page; if it is mapped elsewhere, open that axis's page.
3. Lower `[Dead zone]` so the wheel answers sooner. To skip a dead zone the game applies of its own, raise `[Anti-dead zone]`, or pick a ready-made setting such as `[80% Controller Anti-DeadZone]`.
4. Click `[Save All]`.

Note: Some games have control issues when the anti-dead zone is `100%`.

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

## Pedals on the two triggers move one axis in the game

A game that reads controllers through DirectInput, as many older racing games do, sees the emulated Xbox 360 controller's two triggers as one axis, `Z`: one trigger moves it one way from the middle and the other trigger the other way. Microsoft's Xbox 360 driver presents them like that, and x360ce cannot change it. A game that reads XInput sees two separate triggers.

1. If the game has an XInput or Xbox controller option, choose it.
2. If it has none, map the pedals to two stick axes instead of the triggers, for example the accelerator to Right Thumb Axis Y and the brake to Right Thumb Axis X, and choose those axes in the game.
3. If the game then uses only half of a pedal's travel, it counts from the stick's middle. Put the whole pedal into that half with a formula in the stick's box: `=(a3+1)/2` for a pedal on `Axis 3`, or `=s1` for one on `Slider 1`. If the pedal reads full while released, use `=(1-a3)/2`. See "Expressions" below.

Do not calibrate the emulated controller in Windows or with a tool such as DXTweak instead. Windows keeps that calibration and applies it in every DirectInput game; if the readings went wrong after one, set the tool back to its defaults.

## What are real life steering wheel degrees?

- 1080° (3.0 x 360°) - Heavy cars, trucks.
- 900° (2.5 x 360°) - Average road cars, sports cars.
- 720° (2.0 x 360°) - Drift cars. Multiple classes of Rally cars (group N).
- 540° (1.5 x 360°) - GT1 and 3 spec race cars, WRC Rally cars.
- 360° (1.0 x 360°) - Formula 1 cars.

## Steering a shorter range than the wheel turns

`[Wheel range]` on the `[Wheel]` page sets the range of a Logitech G25, G27, G29 or Driving Force GT. Any other wheel turns the range its own software sets, and the page says so: set the range there.

To leave the wheel at its range and steer as a shorter one, put a formula in the box that steers, usually `Left Thumb Axis X`: `=a1*900/270` steers as 270° on a wheel set to 900°, so the game reaches full lock at 135° each way. Past that the wheel turns on and nothing more happens. Write the wheel's range in place of 900, the range you want in place of 270, and keep the axis the box held: `=a1*900/270` for `Axis 1`. See "Expressions" below.

## Wheel centering spring

A game made for a gamepad sends only rumble, so a wheel turns freely between bumps. On the wheel's controller tab, open the `[Wheel]` page and tick `[Centering spring]` to hold it at its centre. Take your hands off the wheel and press `[Auto]`. It measures the wheel's friction, the least force that turns it, and sets `[Friction]` a tenth above it.

A few degrees from the centre the spring reaches `[Friction]` and never pushes less beyond, so the wheel comes home from any angle and settles without shaking. `[Strength]` adds the car's steering on top. Friction is kept with the wheel, for every car. It starts at 20 %, which brings even a geared or belt wheel, such as a Logitech G27, home from its stops; Auto measures about 30 % on one. A direct drive wheel turns freely and needs little: lower `[Friction]` by hand when the wheel comes home harder than you like.

The car's part pulls like a car's steering: its force grows from the centre to a peak, then eases off towards the stop. Choose a car from the `[Car]` list, or set `[Centre]`, `[Peak]`, `[Lightening]`, `[Damping]` and `[Speed]` yourself. A car also sets `[Wheel range]` to the range its name says: every car on the list is a road car, so 900°. The chart beside them shows the force at every angle, with Friction as a grey band the curve stays above except near the centre, and its dot follows the wheel as it turns.

The spring works without `[Enable]` on the `[Force Feedback]` page, which passes on the game's vibration. While the spring is off, a Logitech wheel keeps its own centering, under the game's vibration too: the G25, G27, G29, Driving Force, Driving Force Pro, Driving Force GT, MOMO and Speed Force Wireless. Another wheel gets its own centering back once X360CE sends it no force. The spring works only while the controller tab is switched on for the current game, and the wheel is ticked in the tab's list and on the `[Devices]` page. When the wheel is on two controller tabs, the settings of the lowest enabled tab drive it, whatever its own `[Enable]` says. The page says so when the spring does nothing.

The wiki page "How a real car centres its steering wheel" says what each setting copies from a real car, and what each car on the list is based on.

## Flying an RC transmitter in a game

A radio control transmitter for drones and planes (EdgeTX, OpenTX, RadioMaster, Jumper, DJI and
others) shows up as a joystick when it is plugged in by USB. x360ce turns it into an Xbox
controller, so a game that reads only a gamepad can be flown with the real sticks.

1. Plug the radio in by USB and choose its joystick mode. EdgeTX asks when the cable goes in: choose `USB Joystick (HID)`.
2. Add the radio on the `[Controller 1]` tab, as in "Adding DirectInput Device (Controller)" above.
3. Start from the preset. Open [x360ce-v4-rc-transmitter.xml](.attachments/x360ce-v4-rc-transmitter.xml), save it with GitHub's `Download raw file` button, then choose `[Load Preset...]` → `[Open File...]` and open it. It puts throttle and yaw on the left stick (Axis 3 up and down, Axis 4 left and right), pitch and roll on the right stick (Axis 2 and Axis 1), and the arm switch (Slider 1) on the left bumper.
4. Radios send their channels in different orders, so the preset is a starting point. Move each stick and switch and watch the `[Input]` column. When a box names the wrong control, click it, choose `[Record]` and move the right stick. When a stick works backwards, click its box and choose `[Invert]`.
5. The arm switch works as a button. Record it by flipping the switch into the armed position: the status bar then says which position presses the button. If the bumper is held while you are disarmed, click the box and choose `[Invert]`. The preset presses the bumper only in the last quarter of the switch's travel, so the middle of a three-position switch leaves it released; that point is the Left Bumper's press point on the `[Buttons]` tab.
6. Hide the radio from the game with HID Hide, as in "Hiding the real controller from games (HID Hide)", so the game reads only the emulated controller.
7. Click `[Save All]`, then keep x360ce running while you fly. Minimise it rather than closing it: the game sees the controller only while x360ce runs.

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
- `now` Clock, counts up. Milliseconds since the program started. Divide by 1000 for seconds, or by 60000 for minutes. The only source that is not a control. It counts from 0 again every four hours, which every second, minute and hour divide evenly, so it stays exact to the millisecond however long the program runs.

  Counting minutes: `=now/60000`

A control of any other device mapped for the game: put its controller and device in front, as the controller tab's `AHK` column shows them while AutoHotkey scripts are allowed: `d1` is the tab's first device row. `c1.d2.b1` is button 1 of device 2 on Controller 1, whichever mapping the formula is in, so one controller can use another device's buttons, such as a wheel's on the pedals' mapping: `=c1.d1.b5`. On that device's own mapping, `=c1.d2.b1` reads what `=b1` reads. A device that is not connected, or not ticked, reads as at rest.

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
- `=b1*floor(now%100/50)` - turbo: presses ten times a second while the button is held. `[Turbo]` in the menu of a box mapped to a button writes it. For five a second, write `200` and `100` in place of `100` and `50`.
- `=b1*(1-b5)` in the `A` box and `=b1*b5` in the `Y` box - a shift layer: button 1 presses A, or Y while button 5 is held. Empty the box that holds `Button 5`, or the shift button presses that box's button as well.

## Examples

- `=a1*abs(a1)` - fine control near the centre, full speed at the edge.
- `=sign(a1)*sqrt(abs(a1))` - quick to respond, gentler at the edge.
- `=a1*1.5` - more sensitive everywhere.
- `=a1*0.5` - less sensitive, for aiming through a scope.
- `=a1*(0.5+a2*0.5)` - walk slowly, run when the trigger is held.
- `=a1-0.05` - correct a stick that drifts off centre. `[Recentre]` in the box's menu measures the offset and writes it.
- `=a1*sqrt(1-a2^2/2)` - round the corners of a stick that reaches the corners of a square, so a full diagonal is no stronger than straight ahead. Use it for Stick X; a1 and a2 are the stick's own two axes.
- `=-a2*sqrt(1-a1^2/2)` - the same for Stick Y. Most devices report up as the low end of Axis 2, so Stick Y holds `IAxis 2` and the formula keeps its minus; drop the minus when Stick Y holds plain `Axis 2`.
- `=max(a1,0)` - one pedal axis split into the accelerator.
- `=-min(a1,0)` - the same axis, its braking half.
- `=a1-a2` - separate accelerator and brake onto one axis.
- `=a1*900/270` - steer as 270° on a wheel set to 900° in its own software.

## Things worth knowing

- Anything that is not a real number, such as dividing by zero, becomes 0.
- A decimal point is always a dot, whatever language Windows is set to.
- Older versions of this program ignore expressions, so a configuration using one loses that mapping when it is opened in them.
- A formula can be up to 128 characters including the equals sign, which is the space a mapping is stored in. Longer formulas are refused as you type them.

## AutoHotkey scripts

An [AutoHotkey v2](https://www.autohotkey.com/) script can watch a controller the way it watches keys, change what the controller's mapping reads, and turn a controller into a keyboard or a mouse, or a keyboard and mouse into a controller. x360ce adds an object for each device, whose properties are its controls; everything else is AutoHotkey itself.

1. `[Options]` tab → `[Plugins]` → tick `Allow AutoHotkey scripts`. x360ce writes `x360ce.ahk` to `Documents\AutoHotkey\Lib`.
2. Start the script with `#Include <x360ce>`, and run it as any AutoHotkey script.

```autohotkey
#Requires AutoHotkey v2.0
#Include <x360ce>

dev := X360ce.c1.d1                              ; device 1 on Controller 1
dev.Hotkey("b1", (*) => Send("{Space down}"))   ; its button 1 holds the space bar
dev.Hotkey("b1 up", (*) => Send("{Space up}"))
XButton2::dev.b2 := 1                            ; the mouse's side button presses its button 2
XButton2 up::dev.b2 := 0
```

Devices, controls, hotkeys and more examples: [Help.AutoHotkey](Help.AutoHotkey.md).

## AI assistant access

Lets an AI assistant that speaks the Model Context Protocol, or a script, inspect and operate this
program. It is off until you tick AI assistant access on the Options tab's AI page, on its MCP Server - AI
Assistant Access tab, and choose a level.
**Read**: read the interface, the
devices and the help, and point at things; changes no setting. **Configure**: everything a person does on the tabs; Windows
may still ask for an administrator, as it asks a person, when a step needs one.
**Administer**: also the actions that install or remove drivers. The
level, the port and the token are changed here and only here; an assistant cannot change them.

The quickest start is Copy prompt on the AI page: it copies instructions for any AI, with
both ways to connect and a first request, "list my controllers". Paste it into the assistant's
chat and it takes it from there. The two ways, both shown on the page:

- **An assistant on this computer that runs commands.** Copy the assistant snippet into its MCP
  settings. It launches `x360ce.exe /Mcp`, which finds the running program, starts it if need be,
  and needs no token from you.
- **An assistant that connects to a URL**, on this computer or another. Copy the URL and give it
  as an HTTP MCP server, with the header `Authorization: Bearer <token>` where `<token>` is the
  token on the AI page. If you allow it with Trust local connections, an assistant on this
  computer needs no token, and Copy prompt leaves it out; on every network the token is always
  needed. For another computer, set Address to `0.0.0.0` first; Windows then
  asks once, through the Fix button on the Issues tab, for permission to listen on every network.
  The connection is plain HTTP, so use it only on a network you trust. The default address,
  `127.0.0.1`, keeps the door on this computer.

**ChatGPT on the web** runs on OpenAI's servers and cannot reach the door. The AI page's OpenAI Tunnel tab opens an OpenAI Secure MCP Tunnel to it. The program downloads OpenAI's tunnel-client from github.com/openai/tunnel-client into `%LOCALAPPDATA%\X360CE\TunnelClient`, checks it against the release's SHA-256 sums, and runs it while Open the tunnel and AI assistant access are both ticked. tunnel-client connects out to OpenAI over HTTPS and passes each of ChatGPT's calls to the door with the door's token, so no port is opened on this computer. Create makes the tunnel with an OpenAI admin key, which is used for that call only and never kept. The tunnel runs with a restricted API key, kept encrypted for your Windows account. [AI assistants](Help.AiAssistants.md#openai-tunnel) gives the steps in ChatGPT.

An assistant can read and answer the other windows the program opens over the main one.

An assistant can also point: ask it where something is and it brings the page to the front,
frames the control and shows a balloon with its words beside it for a few seconds. It can string
such steps into a short script, pointing, pausing, pressing and setting in turn, so it walks you
through a task rather than describing it, and it can find any control by words from its name or
purpose, in any order, rather than reading the whole interface first.

Everything done through the door is written down: Open log on the AI page shows each call,
its arguments and what came of it, with the time, so you can see afterwards what an assistant did.

From a command prompt, `x360ce.exe /Ai` lists the tools and `x360ce.exe /Ai=devices_list` calls
one; a batch file that needs the exit code runs `start /wait x360ce.exe /Ai=...`. Git Bash turns
`/Ai=` into a path, so there write `-Ai=devices_list`. Regenerate the
token to revoke access.

The AI settings are kept in their own file, `x360ce.AI.xml`, beside the other settings: `C:\ProgramData\X360CE\Settings` unless they live elsewhere. It holds whether the door is on, its level, address, port and token, and the tunnel's ID, organization and API key. Every account on this computer can read the file, so the token tells one program from another, not one person from another. The API key in it is encrypted with Windows DPAPI for the Windows account that saved it, so other accounts and other computers cannot read it.

### Ports

x360ce's ports follow one pattern, `37ACD`, so a port says what it reaches:

- `A`: the instance of x360ce, `1` for the first.
- `C`: `0` for x360ce itself, `1` to `4` for a controller, `5` to `9` for plugins.
- `D`: the service of x360ce itself, or the device's number on the controller, as the controller tab's `AHK` column shows it.

| Port | What listens |
|---|---|
| 37101 | AI assistant access, x360ce 4's MCP server |
| 37102 | AI assistant access of x360ce 3 |
| 37103 | The x360ce Test Game's MCP server: a program in x360ce's source code that records what a game receives, for testing |
| 37111 to 37149 | A plugin device's own MCP server, such as a Remote MCP Controller's: controller `C`, device `D`. 37141 is the first device on controller 4. |
| 37150 to 37199 | Servers plugins open themselves |

A port typed on the AI page or on a device's MCP Server page is kept in place of the plan's. A device's server moves with its mapping: mapped to another controller or place, it listens on that one's port; on no controller, it has none until it is mapped or a port is typed.

## The AI skill

A skill is a set of instructions an AI agent reads when a task calls for it. The x360ce skill tells
an agent what this program does, where its settings are and how to use AI assistant access, so it
answers from the program instead of guessing. It works only through AI assistant access, at the level
you chose, and never by taking over your screen, mouse or keyboard; to show you something it frames it
on your screen. The AI page's AI Skill tab installs it:

- **Claude Code**: into the skills folder Claude Code reads, `%USERPROFILE%\.claude\skills`, or the
  one in `CLAUDE_CONFIG_DIR` when that is set.
- **Other agents**: into `%USERPROFILE%\.agents\skills`, which Codex, GitHub Copilot, Gemini CLI,
  Cursor, OpenCode and Windsurf read.
- **Claude app**: Save as ZIP, then add the file in Claude under Customize, Skills.

An install replaces only the `x360ce` folder. It holds the skill, this help and a description of
every page, box and button, stamped with this program's version. The skill comes from the program
itself, so it matches this version's tools. When an installed copy is older than the program, the
Issues tab offers to update it; when none is installed, nothing is said. A copy from a newer program
is left alone. An assistant cannot press these buttons.

An agent that finds the program without the skill can read it from the program itself:
`x360ce.exe /Skill` prints it, as described under Command-line switches below. The same skill is
published with the program's source, in the `skills/x360ce` folder of its GitHub repository, where
skill sites and installers find it.

## Command-line switches

Write a switch after the program's name, for example `x360ce.exe /WindowState=Minimized`. A switch can start with `-` instead of `/`; Git Bash turns `/Skill` into a path, so there write `-Skill`.

- `/?`, `-h` or `--help` prints these switches and starts nothing.
- `/Skill` prints the x360ce skill: instructions that teach an AI agent what this program does and how to use it, whether AI assistant access is on or not. `/Skill=help` prints this help and `/Skill=ui-tree` a description of every page, box and button; `/Skill=<name>` prints any other file of the skill, such as `ui-tree-v4.json` or `help-v3.md`. `/Skill=<folder>` writes the skill into that skills folder, which must exist, as the AI page's Install does.
- `/WindowState=Minimized` starts the program minimised, to the notification area when `[Minimize to Tray]` is ticked. `/WindowState=Maximized` starts it maximised. `[Options]` tab → `[General]` → `[Start with Windows]` uses this switch.
- `/Exit` closes the program where it is already running, for example from a script that runs when a game ends.
- `/Settings` opens the folder the settings are kept in, the one `[Options]` tab → `[Settings]` names, and starts nothing else.
- `/Profile=<name>` keeps a separate set of settings in the `Profiles\<name>` folder beside the usual ones, and `/Profile=<folder>` keeps them in that folder. A second copy runs beside the first only when its settings have `[Allow only one copy of Application at a time]` unticked.
- `/ExportUi=<folder>` writes a description of every page, box and button into the folder, or, when none is given, into `skills\x360ce\references` of the source the program was built from (beside the program when it was not built from one), whatever folder it is started from, then closes. The window is built off the screen while it does this.
- `/PluginKit=<folder>` writes a folder to start a plugin in, and starts nothing else: the types a plugin implements, the build settings, the Test Controller's source as a sample, and the plugin help.
- `/Ai` lists the tools a script or an AI assistant can call once AI assistant access is on, and `/Ai=<tool>` calls one. `/Mcp` offers them to an assistant over MCP. Both reach the copy that runs with its own settings, a `/Profile` included. The help's AI assistant access section says more.

The program also starts a copy of itself with other switches when it needs an administrator, for example to install the driver. Those are not meant to be typed.
