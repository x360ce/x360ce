# AutoHotkey scripts

An [AutoHotkey v2](https://www.autohotkey.com/) script can watch a controller the way it watches keys, change what the
controller's mapping reads, and turn a controller into a keyboard or a mouse, or a keyboard and mouse into a controller.

x360ce adds only what AutoHotkey cannot do by itself: an object for each device, whose properties are its controls.
Everything else is AutoHotkey as you know it: `Send()`, `Sleep()`, `SetTimer()`, `MouseMove()`, `WinActive()`,
`#HotIf` and key hotkeys such as `w::` or `XButton2::`.

```autohotkey
#Requires AutoHotkey v2.0
#Include <x360ce>

dev := X360ce.c1.d1                        ; device 1 on Controller 1
dev.Hotkey("b1", (*) => Send("{Space}"))   ; its button 1 presses the space bar
XButton2::dev.b2 := 1                      ; the mouse's side button presses its button 2
XButton2 up::dev.b2 := 0
```

One script reaches every device on every controller tab, whether x360ce reads it through DirectInput, Raw Input or a
plugin. It can start and stop while x360ce runs; nothing is copied into games.

## Turning it on

1. `[Options]` tab → `[Plugins]` → tick `Allow AutoHotkey scripts`. x360ce writes `x360ce.ahk` to
   `Documents\AutoHotkey\Lib`, where AutoHotkey finds it.
2. Start a script with `#Include <x360ce>`, and run it as any AutoHotkey script.

## Online games and anti-cheat

Some online games forbid scripts, and their anti-cheat can refuse to run or flag the player while AutoHotkey runs,
whatever the script does. Check a game's rules before using a script in it online, as for virtual controllers: see
[Games with anti-cheat](Help.v4.md#games-with-anti-cheat).

## How `#Include <x360ce>` connects the script to x360ce

`#Include <x360ce>` adds the file `x360ce.ahk` to your script. The angle brackets tell AutoHotkey to look in its
library folders, and `Documents\AutoHotkey\Lib` is one of them. A copy of `x360ce.ahk` beside the script works too,
with `#Include x360ce.ahk`. The file is plain AutoHotkey, which you can open and read: no DLL, nothing installed into
AutoHotkey, nothing copied into games.

x360ce.exe and the AutoHotkey program running your script stay two separate programs. They talk through shared
memory: a small block of memory that both can see, which x360ce makes while `Allow AutoHotkey scripts` is ticked.

```text
   controllers: pads, wheels, pedals
         │
         ▼
┌─ x360ce.exe ─────────────────────────┐  shared memory  ┌─ AutoHotkey64.exe ───────────────────┐
│ 1000 passes a second:                │                 │ your script, with x360ce.ahk:        │
│ reads each device ───────────────────┼─ state ────────►│ x := dev.a4                          │
│ a watched control changed ───────────┼─ message ──────►│ dev.Hotkey functions run             │
│ applies commands next pass           │◄─ commands ─────┤ dev.b1 := 1, dev.a2 := ""            │
│ checks the script is there           │◄─ heartbeat ────┤ a heartbeat every 15 ms              │
│ maps, combines                       │                 │ Send, MouseMove ─────────────────────┼──► keyboard, mouse
└────────┬─────────────────────────────┘                 └──────────────────────────────────────┘
         │
         ▼
   virtual Xbox controllers ──► game
```

1. When the script starts, the library opens the shared memory and tells x360ce which window to send messages to. If
   x360ce is not running yet, it tries again with every heartbeat, so the two can start in either order.
   `X360ce.Connected` is true while x360ce applies what the script sends.
2. `dev.Hotkey` writes there which controls the script watches, and which buttons it takes over.
3. On every pass, x360ce writes each device's buttons, axes, sliders and hats there. When a control the script
   watches changes, x360ce posts a message to the script's window. The library reads what changed and calls your
   function, as AutoHotkey calls a key's hotkey.
4. Reading a control, `x := dev.a4`, reads the memory directly and waits for nothing. Setting one, `dev.b1 := 1`,
   writes a command there. x360ce applies it on its next pass, to the device's own controls, before the controller's
   mapping: your mappings, dead zones and formulas then apply to what the script set, as they do to the device.
5. A script answers a press about a millisecond later: x360ce reads the press on one pass, and the script's answer is
   there for the next.
6. x360ce never waits for the script, so a busy or stuck script cannot slow the controllers down. When the script
   closes or crashes, x360ce lets go of everything it held on its next pass, and the devices work as if no script had
   run. A script that hangs is found by its heartbeat, which it sends every 15 ms, as often as AutoHotkey's timers go:
   after 250 ms without one, x360ce lets go the same way. Both times are settings in the `AutoHotkey scripts` group of
   `[Options]` tab → `[Plugins]`. A shorter wait for a hung script lets go sooner, but a script busy for longer than it,
   such as in a long `Critical` section, then loses what it held.

## Naming a device and a control

A device is `X360ce.c{controller}.d{device}`: `X360ce.c1.d2` is device 2 on Controller 1.

- **`c1` to `c4`**: the controller tab the device is on.
- **`d1`, `d2`...**: the device on that tab. The `AHK` column of the tab's device list shows each device's name,
  such as `c1.d2`, while scripts are allowed. The number stays when a row is unticked or the list is sorted.

Give a device a name of your own by keeping it in a variable: `g27 := X360ce.c1.d1`, then `g27.a1`.

Its controls are properties named as in [formulas](Help.v4.md#controls-you-can-read), numbered as the tab's
`Direct Input` page shows them as you press or move them:

| Property | Control | Reads | Set it to |
|---|---|---|---|
| `b1` to `b128` | Button | 1 pressed, 0 let go | 1 to press, 0 to let go |
| `a1` to `a24` | Axis, such as a stick | -1 to 1 | -1 to 1 to hold it there |
| `x1` to `x24` | The upper half of an axis | 0 to 1 | cannot be set |
| `s1` to `s8` | Slider, such as a pedal | 0 to 1 | 0 to 1 to hold it there |
| `h1` to `h8` | The upper half of a slider | 0 to 1 | cannot be set |
| `p1` to `p4` | Hat | degrees, -1 at rest | degrees, or -1 for at rest |
| `d1` to `d16` | A hat direction: `d1` to `d4` are hat 1 up, right, down and left; `d5` starts hat 2 | 1 pressed, 0 let go | 1 to push, 0 to let go |

A control reads what the device itself reports, not what the script set. Set any control to `""` to give it back to
its device. A control reads `""` while x360ce does not share it: x360ce is not running, scripts are not allowed, or the
controller has no such device.

The properties are made once, when the script first names the device, so reading or setting one takes nothing apart.

A formula names controls the same way. It may leave the device out, because a formula belongs to one device already;
with it, `=c1.d1.b1` reads button 1 of device 1 on Controller 1 from any mapping.

## Hotkeys

`dev.Hotkey(name, fn)` calls `fn` when one of the device's controls is pressed or let go, as AutoHotkey's
`Hotkey()` does for keys:

| Name | Calls `fn` when |
|---|---|
| `"b1"` | button 1 is pressed |
| `"b1 up"` | button 1 is let go |
| `"~b1"` | button 1 is pressed, and the controller still sees it too |
| `"d1"` | hat 1 is pushed up |
| `"a1 > 80%"` | axis 1 passes 80 % one way; `"a1 < -80%"` the other way |
| `"a1 > 80% up"` | axis 1 comes back from past 80 % |
| `"s1 > 50%"` | slider 1 passes half way |

- A button a hotkey names without `~` belongs to the script: the controller reads only what the script sets for it.
  With `~`, or for a button no hotkey names, the script's presses are added to the device's.
- An axis, slider or hat is the device's until the script sets it, and again once it sets `""`.
- An axis or slider hotkey fires once when it passes its limit, and its `up` hotkey once when it comes back. It does
  not repeat while the stick rests on the limit.
- One script at a time. A second one waits, and connects when the first closes.
- `Sleep()` and `SetTimer()` step every 10 to 16 ms, as they do in every AutoHotkey script: `Sleep(40)` waits 40 to 47 ms.

## Examples

### Change what a controller reads

```autohotkey
#Requires AutoHotkey v2.0
#Include <x360ce>

dev := X360ce.c1.d1

; Button 1 belongs to the script: each press becomes two quick presses.
dev.Hotkey("b1", DoubleJump)

DoubleJump(*) {
	dev.b1 := 1
	Sleep(40)
	dev.b1 := 0
	Sleep(40)
	dev.b1 := 1
	Sleep(40)
	dev.b1 := 0
}

; Button 2 still works (~), and holds button 6 too.
dev.Hotkey("~b2", (*) => dev.b6 := 1)
dev.Hotkey("~b2 up", (*) => dev.b6 := 0)

; Turbo: while button 3 is held, button 3 is pressed ten times a second. Without a script, [Turbo] in the
; mapping menu does the same with a formula.
dev.Hotkey("b3", (*) => SetTimer(Turbo, 50))
dev.Hotkey("b3 up", StopTurbo)

Turbo() {
	static down := false
	down := !down
	dev.b3 := down
}

StopTurbo(*) {
	SetTimer(Turbo, 0)
	dev.b3 := 0
}

; The stick pushed past 80 % to the right holds button 5.
dev.Hotkey("a1 > 80%", (*) => dev.b5 := 1)
dev.Hotkey("a1 > 80% up", (*) => dev.b5 := 0)

; Button 4 on Controller 1 presses button 4 on Controller 2: one player helps another.
helper := X360ce.c2.d1
dev.Hotkey("b4", (*) => helper.b4 := 1)
dev.Hotkey("b4 up", (*) => helper.b4 := 0)
```

### A keyboard and mouse drive a controller

The script changes what the device on Controller 1 reports, and the controller's mapping turns that into the Xbox
controller the game sees. Here axes 1 and 2 drive the left stick and buttons 1 to 4 drive A, B, X and Y, as most
presets map them, so Controller 1 needs a device mapped.

```autohotkey
#Requires AutoHotkey v2.0
#Include <x360ce>

dev := X360ce.c1.d1

; Only while the game is in front; in any other window the keys work as keys. Put the game's file name here.
#HotIf WinActive("ahk_exe game.exe")

; W, A, S and D push the left stick; let go, and the stick is the device's again.
w::dev.a2 := -1
w up::dev.a2 := ""
s::dev.a2 := 1
s up::dev.a2 := ""
a::dev.a1 := -1
a up::dev.a1 := ""
d::dev.a1 := 1
d up::dev.a1 := ""

; Space jumps, E uses.
Space::dev.b1 := 1
Space up::dev.b1 := 0
e::dev.b3 := 1
e up::dev.b3 := 0

; The mouse's side buttons press B and Y.
XButton1::dev.b2 := 1
XButton1 up::dev.b2 := 0
XButton2::dev.b4 := 1
XButton2 up::dev.b4 := 0

; The arrow keys push the hat, which most presets map to the D-dev.
Up::dev.d1 := 1
Up up::dev.d1 := 0
Down::dev.d3 := 1
Down up::dev.d3 := 0

#HotIf
```

### A controller as a keyboard

```autohotkey
#Requires AutoHotkey v2.0
#Include <x360ce>

dev := X360ce.c1.d1

; The hat as arrow keys, held while held.
dev.Hotkey("d1", (*) => Send("{Up down}"))
dev.Hotkey("d1 up", (*) => Send("{Up up}"))
dev.Hotkey("d2", (*) => Send("{Right down}"))
dev.Hotkey("d2 up", (*) => Send("{Right up}"))
dev.Hotkey("d3", (*) => Send("{Down down}"))
dev.Hotkey("d3 up", (*) => Send("{Down up}"))
dev.Hotkey("d4", (*) => Send("{Left down}"))
dev.Hotkey("d4 up", (*) => Send("{Left up}"))

; Buttons as keys.
dev.Hotkey("b1", (*) => Send("{Enter down}"))
dev.Hotkey("b1 up", (*) => Send("{Enter up}"))
dev.Hotkey("b2", (*) => Send("{Escape}"))
dev.Hotkey("b3", (*) => Send("{Space down}"))
dev.Hotkey("b3 up", (*) => Send("{Space up}"))

; The left stick as W, A, S and D past half way.
dev.Hotkey("a2 < -50%", (*) => Send("{w down}"))
dev.Hotkey("a2 < -50% up", (*) => Send("{w up}"))
dev.Hotkey("a1 < -50%", (*) => Send("{a down}"))
dev.Hotkey("a1 < -50% up", (*) => Send("{a up}"))
dev.Hotkey("a2 > 50%", (*) => Send("{s down}"))
dev.Hotkey("a2 > 50% up", (*) => Send("{s up}"))
dev.Hotkey("a1 > 50%", (*) => Send("{d down}"))
dev.Hotkey("a1 > 50% up", (*) => Send("{d up}"))

; Copy and paste, only in Notepad.
dev.Hotkey("b5", (*) => WinActive("ahk_exe notepad.exe") && Send("^c"))
dev.Hotkey("b6", (*) => WinActive("ahk_exe notepad.exe") && Send("^v"))
```

### A controller as a mouse

Which axes a stick uses differs between devices: many pads report the right stick as axes 3 and 6, Xbox pads read
through DirectInput as 4 and 5.

```autohotkey
#Requires AutoHotkey v2.0
#Include <x360ce>

dev := X360ce.c1.d1

; The right stick moves the pointer.
SetTimer(MovePointer, 10)

MovePointer() {
	x := dev.a4
	y := dev.a5
	if x = "" || y = ""
		return
	; A dead zone in the middle, then faster the further the stick is pushed.
	dx := Abs(x) < 0.15 ? 0 : x * Abs(x) * 25
	dy := Abs(y) < 0.15 ? 0 : y * Abs(y) * 25
	if dx || dy
		MouseMove(dx, dy, 0, "R")
}

; Buttons as mouse buttons, held while held, so dragging works.
dev.Hotkey("b1", (*) => Click("Down"))
dev.Hotkey("b1 up", (*) => Click("Up"))
dev.Hotkey("b2", (*) => Click("Right Down"))
dev.Hotkey("b2 up", (*) => Click("Right Up"))

; The hat scrolls.
dev.Hotkey("d1", (*) => Send("{WheelUp}"))
dev.Hotkey("d3", (*) => Send("{WheelDown}"))
```

### Type into Notepad

Every key goes to Notepad only while Notepad is the active window, so nothing is typed anywhere else.

```autohotkey
#Requires AutoHotkey v2.0
#Include <x360ce>

dev := X360ce.c1.d1
InNotepad() => WinActive("ahk_exe notepad.exe")

; Whole words with one button.
dev.Hotkey("b1", (*) => InNotepad() && SendText("Hello from x360ce! "))
dev.Hotkey("b2", (*) => InNotepad() && SendText(FormatTime(, "yyyy-MM-dd HH:mm") " "))

; New line, delete, save.
dev.Hotkey("b3", (*) => InNotepad() && Send("{Enter}"))
dev.Hotkey("b4", (*) => InNotepad() && Send("{Backspace}"))
dev.Hotkey("b5", (*) => InNotepad() && Send("^s"))

; The hat moves the text cursor.
dev.Hotkey("d1", (*) => InNotepad() && Send("{Up}"))
dev.Hotkey("d2", (*) => InNotepad() && Send("{Right}"))
dev.Hotkey("d3", (*) => InNotepad() && Send("{Down}"))
dev.Hotkey("d4", (*) => InNotepad() && Send("{Left}"))
```

## Trying a script without a controller

The Remote MCP Controller is a pretend controller inside x360ce whose controls are set by typing them. On a controller
tab press `[Add...]`, select `Remote MCP Controller 1` and press `[Add Selected Device]`. On its tab, `State` takes the
controls to change, named and valued as on this page: `{"b1":1,"a1":-0.5}` presses button 1 and holds axis 1 half way
left; `{"rest":1}` lets go of everything. `Now` shows what it holds. An AI assistant can set `State` over MCP, so it
can try a script with you without a controller in anyone's hands: through x360ce's AI assistant access with
`device_call`, or on the controller's own MCP server, which its tab opens, with `set_state`.

## When nothing happens

- **The script does nothing.** Tick `Allow AutoHotkey scripts`. x360ce must be running, with the game it maps for
  selected. If x360ce runs as administrator, run the script as administrator too.
- **A control reads `""`, or setting it does nothing.** The controller needs a device on its tab, ticked. Check the
  device's name in the `AHK` column, and its button numbers on the `Direct Input` page.
- **A game ignores keys the script sends.** Some games read the keyboard in a way `Send()` does not reach. Run the
  script as administrator if the game runs as administrator.
- **A second script does nothing.** One script at a time: it connects when the first closes.
