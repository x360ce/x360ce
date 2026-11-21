# FitnessMachine

An x360ce plugin. How plugins work, and how to write one: ../plugins.md.

- Source: embedded in x360ce.exe
- Signed by: part of x360ce
- State: loaded
- Adds a page to the main window, on a tab after the Devices tab: FitnessPage. ui_pages lists it, and ui_open opens it.

## Fitness Machine

- Product GUID: 6e1793da-b362-49b0-ad25-08433feac3f8
- Controls: axes 6, sliders 2, hat switches 0, buttons 4
- Recommended mapping, which a controller tab takes when the device is put on it and from Auto Preset, in SDL's form: righttrigger:a0,lefttrigger:a1
- To use it: the list a controller tab's Add opens offers one until one is added, and Add Plugin Device on the Devices tab adds another; map it to a controller like any device. devices_list lists its tools, and device_call calls them.

An exercise bike, treadmill, rower or cross trainer as a controller, read over Bluetooth: pedal, run or row to drive a game. Experimental: tested on a Tunturi E60R exercise bike only; use it with care and at your own risk. It reads any machine that uses Bluetooth's standard Fitness Machine Service (FTMS), as most smart bikes and trainers do, and needs Windows 10 or later. Find and connect the machine on the Fitness tab, which adds this device to the Devices list as the machine connects.

Axis 1 is your speed going forward and axis 2 your speed in reverse, from standing still to Max speed. Axis 3 rests in the middle and moves up as you go forward and down in reverse, as a stick does for walking. Axis 4 is your cadence, axis 5 your power and axis 6 your heart rate; slider 1 is your speed either way, and slider 2 the machine's resistance. Button 1 is held while you move, button 2 while in reverse, button 3 for a moment at each heartbeat, and button 4 while your heart rate is at or above Heart rate limit. A machine that sends nothing for 3 seconds reads as standing still.

Map to on the Fitness tab, or the device put on a controller, maps it to drive: Right Trigger to Axis 1 and Left Trigger to Axis 2, as Auto Preset does too. To walk or run on foot, map Left Thumb Axis Y to IAxis 3.

Reverse: tick Reverse, or set Reverse gear, and turning the machine's own resistance down to that level drives backwards. A cross trainer pedalled backwards drives backwards by itself.

Force feedback: tick Enable on the controller's Force Feedback page, and the game's rumble makes the machine harder to pedal, by up to Extra resistance levels, or makes the road climb, by up to Road grade, as Force feedback to says. When the rumble stops, the machine goes back to the level you set on it. While your heart rate is at or above Heart rate limit, the rumble adds nothing.

Unticked, disabled or taken off every controller, the device lets go of the machine, which goes back to the level you set on it; read again, it connects to the machine again.

A computer's Bluetooth cannot connect to itself, so a transmitter such as the Virtual Fitness Machine Transmitter must run on another device, not on this computer.

### Its tab

A tab named Fitness Machine on the controller page the device is mapped to. Each device keeps its own values; Reset gives back the first ones.

- Max speed (km/h) (Ranges, a number from 1 to 200, starts at 40): The speed that moves axes 1, 2 and 3 and slider 1 all the way.
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
- Connection (Status, read only): What the Fitness tab's connection is doing: looking for a machine, connecting, connected, or why it cannot.
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
