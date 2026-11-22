# FitnessMachine

An x360ce plugin. How plugins work, and how to write one: ../plugins.md.

- Source: embedded in x360ce.exe
- Signed by: part of x360ce
- State: loaded

## Fitness Machine

- Product GUID: 6e1793da-b362-49b0-ad25-08433feac3f8
- Controls: axes 6, sliders 2, hat switches 0, buttons 4
- Recommended mapping, which a controller tab takes when the device is put on it and from Auto Preset, in SDL's form: righttrigger:a0,lefttrigger:a1
- To use it: the list a controller tab's Add opens offers one until one is added, and Add Plugin Device on the Devices tab adds another; map it to a controller like any device. devices_list lists its tools, and device_call calls them.

An exercise bike, treadmill, rower or cross trainer as a controller, read over Bluetooth: pedal, run or row to drive a game. It reads any machine that uses Bluetooth's standard Fitness Machine Service (FTMS), as most smart bikes and trainers do, and needs Windows 10 or later.

Experimental: walk, drive or row in games with a fitness machine. Tested on a Tunturi E60R exercise bike only. Use it with care and at your own risk.

### Connecting

1. Put the device on a controller: the controller tab's `[Add...]` list offers it, and `[Add Plugin Device]` on the `[Devices]` tab adds another.
2. Turn the machine on. On this tab's `[Connection]` page click `[Scan]`. If the machine is not listed, tick `[Show all Bluetooth devices]`: some machines show the fitness service only once connected.
3. Select the machine in the list. That chooses it for this device, kept as its `[Machine]` setting on the `[Properties]` page; selecting does not connect by itself.
4. The device's `[Enabled]` box connects: ticked, in the controller's list and on the `[Devices]` tab, x360ce connects to the machine, trying again quietly until it connects; unticked, it lets go of it. A device ticked when x360ce starts connects by itself.
5. If the machine asks to be paired, accept only if both show the same number.
6. Removing the device, taking it off every controller, or closing x360ce lets go of the machine too, which goes back to the level you set on it. Each device connects to its own machine, so two devices can read two machines.

### Controls

| Control | Name | Moves with |
|---|---|---|
| Axis 1 | Forward | speed going forward |
| Axis 2 | Reverse | speed in reverse |
| Axis 3 | Walk | speed, up or down |
| Axis 4 | Cadence | cadence |
| Axis 5 | Power | power |
| Axis 6 | HeartRate | heart rate |
| Slider 1 | Speed | speed either way |
| Slider 2 | Resistance | resistance level |
| Button 1 | Moving | moving |
| Button 2 | Reverse | in reverse |
| Button 3 | Heartbeat | each heartbeat |
| Button 4 | HR high | heart rate limit |

- Axis 1, Forward, is your speed going forward: 0 standing still, and 65535, all the way, at `[Max speed (km/h)]`. It stays at 0 in reverse.
- Axis 2, Reverse, is your speed in reverse, the same way. It stays at 0 going forward.
- Axis 3, Walk, rests in the middle, 32767, and moves up toward 0 as you go forward and down toward 65535 in reverse, all the way at Max speed, as a stick does for walking.
- Axis 4, Cadence, goes from 0 to 65535 at `[Max cadence (rpm)]`: pedal turns, steps or strokes a minute.
- Axis 5, Power, goes from 0 to 65535 at `[Max power (W)]`.
- Axis 6, HeartRate, goes from 0 at `[Min heart rate (bpm)]` to 65535 at `[Max heart rate (bpm)]`, and is 0 with no heart rate.
- Slider 1, Speed, is your speed either way, 0 to 65535 at Max speed.
- Slider 2, Resistance, is the machine's resistance level within the range the machine reports, or within 0 to 100 while it reports none.
- Button 1, Moving, is held while you move; Button 2, Reverse, while in reverse; Button 3, Heartbeat, for the first quarter of each heartbeat; and Button 4, HR high, while your heart rate is at or above `[Heart rate limit (bpm)]`.
- A value past its range stays at the end: faster than Max speed is still 65535. A machine that sends nothing for 3 seconds reads as standing still. The ranges are on the `[Properties]` page.

### Mapping

Put on a controller, or with `[Auto Preset]`, the device drives a car: `[Right Trigger]` takes `Axis 1` and `[Left Trigger]` takes `Axis 2`, each released standing still and pressed all the way at Max speed. To walk or run on foot, map `[Left Thumb Axis Y]` to `IAxis 3`.

### Reverse

- Tick `[Reverse]`, and your speed drives backwards: it moves Axis 2 in place of Axis 1, and Axis 3 down in place of up.
- Or set `[Reverse gear]` to a resistance level: turning the machine's own resistance down to it drives backwards, so its gear or resistance lever is the reverse gear.
- A cross trainer pedalled backwards drives backwards by itself.
- A button of another device can shift into reverse while it is held, with formulas: for button 1 of device 2 on Controller 1, write `=s1*(1-c1.d2.b1)` in `[Right Trigger]` and `=s1*c1.d2.b1` in `[Left Trigger]`, where `Slider 1` is your speed either way.

