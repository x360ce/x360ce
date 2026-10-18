; x360ce.ahk: lets an AutoHotkey v2 script read the controllers x360ce maps, and change what their mappings read.
;
; x360ce writes this file to Documents\AutoHotkey\Lib when "Allow AutoHotkey scripts" is turned on (Options, Plugins),
; so a script starts with:
;
;   #Include <x360ce>
;
; A controller is X360ce.c1 to X360ce.c4, and a device on it .d1, .d2...: X360ce.c1.d2 is device 2 on Controller 1, as
; the controller tab's Script column shows it beside the device. Give it a name of your own with a variable:
;
;   g27 := X360ce.c1.d1
;
; Each device has a property for every control, named as in x360ce's formulas, and read and written straight in the
; memory x360ce shares, with no name taken apart on the way:
;   a1 to a24 axes, -1 to 1         x1 to x24 the upper half of each axis, 0 to 1 (read only)
;   s1 to s8 sliders, 0 to 1        h1 to h8 the upper half of each slider, 0 to 1 (read only)
;   b1 to b128 buttons, 0 or 1      p1 to p4 hats, degrees, -1 at rest
;   d1 to d16 hat directions, 0 or 1: d1 to d4 are hat 1 up, right, down and left; d5 starts hat 2
;
;   x := g27.a4                                  ; what the device reports now
;   XButton2::g27.b1 := 1                        ; a mouse button presses a controller button
;   XButton2 up::g27.b1 := 0
;   w::g27.a2 := -1                              ; a key holds an axis at one end
;   w up::g27.a2 := ""                           ; and gives it back to the device
;   g27.Hotkey("b1", (*) => Send("{Space}"))     ; a controller button runs a function, as Hotkey() does for a key
;   g27.Hotkey("a1 > 80%", (*) => Send("{Right}"))
;
; Everything else is AutoHotkey itself: Send, Sleep, SetTimer, MouseMove, WinActive, #HotIf, key hotkeys.
;
; A button named in a hotkey without ~ belongs to the script: its controller reads only what the script sets for it.
; Every other button reads what the device does, plus what the script presses. An axis, slider or hat is the device's
; until the script sets it, and again once it sets "". When the script stops, x360ce lets go of everything it held.

#Requires AutoHotkey v2.0

class X360ce {
	; Layout shared with x360ce: Engine\Input\Script\ScriptChannel.cs. A test compares every value here with it.
	static Name := "Local\X360CE.Script"
	static Magic := 0x43533358
	static Version := 1
	static Size := 16384
	static MaxSlots := 16
	static RingLength := 256
	static MessageId := 0x8360
	static ReleaseValue := -2147483648
	static OffMagic := 0
	static OffVersion := 4
	static OffTableVersion := 12
	static OffScriptWindow := 16
	static OffHeartbeat := 24
	static OffPending := 28
	static OffEventsWritten := 32
	static OffEventsRead := 36
	static OffCommandsWritten := 40
	static OffCommandsRead := 44
	static OffConnected := 56
	static OffHeartbeatInterval := 60
	static OffSlots := 256
	static SlotSize := 512
	static OffEvents := 8448
	static EventSize := 8
	static OffCommands := 10496
	static CommandSize := 16
	static SlotInUse := 0
	static SlotNumbers := 4
	static SlotMoves := 24
	static SlotWatchButtons := 32
	static SlotWatchPovs := 48
	static SlotWatchAxes := 52
	static SlotWatchSliders := 56
	static SlotOwnButtons := 64
	static SlotButtons := 96
	static SlotPovs := 112
	static SlotAxes := 128
	static SlotSliders := 224
	static KindButton := 1
	static KindPov := 2
	static KindAxis := 3
	static KindSlider := 4
	static FlagFromNow := 1

	static P := 0
	static Handle := 0
	static Beat := 0
	static TableSeen := -1
	static Refused := false
	static Interval := 15
	static TickFn := 0
	static Entries := []
	static PovLast := Map()
	static MovesSeen := Map()

