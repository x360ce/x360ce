For games NOT listed in the Game [Compatibility List](CompatibilityList.md)

As of r574, compatibility is very different, this guide will detail how to report which hookmasks work for which games, how to use the hookmasks and I will compile update the Compatibility page over time.

**HookMode is removed entirely as of r574!.**

Games that use Xinput only usually do not require a hooking mask, but others do, particularly those that use unusual methods for detecting Xbox360 pads or support both Xinput and DirectInput pads(these are usually requiring HookCOM).


**X360CE.GDB**

In the GDB file, the HookMasks (HookMask=) are written using a combination of these hex values

  * LoadLibrary(HookLL)		= 0x00000001;
  * COM(HookCOM)			= 0x00000002;
  * SetupAPI(HookSA)		= 0x00000020;
  * WinVerifyTrust(HookWT)		= 0x01000000
  * DirectInput(HookDI)		= 0x00000004;
  * ProductID/VendorID(HookPIDVID)	= 0x00000008;
  * Spoof name(HookName)		= 0x00000010;

Hookmask 0x01000023 enables all for example.
Typically, only a single value will be required in the GDB.

There are also some other values that are really only useful to the developers such as


  * Stop hooking(HOOKSTOP)        = 0x02000000;
  * Disable (HOOKDISABLE) = 0x80000000;


**X360CE.INI**

In the INI, they are entered under the InputHook section as

  * HookLL=(0/1)
  * HookCOM=(0/1)
  * HookSA=(0/1)
  * HookWT=(0/1)
  * HOOKDI=(0/1)
  * HOOKPIDVID=(0/1)
  * HookName=(0/1)

Whilst you can just enable all 4 and forget it, it is advised to only use one of the first 3 at a time, with the 4th (HookWT) only being used specifically in games that require it, we will not simply enable all modes for a game in the GDB without significant proof (and self replication) of it being required as this =may= introduce undesired behavior

Don't hesitate to report if you find any games that crash with all them set, it would be good if it could eventually default to all on and be done with it.
See the thread rules below for further information.

# What HookMasks Do

**HookCOM**
  * Games that support both Direct and X Input API's may end up displaying both controllers in game, resulting in ghosted input or being able to start a 2 player game using a single control (SF4/SSF4 and DMC for example). [HookCOM allows the game to mask the controller's directinput capabilities, which is advised by microsoft themselves on TechNet](http://msdn.microsoft.com/en-us/library/windows/desktop/ee417014(v=vs.85).aspx). HookCOM is the method required in most cases of games not working.

**HookLL**
  * Many XACT games (that is, the Xbox360/PC cross  platform games) require HookLL to run properly. As they do not load the  xinput runtime directly, Dinput8 wrapper is required to redirect back to the local X360CE binary instead of loading the default xinput1/9\_x binary.

**HookSA**
  * This enables the hooking of SetupAPI, only the BeatHazard titles are known to require it thus far, so it should almost never be needed.

**HookWT**
  * This enables the hooking of WinVerifyTrust. This is required for games which utilise WVT for Process Integrity checking.
Only Gears of War is known to use it at this point, so it should almost never be needed.

**HOOKDI**
  * This allows the wrapper to trick some games that detect the controller GUID via DirectInput, The first Assassins Creed is one such title.

**HOOKPIDVID**
  * This works in conjunction with FakePID= and FakeVID=
Without these, it defaults to the Wired Xbox 360 Controller which are 0x28E and 0x45E respectively.

**HookName**
  * This allows x360ce to return a different oemname than that of the controller installed. For instance, in Assassins Creed it changes the name of the xinput device to Xbox 360 Controller. While this is generally not required for functionality, the game mini ninja's is known to check for "Xbox 360 Controller" in the registry and will not work if the name is anything else.

Many games will work even without any of the above set, It would be good to know these as well.


# Rules:
  1. [This thread](http://forums.ngemu.com/showthread.php?t=155113) is specifically for reporting games found to be compatible. You can also use our Issue Tracker
  1. When reporting game compatibility, the HookMask(s) used, full game title and executable name are required for adding to the GDB.
  1. If you find a game crashing that is listed in the compatibility list, its likely not the wrapper, but an issue with your controllers drivers - particularly if it uses an obscure (probably bad) Force Feedback driver. Start your **own** thread or report the issue to the issue tracker.
  1. If a game is not working with any settings, start a thread of your own or report it to the issue tracker.
  1. Do not post in the thread about a game already listed unless you have evidence that settings provided by the original reporter are wrong.