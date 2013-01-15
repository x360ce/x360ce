XBOX 360 Controller emulator
============================

If you appreciate my work, feel free to donate: https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=J4LS7ZXWPN66A&lc=US&item_name=x360ce%20donation&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHostedGuest

Version 3.3.1.x
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

Version 3.3 include new method to hook API calls using EasyHook library. 
That metod is for more compatibility with games. Games souch like Trine, Red Faction Guerrilla and other will now work.
If you using this method you not need dinput8 dlls to block/spoof directinput, it done automaticaly and better (WIM is also spoofed)

Know Issues
===========

Game that initializes xinput DLL dynamically by using LoadLibary will not work with this and all older versions.
Some gamepad FFB drivers may crash with ForceFeedback enabled.

Uninstallation
==============

Delete x360ce.ini, xinput1_3.dll (or any other name of it).

THANKS:
=======

Racer_S for write orginal 3.0 version
pkt-zer0 for help fixing bugs and rewrite some fuctions
Wilds & Seph for Wilds Mod (support for Guitar Hero 4)
Big thanks going to TomeQ for writing with me deutor fuctions
Nexor for dumping data from orginal XBOX 360 Wired Controller
crazycat for game patches

