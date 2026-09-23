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

Kinds: `Tab`, `Tabs`, `Section` and `Group` hold other elements. `Button` and
`Link` are pressed. `CheckBox`, `Choice`, `List`, `Slider`, `Number` and `Text`
are set. `Value`, `Status` and `Grid` are read, not typed in.

```
[Kind]      Where it sits and what it is called                                         # What it is for
            X360 Controller Emulator
[Section]   ├── Controls                                                                # Controls used in more than one place, described once here.
[Control]   │   ├── AxisToButtonUserControl                                             # How far an axis must move before the button mapped to it counts as pressed.
[Value]     │   │   ├── Mapped axis                                                     # The axis this button is mapped to, from the General page.
[Slider]    │   │   ├── Dead zone 0..100                                                # How far the axis must move before the button counts as pressed, in per cent.
[Value]     │   │   ├── Dead zone value                                                 # The dead zone slider's setting.
[Number]    │   │   └── Dead zone 0..32767                                              # How far the axis must move before the button counts as pressed, in raw units.
[Control]   │   ├── GameSettingDetailsUserControl                                       # Which libraries the emulator puts in the game's folder, and how it hooks the game.
[CheckBox]  │   │   ├── Hook COM                                                        # Answers when the game asks Windows for a controller through COM.
[CheckBox]  │   │   ├── Hook Load Library                                               # Answers when the game loads a library, so the replacement is loaded instead.
[CheckBox]  │   │   ├── Hook Direct Input                                               # Answers when the game asks for a controller through Direct Input.
[CheckBox]  │   │   ├── Hook SetupAPI                                                   # Answers when the game asks Windows to list devices.
[CheckBox]  │   │   ├── Hook product and vendor codes                                   # Reports the fake product and vendor codes instead of the real ones.
[CheckBox]  │   │   ├── Hook WinVerifyTrust                                             # Answers when the game checks a file's signature.
[CheckBox]  │   │   ├── Hook name                                                       # Reports a different controller name to the game.
[CheckBox]  │   │   ├── Disable                                                         # Turns every answer off, leaving the game with the real controllers.
[CheckBox]  │   │   ├── Stop                                                            # Stops answering once the game has started.
[CheckBox]  │   │   ├── XInput 9.1, 32-bit                                              # Supplies the 32-bit XInput 9.1 library to the game.
[CheckBox]  │   │   ├── XInput 9.1, 64-bit                                              # Supplies the 64-bit XInput 9.1 library to the game.
[CheckBox]  │   │   ├── XInput 1.1, 32-bit                                              # Supplies the 32-bit XInput 1.1 library to the game.
[CheckBox]  │   │   ├── XInput 1.1, 64-bit                                              # Supplies the 64-bit XInput 1.1 library to the game.
[CheckBox]  │   │   ├── XInput 1.2, 32-bit                                              # Supplies the 32-bit XInput 1.2 library to the game.
[CheckBox]  │   │   ├── XInput 1.2, 64-bit                                              # Supplies the 64-bit XInput 1.2 library to the game.
[CheckBox]  │   │   ├── XInput 1.3, 32-bit                                              # Supplies the 32-bit XInput 1.3 library to the game.
[CheckBox]  │   │   ├── XInput 1.3, 64-bit                                              # Supplies the 64-bit XInput 1.3 library to the game.
[CheckBox]  │   │   ├── XInput 1.4, 32-bit                                              # Supplies the 32-bit XInput 1.4 library to the game.
[CheckBox]  │   │   ├── XInput 1.4, 64-bit                                              # Supplies the 64-bit XInput 1.4 library to the game.
[CheckBox]  │   │   ├── DInput 8, 32-bit                                                # Supplies the 32-bit Direct Input library to the game.
[CheckBox]  │   │   ├── DInput 8, 64-bit                                                # Supplies the 64-bit Direct Input library to the game.
[Value]     │   │   ├── Hook modes                                                      # The hook modes ticked below, as one number.
[Value]     │   │   ├── XInput files                                                    # The XInput libraries ticked below, as one number.
[Value]     │   │   ├── DirectInput files                                               # The DirectInput libraries ticked below, as one number.
[List]      │   │   ├── Architecture                                                    # Whether the game is a 32-bit or a 64-bit program, which decides the libraries it needs.
[Button]    │   │   ├── Apply/Synchronize Settings                                      # Copies the ticked libraries and the settings into the game's folder, after listing what differs.
[Text]      │   │   ├── DirectInput file name                                           # File name stored with this game for its DirectInput library.
[Value]     │   │   ├── Fake product ID in hex                                          # The fake product ID as hexadecimal.
[Value]     │   │   ├── Fake vendor ID in hex                                           # The fake vendor ID as hexadecimal.
[Number]    │   │   ├── Fake vendor ID 0..65535                                         # Vendor number shown to the game in place of the device's own, when PIDVID is ticked.
[Number]    │   │   ├── Fake product ID 0..65535                                        # Product number shown to the game in place of the device's own, when PIDVID is ticked.
[Number]    │   │   ├── Timeout -1..65535                                               # Timeout stored with this game in the game database.
[Toolbar]   │   │   └── Game actions                                                    # Resets this game's settings, and finds help for it on the web.
[Button]    │   │       ├── Reset to Default                                            # Sets this game back to the default settings for it, after asking.
[Button]    │   │       └── Get Help...                                                 # Searches the web for help with this game.
[Button]    │   │           ├── https://www.google.co.uk/?#q=x360ce+X360+Controller+Emulator
[Button]    │   │           ├── Search on NGemu...                                      # Searches the NGemu forum for this game.
[Button]    │   │           └── Open NGemu...                                           # Opens the emulator's thread on the NGemu forum.
[Control]   │   ├── PadControl                                                          # What one emulated Xbox controller is made of: which device feeds it and how each part is mapped.
[Tabs]      │   │   ├── Controller pages                                                # The parts of this controller's settings, one page each.
[Tab]       │   │   │   ├── General                                                     # Which part of the device each button, trigger and stick of the Xbox controller reads.
[Value]     │   │   │   │   ├── Left trigger now                                        # The value the emulated controller reports for the left trigger, 0 to 255.
[Value]     │   │   │   │   ├── Left stick now                                          # The position the emulated controller reports for the left stick, as X;Y.
[List]      │   │   │   │   ├── Left Trigger                                            # Button id; precede with 'a' for an axis; 's' for a slider; 'x' for a half range axis; 'h' for half slider; use '-' to invert ie. x-2.
[List]      │   │   │   │   ├── Left Shoulder                                           # Left Shoulder Button . Disable = 0.
[List]      │   │   │   │   ├── Back                                                    # Back button.
[List]      │   │   │   │   ├── Start                                                   # Start button.
[List]      │   │   │   │   ├── Guide Button                                            # Guide button.
[List]      │   │   │   │   ├── D-pad POV                                               # Disable = 0, POV Index = N.
[List]      │   │   │   │   ├── Left Analog X                                           # Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.
[List]      │   │   │   │   ├── Left Analog Y                                           # Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.
[List]      │   │   │   │   ├── Left Thumb                                              # Left stick button. Disable = 0.
[List]      │   │   │   │   ├── Left Analog Y+ Button                                   # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Left Analog X- Button                                   # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Left Analog X+ Button                                   # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Left Analog Y- Button                                   # Button Id. Disable = 0.
[Value]     │   │   │   │   ├── Right trigger now                                       # The value the emulated controller reports for the right trigger, 0 to 255.
[Value]     │   │   │   │   ├── Right stick now                                         # The position the emulated controller reports for the right stick, as X;Y.
[List]      │   │   │   │   ├── Right Trigger                                           # Button id. [asxh][-][0-128] axis = 'a', slider = 's'; half axis = 'x', half slider = 'h', invert = '-'. Example: 'x-2'.
[List]      │   │   │   │   ├── Right Shoulder                                          # Right Shoulder Button. Disable = 0.
[List]      │   │   │   │   ├── Y                                                       # Button 'Y'
[List]      │   │   │   │   ├── X                                                       # Button 'X'
[List]      │   │   │   │   ├── B                                                       # Button 'B'
[List]      │   │   │   │   ├── A                                                       # Button 'A'
[List]      │   │   │   │   ├── Right Analog X                                          # Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.
[List]      │   │   │   │   ├── Right Analog Y                                          # Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.
[List]      │   │   │   │   ├── Right Thumb                                             # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Right Analog Y+ Button                                  # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Right Analog X- Button                                  # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Right Analog X+ Button                                  # Button Id. Disable = 0.
[List]      │   │   │   │   ├── Right Analog Y- Button                                  # Button Id. Disable = 0.
[List]      │   │   │   │   ├── D-pad Up                                                # D-Pad up button.
[List]      │   │   │   │   ├── D-pad Left                                              # D-Pad left button.
[List]      │   │   │   │   ├── D-pad Right                                             # D-Pad right button.
[List]      │   │   │   │   └── D-pad Down                                              # D-Pad down button.
[Tab]       │   │   │   ├── Axis to Button                                              # How far an axis must move before a button mapped to it counts as pressed.
[Section]   │   │   │   │   └── DInput Axis To Virtual XInput Button DeadZones (Map on [General] Tab First):
[Group]     │   │   │   │       └── Axis to button -> AxisToButtonUserControl           # How far an axis must move before the button mapped to it counts as pressed.
[Tab]       │   │   │   ├── Advanced                                                    # Device type and pass through, combining several devices into one controller, trigger dead zones, and the D-Pad from an axis.
[Section]   │   │   │   │   ├── Triggers
[Value]     │   │   │   │   │   ├── Left trigger dead zone value                        # The left trigger dead zone slider's setting.
[Value]     │   │   │   │   │   ├── Right trigger dead zone value                       # The right trigger dead zone slider's setting.
[Slider]    │   │   │   │   │   ├── Left Trigger Dead Zone 0..100                       # Add deadzone to the left trigger. Range is 0 to 255. Default is 0.
[Slider]    │   │   │   │   │   └── Right Trigger Dead Zone 0..100                      # Add deadzone to the right trigger. Range is 0 to 255. Default is 0.
[Section]   │   │   │   │   ├── Combining
[CheckBox]  │   │   │   │   │   ├── Combined                                            # Allow this controller to be combined into another virtual controller. 0 = OFF, 1 = ON.
[List]      │   │   │   │   │   └── Combined Index                                      # The player that virtual controller will be mapped to. 0 = Player 1, 1 = Player 2, 2 = Player 3, 3 = Player 4.
[Section]   │   │   │   │   ├── Device
[List]      │   │   │   │   │   ├── Controller Type                                     # Device Type. None = 0, Gamepad = 1, Wheel = 2, Stick = 3, FlightStick = 4, DancePad = 5, Guitar = 6, DrumKit = 8.
[CheckBox]  │   │   │   │   │   ├── Pass Through                                        # Bypass x360ce and send all input and vibration data directly to the system. This disables combining, mappings, deadzones, etc. for this controller. 0 = OFF, 1 = ON.
[CheckBox]  │   │   │   │   │   ├── Forces Pass Through                                 # Bypass x360ce for vibration data only. The controller still participates in mappings, deadzones, etc. 0 = OFF, 1 = ON.
[List]      │   │   │   │   │   └── Pass Through Index                                  # The player that controller data will be passed through to or vibration will be read from. 0 = Player 1, 1 = Player 2, 2 = Player 3, 3 = Player 4.
[Section]   │   │   │   │   └── Axis To D-Pad
[Value]     │   │   │   │       ├── D-Pad dead zone value                               # The dead zone slider's setting.
[Value]     │   │   │   │       ├── D-Pad offset value                                  # The offset slider's setting.
[CheckBox]  │   │   │   │       ├── Axis To D-Pad                                       # Axis to control DPad. Disabled = 0, Enabled = 1.
[Slider]    │   │   │   │       ├── Axis To D-Pad Dead Zone 0..100                      # Dead zone for axis.
[Slider]    │   │   │   │       └── Axis To D-Pad Offset 0..100                         # Axis to D-Pad offset.
[Tab]       │   │   │   ├── Left Thumb                                                  # Dead zones and sensitivity of the left stick.
[Group]     │   │   │   │   └── Stick axis -> ThumbUserControl                          # Dead zone, anti dead zone and sensitivity of one axis of a stick.
[Tab]       │   │   │   ├── Right Thumb                                                 # Dead zones and sensitivity of the right stick.
[Group]     │   │   │   │   └── Stick axis -> ThumbUserControl                          # Dead zone, anti dead zone and sensitivity of one axis of a stick.
[Tab]       │   │   │   └── Force Feedback                                              # How the game's vibration reaches the device's motors.
[Section]   │   │   │       ├── Force Feedback
[Value]     │   │   │       │   ├── Overall strength value                              # The overall force feedback strength slider's setting.
[CheckBox]  │   │   │       │   ├── Use Force Feedback                                  # Use Force Feedback. 0 = OFF, 1 = ON.
[CheckBox]  │   │   │       │   ├── Swap Motor                                          # Swap motor. 0 = OFF, 1 = ON.
[List]      │   │   │       │   ├── FFB Type                                            # Force Feedback type. 0 = Constant, 1 = Periodic Sine, 2 = Periodic Sawtooth
[Slider]    │   │   │       │   └── Force Percent 0..100                                # Strength of force feedback. Range is 0 to 100. Default is 100.
[Section]   │   │   │       ├── Left Motor (Big)
[Value]     │   │   │       │   ├── Left motor strength value                           # The left motor strength slider's setting.
[Value]     │   │   │       │   ├── Left motor period value                             # The left motor period slider's setting, in milliseconds.
[List]      │   │   │       │   ├── Left Motor Direction                                # Left motor effect direction. -1, 0, 1.
[Slider]    │   │   │       │   ├── Left Motor Strength 0..100                          # Left motor strength. Range is 0 to 100. Default is 100.
[Slider]    │   │   │       │   └── Left Motor Period 0..100                            # Left motor period at full drive, in milliseconds; it stretches as the drive falls, as the motor slows. The left motor is the low-frequency one. Range is 0 to 400. Default is 160, the measured motor played 4 times slower for a wheel.
[Section]   │   │   │       ├── Right Motor (Small)
[Value]     │   │   │       │   ├── Right motor strength value                          # The right motor strength slider's setting.
[Value]     │   │   │       │   ├── Right motor period value                            # The right motor period slider's setting, in milliseconds.
[List]      │   │   │       │   ├── Right Motor Direction                               # Right motor effect direction. -1, 0, 1.
[Slider]    │   │   │       │   ├── Right Motor Strength 0..100                         # Right motor strength. Range is 0 to 100. Default is 100.
[Slider]    │   │   │       │   └── Right Motor Period 0..100                           # Right motor period at full drive, in milliseconds; it stretches as the drive falls, as the motor slows. The right motor is the high-frequency one. Range is 0 to 400. Default is 64, the measured motor played 4 times slower for a wheel.
[Section]   │   │   │       └── Test Force Feedback
[Button]    │   │   │           ├── Start Test                                          # Runs both motors at the strengths of the two motor sliders until pressed again.
[Number]    │   │   │           └── Test resend interval 0..32767                       # Milliseconds between the test strengths being sent again, so a device that stops on its own keeps running.
[Button]    │   │   ├── Game Controllers...                                             # Opens the Windows Game Controllers panel.
[Button]    │   │   ├── Auto                                                            # Fills every mapping of this controller from what the device offers, after asking.
[Button]    │   │   ├── Clear                                                           # Empties every mapping of this controller, after asking.
[Button]    │   │   ├── Reset                                                           # Reloads this controller's settings from x360ce.ini, dropping what was not saved, after asking.
[Button]    │   │   └── Save                                                            # Writes every setting to x360ce.ini now.
[Control]   │   └── ThumbUserControl                                                    # Dead zone, anti dead zone and sensitivity of one axis of a stick.
[Section]   │       └── X - Horizontal Axis
[Value]     │           ├── Sensitivity value                                           # The sensitivity slider's setting.
[Slider]    │           ├── Sensitivity 0..100                                          # How the stick's movement is curved: slower near the centre, or faster.
[Value]     │           ├── Anti dead zone value                                        # The anti dead zone slider's setting.
[Value]     │           ├── Dead zone value                                             # The dead zone slider's setting.
[Slider]    │           ├── Anti dead zone 0..100                                       # How far the game's own dead zone is skipped, so a small move is not lost, in per cent.
[Slider]    │           ├── Dead zone 0..100                                            # How far the stick must move before the game sees it move, in per cent.
[Number]    │           ├── Anti dead zone 0..32767                                     # How far the game's own dead zone is skipped, so a small move is not lost, in raw units.
[Number]    │           ├── Sensitivity -100..100 (hidden)                              # How the stick's movement is curved, as a number.
[Number]    │           ├── Dead zone 0..32767                                          # How far the stick must move before the game sees it move, in raw units.
[CheckBox]  │           ├── Invert                                                      # Turns the sensitivity curve the other way.
[Toolbar]   │           └── Presets                                                     # Common pairs of dead zone and anti dead zone.
[Button]    │               └── Apply Preset                                            # Sets the dead zone and anti dead zone to one of the common pairs listed.
[Button]    │                   ├── 5% DeadZone, 100% Controller Anti-DeadZone
[Button]    │                   ├── 100% Controller Anti-DeadZone
[Button]    │                   ├── 80% Controller Anti-DeadZone
[Button]    │                   ├── 60% Controller Anti-DeadZone
[Button]    │                   ├── 40% Controller Anti-DeadZone
[Button]    │                   └── 20% Controller Anti-DeadZone
[Section]   ├── App                                                                     # The main window.
[Toolbar]   │   ├── Status bar                                                          # What the program is doing, and which files it uses.
[Status]    │   │   ├── Last action                                                     # The most recent thing the program did, and how many times it has read the controllers.
[Status]    │   │   ├── Events (hidden)                                                 # Whether changes on the pages are being written to x360ce.ini.
[Status]    │   │   ├── Saves (hidden)                                                  # How many times the settings were written since the program started.
[Status]    │   │   ├── Elevated                                                        # Whether the program runs as Administrator.
[Status]    │   │   ├── Settings file                                                   # The settings file the pages edit.
[Status]    │   │   └── Library                                                         # The XInput library the program loaded, and its version.
[Tabs]      │   ├── Main pages                                                          # The four controllers, and the pages for everything else.
[Tab]       │   │   ├── Controller 1                                                    # Settings for the first emulated Xbox controller.
[Group]     │   │   │   └── Controller page -> PadControl                               # What one emulated Xbox controller is made of: which device feeds it and how each part is mapped.
[Tab]       │   │   ├── Controller 2                                                    # Settings for the second emulated Xbox controller.
[Group]     │   │   │   └── Controller page -> PadControl                               # What one emulated Xbox controller is made of: which device feeds it and how each part is mapped.
[Tab]       │   │   ├── Controller 3                                                    # Settings for the third emulated Xbox controller.
[Group]     │   │   │   └── Controller page -> PadControl                               # What one emulated Xbox controller is made of: which device feeds it and how each part is mapped.
[Tab]       │   │   ├── Controller 4                                                    # Settings for the fourth emulated Xbox controller.
[Group]     │   │   │   └── Controller page -> PadControl                               # What one emulated Xbox controller is made of: which device feeds it and how each part is mapped.
[Tab]       │   │   ├── Options                                                         # Settings for the program itself.
[Group]     │   │   │   └── (OptionsPanel)
[Section]   │   │   │       ├── Testing and Logging
[CheckBox]  │   │   │       │   ├── Enable XInput                                       # Not used by this version of the program.
[CheckBox]  │   │   │       │   ├── Use Init Beep                                       # Beep when initialized. 0 = OFF, 1 = ON.
[CheckBox]  │   │   │       │   ├── Log                                                 # Create a log file in the folder 'x360ce logs'. 0 = OFF, 1 = ON.
[CheckBox]  │   │   │       │   ├── Console                                             # Display the console window. 0 = OFF, 1 = ON.
[CheckBox]  │   │   │       │   ├── Debug Mode                                          # Throw or suspend errors. 0 = Suspend, 1 = Throw.
[CheckBox]  │   │   │       │   └── Combine Enabled                                     # Allow multiple controllers to be combined into a virtual controller. 0 = OFF, 1 = ON.
[Section]   │   │   │       ├── Operation
[CheckBox]  │   │   │       │   ├── Allow Only One Copy                                 # Allow only one instance of the application to run at a time. 0 = Allow multiple instances, 1 = Allow only one instance.
[CheckBox]  │   │   │       │   └── Minimize to Tray                                    # Hides the window in the notification area instead of the taskbar when it is minimised.
[Section]   │   │   │       ├── Direct Input Devices
[CheckBox]  │   │   │       │   ├── Exclude Supplemental Devices                        # Leaves supplemental devices, such as the pedals of a wheel, out of the devices the program reads.
[CheckBox]  │   │   │       │   └── Exclude Virtual Devices                             # Leaves vJoy devices and the controllers this program feeds out of the devices it reads, so its output is not read back as input.
[Section]   │   │   │       ├── Configuration
[Text]      │   │   │       │   └── Settings format version                             # Version of the x360ce.ini layout this program writes.
[Section]   │   │   │       ├── Internet
[CheckBox]  │   │   │       │   ├── Internet Features                                   # Enable the use of Internet features like the settings database. 0 = OFF, 1 = ON.
[CheckBox]  │   │   │       │   ├── Auto Load Settings When Tab Selected                # Searches the online database for settings for your controllers each time the Controller Settings page is opened.
[List]      │   │   │       │   └── Settings database address                           # Web address of the online database the Controller Settings page searches and saves to.
[Tabs]      │   │   │       ├── (ProgramScanLocationsTabControl)
[Tab]       │   │   │       │   ├── Game Scan Locations
[List]      │   │   │       │   │   ├── Game scan locations                             # Folders the Scan button searches for games.
[Toolbar]   │   │   │       │   │   └── Scan location actions                           # Adds, removes and fills in the folders the Scan button searches.
[Button]    │   │   │       │   │       ├── Refresh                                     # Adds the Program Files folders of every fixed drive to the scan locations.
[Button]    │   │   │       │   │       ├── Remove                                      # Removes the selected folder from the scan locations.
[Button]    │   │   │       │   │       └── Add...                                      # Adds a folder to the scan locations.
[Tab]       │   │   │       │   └── AI Assistant Access                                 # Lets an AI assistant or a script read and operate this program, off unless switched on here.
[CheckBox]  │   │   │       │       ├── Allow AI assistants                             # Opens the program to an AI assistant or a script on this computer, through the token below.
[List]      │   │   │       │       ├── Access                                          # How much a connected assistant may do: Read, Configure or Administer.
[Number]    │   │   │       │       ├── Port 1024..49151                                # Port on this computer the assistant connects to.
[CheckBox]  │   │   │       │       ├── Register with Windows                           # Registers the program with the Windows agent registry, so agents such as Copilot find it by themselves.
[Value]     │   │   │       │       ├── Token                                           # What a caller must present to be let in. Regenerate it to shut out everyone who has the old one.
[Button]    │   │   │       │       ├── Regenerate                                      # Makes a new token, which shuts out every caller that has the old one.
[Button]    │   │   │       │       ├── Copy MCP Settings                               # Copies what an assistant needs to start the program as an MCP server.
[Button]    │   │   │       │       ├── Log                                             # Opens the record of everything done through the door.
[Label]     │   │   │       │       └── Door                                            # Whether the door is open, where, and why not when it could not open.
[Button]    │   │   │       ├── Save                                                    # Writes every setting to x360ce.ini now.
[Button]    │   │   │       └── Open Settings Folder...                                 # Opens the folder with the game list and the other files the program keeps.
[Tab]       │   │   ├── Game Settings                                                   # Games the emulator is set up for, and how it hooks each one.
[Group]     │   │   │   └── (GameSettingsPanel)
[Tabs]      │   │   │       └── Game lists                                              # Your games, the program's defaults for well-known games, and this computer's identity.
[Tab]       │   │   │           ├── My Game Settings
[Grid]      │   │   │           │   ├── My games                                        # Games the emulator is set up for. Select one to see how it is hooked.
[Toolbar]   │   │   │           │   ├── Game list actions                               # Scans for games, adds, starts, saves and deletes them, and chooses which ones the list shows.
[Button]    │   │   │           │   │   ├── Scan                                        # Searches the scan locations on the Options page for games and adds the ones found.
[Button]    │   │   │           │   │   ├── Add...                                      # Adds a game by choosing its program file.
[Button]    │   │   │           │   │   ├── Delete                                      # Removes the selected games from the list.
[Button]    │   │   │           │   │   ├── Save                                        # Writes the game list to disk now.
[Button]    │   │   │           │   │   ├── Start                                       # Starts the selected game.
[Button]    │   │   │           │   │   ├── Open...                                     # Opens the selected game's folder.
[Button]    │   │   │           │   │   └── Show                                        # Which games the list shows: all, enabled only, or disabled only.
[Button]    │   │   │           │   │       ├── Show: All
[Button]    │   │   │           │   │       ├── Show: Enabled
[Button]    │   │   │           │   │       └── Show: Disabled
[Group]     │   │   │           │   └── Game details -> GameSettingDetailsUserControl   # Which libraries the emulator puts in the game's folder, and how it hooks the game.
[Tab]       │   │   │           ├── Default Settings for Most Popular Games
[Grid]      │   │   │           │   ├── Default game settings                           # How the emulator is set up for well-known games when they are added.
[Toolbar]   │   │   │           │   ├── Default game settings actions                   # Downloads, imports, exports and deletes the default game settings.
[Button]    │   │   │           │   │   ├── Refresh                                     # Downloads the default game settings from the online database.
[Button]    │   │   │           │   │   ├── Import...                                   # Reads default game settings from a file.
[Button]    │   │   │           │   │   ├── Export...                                   # Writes the default game settings to a file.
[Button]    │   │   │           │   │   └── Delete                                      # Removes the selected default game settings.
[Group]     │   │   │           │   └── Game details -> GameSettingDetailsUserControl   # Which libraries the emulator puts in the game's folder, and how it hooks the game.
[Tab]       │   │   │           └── Options
[CheckBox]  │   │   │               ├── Include Enabled                                 # Which default game settings Refresh downloads: enabled ones, disabled ones, or both.
[Number]    │   │   │               ├── Minimum users 0..100                            # Refresh downloads only games set up by at least this many people.
[Text]      │   │   │               ├── Disk ID                                         # Serial number of this computer's system disk, which identifies it to the online database.
[Text]      │   │   │               └── Hashed disk ID                                  # The disk ID as the online database receives it, hashed so the serial number itself is not sent.
[Tab]       │   │   ├── Controller Settings                                             # Settings saved for your controllers, and settings other people shared online.
[Group]     │   │   │   └── (SettingsDatabasePanel)
[List]      │   │   │       ├── Controller                                              # Which controller's settings Save sends.
[List]      │   │   │       ├── Game                                                    # Which game the saved settings are for.
[Text]      │   │   │       ├── Comment                                                 # A note saved with the settings.
[Button]    │   │   │       ├── Save                                                    # Saves the chosen controller's settings online, for the chosen game.
[Tabs]      │   │   │       ├── Settings lists                                          # Your saved settings, the most popular settings for your controllers, and defaults for well-known controllers.
[Tab]       │   │   │       │   ├── My Device Settings
[Grid]      │   │   │       │   │   ├── My device settings                              # Settings saved online for your controllers.
[Toolbar]   │   │   │       │   │   └── My device settings actions                      # Reads, loads and deletes your saved settings, and chooses the controller they load into.
[Button]    │   │   │       │   │       ├── Refresh                                     # Reads your saved settings from the online database again.
[Button]    │   │   │       │   │       ├── Load                                        # Loads the selected settings into their controller.
[Button]    │   │   │       │   │       ├── Delete                                      # Deletes the selected settings from the online database.
[Button]    │   │   │       │   │       └── Map To                                      # Which controller the selected settings load into.
[Button]    │   │   │       │   │           ├── Disabled
[Button]    │   │   │       │   │           ├── Auto
[Button]    │   │   │       │   │           ├── Controller 1
[Button]    │   │   │       │   │           ├── Controller 2
[Button]    │   │   │       │   │           ├── Controller 3
[Button]    │   │   │       │   │           └── Controller 4
[Tab]       │   │   │       │   ├── Most Popular Settings for My Controllers
[Grid]      │   │   │       │   │   ├── Most popular settings                           # The settings other people use most for the controllers you have.
[Toolbar]   │   │   │       │   │   └── Most popular settings actions                   # Reads the most popular settings again, and loads the selected one.
[Button]    │   │   │       │   │       ├── Refresh                                     # Reads the most popular settings from the online database again.
[Button]    │   │   │       │   │       └── Load                                        # Loads the selected popular settings into the chosen controller.
[Tab]       │   │   │       │   └── Default Settings for Most Popular Controllers
[Grid]      │   │   │       │       ├── Controller defaults                             # Default settings for well-known controllers.
[Toolbar]   │   │   │       │       └── Controller defaults actions                     # Reads the controller defaults again, and loads the selected one.
[Button]    │   │   │       │           ├── Refresh                                     # Reads the controller defaults from the online database again.
[Button]    │   │   │       │           └── Load                                        # Loads the selected defaults into the chosen controller.
[Value]     │   │   │       └── Selected comment                                        # The note saved with the selected settings.
[Tab]       │   │   ├── Help                                                            # Instructions for setting up a controller, and answers to common problems.
[Value]     │   │   │   └── Help text                                                   # Instructions for setting up a controller, and answers to common problems.
[Tab]       │   │   └── About                                                           # Version, licence, and what changed in each release.
[Group]     │   │       └── (AboutControl)
[Link]      │   │           ├── http://www.jocys.com
[Link]      │   │           ├── https://github.com/x360ce/x360ce
[Link]      │   │           ├── http://dev.tapek.shst.pl
[Link]      │   │           ├── http://www.tocaedit.com
[Link]      │   │           ├── http://en.wikipedia.org/wiki/Xbox_360_Controller
[Tabs]      │   │           └── (AboutTabControl)
[Tab]       │   │               ├── Changes
[Text]      │   │               │   └── Changes                                         # What changed in each release of this program.
[Tab]       │   │               └── License
[Text]      │   │                   └── License                                         # The licence this program is released under.
[Label]     │   ├── Help text                                                           # What whatever the mouse is over is for.
[List]      │   └── Game                                                                # Which game the emulator is set up for: the folder whose x360ce.ini the pages below edit.
[Section]   └── Tray (hidden)                                                           # The menu behind the icon in the notification area.
[Button]        ├── Open Application                                                    # Brings the window back from the notification area.
[Button]        └── Exit                                                                # Closes the program.
```
