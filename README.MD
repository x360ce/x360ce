"Xbox 360 Controller Emulator" allows your controller (gamepad, joystick, wheel, ...), function as "Xbox 360 Controller". For example, lets you play games such as "Grand Theft Auto" (GTA) or "Mafia II" using Logitech Wheel.

# Guides

[x360ce Libraries Tutorial](http://ngemu.com/threads/x360ce-libraries-tutorial-by-resolutespider5.155310/)

[x360ce App Tutorial](http://ngemu.com/threads/tutorial-by-resolutespider5.155011/)

[Various x360ce Guides](http://ngemu.com/threads/various-x360ce-guides-by-resolutespider5.156807/)

# Compatibility
Games that work with x360ce can be found on the [Compatibility List](Wiki/CompatibilityList.md). If you would like to add to that list or provide updated information place see [Reporting Compatibility](Wiki/ReportingCompatibility.md).


# System Requirements

  1. Windows XP SP3 and newer.
    * Windows XP SP3 is only supported in VS2010 library.
  1. [.NET 3.5 (also installs 2.0 and 3.0)](http://www.microsoft.com/en-us/download/details.aspx?id=21) (included in Windows 7)
  1. [.NET 4.0 (link to 4.5, also installs 4.0)](http://www.microsoft.com/en-us/download/details.aspx?id=30653) (included with Windows 8)
    * .NET 4.5 is not supported on Windows XP SP3, download .NET 4.0 from [here](http://www.microsoft.com/en-us/download/details.aspx?id=17851).
  1. [DirectX End-User Runtimes (June 2010)](http://www.microsoft.com/en-us/download/details.aspx?id=35) (Required regardless of OS)
  1. Depending on the library, either one or both of the following redistributable:
    * [Visual C++ Redistributable for Visual Studio 2010 SP1](http://www.microsoft.com/en-us/download/details.aspx?id=26999)
    * [Visual C++ Redistributable for Visual Studio 2013](http://www.microsoft.com/en-us/download/details.aspx?id=40784)

Notes:
  * Most games executables are 32bit and require x86 x360ce and also x86 redistributable.
  * Windows 7 includes .NET 3.5 (which include 3.0 and 2.0).
  * For Windows XP and Vista, users can obtain it from the above link, and Windows 8 users can enable it in [Programs and Features - Turn Windows Features on and off.](http://msdn.microsoft.com/en-us/library/hh506443.aspx)
  * **.NET MUST be installed prior to the DirectX webupdate as it checks that .NET 2.0/3.0/3.5 is installed and skips the Managed DirectX framework if it is not.**
  * The full DirectX Redistributable will always install the file, however its best to have .NET 3.5 installed anyhow as quite a few Xinput titles utilize .NET for internal dependencies.
  * **Please also look here for more in depth guides: https://code.google.com/p/x360ce/wiki/Manuals***

# Introduction

"Xbox 360 Controller Emulator" files:

xinput1\_3.dll (Library) - Wrapper library that translates the XInput calls to DirectInput calls, for support old, no XInput compatible GamePads.

x360ce.exe - (Application) - Allows edit and test Library settings.
x360ce.ini - (Configuration) - Contain Library settings (button, axis, slider maps).
x360ce.gdb - (Game Database) Includes required hookmasks for various games)
Dinput8.dll - (DirectInput 8 spoof/wrapping file to improve x360ce compatibility in rare cases)

# Details

## Installation

Run this program from the same directory as the game executable. Xinput library files exist with several different names and some games require a change in its name.

Known names:

  * xinput1\_4.dll (Windows 8 / metro apps only)
  * xinput1\_3.dll
  * xinput1\_2.dll
  * xinput1\_1.dll
  * xinput9\_1\_0.dll

Game Database (GDB) and Configuration (Ini) files can be copied to %Allusersprofile%\X360CE on NT6 (Vista/7/8) or %Allusersprofile%\Application Data\X360CE on NT5.1 (XP), Or left in the same directory as the game executable (.EXE)

Note:
  * Games based on source engine in most cases have the dll placed inside the /Bin/ subfolder, as it it is loaded by inputsystem.dll instead of the game executable.
  * The DLL is not currently fully supported by the App, recent versions incorporate HookMasks and fully remove HookModes, and requires placing Version=1 under [options] in the x360ce.ini to quieten a incorrect version warning

## Uninstallation

Delete x360ce.exe, x360ce.ini and all xinput dll from game executable directory.

# Troubleshooting

**Wheel doesn't work in the game, but it works inside x360ce Application.**

Some games will only operate when the controller is considered to be the gamepad, even if it is the steering wheel. Try to:

  1. Run x360ce.exe
  1. Select tab with your Wheel Controller.
  1. Open [Advanced] tab page.
  1. Set "Device Type" drop down list value to: GamePad
  1. Click [Save] button.
  1. Close x360ce Application, run game.

**How to reduce wheel dead zone (GTA, Mafia II, ...)?**

  1. Run x360ce.exe
  1. Select tab with your Wheel Controller.
  1. Open [Advanced] tab page.
  1. Select "Enabled (XInput, 80%)" from "AntiDeadZone" drop down in order to reduce dead zone by 80%.
  1. Click [Save] button.
  1. Close x360ce Application, run game.

> Note: Some games have control issues when the dead zone is reduced by 100%.

**Do I need to run x360ce Application during the game?**

No, You do not need. Close x360ce during the game, because the game does not need it, and the application uses computer resources. The application is just a GUI for editing the x360ce.ini and test controller.

**Warning - Configuration file version does not match x360ce version.**

This occurs because the current version of the application is not completely compatible with the DLL just yet.
You can resolve this by adding Version=1 under the [Options] section in x360ce.ini

**Controller tab won't turn green / Red light on Controller 1,2 etc.**

This can occur for a number of reasons.

  1. The configuration utility DOES NOT work with the 64bit library, it will only work with the 32bit version, This is the case regardless of whether your windows is 32bit or 64bit.
  1. The DInput state of the control might be incorrect due to previous application crashing and not unloading the control or some other reason. Opening up Joy.cpl (Set Up Usb Game Controllers) and clicking the Advanced button, and then Okaying out of the window that appears can fix it.
  1. The controller profile loaded may match the name of the control but not actually be for the controller you own - in this case you might see button numbers or axes mapped that do not appear in the joy.cpl test page.
  1. The controller profile might have PassThrough set. A growing number of profiles have been uploaded that do this, i have no idea why as it disables x360ce and just bloats the list of available profiles.
  1. There just might not be a profile for your control at all - The light should turn green once atleast the 2 sticks, triggers and Dpad are assigned. Sometimes the application needs to be restarted after assigning these for the light to turn green.

# Screenshots

![http://www.jocys.com/projects/x360ce/Images/x360ce_General.png](http://www.jocys.com/projects/x360ce/Images/x360ce_General.png)