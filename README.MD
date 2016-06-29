"Xbox 360 Controller Emulator" allows your controller (gamepad, joystick, wheel, etc.) to function as an Xbox 360 controller. For example, it lets you play games such as "Grand Theft Auto" (GTA) or "Mafia II" using a Logitech Wheel.

# Download

Digitally Signed Application v3.2.9.81 (2015-10-04) and Libraries 3.4.1.1357 (2015-08-27)

[32-bit Download - x360ce.zip](http://www.x360ce.com/Files/x360ce.zip) - for 32-bit games

[64-bit Download - x360ce_x64.zip](http://www.x360ce.com/Files/x360ce_x64.zip) - for 64-bit games 

DInput.dll Plugin (required for some games):

[32-bit Download - dinput_x86.dll](https://github.com/x360ce/x360ce/raw/master/x360ce.App/Resources/dinput_x86.dll) - for 32-bit games 

[64-bit Download - dinput_x64.dll](https://github.com/x360ce/x360ce/raw/master/x360ce.App/Resources/dinput_x64.dll) - for 64-bit games 

# Reporting Issues

Report only crashes or other bugs, note that the Issue tracker is not a help desk! 

**DO NOT ASK FOR HELP IN INSTALLATION OR CONFIGURATION!!!**

**If you have questions about installation or configuration, please go to our [forum @NGemu](http://ngemu.com/forums/x360ce.140)**

# Guides [OUTDATED!]

[x360ce Libraries Tutorial](http://ngemu.com/threads/x360ce-libraries-tutorial-by-resolutespider5.155310/)

[x360ce Application Tutorial](http://ngemu.com/threads/tutorial-by-resolutespider5.155011/)

[Various x360ce Guides](http://ngemu.com/threads/various-x360ce-guides-by-resolutespider5.156807/)

# Compatibility
Games that work with x360ce can be found on the [Compatibility List](Wiki/CompatibilityList.md). If you would like to add to that list or provide updated information place see [Reporting Compatibility](Wiki/ReportingCompatibility.md).

# System Requirements

  1. Windows Vista or newer.
  1. [.NET 3.5 (also installs 2.0 and 3.0)](http://www.microsoft.com/en-us/download/details.aspx?id=22) (included in Windows 7)
  1. [.NET 4.0 (link to 4.6, also installs 4.0)](http://www.microsoft.com/en-us/download/details.aspx?id=48130) (included in Windows 8 and 10)
  1. [DirectX End-User Runtime (June 2010)](http://www.microsoft.com/en-us/download/details.aspx?id=8109) (Required regardless of OS)
  1. [Visual C++ Redistributable for Visual Studio 2013](http://www.microsoft.com/en-us/download/details.aspx?id=40784) (For x64 systems install both x86 and x64 redistributables)
  1. If you upgraded to Windows 10, you must Uninstall and Reinstall the Visual C++ Redistributable

Notes:
  * Most game's executables are 32-bit and require x86 x360ce and also the x86 redistributable.
  * Vista users can obtain .NET 3.5 from the above link, and Windows 8 users can enable it in [Programs and Features - Turn Windows Features on and off.](http://msdn.microsoft.com/en-us/library/hh506443.aspx)
  * **.NET MUST be installed prior to the DirectX webupdate as it checks that .NET 2.0/3.0/3.5 is installed and skips the Managed DirectX framework if it is not.**
  * The full DirectX Redistributable will always install the file, however it's best to have .NET 3.5 installed anyhow as quite a few XInput titles utilize .NET for internal dependencies.

# Files

xinput1\_3.dll (Wrapper Library) - Translates XInput calls to DirectInput calls - supports old, non-XInput compatible GamePads.<br />
x360ce.exe - (Application) - Allows for editing and testing of Library settings.<br />
x360ce.ini - (Configuration) - Contain Library settings (button, axis, slider maps).<br />
x360ce.gdb - (Game Database) Includes required hookmasks for various games).<br />
Dinput8.dll - (DirectInput 8 spoof/wrapping file to improve x360ce compatibility in rare cases).<br />

# Installation

Run this program from the same directory as the game executable. XInput library files exist with several different names and some games require a change in its name. Known names:

  * xinput1\_4.dll (Windows 8 / metro apps only)
  * xinput1\_3.dll
  * xinput1\_2.dll
  * xinput1\_1.dll
  * xinput9\_1\_0.dll

Game Database (GDB) and Configuration (INI) files can be copied to %ALLUSERSPROFILE%\X360CE on NT6 (Vista/7/8) or %ALLUSERSPROFILE%\Application Data\X360CE on NT5.1 (XP), or be left in the same directory as the game's executable (.EXE)

Note:
  * In most cases games based on the Source engine have the DLL placed inside the /Bin/ sub folder, as it is loaded by inputsystem.dll instead of the game executable.
  * The DLL is not currently fully supported by the App, recent versions incorporate HookMasks and fully remove HookModes, and requires placing Version=1 under [options] in the x360ce.ini to quieten a warning about incorrect version.

# Uninstallation

Delete x360ce.exe, x360ce.ini and all XInput DLLs from the game's executable directory.

# Troubleshooting

Some games have control issues when Dead Zone is reduced to 0%.

You may need to increase the Anti-Dead Zone value if there is gap between the moment when you start to push the axis-related button and the reaction in game.

**Wheel doesn't work in the game, but it works inside x360ce Application.**

Some controllers will only operate in game if they are set as "GamePad". Try to:

  1. Run x360ce.exe
  2. Select the [Controller #] tab page corresponding to your controller.
  3. Open [Advanced] tab page.
  4. Set "Device Type" drop down list value to: GamePad.
  5. Click [Save] button.
  6. Close x360ce.exe.
  7. Run the game.
 
Only one controller, mapped to PAD1, may work correctly in some games. Try to:

  1. Run x360ce.exe
  2. Select the [Controller #] tab page corresponding to your controller.
  3. Open the [Direct Input Device] tab page (visible when the controller is connected).
  4. Set "Map To" drop down list value to: 1.
  5. Set "Map To" drop down list values *(repeat steps 2. to 4.)* for other controllers, if you have them, to: 2, 3 or 4.
  6. Click [Save] button.
  7. Close x360ce.exe.
  8. Run the game.
 
To use more than one controller in game, you may need to combine them. Try to:

  1. Run x360ce.exe
  2. Select the [Controller #] tab page corresponding to your additional controller.
  3. Open the [Advanced] tab page.
  4. Set "Combine Into" drop down list value to: One.
  5. Select [Options] tab page.
  6. Check "Enable Combining" check-box. *(Note: Uncheck "Enable Combining" check-box when you want to configure the controller.)*
  7. Click [Save] button.
  8. Close x360ce.exe.
  9. Run the game.

**Do I need to run x360ce Application during the game?**

The x360ce.exe application can be closed before launching the game; the game doesn't need it and it uses your computer's resources. The x360ce.exe application is just a GUI for editing x360ce.ini and testing your controller. 

**Warning - Configuration file version does not match x360ce version.**

This occurs because the current version of the application is not completely compatible with the DLL just yet.
You can resolve this by adding Version=1 under the [Options] section in x360ce.ini

**Controller tab won't turn green / Red light on Controller 1, 2 etc.**

This can occur for a number of reasons:

  1. The configuration utility DOES NOT work with the 64-bit library, it will only work with the 32-bit version, This is the case regardless of whether your Windows is 32-bit or 64-bit.
  2. The DInput state of the controller might be incorrect due to an application crashing previously and not unloading the controller or some other reason. Opening up Joy.cpl (Set Up USB Game Controllers) and clicking the Advanced button, and then Okaying out of the window that appears can fix it.
  3. The controller profile loaded may match the name of the controller but not actually be for the controller you own - in this case you might see button numbers or axes mapped that do not appear in the Joy.cpl test page.
  4. The controller profile might have PassThrough set. A growing number of profiles have been uploaded that do this, I have no idea why as it disables x360ce and just bloats the list of available profiles.
  5. There just might not be a profile for your control at all - The light should turn green at least once the 2 sticks, triggers and D-pad are assigned. Sometimes the application needs to be restarted after assigning these for the light to turn green.

# Screenshots

![http://www.jocys.com/projects/x360ce/Images/x360ce_General.png](http://www.jocys.com/projects/x360ce/Images/x360ce_General.png)
