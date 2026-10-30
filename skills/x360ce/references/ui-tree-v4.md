# X360 Controller Emulator navigation tree

Version 4.28.14.0, built 2026-10-29.
Written by the program itself, so it describes the build it came from.
Regenerate with `x360ce.exe /ExportUi=<folder>`.

- **Controls** describes each control that appears in more than one place, once.
- **App** is the main window. A `-> Name` line stands for a control described above.
- **Tray** is the menu behind the icon in the notification area.

Every line carries three things in columns of their own, so each can be read
straight down: what kind of element it is, where it sits and what it is called,
and what it is for. A range such as `0..100` appears where the element holds a
number, and says what it will accept.

A setting offered through several controls at once is listed once, unless they
accept different ranges - a slider in per cent beside a box in raw units are two
different things to set, so both are kept.

Kinds: `Tab`, `Tabs`, `Section`, `Group`, `Toolbar` and `Control` hold other
elements. `Button` and `Link` are pressed. `CheckBox`, `Choice`, `List`, `Slider`,
`Number` and `Text` are set. `Value`, `Status`, `Label`, `Picture`, `Progress` and
`Grid` are read, not typed in.

A name followed by `(hidden)` stands for an element that is present but
not shown in this state of the program.

```
[Kind]      Where it sits and what it is called                                         # What it is for
            X360 Controller Emulator
[Section]   ├── Controls                                                                # Controls used in more than one place, described once here.
[Control]   │   ├── AxisMapUserControl                                                  # Shapes what the game receives from one control, without changing the device.
[Section]   │   │   └── Mapped control settings                                         # Dead zone, anti-dead zone and sensitivity for this one control.
[Value]     │   │       ├── Sensitivity                                                 # Bends the middle of the travel and leaves both ends where they are.
[Slider]    │   │       ├── Sensitivity 0..100                                          # Bends the middle of the travel and leaves both ends where they are.
[Picture]   │   │       ├── Response curve                                              # Draws what the game receives for every position of the control.
[Value]     │   │       ├── Anti-dead zone                                              # Skips past a dead zone the game applies of its own, so small movements are felt.
[Value]     │   │       ├── Dead zone                                                   # How far the control must move before the game sees anything. Removes drift at rest.
[Slider]    │   │       ├── Anti-dead zone 0..100                                       # Skips past a dead zone the game applies of its own, so small movements are felt.
[Slider]    │   │       ├── Dead zone 0..100                                            # Add deadzone to the left trigger. Range is 0 to 255. Default is 0. The slider shows it as a percentage: 100 is 256.
[Number]    │   │       ├── Anti-dead zone                                              # Decrease in-game deadzone for left trigger. Range is 0 to 255. Default is 0.
[Number]    │   │       ├── Dead zone                                                   # How far the control must move before the game sees anything. Removes drift at rest.
[Number]    │   │       ├── Sensitivity -100..100 (hidden)                              # Increase sensitivity near the bottom of left trigger. Range is -100 to 100. Default is 0.
[CheckBox]  │   │       ├── Invert sensitivity                                          # Bends the middle the other way: more sensitive in the centre instead of less.
[List]      │   │       └── Preset                                                      # Fills the dead zone, anti-dead zone and sensitivity from a common combination as soon as one is picked; "Clear" sets all three to nought. The list then shows "Apply Preset" again.
[Control]   │   ├── AxisToButtonUserControl                                             # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Value]     │   │   ├── Mapped control                                                  # Which control on your device works this button.
[Slider]    │   │   ├── Press point 0..100                                              # How far the control must move before this button counts as pressed.
[Value]     │   │   ├── Press point                                                     # How far the control must move before this button counts as pressed.
[Number]    │   │   └── Press point 0..32767                                            # Axis to A Button Dead Zone.
[Control]   │   ├── AxisToDPadChart                                                     # The X and Y axes as a square: the shaded sides are where a direction is pressed, the dot is the device's reading, and a side it presses is lit.
[Control]   │   ├── DirectInputUserControl                                              # What the mapped device reports about itself, and its values as they change.
[Tabs]      │   │   ├── Device detail pages                                             # What the device reports it can do.
[Tab]       │   │   │   ├── Device Objects                                              # Every axis, button and hat the device reports having.
[Grid]      │   │   │   │   └── Objects the device reports it has                       # Every axis, button and hat the device reports having.
[Tab]       │   │   │   └── Force Feedback Effects                                      # The vibration effects the device says it can produce.
[Grid]      │   │   │       └── Force feedback effects the device supports              # The vibration effects the device says it can produce.
[Grid]      │   │   ├── Axis values reported by the device                              # What each axis reads right now. Move a control to see which line changes.
[Grid]      │   │   ├── Slider values reported by the device                            # What each slider reads right now.
[Grid]      │   │   ├── Point of view hat values                                        # What each hat switch reads right now.
[Value]     │   │   ├── Vendor name                                                     # Name of the company that made the device.
[Value]     │   │   ├── Vendor code                                                     # Number identifying the maker, as reported over USB.
[Value]     │   │   ├── Number of axes                                                  # How many axes the device has, such as sticks and pedals.
[Value]     │   │   ├── Force feedback state                                            # Whether force feedback is available on the device right now.
[Value]     │   │   ├── Product name                                                    # Device product name.
[Value]     │   │   ├── Product code                                                    # Number identifying the model, as reported over USB.
[Value]     │   │   ├── Number of sliders                                               # How many sliders the device has.
[Value]     │   │   ├── Number of force feedback actuators                              # How many motors the device has for force feedback.
[Value]     │   │   ├── Product identifier                                              # Device product GUID.
[Value]     │   │   ├── Hardware revision                                               # Hardware revision the device reports.
[Value]     │   │   ├── Number of point of view hats                                    # How many hat switches the device has.
[Value]     │   │   ├── Buttons pressed                                                 # Which buttons are pressed right now, by the numbers the mapping boxes use.
[Value]     │   │   ├── Instance identifier                                             # Device instance GUID.
[Value]     │   │   ├── Device type                                                     # What kind of device Windows considers this to be.
[Value]     │   │   └── Number of buttons                                               # How many buttons the device has.
[Control]   │   ├── InputChipGroup                                                      # The device's buttons, numbered as the mapping list numbers them.
[Control]   │   ├── InputUserControl                                                    # Every control of the selected device, lit while it is used, before anything is mapped.
[Label]     │   │   ├── Input source                                                    # The device the chips below belong to, or that none is selected.
[Section]   │   │   ├── Buttons                                                         # One chip per button, lit while it is held. Click or drag a chip into a mapping box.
[Group]     │   │   │   └── Button chips -> InputChipGroup                              # The device's buttons, numbered as the mapping list numbers them.
[Section]   │   │   ├── Axes                                                            # One chip per axis with its reading, lit while it moves and for half a second after.
[Group]     │   │   │   └── Axis chips -> InputChipGroup                                # The device's axes, numbered by the slot each answers to, as the mapping list numbers them.
[Section]   │   │   ├── Sliders                                                         # One chip per slider with its reading, lit while it moves and for half a second after.
[Group]     │   │   │   └── Slider chips -> InputChipGroup                              # The device's sliders, numbered as the mapping list numbers them.
[Section]   │   │   └── POVs                                                            # One chip per POV with its reading in degrees, then its four directions, lit while pressed.
[Group]     │   │       └── POV chips -> InputChipGroup                                 # Each POV followed by U, R, D and L: its up, right, down and left as mappable buttons.
[Control]   │   ├── MapExpressionToggle                                                 # Writes the Left Trigger mapping as a formula instead of choosing one control.
[Control]   │   ├── MouseStickUserControl                                               # How a mouse moves one stick.
[Section]   │   │   └── Left Stick                                                      # How a mouse moves this stick.
[List]      │   │       ├── Style                                                       # How a mouse moves the left stick. 0 = Position: the stick follows how far the mouse has moved, and stays there. 1 = Speed: the stick follows how fast the mouse moves, and returns to the centre when it stops. 2 = Screen: the stick follows where the pointer is on the screen. Default is 0.
[Slider]    │   │       ├── Sensitivity across 1..400                                   # How far the left stick moves for a movement across, in percent. In Position, 100 moves it from the centre to the end in 2,048 counts; in Speed, 100 reaches the end at 4,096 counts a second; in Screen, 100 reaches it half the screen's height from the centre. Range is 1 to 400. Default is 100.
[Value]     │   │       ├── Sensitivity across value                                    # The sensitivity across the slider is set to, in percent.
[Slider]    │   │       ├── Sensitivity up and down 1..400                              # How far the left stick moves for a movement up or down, in percent. In Position, 100 moves it from the centre to the end in 2,048 counts; in Speed, 100 reaches the end at 4,096 counts a second; in Screen, 100 reaches it half the screen's height from the centre. Range is 1 to 400. Default is 100.
[Value]     │   │       ├── Sensitivity up and down value                               # The sensitivity up and down the slider is set to, in percent.
[Slider]    │   │       ├── Smoothing 0..500                                            # How long the left stick takes to follow a change in Speed and Screen, in milliseconds, which evens out a mouse's uneven reports. Range is 0 to 500. Default is 30.
[Slider]    │   │       ├── Back to centre 0..2000                                      # How long the left stick takes to go back to the centre, in milliseconds. In Position, 0 leaves it where the mouse left it; in Speed, 0 returns it as fast as it smooths. Range is 0 to 2000. Default is 0.
[Value]     │   │       ├── Smoothing value                                             # The smoothing the slider is set to, in milliseconds.
[List]      │   │       ├── Active                                                      # When the mouse moves the left stick. 0 = Always. 1 = While the button is held; let go, the stick returns to the centre. 2 = While the button is held; let go, the stick stays where it was. 3 = The button turns it on and off; off, the stick is at the centre. Default is 0.
[Value]     │   │       ├── Back to centre value                                        # The time back to the centre the slider is set to, in milliseconds.
[List]      │   │       └── Button                                                      # The button that turns the mouse on the left stick on and off, counted from 1: the button of the same device. 0 = none. Default is 0.
[Control]   │   ├── PadControl                                                          # Everything about one emulated Xbox controller: what works it, and how.
[Tabs]      │   │   ├── Controller pages                                                # Settings for this controller, grouped by the part being mapped.
[Tab]       │   │   │   ├── General                                                     # Says which control on your device works each part of the Xbox controller.
[Group]     │   │   │   │   ├── Input -> InputUserControl                               # Every control of the selected device, lit while it is used, before anything is mapped.
[Value]     │   │   │   │   ├── Left trigger value                                      # What the game is being given for the left trigger right now.
[Value]     │   │   │   │   ├── Left stick value                                        # Across and up positions the game is being given for the left stick.
[List]      │   │   │   │   ├── Left Trigger                                            # Button id; precede with 'a' for an axis; 's' for a slider; 'x' for a half range axis; 'h' for half slider; use '-' to invert ie. x-2.
[List]      │   │   │   │   ├── Left Shoulder                                           # Left Shoulder Button . Disable = 0.
[List]      │   │   │   │   ├── Button Back                                             # Back button.
[List]      │   │   │   │   ├── Button Start                                            # Start button.
[List]      │   │   │   │   ├── Button Guide                                            # Guide button.
[List]      │   │   │   │   ├── D-Pad                                                   # Disable = 0, POV Index = N.
[List]      │   │   │   │   ├── Left Thumb Axis X                                       # Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.
[List]      │   │   │   │   ├── Left Thumb Axis Y                                       # Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.
[List]      │   │   │   │   ├── Left Thumb Button                                       # Left stick button. Disable = 0.
[List]      │   │   │   │   ├── Left Thumb Up                                           # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Left Thumb Left                                         # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Left Thumb Right                                        # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Left Thumb Down                                         # Button Id. Disable = 0.
[CheckBox]  │   │   │   │   ├── Left Trigger formula -> MapExpressionToggle             # Writes the Left Trigger mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── D-Pad formula -> MapExpressionToggle                    # Writes the D-Pad mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Button Guide formula -> MapExpressionToggle             # Writes the Button Guide mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Button Back formula -> MapExpressionToggle              # Writes the Button Back mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Button Start formula -> MapExpressionToggle             # Writes the Button Start mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Left Shoulder formula -> MapExpressionToggle            # Writes the Left Shoulder mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Left Thumb Axis X formula -> MapExpressionToggle        # Writes the Left Thumb Axis X mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Left Thumb Axis Y formula -> MapExpressionToggle        # Writes the Left Thumb Axis Y mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Left Thumb Right formula -> MapExpressionToggle         # Writes the Left Thumb Right mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Left Thumb Left formula -> MapExpressionToggle          # Writes the Left Thumb Left mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Left Thumb Up formula -> MapExpressionToggle            # Writes the Left Thumb Up mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Left Thumb Down formula -> MapExpressionToggle          # Writes the Left Thumb Down mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Left Thumb Button formula -> MapExpressionToggle        # Writes the Left Thumb Button mapping as a formula instead of choosing one control.
[Group]     │   │   │   │   ├── Controller picture -> XboxImageUserControl              # Lights up each part of the controller as it is used, so a mapping can be checked by eye.
[List]      │   │   │   │   ├── D-Pad Up                                                # D-Pad up button.
[List]      │   │   │   │   ├── D-Pad Left                                              # D-Pad left button.
[List]      │   │   │   │   ├── D-Pad Right                                             # D-Pad right button.
[List]      │   │   │   │   ├── D-Pad Down                                              # D-Pad down button.
[List]      │   │   │   │   ├── Button layout                                           # Names the buttons after the controller in your hands instead of an Xbox one.
[Button]    │   │   │   │   ├── Remap All                                               # Clears the mapping, then asks you to press each control in turn.
[CheckBox]  │   │   │   │   ├── D-Pad Up formula -> MapExpressionToggle                 # Writes the D-Pad Up mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── D-Pad Down formula -> MapExpressionToggle               # Writes the D-Pad Down mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── D-Pad Left formula -> MapExpressionToggle               # Writes the D-Pad Left mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── D-Pad Right formula -> MapExpressionToggle              # Writes the D-Pad Right mapping as a formula instead of choosing one control.
[Value]     │   │   │   │   ├── Right trigger value                                     # What the game is being given for the right trigger right now.
[Value]     │   │   │   │   ├── Right stick value                                       # Across and up positions the game is being given for the right stick.
[List]      │   │   │   │   ├── Right Trigger                                           # Button id. [asxh][-][0-128] axis = 'a', slider = 's'; half axis = 'x', half slider = 'h', invert = '-'. Example: 'x-2'.
[List]      │   │   │   │   ├── Right Shoulder                                          # Right Shoulder Button. Disable = 0.
[List]      │   │   │   │   ├── Button Y                                                # Button 'Y'
[List]      │   │   │   │   ├── Button X                                                # Button 'X'
[List]      │   │   │   │   ├── Button B                                                # Button 'B'
[List]      │   │   │   │   ├── Button A                                                # Button 'A'
[List]      │   │   │   │   ├── Right Thumb Axis X                                      # Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.
[List]      │   │   │   │   ├── Right Thumb Axis Y                                      # Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.
[List]      │   │   │   │   ├── Right Thumb Button                                      # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Right Thumb Up                                          # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Right Thumb Left                                        # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Right Thumb Right                                       # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Right Thumb Down                                        # Button Id. Disable = 0.
[CheckBox]  │   │   │   │   ├── Right Trigger formula -> MapExpressionToggle            # Writes the Right Trigger mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Button A formula -> MapExpressionToggle                 # Writes the Button A mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Button B formula -> MapExpressionToggle                 # Writes the Button B mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Button X formula -> MapExpressionToggle                 # Writes the Button X mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Button Y formula -> MapExpressionToggle                 # Writes the Button Y mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Right Shoulder formula -> MapExpressionToggle           # Writes the Right Shoulder mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Right Thumb Axis X formula -> MapExpressionToggle       # Writes the Right Thumb Axis X mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Right Thumb Axis Y formula -> MapExpressionToggle       # Writes the Right Thumb Axis Y mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Right Thumb Right formula -> MapExpressionToggle        # Writes the Right Thumb Right mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Right Thumb Left formula -> MapExpressionToggle         # Writes the Right Thumb Left mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Right Thumb Up formula -> MapExpressionToggle           # Writes the Right Thumb Up mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   ├── Right Thumb Down formula -> MapExpressionToggle         # Writes the Right Thumb Down mapping as a formula instead of choosing one control.
[CheckBox]  │   │   │   │   └── Right Thumb Button formula -> MapExpressionToggle       # Writes the Right Thumb Button mapping as a formula instead of choosing one control.
[Tab]       │   │   │   ├── Advanced                                                    # Settings only x360ce 3's XInput library reads, from the x360ce.ini file this version does not write. Greyed out, and kept with the settings so x360ce 3's settings in the cloud stay as they are.
[Section]   │   │   │   │   ├── Combining                                               # x360ce 3 only: combined this controller into another virtual controller.
[CheckBox]  │   │   │   │   │   ├── Combine this controller                             # Allow this controller to be combined into another virtual controller. 0 = OFF, 1 = ON.
[List]      │   │   │   │   │   └── Combine Into                                        # The player that virtual controller will be mapped to. 0 = Player 1, 1 = Player 2, 2 = Player 3, 3 = Player 4.
[Section]   │   │   │   │   └── Device                                                  # What x360ce 3's library told a game about the device, and whether it passed the real controller straight through.
[List]      │   │   │   │       ├── Device Sub Type                                     # Device Type. None = 0, Gamepad = 1, Wheel = 2, Stick = 3, FlightStick = 4, DancePad = 5, Guitar = 6, DrumKit = 8.
[CheckBox]  │   │   │   │       ├── Pass Through                                        # Bypass x360ce and send all input and vibration data directly to the system. This disables combining, mappings, deadzones, etc. for this controller. 0 = OFF, 1 = ON.
[List]      │   │   │   │       └── Pass Through ID                                     # The player that controller data will be passed through to or vibration will be read from. 0 = Player 1, 1 = Player 2, 2 = Player 3, 3 = Player 4.
[Tab]       │   │   │   ├── Buttons                                                     # How far a stick or pedal must move before a button mapped to it counts as pressed.
[Section]   │   │   │   │   └── Press points                                            # For each button mapped to a stick or pedal, how far it must move to count as pressed.
[Group]     │   │   │   │       ├── A Button press point -> AxisToButtonUserControl     # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── B Button press point -> AxisToButtonUserControl     # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── X Button press point -> AxisToButtonUserControl     # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── Y Button press point -> AxisToButtonUserControl     # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── Left Bumper press point -> AxisToButtonUserControl  # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── Back press point -> AxisToButtonUserControl         # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── Start press point -> AxisToButtonUserControl        # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── D-Pad Up press point -> AxisToButtonUserControl     # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── Right Bumper press point -> AxisToButtonUserControl # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── D-Pad Down press point -> AxisToButtonUserControl   # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── Left Stick Button press point -> AxisToButtonUserControl  # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── Right Stick Button press point -> AxisToButtonUserControl  # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       ├── D-Pad Left press point -> AxisToButtonUserControl   # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Group]     │   │   │   │       └── D-Pad Right press point -> AxisToButtonUserControl  # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Tab]       │   │   │   ├── D-Pad                                                       # Axis to D-Pad, which only x360ce 3's XInput library reads. Greyed out, and kept with the settings.
[Section]   │   │   │   │   └── Axis to D-Pad                                           # Turns the device's X and Y axes, its main stick, into the four D-Pad directions, as well as whatever else the D-Pad is mapped to: for a pad whose direction buttons report as a stick. The stick's own mappings stay as they are.
[Value]     │   │   │   │       ├── Dead zone                                           # How far an axis must move from the centre before a direction is pressed. 100 % presses close to the end, where a pad's direction buttons put it even when it falls a little short.
[Value]     │   │   │   │       ├── Centre offset                                       # Moves the point counted as centre towards the right and the bottom, as part of half the travel, for a device that does not rest in the middle.
[CheckBox]  │   │   │   │       ├── Enabled                                             # Axis to control DPad. Disabled = 0, Enabled = 1.
[Slider]    │   │   │   │       ├── Dead zone 0..100                                    # Dead zone for axis. The slider shows it as a percentage: 100 is 256.
[Slider]    │   │   │   │       ├── Centre offset 0..100                                # Axis to D-Pad offset. The slider shows it as a percentage: 100 is 256.
[Group]     │   │   │   │       └── Axis to D-Pad chart -> AxisToDPadChart              # The X and Y axes as a square: the shaded sides are where a direction is pressed, the dot is the device's reading, and a side it presses is lit.
[Tab]       │   │   │   ├── Triggers                                                    # Shapes what the game receives from the two triggers.
[Group]     │   │   │   │   ├── (LeftTriggerUserControl) -> AxisMapUserControl          # Shapes what the game receives from one control, without changing the device.
[Group]     │   │   │   │   └── (RightTriggerUserControl) -> AxisMapUserControl         # Shapes what the game receives from one control, without changing the device.
[Tab]       │   │   │   ├── Left Thumb                                                  # Shapes what the game receives from the left stick.
[Group]     │   │   │   │   ├── (LeftThumbXUserControl) -> AxisMapUserControl           # Shapes what the game receives from one control, without changing the device.
[Group]     │   │   │   │   └── (LeftThumbYUserControl) -> AxisMapUserControl           # Shapes what the game receives from one control, without changing the device.
[Tab]       │   │   │   ├── Right Thumb                                                 # Shapes what the game receives from the right stick.
[Group]     │   │   │   │   ├── (RightThumbXUserControl) -> AxisMapUserControl          # Shapes what the game receives from one control, without changing the device.
[Group]     │   │   │   │   └── (RightThumbYUserControl) -> AxisMapUserControl          # Shapes what the game receives from one control, without changing the device.
[Tab]       │   │   │   ├── Force Feedback                                              # Turns vibration on and sets how strong it is.
[Section]   │   │   │   │   ├── Force feedback                                          # Vibration settings shared by both motors.
[Value]     │   │   │   │   │   ├── Overall strength                                    # Scales all vibration, so a device that shakes too hard can be calmed.
[CheckBox]  │   │   │   │   │   ├── Enable                                              # Use Force Feedback. 0 = OFF, 1 = ON.
[List]      │   │   │   │   │   ├── Effect type                                         # Force Feedback type. 0 = Constant, 1 = Periodic Sine, 2 = Periodic Sawtooth
[CheckBox]  │   │   │   │   │   ├── Pass Through                                        # Send the force feedback a game asks for on to a real XInput controller, which an emulated one cannot feel. 0 = OFF, 1 = ON.
[List]      │   │   │   │   │   ├── Pass through to                                     # Which XInput place the force feedback is sent to. 0 = work it out, 1 to 4 = that XInput place.
[CheckBox]  │   │   │   │   │   ├── Swap Motors                                         # Swap motor. 0 = OFF, 1 = ON.
[Button]    │   │   │   │   │   ├── About the motors                                    # Shows what each motor is used for, their measured speeds, and what the multipliers mean.
[List]      │   │   │   │   │   ├── Motor periods                                       # Sets both motor periods to the measured motors played this many times slower; 4x suits most wheels. Greyed out under Constant, which has no period.
[Slider]    │   │   │   │   │   ├── Overall strength 0..100                             # Strength of force feedback. Range is 0 to 100. Default is 100.
[Label]     │   │   │   │   │   └── Effect description                                  # What the chosen effect type does, or, for a device read through Raw Input whose DirectInput side takes no force feedback, that none reaches it.
[Button]    │   │   │   │   ├── Defaults                                                # Puts every setting on this page back to its default.
[Section]   │   │   │   │   ├── Force source (hidden)                                   # Where the selected controller's force comes from. Shown only while the controller, or its twin read through another source, is on another controller tab too.
[Label]     │   │   │   │   │   └── Force source                                        # Which controller tab's settings shape the controller's force, the lowest enabled tab it is on, and which controllers' force it plays: on several, the strongest of each motor. Or why no force reaches it. On every other tab the settings that shape the force are greyed out.
[Section]   │   │   │   │   ├── Left motor                                              # The big, slow motor, which produces the heavy rumble.
[Value]     │   │   │   │   │   ├── Left motor strength                                 # How hard this motor runs when the game asks for vibration.
[Value]     │   │   │   │   │   ├── Left motor period                                   # How long one pulse of the Sine or Sawtooth effect lasts. Greyed out under Constant, which has no period.
[Value]     │   │   │   │   │   ├── Test left motor                                     # Runs this motor at the chosen strength, so you can feel it without a game.
[List]      │   │   │   │   │   ├── Left motor direction                                # Left motor effect direction. -1, 0, 1.
[Slider]    │   │   │   │   │   ├── Left motor strength 0..100                          # Left motor strength. Range is 0 to 100. Default is 100.
[Slider]    │   │   │   │   │   ├── Left motor period 0..100                            # Left motor period at full drive, in milliseconds; it stretches as the drive falls, as the motor slows. The left motor is the low-frequency one. Range is 0 to 400. Default is 160, the measured motor played 4 times slower for a wheel. The slider shows it as a percentage: 100 is 400.
[Slider]    │   │   │   │   │   └── Test left motor 0..100                              # Runs this motor at the chosen strength, so you can feel it without a game.
[Section]   │   │   │   │   ├── Right motor                                             # The small, fast motor, which produces the light buzz.
[Value]     │   │   │   │   │   ├── Right motor strength                                # How hard this motor runs when the game asks for vibration.
[Value]     │   │   │   │   │   ├── Right motor period                                  # How long one pulse of the Sine or Sawtooth effect lasts. Greyed out under Constant, which has no period.
[Value]     │   │   │   │   │   ├── Test right motor                                    # Runs this motor at the chosen strength, so you can feel it without a game.
[List]      │   │   │   │   │   ├── Right motor direction                               # Right motor effect direction. -1, 0, 1.
[Slider]    │   │   │   │   │   ├── Right motor strength 0..100                         # Right motor strength. Range is 0 to 100. Default is 100.
[Slider]    │   │   │   │   │   ├── Right motor period 0..100                           # Right motor period at full drive, in milliseconds; it stretches as the drive falls, as the motor slows. The right motor is the high-frequency one. Range is 0 to 400. Default is 64, the measured motor played 4 times slower for a wheel. The slider shows it as a percentage: 100 is 400.
[Slider]    │   │   │   │   │   └── Test right motor 0..100                             # Runs this motor at the chosen strength, so you can feel it without a game.
[Value]     │   │   │   │   └── About force feedback                                    # Explains what the settings on this page do.
[Tab]       │   │   │   ├── Wheel                                                       # The centering spring that holds a wheel at its centre in games that send no steering force, with a chart of its force at every angle, and the wheel's steering range.
[Group]     │   │   │   │   ├── (WheelChartUserControl) -> WheelChartUserControl        # The centering spring's force at every angle of the wheel, and the wheel's angle and force now.
[CheckBox]  │   │   │   │   ├── Centering spring                                        # Use the centering spring. 0 = OFF, 1 = ON. Off by default, so a wheel's own software can hold the centre instead.
[List]      │   │   │   │   ├── Car                                                     # Fills Centre, Peak, Lightening, Damping, Speed and Wheel range with a kind of car's values, and shows the car the values match, or Custom. No standard sets how a steering wheel must feel, so each car names what it is based on: the family car at 100 km/h is worked out from measurements in the ISO 13674-1 weave test; in town and parking are the same car at 40 and 10 km/h; the sports car and the heavy van are estimates at the firm and slow ends of the range measured in that test. Each is a road car, so each sets the Wheel range for road and sports cars. Friction and Strength are left as they are, and the car is not saved.
[Slider]    │   │   │   │   ├── Friction 1..100                                         # The least force that turns this wheel and brings it home, in percent of its force. The spring reaches it a few degrees from the centre and never pushes less beyond, so the wheel comes home from any angle, with every car. 20 % until Auto measures it and adds a tenth. A geared or belt wheel needs much, a G27 about 30 %; a direct drive wheel turns freely and needs little, so lower it by hand when the wheel comes home harder than you like. Kept with the wheel. Greyed out while no wheel is selected.
[Value]     │   │   │   │   ├── Friction                                                # The least force that turns this wheel and brings it home, in percent of its force. The spring reaches it a few degrees from the centre and never pushes less beyond, so the wheel comes home from any angle, with every car. 20 % until Auto measures it and adds a tenth. A geared or belt wheel needs much, a G27 about 30 %; a direct drive wheel turns freely and needs little, so lower it by hand when the wheel comes home harder than you like. Kept with the wheel. Greyed out while no wheel is selected.
[Button]    │   │   │   │   ├── Auto                                                    # Measures the wheel's friction from both stops, with hands off the wheel, sets Friction to it and a tenth more, and checks that the spring then brings the wheel home without swinging. Strength is left as it is.
[Slider]    │   │   │   │   ├── Strength 0..100                                         # Strength of the car's steering, which the always-on centering spring adds on top of the wheel's friction. Range is 0 to 100; nought turns the spring off. Default is 30; the spring is still off until enabled.
[Value]     │   │   │   │   ├── Strength                                                # The car's steering force at Peak, in percent of the wheel's force, added to Friction. Nought is off.
[Slider]    │   │   │   │   ├── Centre 1..450                                           # The steering angle in degrees at which the centering spring reaches half its strength, for the car at 100 km/h. Range is 1 to 450. Default is 12, a family car.
[Value]     │   │   │   │   ├── Centre                                                  # The steering angle in degrees at which the spring reaches half its strength, for the car at 100 km/h. Smaller is firmer on the centre. Widened when the wheel could not follow so steep a spring.
[Slider]    │   │   │   │   ├── Peak 0..450                                             # The steering angle in degrees of the centering spring's strongest force, for the car at 100 km/h. Beyond it the force eases off towards the stop. 0 puts the peak at the stop. Range is 0 to 450. Default is 60, a family car.
[Value]     │   │   │   │   ├── Peak                                                    # The steering angle in degrees of the spring's strongest force, for the car at 100 km/h. Beyond it the force eases off towards the stop. Stop puts the strongest force at the stop.
[Slider]    │   │   │   │   ├── Lightening 0..100                                       # How much of the centering spring's strongest force is left at the stop, in percent. Range is 0 to 100. Default is 40, a family car.
[Value]     │   │   │   │   ├── Lightening                                              # How much of the spring's strongest force is left at the stop, in percent.
[Slider]    │   │   │   │   ├── Damping 0..100                                          # The centering spring's resistance to the wheel's speed, the same at every angle, in percent of what the device can do. Range is 0 to 100. Default is 5.
[Value]     │   │   │   │   ├── Damping                                                 # Resistance to the wheel's speed, the same at every angle, in percent of what the wheel can do. Calms a wheel that swings past the centre.
[Slider]    │   │   │   │   ├── Speed 0..250                                            # The car's speed in km/h that the centering spring's curve is read at. At a lower speed the force builds over a wider angle. Below 10 counts as 10. A program that knows the car's speed can later set it live, without saving it. Range is 0 to 250. Default is 100.
[Value]     │   │   │   │   ├── Speed                                                   # The car's speed in km/h the spring is read at. Slower spreads the force over a wider angle. Below 10 counts as 10. A program that knows the car's speed, such as one reading a game's own data, can later set it live, without saving it.
[List]      │   │   │   │   ├── Wheel range                                             # Steering range in degrees sent to a Logitech wheel, which powers up at 200. Range is 40 to 900. Default is 0 (leave the wheel as it is).
[Label]     │   │   │   │   └── Wheel message                                           # What Auto is doing and what it found, why the centering spring does nothing now, or that x360ce cannot set this wheel's range.
[Tab]       │   │   │   ├── Mouse                                                       # How a mouse, or any control that reports movement, moves each stick: by how far it moved, how fast it moves, or where the pointer is, always or while a button is held.
[Label]     │   │   │   │   ├── What the Mouse page sets                                # The mouse's axes are chosen in the stick's boxes on the General page; this page sets how they move the stick.
[Group]     │   │   │   │   ├── (LeftMouseStickPanel) -> MouseStickUserControl          # How a mouse moves one stick.
[Group]     │   │   │   │   └── (RightMouseStickPanel) -> MouseStickUserControl         # How a mouse moves one stick.
[Tab]       │   │   │   └── Direct Input                                                # What the mapped device reports about itself, and its values as they change. Titled Raw Input for a device read through Raw Input.
[Group]     │   │   │       └── (DirectInputPanel) -> DirectInputUserControl            # What the mapped device reports about itself, and its values as they change.
[Toolbar]   │   │   ├── Mapped device actions                                           # Adds, removes and enables the devices that work this controller.
[Button]    │   │   │   ├── Remove                                                      # Stops the selected device working this controller.
[Button]    │   │   │   ├── Add...                                                      # Chooses another device to work this controller.
[CheckBox]  │   │   │   ├── Auto Map                                                    # Not available yet, so greyed out: the program does not map devices by itself. Add maps a device.
[CheckBox]  │   │   │   ├── Enable                                                      # Turns this controller on or off for the selected game.
[CheckBox]  │   │   │   └── Show XInput State                                           # Shows the values read back from XInput - what the game actually receives - instead of the values worked out from your device. The emulated controller works either way.
[Button]    │   │   ├── Game Controllers...                                             # Opens the Windows game controller panel for the selected device.
[Button]    │   │   ├── DX Tweak...                                                     # Opens DX Tweak, a separate tool for adjusting the device itself.
[Button]    │   │   ├── Load Preset...                                                  # Replaces this controller's settings with a saved set.
[Button]    │   │   ├── Auto Preset                                                     # Fills the mapping from a preset that matches the selected device.
[Button]    │   │   ├── Clear                                                           # Empties every mapping on this controller.
[Button]    │   │   ├── Reset                                                           # Puts every setting on this controller back to its default. Asks first.
[Button]    │   │   ├── Save Preset...                                                  # Writes this controller's settings to a file, to load again or pass on.
[Button]    │   │   ├── Copy Preset                                                     # Copies this controller's settings to the clipboard as YAML, the easiest to read.
[Button]    │   │   ├── Copy Preset as                                                  # Copies this controller's settings in the format you choose: YAML, XML or JSON.
[Button]    │   │   ├── Paste Preset                                                    # Applies settings from the clipboard to this controller; YAML, XML and JSON are recognised.
[Grid]      │   │   └── Mapped devices                                                  # Configuration name of the section which is mapped to PAD1.
[Control]   │   ├── WheelChartUserControl                                               # The centering spring's force at every angle of the wheel, and the wheel's angle and force now.
[Label]     │   │   ├── Wheel angle                                                     # The wheel's angle now, in degrees from the centre; negative is to the left.
[Label]     │   │   ├── Spring force                                                    # The spring's force at that angle, in percent of the wheel's force, and which way it pushes.
[Picture]   │   │   └── Centering chart                                                 # The red curve is the spring's force towards the centre at every angle, from one stop to the other, as it is played: above the middle it pushes right, below it pushes left. The grey band is Friction, forces too weak to turn the wheel: the curve stays above it everywhere but a few degrees round the centre, where it rises gently so the wheel settles there instead of shaking. The crosshair and the dot are the wheel's angle and force now.
[Control]   │   └── XboxImageUserControl                                                # Lights up each part of the controller as it is used, so a mapping can be checked by eye.
[Section]   ├── App                                                                     # The main window.
[Toolbar]   │   ├── Status bar                                                          # What the program is doing, and how fast it is doing it.
[Status]    │   │   ├── Last action                                                     # The most recent thing the program did.
[Status]    │   │   ├── Controller rate                                                 # Times a second the program reads the controllers. Higher is better.
[CheckBox]  │   │   ├── Interface rate                                                  # Times a second the window redraws itself. Press to stop it redrawing, which leaves the controllers being read as before.
[Status]    │   │   ├── Device reads                                                    # How many times the whole device list has been read again.
[Status]    │   │   ├── Cloud messages                                                  # Messages waiting to be sent to the online database.
[Status]    │   │   ├── Suspended events (hidden)                                       # Setting changes held back while a page is being filled in.
[Status]    │   │   ├── Saving (hidden)                                                 # Shown while settings are being written to disk.
[Status]    │   │   ├── Administrator                                                   # Whether the program is running with Administrator rights.
[Status]    │   │   ├── AI assistant access                                             # Whether an assistant may read or change this program right now. Opens the Options tab.
[Status]    │   │   ├── Error reports                                                   # Opens the error report window
[Status]    │   │   └── XInput library                                                  # Which XInput library the program loaded, and its version.
[Label]     │   ├── Help subject                                                        # Name of whatever the mouse is over.
[Label]     │   ├── Help text                                                           # What whatever the mouse is over is for.
[Toolbar]   │   ├── Game bar                                                            # Chooses the game being set up, and saves the settings.
[List]      │   │   ├── Game                                                            # Which game the settings on every page below belong to.
[Button]    │   │   ├── Save All                                                        # Writes every setting to disk now.
[Button]    │   │   ├── Test... (hidden)                                                # Opens a window for trying the emulated controller without a game.
[Button]    │   │   └── Add Game...                                                     # Sets this program up for another game.
[Tabs]      │   └── Main                                                                # The four controllers, and the pages for everything else.
[Tab]       │       ├── Controller 1                                                    # Settings for the first emulated Xbox controller.
[Group]     │       │   └── (ControlPad1) -> PadControl                                 # Everything about one emulated Xbox controller: what works it, and how.
[Tab]       │       ├── Controller 2                                                    # Settings for the second emulated Xbox controller.
[Group]     │       │   └── (ControlPad2) -> PadControl                                 # Everything about one emulated Xbox controller: what works it, and how.
[Tab]       │       ├── Controller 3                                                    # Settings for the third emulated Xbox controller.
[Group]     │       │   └── (ControlPad3) -> PadControl                                 # Everything about one emulated Xbox controller: what works it, and how.
[Tab]       │       ├── Controller 4                                                    # Settings for the fourth emulated Xbox controller.
[Group]     │       │   └── (ControlPad4) -> PadControl                                 # Everything about one emulated Xbox controller: what works it, and how.
[Tab]       │       ├── Options                                                         # Settings for the program itself, and for the drivers it needs.
[Group]     │       │   └── (OptionsPanel)                                              # Settings for the program itself, rather than for one controller.
[Tabs]      │       │       └── Options pages                                           # Settings for the program itself, rather than for one controller.
[Tab]       │       │           ├── General                                             # How the program starts and runs, and which tabs it shows.
[Section]   │       │           │   ├── Testing                                         # Switching the emulated controllers off to try the real ones, and the pages that show more detail.
[CheckBox]  │       │           │   │   ├── Enable XInput                               # Turns the emulated controllers on. Off leaves games with the real ones.
[CheckBox]  │       │           │   │   ├── Show [Programs] Tab                         # Shows the Programs page, which lists what has been set up for each program.
[CheckBox]  │       │           │   │   ├── Show [Devices] Tab                          # Shows the Devices page, which lists every controller found.
[CheckBox]  │       │           │   │   └── Show [Settings] Tab                         # Shows the Settings page, which lists every stored setting.
[Section]   │       │           │   ├── Operation                                       # How the program behaves while it runs.
[CheckBox]  │       │           │   │   ├── Allow only one copy of Application at a time  # Allow only one instance of the application to run at a time.
[CheckBox]  │       │           │   │   ├── Minimize to Tray                            # Hides the window to the notification area instead of the taskbar.
[CheckBox]  │       │           │   │   ├── Always on Top                               # Make program Top Window
[CheckBox]  │       │           │   │   ├── Start with Windows:                         # Starts this copy of the program when you sign in. The tick is Windows' own setting, the one Task Manager shows on its Startup apps page, so it is unticked while Windows starts another copy or Task Manager has it switched off, and ticking it puts this copy in.
[List]      │       │           │   │   ├── Start with Windows                          # How the window appears when the program starts with Windows. Greyed out while it does not start with Windows.
[List]      │       │           │   │   └── Theme                                       # Light or dark colours. System follows the Windows setting for apps.
[Section]   │       │           │   ├── Developing                                      # Aids for working on the program itself.
[CheckBox]  │       │           │   │   ├── Show Form Info on CTRL+SHIFT+RMB            # Enable Form Info (CTRL+SHIFT+RMB)
[CheckBox]  │       │           │   │   └── Show [Test...] Button                       # Show [Test...] Button.
[Section]   │       │           │   ├── Direct Input Devices                            # Which devices the program lists and reads.
[CheckBox]  │       │           │   │   ├── Exclude Supplemental Devices                # For Auto Map, which is not available yet: leaves out the extra parts a device reports beside its main controls. Nothing uses it until then.
[CheckBox]  │       │           │   │   ├── Exclude Virtual Devices                     # For Auto Map, which is not available yet: leaves out vJoy virtual devices, so they are not mapped to themselves. Nothing uses it until then.
[CheckBox]  │       │           │   │   └── Read Every Change                           # Read every change a device reports, not only its state at each pass.
[Section]   │       │           │   ├── Configuration                                   # Switching settings to the game in front.
[CheckBox]  │       │           │   │   └── Auto switch configuration when game focused # Autodetect currently focussed game.
[Section]   │       │           │   ├── Guide Button                                    # What happens when the Guide button is pressed.
[Text]      │       │           │   │   └── Guide button action                         # Program or command run when the Guide button is pressed.
[Section]   │       │           │   ├── Hotkeys                                         # Keys and controller buttons that reach this program from inside a game.
[CheckBox]  │       │           │   │   ├── Emulation                                   # Turns the emulation hotkey on. Off by default, so the keys stay with other programs until you choose.
[Value]     │       │           │   │   ├── Emulation hotkey                            # Keys that turn the emulated controllers on and off from inside a game, once the hotkey is on. Click the field and press other keys to change them.
[CheckBox]  │       │           │   │   ├── Controller                                  # Turns the emulation buttons on. Off by default, so the buttons do only what the game makes of them until you choose.
[Value]     │       │           │   │   ├── Emulation buttons                           # Controller buttons that, held together, turn the emulated controllers on and off from inside a game, once they are on. Click the field and hold other buttons to change them.
[CheckBox]  │       │           │   │   ├── Show a note on the screen when pressed      # Shows a short note on the screen when the hotkey is pressed, where a game would hide the usual notification.
[CheckBox]  │       │           │   │   └── Show a note when a device disconnects       # Shows a short note on the screen when a device mapped to the current game disconnects or connects again.
[Tabs]      │       │           │   ├── Scan locations                                  # Folders searched when looking for installed games.
[Tab]       │       │           │   │   └── Game Scan Locations                         # Folders searched when looking for installed games.
[List]      │       │           │   │       ├── Scanned folders                         # The locations to scan for games.
[Toolbar]   │       │           │   │       └── Scan location actions                   # Adds and removes the folders searched for games.
[Button]    │       │           │   │           ├── Refresh                             # Reads the folder list again.
[Button]    │       │           │   │           ├── Remove                              # Stops searching the selected folder.
[Button]    │       │           │   │           └── Add...                              # Adds a folder to search for games.
[Button]    │       │           │   └── Developer Tools...                              # Opens a window of aids for working on the program.
[Tab]       │       │           ├── Internet                                            # Whether settings are shared with the online database, and the account used.
[Group]     │       │           │   └── (InternetPanel)                                 # Whether settings are shared with the online database, and the account used.
[Section]   │       │           │       ├── Default settings                            # How settings shared by other people are chosen.
[Number]    │       │           │       │   ├── Minimum instances 0..100                # How many people must use a setting before it is offered as the default.
[CheckBox]  │       │           │       │   └── Include Enabled                         # Counts only games that are switched on when choosing a default.
[Section]   │       │           │       ├── Internet                                    # Whether the program contacts the online settings database at all.
[CheckBox]  │       │           │       │   ├── Enable Internet Features                # Enable the use of Internet features like the settings database.
[CheckBox]  │       │           │       │   ├── Load Settings from Cloud                # Meant to load settings for a device from the Internet Database as soon as it is added. Nothing does that yet: use Download From Cloud on the Cloud page.
[CheckBox]  │       │           │       │   ├── Save Settings to Cloud                  # Auto save settings to Internet Database.
[List]      │       │           │       │   ├── Web service address                     # Internet settings database URL.
[Button]    │       │           │       │   └── Test                                    # Asks the web service at that address who it is and says whether it answers.
[Section]   │       │           │       ├── Online account                              # Identifies this computer to the online database.
[Text]      │       │           │       │   ├── Computer disk                           # Disk the computer identifier is taken from.
[Text]      │       │           │       │   ├── Computer identifier                     # Anonymous identifier for this computer.
[Text]      │       │           │       │   ├── Profile path                            # Folder the profile identifier is taken from.
[Button]    │       │           │       │   ├── Open                                    # Opens the folder the profile identifier is taken from.
[Text]      │       │           │       │   └── Profile identifier                      # Anonymous identifier for this profile.
[Section]   │       │           │       └── Sign in (hidden)                            # Signs in, so settings can be kept with an account instead of this computer.
[Text]      │       │           │           ├── Username                                # E-mail address the account was created with.
[Text]      │       │           │           ├── Password                                # Password for the account.
[Button]    │       │           │           ├── Log In                                  # Signs in with the username and password above.
[Button]    │       │           │           ├── Create...                               # Creates an account on the online database.
[Button]    │       │           │           └── Reset...                                # Sends a password reset to the address above.
[Tab]       │       │           ├── Virtual Device                                      # The driver that presents the emulated controllers to Windows.
[Section]   │       │           │   ├── Allow Remote Controllers (hidden)               # Lets another computer on the network work these controllers.
[Text]      │       │           │   │   ├── Remote password                             # Password another computer must give before it may work these controllers.
[CheckBox]  │       │           │   │   ├── Allow remote controller 1                   # Lets a remote computer work controller 1.
[CheckBox]  │       │           │   │   ├── Allow remote controller 2                   # Lets a remote computer work controller 2.
[CheckBox]  │       │           │   │   ├── Allow remote controller 3                   # Lets a remote computer work controller 3.
[CheckBox]  │       │           │   │   ├── Allow remote controller 4                   # Lets a remote computer work controller 4.
[Number]    │       │           │   │   ├── Remote port 1024..49151                     # Network port listened on for a remote controller.
[CheckBox]  │       │           │   │   └── Enabled                                     # Accepts controllers from another computer.
[Section]   │       │           │   └── Virtual controller driver                       # The driver that presents the emulated controllers to Windows.
[List]      │       │           │       ├── Polling rate                                # Virtual Controller update frequency.
[Text]      │       │           │       ├── Driver version                              # Which version of the virtual controller driver is installed.
[Button]    │       │           │       ├── Refresh                                     # Checks the driver again.
[Button]    │       │           │       ├── Install                                     # Installs the virtual controller driver. Needs Administrator.
[Button]    │       │           │       ├── Uninstall                                   # Removes the virtual controller driver. Needs Administrator.
[Link]      │       │           │       └── Driver author                               # Opens the page the virtual controller driver comes from.
[Tab]       │       │           ├── HID Hide                                            # Hides the real controller from games, so only the emulated one is seen.
[Section]   │       │           │   └── HID Hide                                        # Hides the real controller from games, so only the emulated one is seen.
[Value]     │       │           │       ├── HID Hide state                              # Whether HID Hide is installed, and which version.
[Button]    │       │           │       ├── Refresh                                     # Checks HID Hide again.
[Button]    │       │           │       ├── Download HID Hide...                        # Opens the page HID Hide is downloaded from.
[Button]    │       │           │       ├── Open Configuration                          # Opens the HID Hide program, where hidden devices are chosen.
[CheckBox]  │       │           │       └── Configure automatically                     # Hide a device through HID Hide while it is mapped to a controller.
[Tab]       │       │           ├── HID Guardian (obsolete)                             # The tool HID Hide replaced. Kept so an old installation can be removed.
[Section]   │       │           │   ├── HID Guardian                                    # The tool HID Hide replaced. Kept so an old installation can be removed.
[Text]      │       │           │   │   ├── HID Guardian state                          # Whether HID Guardian is still installed.
[Button]    │       │           │   │   ├── Install                                     # Installs HID Guardian. Use HID Hide instead.
[Button]    │       │           │   │   ├── Uninstall                                   # Removes HID Guardian. Needs Administrator.
[Button]    │       │           │   │   └── Refresh                                     # Checks HID Guardian again.
[Value]     │       │           │   └── HID Guardian notes                              # Explains why HID Guardian is no longer recommended.
[Tab]       │       │           ├── Settings                                            # Where your settings are kept, and how to move them somewhere else.
[Group]     │       │           │   └── (SettingsPanel)                                 # Where your settings are kept, and how to move them somewhere else.
[Value]     │       │           │       ├── Settings folder in use                      # Folder the settings are being read from and written to.
[Button]    │       │           │       ├── Open Folder                                 # Opens the settings folder in Explorer.
[List]      │       │           │       ├── Keep settings in                            # Which folder to keep settings in. Your own user folder cannot be locked by another account.
[List]      │       │           │       ├── What to do with existing settings           # Whether the settings you have are copied to the new folder, or left behind.
[Button]    │       │           │       └── Apply                                       # Moves the settings to the chosen folder and starts using it.
[Tab]       │       │           ├── Update                                              # Looks for a newer version of the program and installs it.
[Group]     │       │           │   └── Update                                          # Looks for a newer version of the program and installs it, step by step, in the log below.
[CheckBox]  │       │           │       ├── Check for updates on startup                # Look for a newer version after the program starts. Off means no request is made.
[Label]     │       │           │       ├── What the check sends                        # When the check runs and the one thing it sends: this program's version, to github.com.
[Button]    │       │           │       ├── Check now                                   # Looks for a newer version now, downloads it, checks it and installs it.
[CheckBox]  │       │           │       ├── Check Digital Signature                     # Install only an update whose program file carries a trusted digital signature.
[CheckBox]  │       │           │       ├── Check Version                               # Install only an update whose program file is newer than this one and is the version its release announces.
[Value]     │       │           │       └── Update log                                  # Each step of the last check and what it found.
[Tab]       │       │           ├── AI                                                  # The skill that teaches AI agents to use this program, AI assistant access, and the tunnel for ChatGPT on the web.
[Group]     │       │           │   └── AI assistant access                             # Lets an AI assistant or a script read or operate this program, and installs the skill that teaches it how.
[Group]     │       │           │       ├── About AI assistant access                   # What AI assistant access does, and an example of what to ask an assistant once it is on.
[Label]     │       │           │       │   ├── What AI assistant access does           # Says that an AI assistant you run can read and use this program only at the level you choose, and that nothing can reach it until AI assistant access is ticked.
[Value]     │       │           │       │   ├── Example request                         # Something to ask an AI assistant that has this program's skill, once AI assistant access is on. It can be selected and copied.
[Button]    │       │           │       │   └── Copy example                            # Copies the example request to the clipboard, to paste into an AI assistant.
[Tabs]      │       │           │       └── AI tabs                                     # The skill, the door, and the tunnel for ChatGPT on the web, each on a tab of its own.
[Tab]       │       │           │           ├── AI Skill                                # Installs the skill that teaches an AI agent how to use this program.
[Group]     │       │           │           │   └── AI skill                            # Installs the skill that teaches an AI agent what this program does and how to use it, where agents read skills.
[Value]     │       │           │           │       ├── Claude Code skills folder       # Where Claude Code reads skills.
[Button]    │       │           │           │       ├── Install for Claude Code         # Writes the skill and its reference files into the Claude Code folder, replacing only this program's skill.
[Label]     │       │           │           │       ├── Claude Code skill               # Whether the skill is in that folder, and whether it is this program's version.
[Value]     │       │           │           │       ├── Other agents' skills folder     # Where Codex, GitHub Copilot, Gemini CLI, Cursor, OpenCode and Windsurf read skills.
[Button]    │       │           │           │       ├── Install for other agents        # Writes the skill and its reference files into the folder the other agents share, replacing only this program's skill.
[Label]     │       │           │           │       ├── Other agents' skill             # Whether the skill is in that folder, and whether it is this program's version.
[Label]     │       │           │           │       ├── Claude app                      # What to do with the ZIP, or why it was not saved.
[Button]    │       │           │           │       └── Save as ZIP                     # Saves the skill as a ZIP, which the Claude app takes under Customize, Skills.
[Tab]       │       │           │           ├── MCP Server - AI Assistant Access        # Opens the door an AI assistant or a script reaches this program by, at the level chosen there.
[Group]     │       │           │           │   └── AI assistant access (MCP server)    # Lets an AI assistant or a script read or operate this program, at the level chosen here.
[CheckBox]  │       │           │           │       ├── AI assistant access             # Lets an AI assistant or a script reach this program at all. Off until a person ticks it.
[List]      │       │           │           │       ├── AI assistant level              # Read, Configure or Administer: how much a connected assistant or script may do.
[List]      │       │           │           │       ├── AI assistant address            # 127.0.0.1 keeps the door on this computer; 0.0.0.0 opens it to every network the computer is on, where calls and the token travel unencrypted.
[CheckBox]  │       │           │           │       ├── Trust local connections (no token)  # Lets programs on this computer reach the door without the token. Greyed out while the address is 0.0.0.0: on every network the token is always needed.
[Text]      │       │           │           │       ├── AI assistant port               # Local port an assistant connects to, from 1024 to 49151. Change it if another program holds it.
[Value]     │       │           │           │       ├── AI assistant URL                # Address an agent that connects over HTTP is given, with the token as a bearer header unless local connections are trusted.
[Button]    │       │           │           │       ├── Copy URL                        # Copies the URL to the clipboard.
[Value]     │       │           │           │       ├── AI assistant token              # What a caller must present to be let in. Made by this program.
[Button]    │       │           │           │       ├── Regenerate token                # Makes a new token, so anything holding the old one is shut out.
[Value]     │       │           │           │       ├── Assistant snippet               # Settings to paste into an assistant so it can start this program as an MCP server.
[Button]    │       │           │           │       ├── Copy snippet                    # Copies the assistant snippet to the clipboard. The snippet holds no token.
[Button]    │       │           │           │       ├── Copy prompt                     # Copies instructions for any AI: how to connect to this program, both ways, and a first thing to ask. Unless local connections are trusted it holds the token, so paste it only to an assistant you trust.
[Button]    │       │           │           │       ├── Open log                        # Opens the record of everything an assistant did through this door: each call, its arguments and what came of it.
[Label]     │       │           │           │       └── Door state                      # Whether the door is open and at which level, or why it could not open; after a press, what it did.
[Tab]       │       │           │           └── OpenAI Tunnel                           # Lets ChatGPT on the web reach this program through the door. Only the person can change it.
[Group]     │       │           │               └── ChatGPT on the web (OpenAI tunnel)  # Opens an OpenAI Secure MCP Tunnel, so ChatGPT on the web reaches this program through the door. Only the person can change it.
[Label]     │       │           │                   ├── About the tunnel                # Why ChatGPT on the web needs a tunnel, and that its IDs and keys come from the OpenAI Platform.
[Text]      │       │           │                   ├── OpenAI organization ID          # The OpenAI organization new tunnels are made in, org- and letters and digits. Personal for ChatGPT Plus or Pro.
[Button]    │       │           │                   ├── Find organization ID            # Opens the OpenAI Platform page that shows the organization ID.
[Text]      │       │           │                   ├── OpenAI admin key                # An admin key of the organization, used only to list, create and delete tunnels and never kept.
[Button]    │       │           │                   ├── Get admin key                   # Opens the OpenAI Platform page that makes admin keys.
[List]      │       │           │                   ├── Tunnel ID                       # The tunnel ChatGPT's Add custom MCP server asks for, tunnel_ and 32 characters. Opening the list shows the organization's tunnels, listed with the admin key, so the one a ChatGPT plugin was made with can be picked; or made by Create, or pasted.
[Button]    │       │           │                   ├── Create tunnel                   # Keeps the ID of the tunnel made earlier for this computer, or makes one in the organization with the admin key when there is none.
[Button]    │       │           │                   ├── Copy tunnel ID                  # Copies the tunnel ID, to paste into ChatGPT.
[Button]    │       │           │                   ├── Delete tunnel                   # Deletes the tunnel at OpenAI with the admin key, after asking, and forgets its ID.
[Text]      │       │           │                   ├── OpenAI API key                  # The restricted API key the tunnel runs with, made in the same organization with Tunnels: Read and Use. Kept encrypted for this Windows account.
[Button]    │       │           │                   ├── Get API key                     # Opens the OpenAI Platform page that makes the API key.
[CheckBox]  │       │           │                   ├── Open the tunnel                 # Opens the tunnel while AI assistant access is on, now and each time this program starts.
[Button]    │       │           │                   ├── Tunnel status page              # Opens tunnel-client's own page, which shows whether ChatGPT's calls arrive. Only while the tunnel runs.
[Label]     │       │           │                   ├── In ChatGPT                      # What to choose in ChatGPT's Add custom MCP server once the tunnel is open.
[Button]    │       │           │                   ├── Open ChatGPT                    # Opens the ChatGPT page where custom MCP servers are added.
[Value]     │       │           │                   ├── Tunnel settings file            # The file the tunnel's ID, organization and encrypted API key are kept in.
[Button]    │       │           │                   ├── Show file                       # Shows the settings file in File Explorer.
[Label]     │       │           │                   ├── Where the keys are              # How the API key is kept, that the admin key is never kept, and where tunnel-client is downloaded to.
[Label]     │       │           │                   └── Tunnel state                    # Whether the tunnel is open, why not, or what the last press did.
[Tab]       │       │           └── Plugins                                             # The plugins found, the devices each adds, whether plugins from others may load, and whether AutoHotkey scripts may change controllers.
[Group]     │       │               └── (PluginsPanel)
[CheckBox]  │       │                   ├── Allow third-party plugins                   # Load plugins that are not signed by the publisher of x360ce.
[Label]     │       │                   ├── About plugins                               # Which plugins load by themselves, and why plugins from others need permission.
[Value]     │       │                   ├── Plugins folder                              # The folder plugins load from: a folder for each plugin, holding the DLL named like it and the DLLs it needs.
[Button]    │       │                   ├── Open Folder                                 # Opens the folder plugins load from. A plugin put there in a folder of its own, MyDevice\MyDevice.dll, loads at once.
[Grid]      │       │                   ├── Plugins                                     # Each plugin found: the devices it adds, whether it loaded, who signed it, and why it did not load or was switched off.
[Section]   │       │                   └── AutoHotkey scripts                          # Whether an AutoHotkey script may read the devices and change what their controllers read, and how quickly x360ce notices one that stopped.
[CheckBox]  │       │                       ├── Allow AutoHotkey scripts                # Let AutoHotkey scripts read devices and change what controllers read.
[Label]     │       │                       ├── About AutoHotkey scripts                # How a script reaches x360ce, and where the library it includes is written.
[Number]    │       │                       ├── Heartbeat every (ms) 10..1000           # How often an AutoHotkey script tells x360ce it still runs, in milliseconds.
[Number]    │       │                       └── Let go of a hung script after (ms) 50..10000  # How long x360ce waits before it lets go of what a hung AutoHotkey script held, in milliseconds.
[Tab]       │       ├── Games                                                           # Games this program is set up for, and what it does for each one.
[Group]     │       │   └── (GameSettingsPanel)                                         # Games this program is set up for.
[Grid]      │       │       ├── Games                                                   # Games this program is set up for. The tick says whether it is switched on.
[Toolbar]   │       │       ├── Game actions                                            # Finds, adds, starts and removes games.
[Button]    │       │       │   ├── Scan                                                # Searches the folders listed in Options for games it knows.
[Button]    │       │       │   ├── Add...                                              # Adds a game by choosing its program file.
[Button]    │       │       │   ├── Delete                                              # Removes the selected game from the list.
[Button]    │       │       │   ├── Save                                                # Writes the settings file the selected game reads.
[Button]    │       │       │   ├── Start                                               # Runs the selected game.
[Button]    │       │       │   ├── Open...                                             # Opens the folder the selected game is installed in.
[Button]    │       │       │   └── Show                                                # Limits the list to games that are switched on, or off.
[Button]    │       │       │       ├── Show: All                                       # Lists every game.
[Button]    │       │       │       ├── Show: Enabled                                   # Lists only the games that are switched on.
[Button]    │       │       │       └── Show: Disabled                                  # Lists only the games that are switched off.
[Group]     │       │       └── (GameDetailsControl)                                    # How the selected game is set up.
[Section]   │       │           ├── Hook mask                                           # Which questions a game asks about controllers this program answers for it.
[CheckBox]  │       │           │   ├── Hook COM                                        # Answers when the game asks Windows for a controller through COM.
[CheckBox]  │       │           │   ├── Hook Direct Input                               # Answers when the game asks for a controller through Direct Input.
[CheckBox]  │       │           │   ├── Hook product and vendor codes                   # Reports the fake product and vendor codes instead of the real ones.
[CheckBox]  │       │           │   ├── Hook name                                       # Reports a different controller name to the game.
[CheckBox]  │       │           │   ├── Stop                                            # Stops answering once the game has started.
[CheckBox]  │       │           │   ├── Hook Load Library                               # Answers when the game loads a library, so the replacement is loaded instead.
[CheckBox]  │       │           │   ├── Hook SetupAPI                                   # Answers when the game asks Windows to list devices.
[CheckBox]  │       │           │   ├── Hook WinVerifyTrust                             # Answers when the game checks a file's signature.
[CheckBox]  │       │           │   └── Disable                                         # Turns every answer off, leaving the game with the real controllers.
[Section]   │       │           ├── XInput files                                        # Which XInput library files this program supplies to the game.
[CheckBox]  │       │           │   ├── XInput 9.1, 32-bit                              # Supplies the 32-bit XInput 9.1 library to the game.
[CheckBox]  │       │           │   ├── XInput 9.1, 64-bit                              # Supplies the 64-bit XInput 9.1 library to the game.
[CheckBox]  │       │           │   ├── XInput 1.1, 32-bit                              # Supplies the 32-bit XInput 1.1 library to the game.
[CheckBox]  │       │           │   ├── XInput 1.1, 64-bit                              # Supplies the 64-bit XInput 1.1 library to the game.
[CheckBox]  │       │           │   ├── XInput 1.2, 32-bit                              # Supplies the 32-bit XInput 1.2 library to the game.
[CheckBox]  │       │           │   ├── XInput 1.2, 64-bit                              # Supplies the 64-bit XInput 1.2 library to the game.
[CheckBox]  │       │           │   ├── XInput 1.3, 32-bit                              # Supplies the 32-bit XInput 1.3 library to the game.
[CheckBox]  │       │           │   ├── XInput 1.3, 64-bit                              # Supplies the 64-bit XInput 1.3 library to the game.
[CheckBox]  │       │           │   ├── XInput 1.4, 32-bit                              # Supplies the 32-bit XInput 1.4 library to the game.
[CheckBox]  │       │           │   └── XInput 1.4, 64-bit                              # Supplies the 64-bit XInput 1.4 library to the game.
[Section]   │       │           ├── Auto map                                            # Not available yet, so greyed out: the program does not map devices to controllers by itself.
[CheckBox]  │       │           │   ├── Auto map controller 1                           # Lets the program assign controller 1 to a device when this game starts.
[CheckBox]  │       │           │   ├── Auto map controller 2                           # Lets the program assign controller 2 to a device when this game starts.
[CheckBox]  │       │           │   ├── Auto map controller 3                           # Lets the program assign controller 3 to a device when this game starts.
[CheckBox]  │       │           │   └── Auto map controller 4                           # Lets the program assign controller 4 to a device when this game starts.
[Section]   │       │           ├── DInput file                                         # Whether the Direct Input library is supplied to the game as well.
[CheckBox]  │       │           │   ├── DInput 8, 32-bit                                # Supplies the 32-bit Direct Input library to the game.
[CheckBox]  │       │           │   └── DInput 8, 64-bit                                # Supplies the 64-bit Direct Input library to the game.
[Section]   │       │           ├── Help                                                # Searches the web for other people's settings for this game.
[Button]    │       │           │   ├── Search on Google...                             # Searches the web for this game.
[Button]    │       │           │   ├── Search on NGemu...                              # Searches the NGemu forum for this game.
[Button]    │       │           │   └── Open NGemu...                                   # Opens the NGemu forum.
[Section]   │       │           ├── Other options                                       # How the game is started, and what it is told about the controller.
[Number]    │       │           │   ├── Fake vendor code 0..65535                       # Vendor code reported to a game that only accepts one make of controller.
[Value]     │       │           │   ├── Fake vendor code                                # Vendor code reported to a game that only accepts one make of controller.
[Number]    │       │           │   ├── Fake product code 0..65535                      # Product code reported to a game that only accepts one model of controller.
[Value]     │       │           │   ├── Fake product code                               # Product code reported to a game that only accepts one model of controller.
[Number]    │       │           │   ├── Timeout -1..65535                               # How long to wait for the game before giving up.
[Text]      │       │           │   ├── XInput path                                     # Folder the XInput library is written into.
[List]      │       │           │   ├── Architecture                                    # Whether the game is a 32-bit or a 64-bit program.
[Text]      │       │           │   ├── DInput file                                     # Name of the Direct Input library written for the game.
[List]      │       │           │   └── Emulation                                       # Whether the controller is presented by the virtual driver or by replaced library files.
[Section]   │       │           └── Action                                              # Undoes every change made to this game.
[Button]    │       │               └── Reset to Default                                # Puts this game's settings back the way they started.
[Tab]       │       ├── Devices                                                         # Every controller the program can see, whether mapped or not.
[Group]     │       │   ├── (XInputDevicesPanel)
[Grid]      │       │   │   ├── XInput devices                                          # Every emulated controller and the XInput place it holds, in the order games see them.
[Group]     │       │   │   ├── Order notice (hidden)                                   # Opens under the list after Apply or Auto-Order: the steps that will be taken, each step as it is taken, then how it went.
[Label]     │       │   │   │   ├── Notice subject                                      # What the notice is about: an order waiting for OK, the order being made, or how it went.
[Label]     │       │   │   │   ├── Notice text                                         # The steps, what happened at each, and why it stopped when it did.
[Button]    │       │   │   │   ├── OK                                                  # Puts the controllers in the order shown. Once it is over, closes the notice.
[Button]    │       │   │   │   └── Cancel                                              # Closes the notice without touching any controller.
[Toolbar]   │       │   │   └── Emulated controller actions                             # Buttons that act on the emulated controllers shown below.
[Button]    │       │   │       ├── Apply                                               # Recreates the controllers in the order shown. Needs Administrator.
[Button]    │       │   │       ├── Auto-Order                                          # Orders the list so Controller N's virtual controller takes XInput N and real controllers take the places left, then applies it.
[Button]    │       │   │       └── Refresh                                             # Reads the controllers again, for when one has arrived or left.
[Group]     │       │   └── (DevicesPanel)                                              # Every controller the program can see.
[Grid]      │       │       ├── Devices                                                 # Every controller the program can see, once for each source it is read through: DirectInput rows first, tinted blue, then Raw Input rows, teal, then plugin devices, violet. Unplugged ones are dimmed. In the list Add opens, each device a loaded plugin offers is listed too, until it has been added.
[Toolbar]   │       │       └── Device actions                                          # Refreshes the list and works on the selected device.
[Button]    │       │           ├── Refresh                                             # Reads every device again.
[Button]    │       │           ├── Delete                                              # Forgets the selected device and its settings.
[CheckBox]  │       │           ├── Show System Devices (hidden)                        # Lists devices Windows files as system devices too, such as a Logitech G13, so they can be mapped. Shown when choosing a device for a controller.
[Button]    │       │           ├── Hardware...                                         # Opens the selected device in Windows Device Manager.
[Button]    │       │           ├── Add Plugin Device                                   # Adds a device a plugin offers, such as the Test Controller, a pretend controller for trying the program without hardware. With several plugin devices it opens a list to pick from.
[Button]    │       │           ├── Remove Leftover Pads                                # Removes emulated controllers left behind by runs that ended badly. Needs Administrator.
[Button]    │       │           └── HID Hide                                            # Hides devices from games through HID Hide, which must be installed: see the HID Hide page on the Options tab.
[Button]    │       │               ├── Show Gaming Devices                             # Lists the devices HID Hide counts as game controllers, with the path it knows each by.
[Button]    │       │               ├── Show Hidden Devices                             # Lists the devices HID Hide is hiding from games.
[Button]    │       │               ├── Unhide All Devices                              # Makes every device HID Hide hides visible to games again, and clears every Hide box.
[Button]    │       │               ├── Synchronize To HID Hide                         # Makes HID Hide hide exactly the devices whose Hide box is ticked, and lets this program see them.
[Button]    │       │               └── Open HID Hide                                   # Opens HID Hide's own program, or its download page while it is not installed.
[Tab]       │       ├── Cloud                                                           # Settings waiting to be sent to or fetched from the online database.
[Group]     │       │   └── (CloudPanel)                                                # Settings waiting to be sent to or fetched from the online database.
[Grid]      │       │       ├── Cloud tasks                                             # Settings waiting to be sent to or fetched from the online database.
[Toolbar]   │       │       └── Cloud actions                                           # Sends and fetches settings, and clears the queue.
[Button]    │       │           ├── Refresh                                             # Reads the queue again.
[Button]    │       │           ├── Upload To Cloud                                     # Sends your settings to the online database.
[Button]    │       │           ├── Download From Cloud                                 # Fetches settings other people have shared.
[Status]    │       │           ├── Next run                                            # How long until the queue is worked through again.
[Status]    │       │           ├── Queue state                                         # Whether the queue is running, waiting, or stopped.
[Button]    │       │           └── Delete                                              # Removes the selected task from the queue.
[Tab]       │       ├── Help                                                            # Instructions for setting up a controller, and answers to common problems.
[Value]     │       │   └── Help text                                                   # Instructions for setting up a controller, and answers to common problems.
[Tab]       │       ├── About                                                           # Version, licence, and what changed in each release.
[Group]     │       │   └── (AboutControl)                                              # Version, licence, and what changed in each release.
[Link]      │       │       ├── https://www.x360ce.com                                  # Opens the program's website.
[Link]      │       │       ├── https://github.com/x360ce/x360ce                        # Opens the source code.
[Link]      │       │       ├── https://www.jocys.com                                   # Opens the publisher's website.
[Link]      │       │       ├── https://github.com/Nucleoprotein                        # Opens a contributor's page.
[Link]      │       │       ├── https://github.com/nefarius                             # Opens the page the virtual controller driver comes from.
[Link]      │       │       ├── http://www.tocaedit.com                                 # Opens the site of the person who wrote the first version.
[Tabs]      │       │       └── About pages                                             # What changed in each release, and the licence.
[Tab]       │       │           ├── Changes                                             # What changed in each release.
[Text]      │       │           │   └── Change log                                      # What changed in each release.
[Tab]       │       │           └── License                                             # Terms this program is given under.
[Text]      │       │               └── Licence text                                    # Terms this program is given under.
[Tab]       │       └── Issues                                                          # Problems the program found, and what to do about each one.
[Group]     │           ├── Issues list                                                 # Problems the program found, and what to do about each one.
[Grid]      │           │   ├── Issues                                                  # Problems the program found, with what to do about each one.
[Toolbar]   │           │   └── Issue actions                                           # Hides issues you have decided to live with.
[CheckBox]  │           │       ├── Ignore All                                          # Stops reporting every issue listed.
[Button]    │           │       ├── Ignore                                              # Stops reporting the selected issue.
[Button]    │           │       ├── Exception Info (hidden)                             # Shows the fault behind the selected issue in full.
[Status]    │           │       ├── Check state                                         # What the program is checking right now.
[Status]    │           │       ├── Next check                                          # How long until the checks run again.
[Status]    │           │       └── Check state                                         # Whether the checks are running or waiting.
[Group]     │           └── (RecorderPanel)                                             # Records what the program reads and sends, into a file to post with a bug report.
[Section]   │               └── Record for a bug report                                 # Records what the program reads from devices and sends to games and devices into a text file to post with a bug report. Names, folders, addresses and keys are left out, devices are written as D1, D2 and so on, and keyboards and mice are not recorded.
[Label]     │                   ├── What the recording leaves out                       # Says that the file holds no names, folders or keys, so it can be posted in a GitHub issue.
[CheckBox]  │                   ├── Settings and devices                                # A recording for a bug report holds the options, the game's settings, each controller's mapping and every device.
[CheckBox]  │                   ├── Device input                                        # A recording for a bug report holds what each device reports: buttons, axes, sliders and hats as they change, and each failed read. Keyboards and mice are never recorded.
[CheckBox]  │                   ├── Controller output                                   # A recording for a bug report holds what each controller sends to the game, its virtual controller, and what XInput reads back.
[CheckBox]  │                   ├── Force feedback                                      # A recording for a bug report holds the vibration each game asks for, the force sent to each device, and how each device is held.
[CheckBox]  │                   ├── Engine timing                                       # A recording for a bug report holds how fast the engine runs, and each device call that takes long or does not return.
[CheckBox]  │                   ├── Window and errors                                   # A recording for a bug report holds whether the window answers, which program is in front, the handles and memory x360ce holds, and its errors.
[Button]    │                   ├── Start Recording                                     # Starts a new recording, or stops the one that runs and keeps its file. A recording stops by itself after 30 minutes or 20 MB.
[Button]    │                   ├── Open Folder                                         # Opens the folder the recordings are saved in, with the last one selected.
[Label]     │                   └── Recording status                                    # How long the recording has run and how big it is, or the file it was saved to.
[Section]   └── Tray                                                                    # The menu behind the icon in the notification area.
[Button]        ├── Open Application                                                    # Brings the window back from the notification area.
[CheckBox]      ├── Enable XInput                                                       # Turns the emulated controllers on or off without opening the window.
[Button]        └── Exit                                                                # Closes the program and stops the emulated controllers.
```