	; True while x360ce applies what this script sends.
	static Connected => this.P && NumGet(this.P, this.OffConnected, "Int") = 1

	static __New() {
		if this != X360ce
			return
		ListLines 0
		Persistent
		OnMessage(this.MessageId, ObjBindMethod(this, "OnEngine"), 10)
		this.TickFn := ObjBindMethod(this, "Tick")
		SetTimer(this.TickFn, this.Interval)
		this.Tick()
	}

	; The controllers. Each makes its devices when a script first names one.
	static c1 := X360ce.Controller(1)
	static c2 := X360ce.Controller(2)
	static c3 := X360ce.Controller(3)
	static c4 := X360ce.Controller(4)

	class Controller {
		__New(pad) {
			this.Pad := pad
			this.Devices := Map()
			loop 16
				this.DefineProp("d" A_Index, {get: X360ce.Controller.Getter(A_Index)})
		}

		static Getter(number) => (controller) => controller.Device(number)

		Device(number) {
			if !this.Devices.Has(number)
				this.Devices[number] := X360ce.Device(this.Pad, number)
			return this.Devices[number]
		}
	}

	class Device {
		__New(pad, number) {
			this.Pad := pad
			this.Number := number
			; One property for each control, made once, so reading or setting one takes nothing apart.
			for letter, count in Map("a", 24, "b", 128, "d", 16, "h", 8, "p", 4, "s", 8, "x", 24)
				loop count
					this.DefineProp(letter A_Index, X360ce.Device.Property(letter, A_Index))
		}

		static Property(letter, index) => {get: (device) => X360ce.ReadControl(device, letter, index), set: (device, value) => X360ce.WriteControl(device, letter, index, value)}

		; Calls fn when one of this device's controls is pressed or let go, as AutoHotkey's own Hotkey() does for keys:
		; "b1", "~b1", "b1 up", "d1" (hat 1 up), "a1 > 80%", "s1 < 10% up".
		Hotkey(name, fn) => X360ce.AddHotkey(this, name, fn)
	}

	; ---- Inside ----

	; What a device's control reports now; "" while x360ce does not share it.
	static ReadControl(device, letter, index) {
		slot := this.Slot(device)
		return slot < 0 ? "" : this.Value(slot, letter, index)
	}

