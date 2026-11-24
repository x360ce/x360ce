# WheelTelemetry.Games

An x360ce plugin. How plugins work, and how to write one: ../plugins.md.

- Source: embedded in x360ce.exe
- Signed by: part of x360ce
- State: loaded

## Wheel Telemetry Games

- Kind: game plugin (`IPluginGame`), a tab named Wheel Telemetry Games on the Games tab while a game it is for is selected.
- Its page: WheelTelemetryGamesPage, then Help with the text below.

Helps a game send its telemetry to x360ce's Wheel Telemetry plugin, so your wheel feels the road. Its games: GTA V (Legacy and Enhanced), Forza Horizon 6 and F1 25. Each game's own steps are on its page; these are the steps for every game.

**Read the warnings first.** A game's page starts with them. GTA V needs mods: play story mode only, switch BattlEye off in the game's launcher, and remove the mods before you play GTA Online. Rockstar can ban a player for mods, and x360ce cannot promise it will not.

1. On the Options tab, open the Plugins page.
2. Tick Enabled on the WheelTelemetry.Games row. It is off until you tick it.
3. On the Games tab, press Add... and choose the game's program file, or press Scan.
4. Select the game in the list.
5. Under the list, open the Wheel Telemetry Games tab. Its first page is named after the game.
6. Read the warnings at the top of the page.
7. Do the page's steps in order. For a mod, press Official page, download it, press Open game folder, and unzip it there. Its line says Found when it is there. x360ce does not include or download mods.
8. Where the page has Install, press it. It writes only the files it lists into the game's folder. Remove deletes them.
9. Leave Port at 37150, where x360ce's Wheel Telemetry plugin listens. A game set in its own menus is given the port in its steps.
10. Open the controller tab your wheel is mapped to, such as Controller 1. Its device list shows the wheel, ticked.
11. On its Wheel tab, tick Centering spring. x360ce uses a game's telemetry only while the centering spring is on, for a wheel with force feedback.
12. On its Game Telemetry tab, on the Telemetry page, keep Enable live game telemetry ticked and Listen port at 37150, the port the game sends to. Both are so at first.
13. Start the game and drive.
14. On the Game Telemetry tab, Live Telemetry shows the format, the speed and the packets a second, such as Forza Data Out - 72 km/h (60 Hz). It says Waiting for game telemetry... until packets arrive.

**When nothing arrives**

- The game and Listen port must name the same port. The line under the status says whether another program holds it.
- The game sends to 127.0.0.1, this computer, so no firewall rule is needed.
- Forza Horizon 6 from the Microsoft Store or the Xbox app needs the one command its page shows.
- F1 25 sends what x360ce reads with UDP Format 2025.
- GTA V: after the game is updated, Script Hook V works again only once its own update for that game version is out. Download it again. Script Hook V .NET writes ScriptHookVDotNet.log in the game's folder; an error about x360ce.Telemetry.3.cs there says why the script did not start.

**To remove everything**

1. Press Remove on the game's page.
2. Delete from the game's folder the files you unzipped there, such as ScriptHookV.dll, dinput8.dll or xinput1_4.dll, and Script Hook V .NET's files.
3. In a game set in its own menus, switch its telemetry off there.
4. On the Options tab's Plugins page, untick Enabled on the WheelTelemetry.Games row.
