XBOX 360 Controller emulator
============================

Version 3.1.4.0
===============

Configuration
=============

Edit x360ce.ini, options have comments how to use.
You may use included XInputTest.exe to test gamepad.

Installation
============

Copy xinput dll and x360ce.ini to game executable directory
The xinput dll can have several names, you can rename the dll to those below till you find one that works:

xinput9_1_0.dll
xinput1_3.dll
xinput1_2.dll
xinput1_1.dll

Fake API calls
==============

Version 3.1 include new method to fake API calls using Detour library. 
That metod is for more compatibility with games. Games souch like Trine, Red Faction Guerrilla and other will now work.
For use this new feature you must configure properly x360ce:

	Set main option "FakeAPI" to 1
	Get and set correct VID/PID for you gamepad in pad options

To get VID/PID of gamepad:

	Open "Device Manager"
	In "Human Interface Devices" find "HID-compliant game controller"
	Double click to open properties, go to "Details" tab
	From combo box select "Hardware Ids"
	In listbox you will see a list of id like: "HID\VID_044F&PID_B323"
	So you VID is a hex number after "VID_" ie. in upper example 0x044F and PID 0xB323
	Write valuses to x360ce.ini
	Play :)

If you using this method you not need dinput8 dlls to block/spoof directinput, it done automaticaly and better (WIM is also spoofed)

Know Issues
===========

Prototype regression:
	Game will work very slow. crazycat create a patch for force vibration and sound mnormalization.
	First fix not only vibration, also fix slowness, but for me sound normalization breaks some sounds, so I created a patch without it.
	Available here: http://virusdev.ovh.org/files/xinputemu/prot_x360ce_supp.zip
	All credits go to crazycat

H.A.W.X:
	Currently working for making game to work property using xinput, crazycat write to me with a fix, but that fix already tried.
	So I was done some debbuging, and this game has 3 complicated checks for xinput, but get it to work with xinput - with problems, ie. currently state of this game is WIP

Uninstallation
==============

Delete x360ce.ini, xinput1_3.dll file, and dinput8.dll (if used).


THANKS:
=======

Racer_S for write orginal 3.0 version
pkt-zer0 for help fixing bugs and rewrite some fuctions
Wilds & Seph for Wilds Mod (support for Guitar Hero 4)
Big thanks going to TomeQ for writing with me deutor fuctions
Nexor for dumping data from orginal XBOX 360 Wired Controller
crazycat for game patches

