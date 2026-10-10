---
name: x360ce
description: >-
  Operate X360CE only through its own AI door, never by controlling the screen, mouse or keyboard: configure
  and troubleshoot the Xbox 360 Controller Emulator (version 4, Windows). Use this skill whenever the person
  mentions x360ce or an Xbox 360 controller emulator, or asks why a game does not see their controller, wheel,
  pedals, joystick or gamepad, how to map buttons or axes, set a dead zone, fix rumble, vibration or force
  feedback, set up ViGEmBus or HID Hide, or make an old controller work as an XInput (Xbox) controller, even
  when they do not name x360ce.
license: LGPL-3.0
metadata:
  # The program writes its own version here when it installs this skill.
  version: "4.25.64.0"
---

# X360CE

X360CE maps real game controllers that Windows reads through DirectInput (wheels, pedals, joysticks, flight
sticks, older gamepads) to virtual Xbox 360 controllers. Games that only understand Xbox controllers (XInput)
then see them. Version 4 makes the virtual controllers with the ViGEmBus driver; nothing is copied into games.
It offers four virtual controllers, one per tab (Controller 1 to Controller 4). Mappings are kept per game: the
Game box at the top picks which game's settings the tabs show, and `x360ce.exe` itself is the default entry.

## Only through the door

Reach the program only through its door: the MCP tools or the `-Ai` command line below. It answers from what is
really on screen and connected, so ask it instead of answering from memory: settings and device names differ on
every computer. Do not read the program's settings files or logs instead. They lag behind the screen, and they
hold the access token, which is the person's to give.

Never control the screen, mouse or keyboard: no computer-use tool, no screenshot to decide where to click, no UI
Automation, no SendKeys or other simulated input, even when the environment offers them. The door does what the
person allowed and refuses the rest; controlling the screen would go around their choice.

- If the door cannot do something, say so, and `ui_show` the person where to do it themselves.
- "Show me" means `ui_show`: a frame with your words on the person's own screen, never a screenshot.
- If the door is off, ask the person to switch it on; never switch it on yourself.

## Connecting

The door is the person's to open: Options tab, then the AI page, AI assistant access, where they also choose its
level. Until they do, nothing can reach the program.

Use the first way that works:

1. **MCP tools.** If tools named `devices_list`, `ui_find` and `ui_show` are available (server `x360ce`), call
   them directly, with the arguments in the table below. An agent that connects by URL gets the address and the
   token from the person (Copy prompt on the AI page); the URL needs no token when the person allowed local
   connections without one.
2. **Command line.** Call the program itself from PowerShell; it passes the call to the running copy. Find it and
   make the first call in one command, which saves a turn:
   ```powershell
   $exe = (Get-Process x360ce)[0].Path; & $exe -Ai=devices_list | Out-String; "exit=$LASTEXITCODE"
   ```
   - Later calls: `& $exe -Ai=<tool> -<argument>=<value> | Out-String`. Start each command with the `$exe = ...`
     part again, because most agents run every command in a fresh shell. The pipe makes PowerShell wait for the
     answer.
   - Use the dash form `-Ai=`. Git Bash turns `/Ai=...` into a file path, and the program then answers nothing.
   - If x360ce is not running, ask the person where `x360ce.exe` is; it is a single file that can live anywhere.
   - The call uses the door of the copy that runs, whatever `/Profile` the person started it with.
   - Quote every `-<argument>=<value>` that holds a space or a dot: `"-query=dead zone"`,
     `"-text=This is the dead zone."`. PowerShell splits an unquoted argument at a dot.
   - Exit code 0: the answer is printed. 1: refused or failed, and the printed sentence says why. 2: the door is
     off; ask the person to switch it on as described above.
   - `& $exe -Ai | Out-String` lists every tool with the level it needs and its arguments. A tool refuses an
     argument it does not take and names the ones it does.

| Tool | Arguments | What it gives |
|---|---|---|
| `devices_list` | none | Every controller: Product, Vendor, Online, Controllers (1 to 4 for the current game), XInputPlaces, Source |
| `ui_current` | none | The window and page on screen, in a few hundred bytes |
| `ui_find` | `query` | Elements whose name, purpose, field name or path holds every word; best first, at most 40 |
| `ui_read` | `path` | That branch of the interface with its current values |
| `ui_show` | `path`, `text`, `seconds`, `walk` | Brings the page to the front and frames the element with your words; `seconds=0` keeps the frame until the person acts |
| `ui_hide` | none | Takes the frame away |
| `ui_script` | `script` | Several show, walk, hide, wait, click and set steps in one call |
| `help` | none | The program's help as Markdown, about 34 KB |
| `ui_set`, `ui_invoke` | `path`, and `value` to set | Sets a value, presses a button (Configure) |