### Force feedback

On the controller's `[Force Feedback]` page tick `[Enable]`. The game's rumble then makes the machine harder to pedal, by up to `[Extra resistance (levels)]` on top of the level you set on it, or makes the road climb, by up to `[Road grade (%)]`, as `[Force feedback to]` says; a lighter rumble adds less. When the rumble stops, or x360ce lets go of the machine, it goes back to the level you set on it. x360ce asks the machine for control first, and sends a new target at most four times a second, only once it is a whole level or half a percent of grade from the last.

While your heart rate is at or above `[Heart rate limit (bpm)]`, the rumble adds nothing and Button 4 is held. 0 switches the limit off.

### The Connection page

- `[Scan]` lists the fitness machines nearby, with the kind of machine and the signal; `[Show all Bluetooth devices]` lists every device that says its name. The machine chosen is selected.
- At the top right, where the connection stands and, below it, what the machine measures and the targets it takes.
- `[Start]`, `[Pause]`, `[Stop]` and `[Reset]` send the machine's own commands.
- `[Target power]`, `[Target resistance]` and `[Road grade]`: move the slider, then click `[Set]` to send it. The sliders take the ranges the machine reports. A command or target the machine does not take is greyed out.
- The values the machine sends, each with how often it changes, and what happened, newest first.

### One computer, one radio

A computer's Bluetooth cannot connect to itself, so a transmitter such as the Virtual Fitness Machine Transmitter must run on another device, not on this computer. A machine connected to another app may not be listed at all, and while x360ce is connected the machine's own display may dim.

### Its tab

A tab named Fitness Machine on the controller page the device is mapped to, with a page for each part: the plugin's own control first, then MCP Server, Properties with the settings below, and Help with the text above. Each device keeps its own values; Reset gives back the first ones.

- Machine (Machine, text, starts empty): The machine this device connects to while ticked: its name and Bluetooth address. The Connection page sets it.
- Max speed (km/h) (Ranges, a number from 1 to 200, starts at 40): The speed that moves axes 1, 2 and 3 and slider 1 all the way, to 65535.
- Max cadence (rpm) (Ranges, a number from 10 to 250, starts at 120): The cadence that moves axis 4 all the way.
- Max power (W) (Ranges, a number from 50 to 3000, starts at 400): The power that moves axis 5 all the way.
- Min heart rate (bpm) (Ranges, a number from 30 to 220, starts at 60): The heart rate at which axis 6 starts to move.
- Max heart rate (bpm) (Ranges, a number from 60 to 240, starts at 190): The heart rate that moves axis 6 all the way.
- Reverse (Reverse, true or false, starts at False): Drives in reverse: your speed moves axis 2 in place of axis 1, and axis 3 down in place of up.
- Reverse gear (Reverse, a number from 0 to 100, starts at 0): Drives in reverse while the resistance you set on the machine is at or below this level, so the machine's own gear or resistance lever puts it in reverse. 0 never does.
- Force feedback to (Force feedback, one of: Off, Resistance, Road grade, starts at Resistance): What the game's rumble does once Enable is ticked on the controller's Force Feedback page. Resistance: the machine gets harder to pedal. Road grade: the road climbs, on a machine that simulates a road. Off: nothing.
- Extra resistance (levels) (Force feedback, a number from 0 to 100, starts at 10): The resistance levels the game's strongest rumble adds to the level you set on the machine; a lighter rumble adds less.
- Road grade (%) (Force feedback, a number from 0 to 20, starts at 8): The road grade, in percent, the game's strongest rumble climbs; a lighter rumble climbs less.
- Heart rate limit (bpm) (Heart rate, a number from 0 to 240, starts at 0): While your heart rate is at or above it, the game's rumble adds nothing and button 4 is held. 0 is off.
- Connection (Status, read only): What the device's connection to its machine is doing: no machine chosen, waiting for the device to be ticked, connecting, connected, or why it cannot.
- Features (Status, read only): What the machine measures, and the targets it accepts, as it says itself.
- Speed (Status, read only): Your speed now.
- Cadence (Status, read only): Your cadence now: pedal turns, steps or strokes a minute.
- Power (Status, read only): Your power now.
- Heart rate (Status, read only): Your heart rate now, from the machine's own heart rate sensor or a strap paired with it.
- Resistance (Status, read only): The machine's resistance level now, and the range it goes over.
- Distance (Status, read only): How far you have gone since the machine started counting.
- Elapsed time (Status, read only): How long you have trained since the machine started counting.
- Force (Status, read only): How strong the game's rumble is, and the target it sets on the machine.
- Updates per second (Status, read only): How many sets of values the machine sent in the last second.
- The plugin's own control, FitnessConnection, on the first page, before Properties.
