# X360 Controller Emulator navigation tree

Written by the program itself, so it describes the build it came from.
Regenerate with `x360ce.exe /ExportUi=<folder>`. A relative folder is taken
from the program's own folder, because that is where the program works from.

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

Kinds: `Tab`, `Tabs`, `Section` and `Group` hold other elements. `Button`,
`Command` and `Link` are pressed. `CheckBox`, `Choice`, `List`, `Slider`,
`Number` and `Text` are set. `Value`, `Status` and `Grid` are read, not typed in.

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
[Slider]    │   │       ├── Dead zone 0..100                                            # Add deadzone to the left trigger. Range is 0 to 255. Default is 0.
[Number]    │   │       ├── Anti-dead zone                                              # Decrease in-game deadzone for left trigger. Range is 0 to 255. Default is 0.
[Number]    │   │       ├── Sensitivity -100..100 (hidden)                              # Increase sensitivity near the bottom of left trigger. Range is -100 to 100. Default is 0.
[Number]    │   │       ├── Dead zone                                                   # How far the control must move before the game sees anything. Removes drift at rest.
[CheckBox]  │   │       ├── Invert sensitivity                                          # Bends the middle the other way: more sensitive in the centre instead of less.
[Toolbar]   │   │       └── Ready-made settings                                         # Common dead zone and anti-dead zone combinations, applied in one click.
[Command]   │   │           └── Apply a ready-made setting                              # Fills the three settings above from a common combination.
[Command]   │   │               ├── Clear                                               # Clears the dead zone, anti-dead zone and sensitivity.
[Command]   │   │               ├── 5% DeadZone, 100% Controller Anti-DeadZone          # Sets the dead zone to 5% and the anti-dead zone to 100%.
[Command]   │   │               ├── 100% Controller Anti-DeadZone                       # Sets the dead zone to 0% and the anti-dead zone to 100%.
[Command]   │   │               ├── 80% Controller Anti-DeadZone                        # Sets the dead zone to 0% and the anti-dead zone to 80%.
[Command]   │   │               ├── 60% Controller Anti-DeadZone                        # Sets the dead zone to 0% and the anti-dead zone to 60%.
[Command]   │   │               ├── 40% Controller Anti-DeadZone                        # Sets the dead zone to 0% and the anti-dead zone to 40%.
[Command]   │   │               └── 20% Controller Anti-DeadZone                        # Sets the dead zone to 0% and the anti-dead zone to 20%.
[Control]   │   ├── AxisToButtonUserControl                                             # How far a stick or pedal must move before the button mapped to it counts as pressed.
[Value]     │   │   ├── Mapped control                                                  # Which control on your device works this button.
[Slider]    │   │   ├── Press point 0..100                                              # How far the control must move before this button counts as pressed.
[Value]     │   │   ├── Press point                                                     # How far the control must move before this button counts as pressed.
[Number]    │   │   └── Press point 0..32767                                            # Axis to A Button Dead Zone.
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
[Value]     │   │   ├── Instance identifier                                             # Device instance GUID.
[Value]     │   │   ├── Device type                                                     # What kind of device Windows considers this to be.
[Value]     │   │   ├── Number of buttons                                               # How many buttons the device has.
[List]      │   │   ├── Map to                                                          # Index of the PAD which this controller will map to. Auto = 0 or PAD Index 1-4.
[Grid]      │   │   └── Button values reported by the device                            # Which buttons are pressed right now.
[Control]   │   ├── MapExpressionToggle                                                 # Writes the Left Trigger mapping as a formula instead of choosing one control.
[Control]   │   ├── PadControl                                                          # Everything about one emulated Xbox controller: what works it, and how.
[Tabs]      │   │   ├── Controller pages                                                # Settings for this controller, grouped by the part being mapped.
[Tab]       │   │   │   ├── General                                                     # Says which control on your device works each part of the Xbox controller.
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
[Tab]       │   │   │   ├── D-Pad                                                       # Turns a stick or wheel into the directional pad.
[Section]   │   │   │   │   └── Axis to D-Pad                                           # Turns one axis, a steering wheel among them, into the four pad directions.
[Value]     │   │   │   │       ├── Dead zone                                           # How far the axis must move from the centre before a direction is pressed.
[Value]     │   │   │   │       ├── Centre offset                                       # Moves the point counted as centre, for a device that does not rest in the middle.
[CheckBox]  │   │   │   │       ├── Enabled                                             # Axis to control DPad. Disabled = 0, Enabled = 1.
[Slider]    │   │   │   │       ├── Dead zone 0..100                                    # Dead zone for axis.
[Slider]    │   │   │   │       └── Centre offset 0..100                                # Axis to D-Pad offset.
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
[CheckBox]  │   │   │   │   │   ├── Swap Motors                                         # Swap motor. 0 = OFF, 1 = ON.
[List]      │   │   │   │   │   ├── Effect type                                         # Force Feedback type. 0 = Constant, 1 = Periodic Sine, 2 = Periodic Sawtooth
[Slider]    │   │   │   │   │   └── Overall strength 0..100                             # Strength of force feedback. Range is 0 to 100. Default is 100.
[Section]   │   │   │   │   ├── Left motor                                              # The big, slow motor, which produces the heavy rumble.
[Value]     │   │   │   │   │   ├── Left motor strength                                 # How hard this motor runs when the game asks for vibration.
[Value]     │   │   │   │   │   ├── Left motor period                                   # How long one pulse lasts, when the effect is repeated rather than held.
[Value]     │   │   │   │   │   ├── Test left motor                                     # Runs this motor at the chosen strength, so you can feel it without a game.
[List]      │   │   │   │   │   ├── Left motor direction                                # Left motor effect direction. -1, 0, 1.
[Slider]    │   │   │   │   │   ├── Left motor strength 0..100                          # Left motor strength. Range is 0 to 100. Default is 100.
[Slider]    │   │   │   │   │   ├── Left motor period 0..100                            # Left motor period. Range is 0 to 500. Default is 60.
[Slider]    │   │   │   │   │   └── Test left motor 0..100                              # Runs this motor at the chosen strength, so you can feel it without a game.
[Section]   │   │   │   │   ├── Right motor                                             # The small, fast motor, which produces the light buzz.
[Value]     │   │   │   │   │   ├── Right motor strength                                # How hard this motor runs when the game asks for vibration.
[Value]     │   │   │   │   │   ├── Right motor period                                  # How long one pulse lasts, when the effect is repeated rather than held.
[Value]     │   │   │   │   │   ├── Test right motor                                    # Runs this motor at the chosen strength, so you can feel it without a game.
[List]      │   │   │   │   │   ├── Right motor direction                               # Right motor effect direction. -1, 0, 1.
[Slider]    │   │   │   │   │   ├── Right motor strength 0..100                         # Right motor strength. Range is 0 to 100. Default is 100.
[Slider]    │   │   │   │   │   ├── Right motor period 0..100                           # Right motor period. Range is 0 to 500. Default is 120.
[Slider]    │   │   │   │   │   └── Test right motor 0..100                             # Runs this motor at the chosen strength, so you can feel it without a game.
[Value]     │   │   │   │   └── About force feedback                                    # Explains what the settings on this page do.
[Tab]       │   │   │   └── Direct Input                                                # What the mapped device reports about itself, and its values as they change.
[Group]     │   │   │       └── (DirectInputPanel) -> DirectInputUserControl            # What the mapped device reports about itself, and its values as they change.
[Toolbar]   │   │   ├── Mapped device actions                                           # Adds, removes and enables the devices that work this controller.
[Command]   │   │   │   ├── Remove                                                      # Stops the selected device working this controller.
[Command]   │   │   │   ├── Add...                                                      # Chooses another device to work this controller.
[Command]   │   │   │   ├── Auto Map                                                    # Lets the program pick a device for this controller by itself when a game starts.
[Command]   │   │   │   ├── Enable                                                      # Turns this controller on or off for the selected game.
[Command]   │   │   │   └── Show XInput State                                           # Shows the values read back from XInput - what the game actually receives - instead of the values worked out from your device. The emulated controller works either way.
[Button]    │   │   ├── Game Controllers...                                             # Opens the Windows game controller panel for the selected device.
[Button]    │   │   ├── DX Tweak...                                                     # Opens DX Tweak, a separate tool for adjusting the device itself.
[Button]    │   │   ├── Load Preset...                                                  # Replaces this controller's settings with a saved set.
[Button]    │   │   ├── Auto Preset                                                     # Fills the mapping from a preset that matches the selected device.
[Button]    │   │   ├── Clear                                                           # Empties every mapping on this controller.
[Button]    │   │   ├── Reset                                                           # Puts every setting on this controller back to its default. Asks first.
[Button]    │   │   ├── Copy Preset                                                     # Copies this controller's settings to the clipboard.
[Button]    │   │   ├── Paste Preset                                                    # Applies settings from the clipboard to this controller.
[Grid]      │   │   ├── Mapped devices                                                  # Configuration name of the section which is mapped to PAD1.
[Button]    │   │   └── Save Preset                                                     # Stores the current settings as a preset you can load again.
[Control]   │   └── XboxImageUserControl                                                # Lights up each part of the controller as it is used, so a mapping can be checked by eye.
[Section]   ├── App                                                                     # The main window.
[Toolbar]   │   ├── Status bar                                                          # What the program is doing, and how fast it is doing it.
[Status]    │   │   ├── Last action                                                     # The most recent thing the program did.
[Status]    │   │   ├── Controller rate                                                 # Times a second the program reads the controllers. Higher is better.
[Command]   │   │   ├── Interface rate                                                  # Times a second the window redraws itself. Press to stop it redrawing, which leaves the controllers being read as before.
[Status]    │   │   ├── Device reads                                                    # How many times the whole device list has been read again.
[Status]    │   │   ├── Cloud messages                                                  # Messages waiting to be sent to the online database.
[Status]    │   │   ├── Suspended events (hidden)                                       # Setting changes held back while a page is being filled in.
[Status]    │   │   ├── Saving (hidden)                                                 # Shown while settings are being written to disk.
[Status]    │   │   ├── Administrator                                                   # Whether the program is running with Administrator rights.
[Status]    │   │   ├── No error reports                                                # Opens the error report window
[Status]    │   │   └── XInput library                                                  # Which XInput library the program loaded, and its version.
[Label]     │   ├── Help subject                                                        # Name of whatever the mouse is over.
[Label]     │   ├── Help text                                                           # What whatever the mouse is over is for.
[Toolbar]   │   ├── Game bar                                                            # Chooses the game being set up, and saves the settings.
[List]      │   │   ├── Game                                                            # Which game the settings on every page below belong to.
[Command]   │   │   ├── Save All                                                        # Writes every setting to disk now.
[Command]   │   │   ├── Test... (hidden)                                                # Opens a window for trying the emulated controller without a game.
[Command]   │   │   └── Add Game...                                                     # Sets this program up for another game.
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
[Tab]       │       │           ├── General                                             # How the program starts, what it logs, and which tabs it shows.
[Section]   │       │           │   ├── Testing and Logging                             # Extra output, useful when something is not working and needs reporting.
[CheckBox]  │       │           │   │   ├── Enable XInput                               # Turns the emulated controllers on. Off leaves games with the real ones.
[CheckBox]  │       │           │   │   ├── Enable Logging                              # Create a log file in the folder 'x360ce logs'. 0 = OFF, 1 = ON.
[CheckBox]  │       │           │   │   ├── Enable Console                              # Display the console window. 0 = OFF, 1 = ON.
[CheckBox]  │       │           │   │   ├── Debug Mode                                  # Throw or suspend errors. 0 = Suspend, 1 = Throw.
[CheckBox]  │       │           │   │   ├── Show [Programs] Tab                         # Shows the Programs page, which lists what has been set up for each program.
[CheckBox]  │       │           │   │   ├── Show [Devices] Tab                          # Shows the Devices page, which lists every controller found.
[CheckBox]  │       │           │   │   └── Show [Settings] Tab                         # Shows the Settings page, which lists every stored setting.
[Section]   │       │           │   ├── Operation                                       # How the program behaves while it runs.
[CheckBox]  │       │           │   │   ├── Allow only one copy of Application at a time  # Allow only one instance of the application to run at a time.
[CheckBox]  │       │           │   │   ├── Minimize to Tray                            # Hides the window to the notification area instead of the taskbar.
[CheckBox]  │       │           │   │   ├── Always on Top                               # Make program Top Window
[CheckBox]  │       │           │   │   ├── Start with Windows:                         # Start with Windows.
[List]      │       │           │   │   └── Start with Windows                          # Windows State when program starts with Windows.
[Section]   │       │           │   ├── Developing                                      # Aids for working on the program itself.
[CheckBox]  │       │           │   │   ├── Show Form Info on CTRL+SHIFT+RMB            # Enable Form Info (CTRL+SHIFT+RMB)
[CheckBox]  │       │           │   │   └── Show [Test...] Button                       # Show [Test...] Button.
[Section]   │       │           │   ├── Direct Input Devices                            # Which devices the program lists and reads.
[CheckBox]  │       │           │   │   ├── Exclude Supplemental Devices                # Leaves out the extra parts a device reports beside its main controls.
[CheckBox]  │       │           │   │   ├── Exclude Virtual Devices                     # Leaves out the controllers this program creates, so they are not mapped to themselves.
[CheckBox]  │       │           │   │   └── Use Device Buffered Data                    # Device Use Buffered Data: false - device.GetCurrentState(), 1 - device.GetBufferedData().
[Section]   │       │           │   ├── Configuration                                   # What the settings file written for games contains.
[Text]      │       │           │   │   ├── Configuration version                       # The configuration file version.
[CheckBox]  │       │           │   │   ├── Include [Products]                          # Writes the device list into the settings file games read.
[CheckBox]  │       │           │   │   └── Auto switch configuration when game focused # Autodetect currently focussed game.
[Section]   │       │           │   ├── Guide Button                                    # What happens when the Guide button is pressed.
[Text]      │       │           │   │   └── Guide button action                         # Program or command run when the Guide button is pressed.
[Tabs]      │       │           │   ├── Scan locations                                  # Folders searched when looking for installed games.
[Tab]       │       │           │   │   └── Game Scan Locations                         # Folders searched when looking for installed games.
[List]      │       │           │   │       ├── Scanned folders                         # The locations to scan for games.
[Toolbar]   │       │           │   │       └── Scan location actions                   # Adds and removes the folders searched for games.
[Command]   │       │           │   │           ├── Refresh                             # Reads the folder list again.
[Command]   │       │           │   │           ├── Remove                              # Stops searching the selected folder.
[Command]   │       │           │   │           └── Add...                              # Adds a folder to search for games.
[Button]    │       │           │   └── Developer Tools...                              # Opens a window of aids for working on the program.
[Tab]       │       │           ├── Internet                                            # Whether settings are shared with the online database, and the account used.
[Group]     │       │           │   └── (InternetPanel)                                 # Whether settings are shared with the online database, and the account used.
[Section]   │       │           │       ├── Default settings                            # How settings shared by other people are chosen.
[CheckBox]  │       │           │       │   ├── Include Enabled                         # Counts only games that are switched on when choosing a default.
[Number]    │       │           │       │   └── Minimum instances 0..100                # How many people must use a setting before it is offered as the default.
[Section]   │       │           │       ├── Updates (hidden)                            # Whether the program looks for a newer version.
[CheckBox]  │       │           │       │   ├── Check for updates on startup            # Check for updates.
[Button]    │       │           │       │   └── Check...                                # Looks for a newer version now.
[Section]   │       │           │       ├── Online account                              # Identifies this computer to the online database.
[Text]      │       │           │       │   ├── Computer disk                           # Disk the computer identifier is taken from.
[Text]      │       │           │       │   ├── Profile path                            # Folder the profile identifier is taken from.
[Text]      │       │           │       │   ├── Computer identifier                     # Anonymous identifier for this computer.
[Text]      │       │           │       │   ├── Profile identifier                      # Anonymous identifier for this profile.
[Button]    │       │           │       │   └── Open                                    # Opens the folder the profile identifier is taken from.
[Section]   │       │           │       ├── Internet                                    # Whether the program contacts the online settings database at all.
[CheckBox]  │       │           │       │   ├── Enable Internet Features                # Enable the use of Internet features like the settings database.
[CheckBox]  │       │           │       │   ├── Load Settings from Cloud                # Auto load settings from Internet Database.
[List]      │       │           │       │   ├── Web service address                     # Internet settings database URL.
[CheckBox]  │       │           │       │   └── Save Settings to Cloud                  # Auto save settings to Internet Database.
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
[Button]    │       │           │       └── Open Configuration                          # Opens the HID Hide program, where hidden devices are chosen.
[Tab]       │       │           ├── HID Guardian (obsolete)                             # The tool HID Hide replaced. Kept so an old installation can be removed.
[Section]   │       │           │   ├── HID Guardian                                    # The tool HID Hide replaced. Kept so an old installation can be removed.
[Text]      │       │           │   │   ├── HID Guardian state                          # Whether HID Guardian is still installed.
[Button]    │       │           │   │   ├── Install                                     # Installs HID Guardian. Use HID Hide instead.
[Button]    │       │           │   │   ├── Uninstall                                   # Removes HID Guardian. Needs Administrator.
[CheckBox]  │       │           │   │   ├── Configure automatically                     # Configure Hid Guardian Automatically.
[Button]    │       │           │   │   └── Refresh                                     # Checks HID Guardian again.
[Value]     │       │           │   └── HID Guardian notes                              # Explains why HID Guardian is no longer recommended.
[Tab]       │       │           └── Settings                                            # Where your settings are kept, and how to move them somewhere else.
[Group]     │       │               └── (SettingsPanel)                                 # Where your settings are kept, and how to move them somewhere else.
[Value]     │       │                   ├── Settings folder in use                      # Folder the settings are being read from and written to.
[Button]    │       │                   ├── Open Folder                                 # Opens the settings folder in Explorer.
[List]      │       │                   ├── Keep settings in                            # Which folder to keep settings in. Your own user folder cannot be locked by another account.
[List]      │       │                   ├── What to do with existing settings           # Whether the settings you have are copied to the new folder, or left behind.
[Button]    │       │                   └── Apply                                       # Moves the settings to the chosen folder and starts using it.
[Tab]       │       ├── Games                                                           # Games this program is set up for, and what it does for each one.
[Group]     │       │   └── (GameSettingsPanel)                                         # Games this program is set up for.
[Grid]      │       │       ├── Games                                                   # Games this program is set up for. The tick says whether it is switched on.
[Toolbar]   │       │       ├── Game actions                                            # Finds, adds, starts and removes games.
[Command]   │       │       │   ├── Scan                                                # Searches the folders listed in Options for games it knows.
[Command]   │       │       │   ├── Add...                                              # Adds a game by choosing its program file.
[Command]   │       │       │   ├── Delete                                              # Removes the selected game from the list.
[Command]   │       │       │   ├── Save                                                # Writes the settings file the selected game reads.
[Command]   │       │       │   ├── Start                                               # Runs the selected game.
[Command]   │       │       │   ├── Open...                                             # Opens the folder the selected game is installed in.
[Command]   │       │       │   └── Show                                                # Limits the list to games that are switched on, or off.
[Command]   │       │       │       ├── Show: All                                       # Lists every game.
[Command]   │       │       │       ├── Show: Enabled                                   # Lists only the games that are switched on.
[Command]   │       │       │       └── Show: Disabled                                  # Lists only the games that are switched off.
[Group]     │       │       └── (GameDetailsControl)                                    # How the selected game is set up.
[Section]   │       │           ├── Hook mask                                           # Which questions a game asks about controllers this program answers for it.
[CheckBox]  │       │           │   ├── Hook COM                                        # Answers when the game asks Windows for a controller through COM.
[CheckBox]  │       │           │   ├── Hook Load Library                               # Answers when the game loads a library, so the replacement is loaded instead.
[CheckBox]  │       │           │   ├── Hook Direct Input                               # Answers when the game asks for a controller through Direct Input.
[CheckBox]  │       │           │   ├── Hook SetupAPI                                   # Answers when the game asks Windows to list devices.
[CheckBox]  │       │           │   ├── Hook product and vendor codes                   # Reports the fake product and vendor codes instead of the real ones.
[CheckBox]  │       │           │   ├── Hook WinVerifyTrust                             # Answers when the game checks a file's signature.
[CheckBox]  │       │           │   ├── Hook name                                       # Reports a different controller name to the game.
[CheckBox]  │       │           │   ├── Disable                                         # Turns every answer off, leaving the game with the real controllers.
[CheckBox]  │       │           │   └── Stop                                            # Stops answering once the game has started.
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
[Section]   │       │           ├── Auto map                                            # Which of the four controllers are assigned to a device by the program itself.
[CheckBox]  │       │           │   ├── Auto map controller 1                           # Lets the program assign controller 1 to a device when this game starts.
[CheckBox]  │       │           │   ├── Auto map controller 3                           # Lets the program assign controller 3 to a device when this game starts.
[CheckBox]  │       │           │   ├── Auto map controller 2                           # Lets the program assign controller 2 to a device when this game starts.
[CheckBox]  │       │           │   └── Auto map controller 4                           # Lets the program assign controller 4 to a device when this game starts.
[Section]   │       │           ├── DInput file                                         # Whether the Direct Input library is supplied to the game as well.
[CheckBox]  │       │           │   ├── DInput 8, 32-bit                                # Supplies the 32-bit Direct Input library to the game.
[CheckBox]  │       │           │   └── DInput 8, 64-bit                                # Supplies the 64-bit Direct Input library to the game.
[Section]   │       │           ├── Other options                                       # How the game is started, and what it is told about the controller.
[List]      │       │           │   ├── Architecture                                    # Whether the game is a 32-bit or a 64-bit program.
[List]      │       │           │   ├── Emulation                                       # Whether the controller is presented by the virtual driver or by replaced library files.
[Value]     │       │           │   ├── Fake product code                               # Product code reported to a game that only accepts one model of controller.
[Text]      │       │           │   ├── XInput path                                     # Folder the XInput library is written into.
[Text]      │       │           │   ├── DInput file                                     # Name of the Direct Input library written for the game.
[Value]     │       │           │   ├── Fake vendor code                                # Vendor code reported to a game that only accepts one make of controller.
[Number]    │       │           │   ├── Fake product code 0..65535                      # Product code reported to a game that only accepts one model of controller.
[Number]    │       │           │   ├── Fake vendor code 0..65535                       # Vendor code reported to a game that only accepts one make of controller.
[Number]    │       │           │   └── Timeout -1..65535                               # How long to wait for the game before giving up.
[Section]   │       │           ├── Help                                                # Searches the web for other people's settings for this game.
[Button]    │       │           │   ├── Search on Google...                             # Searches the web for this game.
[Button]    │       │           │   ├── Search on NGemu...                              # Searches the NGemu forum for this game.
[Button]    │       │           │   └── Open NGemu...                                   # Opens the NGemu forum.
[Section]   │       │           └── Action                                              # Undoes every change made to this game.
[Button]    │       │               └── Reset to Default                                # Puts this game's settings back the way they started.
[Tab]       │       ├── Devices                                                         # Every controller the program can see, whether mapped or not.
[Group]     │       │   └── (DevicesPanel)                                              # Every controller the program can see.
[Grid]      │       │       ├── Devices                                                 # Every controller the program can see. Unplugged ones are dimmed.
[Toolbar]   │       │       └── Device actions                                          # Refreshes the list and works on the selected device.
[Command]   │       │           ├── Refresh                                             # Reads every device again.
[Command]   │       │           ├── Delete                                              # Forgets the selected device and its settings.
[Command]   │       │           ├── Hardware...                                         # Opens the selected device in Windows Device Manager.
[Command]   │       │           ├── Add Demo Device                                     # Adds a pretend controller, for trying the program without hardware.
[Command]   │       │           ├── Remove Leftover Pads                                # Removes emulated controllers left behind by runs that ended badly. Needs Administrator.
[Command]   │       │           └── HID Guardian                                        # Actions for the obsolete HID Guardian tool.
[Command]   │       │               ├── Show Enumerated Devices                         # Lists the devices HID Guardian knows about.
[Command]   │       │               ├── Show Hidden Devices                             # Lists the devices HID Guardian is hiding from games.
[Command]   │       │               ├── Unhide All Devices                              # Makes every hidden device visible to games again.
[Command]   │       │               └── Synchronize To HID Guardian                     # Hides exactly the devices that are mapped to a controller.
[Tab]       │       ├── Cloud                                                           # Settings waiting to be sent to or fetched from the online database.
[Group]     │       │   └── (CloudPanel)                                                # Settings waiting to be sent to or fetched from the online database.
[Grid]      │       │       ├── Cloud tasks                                             # Settings waiting to be sent to or fetched from the online database.
[Toolbar]   │       │       └── Cloud actions                                           # Sends and fetches settings, and clears the queue.
[Command]   │       │           ├── Refresh                                             # Reads the queue again.
[Command]   │       │           ├── Upload To Cloud                                     # Sends your settings to the online database.
[Command]   │       │           ├── Download From Cloud                                 # Fetches settings other people have shared.
[Status]    │       │           ├── Next run                                            # How long until the queue is worked through again.
[Status]    │       │           ├── Queue state                                         # Whether the queue is running, waiting, or stopped.
[Command]   │       │           └── Delete                                              # Removes the selected task from the queue.
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
[Group]     │           └── Jocys.com X360 Controller Emulator 4.19.16 (Build: 2026-08-29) - Issues  # Problems the program found, and what to do about each one.
[Grid]      │               ├── Issues                                                  # Problems the program found, with what to do about each one.
[Toolbar]   │               └── Issue actions                                           # Hides issues you have decided to live with.
[Command]   │                   ├── Ignore All                                          # Stops reporting every issue listed.
[Command]   │                   ├── Ignore                                              # Stops reporting the selected issue.
[Command]   │                   ├── Exception Info (hidden)                             # Shows the fault behind the selected issue in full.
[Status]    │                   ├── Check state                                         # What the program is checking right now.
[Status]    │                   ├── Next check                                          # How long until the checks run again.
[Status]    │                   └── Check state                                         # Whether the checks are running or waiting.
[Section]   └── Tray (hidden)                                                           # The menu behind the icon in the notification area.
[Command]       ├── Open Application                                                    # Brings the window back from the notification area.
[Command]       └── Exit                                                                # Closes the program and stops the emulated controllers.
```