`device_map`, `preset_apply`, `input_wait`, `input_log` and `settings_save` need Configure; the tool list gives their
arguments.

Answers are JSON. A compact way to look at a search:

```powershell
$exe = (Get-Process x360ce)[0].Path; (& $exe -Ai=ui_find "-query=Pad2 LeftThumb dead zone" | Out-String | ConvertFrom-Json) | ForEach-Object { '{0} | {1} | {2} | {3}' -f $_.Role, $_.Name, $_.Value, $_.Path }
```

The parentheses matter: without them Windows PowerShell 5.1 passes the whole array on as one item and prints
`System.Object[]`.

## Access levels

The person picks one on the same AI page:

- **Read**, the default: look at everything and point at things for the person; nothing can be changed.
- **Configure**: everything a person does on the tabs: set values, press buttons, map devices, save.
- **Administer**: also install or remove drivers and switch on debug mode.

When a tool is refused, the answer names the level it needs. Tell the person what you wanted to do and which level
it needs, and let them decide; do not look for a way around it.

## How to work

1. **Look first.** `devices_list` says which controllers there are, who made each (`Vendor`, so a device the person
   names by its brand is found here), whether each is connected, which of Controller 1 to 4 it is on for the
   current game, and where games read it. Call `ui_current` only when the question is about what is on screen now.
2. **Find.** `ui_find` takes a few words, matched separately in any order, best matches first; when more than 40
   match, a last element with Role `Note` says how many. Narrow a search with a page id among the words:
   - each controller tab is `Pad1TabPage` to `Pad4TabPage`, and its pages are `GeneralTabPage`, `ButtonsTabPage`,
     `DPadTabPage`, `TriggersTabPage`, `LeftThumbTabPage`, `RightThumbTabPage`, `ForceFeedbackTabPage` and
     `DirectInputTabPage`;
   - the other tabs are `GamesTabPage`, `DevicesTabPage`, `IssuesTabPage`, `HelpTabPage` and `OptionsTabPage`,
     whose pages include `VirtualDeviceTabPage`, `HidHideTabPage`, `UpdateTabPage` and `AiTabPage`.

   `ui_find` matches the program's own words, not values: a stick is a thumb (`Left Thumb Axis X`), a bumper a
   shoulder. An empty answer `[]` means no element is called that. Before saying the program cannot do it, search
   the help (`& $exe -Ai=help | Select-String '<word>'`): some things are done by a formula in a mapping box rather
   than by a setting of their own. Only when neither has it, say the program has no such setting, and offer the
   nearest real one if there is one. Never describe a setting, a value or a row you have not seen in an answer.
3. **Read a branch.** `ui_read` with a path returns that branch with current values. Keep it small: one controller
   tab is about 140 KB and the whole window more than half a megabyte, so read a page or a list, not a tab.
4. **Point.** `ui_show` brings the page to the front and frames the element, with your words in a balloon beside
   it, and waits for the seconds you give; 8 to 10 is enough. It works at Read, so it is the way to answer
   "where is...?" questions. When the person asks how to get somewhere, add `walk=true`: the program first opens
   each tab on the way and points at it, a moment each, so they see the clicks they would make.
   `ui_script` chains several such steps into a walkthrough.