	; Presses a button or hat direction (1) or lets go of it (0), or holds an axis (-1 to 1), a slider (0 to 1) or a hat
	; (degrees, -1 at rest). "" gives the control back to its device.
	static WriteControl(device, letter, index, value) {
		slot := this.Slot(device)
		switch letter {
		case "b": this.Command(slot, this.KindButton, index - 1, value ? 1 : 0)
		case "a": this.Command(slot, this.KindAxis, index - 1, value = "" ? this.ReleaseValue : Min(65535, Round(32767 + Max(-1, Min(1, value)) * 32767)))
		case "s": this.Command(slot, this.KindSlider, index - 1, value = "" ? this.ReleaseValue : Round(Max(0, Min(1, value)) * 65535))
		case "p": this.Command(slot, this.KindPov, index - 1, value = "" ? this.ReleaseValue : value < 0 ? -1 : Round(Mod(value, 360) * 100))
		case "d": this.Command(slot, this.KindPov, (index - 1) // 4, value ? Mod(index - 1, 4) * 9000 : this.ReleaseValue)
		default: throw ValueError("Half an axis or slider cannot be set: set " (letter = "x" ? "a" : "s") index " instead.", -2)
		}
	}

	; Registers a device's hotkey: "~b1 up", "a1 > 80%". Read once, here; nothing is taken apart when it runs.
	static AddHotkey(device, name, fn) {
		if !RegExMatch(name, "i)^\s*(~)?\s*([abdhpsx])(\d+)\s*(?:([<>])\s*(-?\d+(?:\.\d+)?)\s*%)?\s*(up|down)?\s*$", &m)
			throw ValueError("Not a control: " name, -2)
		letter := StrLower(m[2])
		index := Integer(m[3])
		if !device.HasOwnProp(letter index)
			throw ValueError("No such control: " name, -2)
		if letter = "p"
			throw ValueError("A hat's directions are d1 to d4: " name, -2)
		threshold := InStr("asxh", letter)
		if !threshold != (m[4] = "")
			throw ValueError(threshold ? "An axis or slider hotkey needs a limit, such as a1 > 80%: " name : "Only an axis or a slider takes a limit: " name, -2)
		full := (m[1] ? "~" : "") "c" device.Pad ".d" device.Number "." LTrim(SubStr(Trim(name), m[1] ? 2 : 1))
		this.Add({Name: full, Pad: device.Pad, Number: device.Number, Letter: letter, Index: index, Pass: m[1] = "~", Up: StrLower(m[6]) = "up", Op: m[4], Limit: m[5] = "" ? 0 : Number(m[5]), Fn: fn})
	}

	static Add(entry) {
		entry.Slot := -1
		entry.Active := false
		this.Entries.Push(entry)
		if this.P
			this.Register()
	}

	static Tick() {
		if !this.P && !this.Open()
			return
		P := this.P
		; One script at a time: another that still runs keeps x360ce until it closes.
		other := NumGet(P, this.OffScriptWindow, "Int64")
		if other && other != A_ScriptHwnd && this.IsScriptWindow(other) {
			if !this.Refused {
				this.Refused := true
				MsgBox("x360ce is used by another script:`n" WinGetTitle(other) "`n`nThis one connects when that one closes.", A_ScriptName, "Icon!")
			}
			return
		}
		this.Refused := false
		interval := NumGet(P, this.OffHeartbeatInterval, "Int")
		if interval > 0 && interval != this.Interval {
			this.Interval := interval
			SetTimer(this.TickFn, interval)
		}
		NumPut("Int64", A_ScriptHwnd, P, this.OffScriptWindow)
		this.Beat := (this.Beat + 1) & 0xFFFFFFFF
		NumPut("UInt", this.Beat, P, this.OffHeartbeat)
		if NumGet(P, this.OffTableVersion, "UInt") != this.TableSeen
			this.Register()
		; A message x360ce sent while this script was busy may have been dropped: read what waits.
		if NumGet(P, this.OffPending, "Int")
			this.Read()
	}

	; Whether a window is still the main window of a running AutoHotkey script. A number as WinTitle finds hidden windows too.
	static IsScriptWindow(hwnd) {
		try
			return WinGetClass(hwnd) = "AutoHotkey"
		catch
			return false
	}

	static Open() {
		h := DllCall("OpenFileMappingW", "UInt", 0xF001F, "Int", 0, "Str", this.Name, "Ptr")
		if !h
			return false
		p := DllCall("MapViewOfFile", "Ptr", h, "UInt", 0xF001F, "UInt", 0, "UInt", 0, "UPtr", this.Size, "Ptr")
		if !p || NumGet(p, this.OffMagic, "Int") != this.Magic || NumGet(p, this.OffVersion, "Int") != this.Version {
			if p
				DllCall("UnmapViewOfFile", "Ptr", p)
			DllCall("CloseHandle", "Ptr", h)
			return false
		}
		this.Handle := h
		this.P := p
		this.TableSeen := -1
		return true
	}

	; Writes what the hotkeys watch and own into the slots of the devices they name. Nothing from before carries over.
	static Register() {
		P := this.P
		this.TableSeen := NumGet(P, this.OffTableVersion, "UInt")
		loop this.MaxSlots {
			at := P + this.OffSlots + (A_Index - 1) * this.SlotSize
			loop (this.SlotButtons - this.SlotWatchButtons) // 4
				NumPut("Int", 0, at, this.SlotWatchButtons + (A_Index - 1) * 4)
		}
		this.MovesSeen := Map()
		for e in this.Entries {
			e.Slot := this.Slot(e)
			if e.Slot < 0
				continue
			at := P + this.OffSlots + e.Slot * this.SlotSize
			switch e.Letter {
			case "b":
				this.SetBit(at + this.SlotWatchButtons + ((e.Index - 1) // 32) * 4, (e.Index - 1) & 31)
				if !e.Pass
					this.SetBit(at + this.SlotOwnButtons + ((e.Index - 1) // 32) * 4, (e.Index - 1) & 31)
			case "d":
				hat := (e.Index - 1) // 4
				this.SetBit(at + this.SlotWatchPovs, hat)
				this.PovLast[e.Slot * 4 + hat] := NumGet(at, this.SlotPovs + hat * 4, "Int")
			case "a", "x", "s", "h":
				this.SetBit(at + (InStr("ax", e.Letter) ? this.SlotWatchAxes : this.SlotWatchSliders), e.Index - 1)
				this.MovesSeen[e.Slot] := NumGet(at, this.SlotMoves, "UInt")
				e.Active := this.IsActive(e, this.Value(e.Slot, e.Letter, e.Index))
			}
		}
	}

	static SetBit(address, bit) {
		NumPut("UInt", NumGet(address, "UInt") | (1 << bit), address)
	}

	; The slot of a device, device Number on controller Pad, or -1 when there is none. Looked up again only when x360ce
	; changes its devices; the device keeps the answer until then.
	static Slot(target) {
		if !this.P
			return -1
		table := NumGet(this.P, this.OffTableVersion, "UInt")
		if target.HasOwnProp("SlotTable") && target.SlotTable = table
			return target.SlotIndex
		slot := -1
		loop this.MaxSlots {
			at := this.P + this.OffSlots + (A_Index - 1) * this.SlotSize
			if NumGet(at, this.SlotInUse, "Int") && ((NumGet(at, this.SlotNumbers, "UInt") >> ((target.Pad - 1) * 8)) & 0xFF) = target.Number {
				slot := A_Index - 1
				break
			}
		}
		target.SlotTable := table
		target.SlotIndex := slot
		return slot
	}

	static Command(slot, kind, index, value) {
		P := this.P
		if !P || slot < 0
			return false
		w := NumGet(P, this.OffCommandsWritten, "UInt")
		if ((w - NumGet(P, this.OffCommandsRead, "UInt")) & 0xFFFFFFFF) >= this.RingLength
			return false
		c := P + this.OffCommands + (w & (this.RingLength - 1)) * this.CommandSize
		NumPut("Int", (slot << 24) | (kind << 16) | index, "Int", value, "Int", 0, "Int", this.FlagFromNow, c)
		NumPut("UInt", (w + 1) & 0xFFFFFFFF, P, this.OffCommandsWritten)
		return true
	}

	static OnEngine(wParam, lParam, msg, hwnd) {
		this.Read()
		return 1
	}

	; Reads every event and move x360ce wrote, until a round finds nothing new.
	static Read() {
		P := this.P
		if !P
			return
		loop {
			NumPut("Int", 0, P, this.OffPending)
			if NumGet(P, this.OffTableVersion, "UInt") != this.TableSeen
				this.Register()
			news := this.CheckMoves()
			loop {
				read := NumGet(P, this.OffEventsRead, "UInt")
				if read = NumGet(P, this.OffEventsWritten, "UInt")
					break
				e := P + this.OffEvents + (read & (this.RingLength - 1)) * this.EventSize
				code := NumGet(e, "Int")
				value := NumGet(e, 4, "Int")
				; Marked read before the hotkey runs: a hotkey that waits lets the next message carry on from here.
				NumPut("UInt", (read + 1) & 0xFFFFFFFF, P, this.OffEventsRead)
				news := true
				slot := (code >> 24) & 0xFF
				kind := (code >> 16) & 0xFF
				index := code & 0xFFFF
				if kind = this.KindButton
					this.OnButton(slot, index, value != 0)
				else if kind = this.KindPov
					this.OnPov(slot, index, value)
			}
		} until !news
	}

	static OnButton(slot, index, down) {
		for e in this.Entries {
			if e.Slot = slot && e.Letter = "b" && e.Index = index + 1 && e.Up != down
				e.Fn.Call(e.Name)
		}
	}

	static OnPov(slot, hat, value) {
		key := slot * 4 + hat
		was := this.PovLast.Has(key) ? this.PovLast[key] : -1
		this.PovLast[key] := value
		loop 4 {
			d := A_Index - 1
			now := this.PovDirection(value, d)
			if now = this.PovDirection(was, d)
				continue
			for e in this.Entries {
				if e.Slot = slot && e.Letter = "d" && e.Index = hat * 4 + d + 1 && e.Up != now
					e.Fn.Call(e.Name)
			}
		}
	}

	; Whether a hat at a value (hundredths of a degree) presses a direction: 0 up, 1 right, 2 down, 3 left. As x360ce decides.
	static PovDirection(pov, d) {
		if pov < 0 || pov >= 36000
			return false
		part := Mod((2250 + pov) // 4500, 8)
		switch d {
		case 0: return part <= 1 || part = 7
		case 1: return part >= 1 && part <= 3
		case 2: return part >= 3 && part <= 5
		default: return part >= 5 && part <= 7
		}
	}

	static CheckMoves() {
		P := this.P
		news := false
		slots := []
		for slot in this.MovesSeen
			slots.Push(slot)
		for slot in slots {
			moves := NumGet(P, this.OffSlots + slot * this.SlotSize + this.SlotMoves, "UInt")
			if moves = this.MovesSeen[slot]
				continue
			this.MovesSeen[slot] := moves
			news := true
			for e in this.Entries {
				if e.Slot != slot || !InStr("asxh", e.Letter)
					continue
				active := this.IsActive(e, this.Value(slot, e.Letter, e.Index))
				if active = e.Active
					continue
				e.Active := active
				if active != e.Up
					e.Fn.Call(e.Name)
			}
		}
		return news
	}

	; Past the limit, with 5 % to come back, so a value resting on the limit does not press and let go over and over.
	static IsActive(e, value) {
		percent := value * 100
		if e.Op = ">"
			return e.Active ? percent > e.Limit - 5 : percent > e.Limit
		return e.Active ? percent < e.Limit + 5 : percent < e.Limit
	}

	static Value(slot, letter, index) {
		at := this.P + this.OffSlots + slot * this.SlotSize
		switch letter {
		case "b": return (NumGet(at, this.SlotButtons + ((index - 1) // 32) * 4, "UInt") >> ((index - 1) & 31)) & 1
		case "a": return Max(-1, Min(1, (NumGet(at, this.SlotAxes + (index - 1) * 4, "Int") - 32767) / 32767))
		case "x": return Max(0, Min(1, (NumGet(at, this.SlotAxes + (index - 1) * 4, "Int") - 32767) / 32767))
		case "s": return Max(0, Min(1, NumGet(at, this.SlotSliders + (index - 1) * 4, "Int") / 65535))
		case "h": return Max(0, Min(1, (NumGet(at, this.SlotSliders + (index - 1) * 4, "Int") - 32767) / 32767))
		case "p":
			v := NumGet(at, this.SlotPovs + (index - 1) * 4, "Int")
			return v < 0 || v >= 36000 ? -1 : v / 100
		default: return this.PovDirection(NumGet(at, this.SlotPovs + ((index - 1) // 4) * 4, "Int"), Mod(index - 1, 4)) ? 1 : 0
		}
	}
}