5. **Change** (Configure). `ui_set` sets a value, `ui_invoke` presses a button, `device_map` puts a device on a
   controller, `preset_apply` loads a preset, `input_wait` waits for the person to press or move something, and
   `input_log` lists every press and movement the engine read over some seconds, in order, to check what a device sends.
   In a list (Role `Grid`), `ui_set` on the grid with a row index selects that row, which the pages below show;
   a row's check box, such as a mapped device's Enabled, is ticked with `ui_set` on `<grid>/rows/<n>/<column>` and
   `true` or `false`; a row's button is pressed with `ui_invoke` on the same kind of path. `ui_read` on the grid
   lists its rows with these paths.
   A call that opens a window, such as a question before a preset fills a controller, answers as soon as the
   window waits, with its path and the buttons that answer it: read it with `ui_read` and that path, then press
   one with `ui_invoke`, and what you asked carries on. A file chooser or a system message box cannot be read
   through the door; the answer says so, and the person answers it.
   **Mapping.** A device put on a controller for the first time has nothing mapped: the tab's Mapped devices grid
   shows its `Map %`, and its boxes stay empty until `preset_apply` loads a preset or the boxes are set. Each box on
   the General page (Role `List`) holds the control of the device that works one part of the Xbox controller, such
   as `Axis 1` or `IAxis 2` (inverted), but `ui_set` cannot pick a control by that name. The door sets a box by a
   formula: `ui_set` its `… formula` check box to `true`, read the box, which now holds the row's mapping as a
   formula, then `ui_set` the box to a formula that starts with `=`, such as `=a1` for Axis 1 or `=-a2` for Axis 2
   inverted. The help's formula section says what formulas can do and which letter names which control. Which axis
   moves which way shows only while the person moves the control. So finish and save what they asked for with the
   likely axes (a stick's are usually Axis 1 across and Axis 2 inverted up and down, `IAxis 2`, because most
   devices report up as the low end), then ask them to move it while you read the row's value, such as
   `Left stick value`. Until they have, tell them the direction is unchecked; never call it verified.
6. **Keep.** Changes are saved only by `settings_save`, the same as the Save All button. Read the value back
   afterwards to confirm it took.

Prefer pointing to changing: when the person asks where something is or how to do it, show them, and change it
only when they asked you to.

Some values are read only while their page is on screen: the ViGEmBus version and HID Hide's state on the Options
pages, and a device's live readings on its Direct Input page. If one reads empty, `ui_show` the page, then read it
again before calling it a fault.

## Where things are

- **Controller 1 to 4 tabs.** At the top, the devices on that controller, with an Enable check box per device.
  Below, the pages: General (the button and stick mapping, one box for each part of the Xbox controller: the left
  and right sticks are Left Thumb and Right Thumb, each with an Axis X and an Axis Y box; with a picture of the
  controller and an Input column that lights each control of the device as it moves), Buttons (how far a stick
  or pedal must move to press a button), D-Pad, Triggers, Left Thumb and Right Thumb (the dead zone, anti-dead
  zone and sensitivity of each trigger and stick axis), Force Feedback (rumble strength and motor tests, and for wheels a Wheel section: the
  centering spring with Auto and Centre damping, and Wheel range, the steering range sent to a Logitech wheel, 40
  to 900 degrees), and Direct Input (the device's raw readings).
  Steering is usually mapped to Left Thumb Axis X.
- **The light on each controller tab.** Its left half is the device, its right half the virtual controller. The
  tab's Value says the same in words: `ui_find` with `Pad1TabPage` lists that tab first.
- **XInput places**, in `devices_list` and on the Devices page, are where games read a controller. `Real N` is the
  device itself in place N, which a game reads even with x360ce closed. `Virtual N` is the controller x360ce makes
  from it. `Virtual N (waiting)` is a controller tab whose virtual controller has no place yet, because something
  else holds place N. Many games read only place 1, so a real Xbox controller there hides Controller 1's virtual
  one; Auto-Order on the Devices page moves it out of the way.
- **Source**, in `devices_list` and on the Devices page: a controller can appear twice, once per source. Map one of
  them, not both: the `RawInput` row for an Xbox One controller, which it reads while a game has the focus, and the
  `DirectInput` row for anything else. Force feedback reaches the controller from either.
- **Options** pages: General (start-up, theme, Enable XInput, hotkeys), Internet, Virtual Device (the ViGEmBus
  driver), HID Hide, HID Guardian (obsolete), Settings, Update, AI.
- **Games**: the games with their own settings. **Devices**: every device the program knows, with the XInput place
  of each controller and Auto-Order. **Issues**: problems the program found, each with what to do and often a Fix
  button.
- **Help**: the full help. Call `help` for it as Markdown; it is the authority on how a feature works.

## When a game does not respond

Work through these, reading each from the program rather than assuming it:

1. `devices_list`. Is the device connected (`Online`) and on a controller (`Controllers` not empty) for the game
   that is selected in the Game box? Its `XInputPlaces` says where games read it; `(waiting)` means its
   controller has no place yet.
2. The **Issues** tab. Find it with `ui_find` and `IssuesTabPage`, then read its list. Missing or broken ViGEmBus,
   a controller the game would see twice, a device that cannot be read: each row carries its remedy. Read a row
   before describing it.
3. The controller tab's light, read as the tab's Value, and its device's **Enable** box. Options, General,
   **Enable XInput** must be on, or no virtual controller exists.
4. The game. It must be set to use an Xbox or XInput controller; many games keep a separate controller option.
5. A controller waiting for its place, or games reading the wrong player: the Devices page shows the XInput place
   of each controller, and Auto-Order puts them in order.
6. Seen twice: HID Hide hides the real device from games (Options, HID Hide).
7. Games with anti-cheat may refuse virtual controllers; the help explains what X360CE can and cannot do there.

For wheels and pedals (combined pedals, wheel degrees, dead zones, the centering spring) the help has a section on
each. Read the one that fits with `help`, and find what it names with `ui_find` before pointing at it.

## Rules

- Never change, or ask to change, the AI assistant access settings (on/off, level, address, port, token) through
  the tools; the program refuses, and they are the person's to set.
- Never read the program's files for answers or for the token, never repeat the token, and never suggest listening
  on every network (0.0.0.0).
- Use Administer actions, such as installing or removing a driver, only when the person asked for that.
- Answer from what the tools return. If a tool cannot tell you, say what you could not check.

## Examples

**Which controller is which.** "What controllers do I have and which slot is each on?" Call `devices_list`. Answer
with each device's name, connected or not, and its Controller numbers; mention a device on none.

**Where a setting is.** "Where do I set the dead zone for my wheel?" `devices_list` shows the wheel on Controller 2.
`ui_find` with `Pad2 LeftThumb dead zone`, then `ui_show` the dead zone slider with a short sentence. Tell the
person which tab and page it is on.

**A setting that does not exist.** "Turn on gyro aiming." `ui_find` with `gyro` returns `[]`, and the help says
nothing of it. Say X360CE has no gyro setting, and do not invent one; if the person wants motion controls, say this
program does not offer them.

**The game ignores the pad.** `devices_list` shows the pad on Controller 1 with `XInputPlaces` `Virtual 1
(waiting)`: something else holds place 1, so no virtual controller reaches the game. Say what holds it if the
program says so, and point at Auto-Order on the Devices page with `ui_show`; press it only if asked.

**Access is off.** The command line exits with 2, or the tools are missing. Tell the person: Options tab, AI page,
tick AI assistant access, and choose Read to let you look or Configure to let you change settings.

## Reference files

Beside this skill, in `references/`, are the help and the interface description of each program, named for its
version. The program also prints them, whether AI assistant access is on or not:

- `help-v4.md`, the help version 4 shows: `x360ce.exe -Skill=help`, or the `help` tool.
- `ui-tree-v4.md`, every element of version 4's interface with its purpose: `x360ce.exe -Skill=ui-tree`, or the
  `ui_tree` tool. `ui-tree-v4.json` is the same for a program to read.
- `help-v3.md`, `ui-tree-v3.md` and `ui-tree-v3.json`: the same for version 3, the older program that works through a
  library copied into the game's folder. It has AI assistant access too; `x360ce.exe -Ai` lists its tools.

Any of them prints with `x360ce.exe "-Skill=<name>"`, such as `"-Skill=ui-tree-v3.json"`. `x360ce.exe -Skill`
alone prints this file, which is how an agent without it can learn the program.

## Keeping this skill current

The `version` line at the top names the program this copy describes; the program writes it there. A copy from a
skill site or from the source repository describes the newest build, and the program on the person's computer may
be older or newer. Before relying on a page, button or tool named here:

1. Read the running program's version: the first line of `& $exe -h | Out-String`.
2. If it differs from this skill's version, read the program's own copy with `& $exe -Skill | Out-String` and
   follow that instead: it names exactly what that program has.
3. To replace the installed copy, ask the person, then have the program write its own into the skills folder the
   agent reads: `& $exe "-Skill=$env:USERPROFILE\.claude\skills"` for Claude Code, or
   `& $exe "-Skill=$env:USERPROFILE\.agents\skills"` for Codex, GitHub Copilot, Gemini CLI, Cursor and OpenCode.
   The person can do the same with Install or Update on the Options tab's AI page, and the Issues tab offers it
   when an installed copy is older than the program.

The newest copy is published with the program's source at `github.com/x360ce/x360ce`, in `skills/x360ce`, where
skill installers find it, for example `npx skills add x360ce/x360ce`.
