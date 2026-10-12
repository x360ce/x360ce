# TestController

An x360ce plugin. How plugins work, and how to write one: ../plugins.md.

- Source: embedded in x360ce.exe
- Signed by: part of x360ce
- State: loaded

## Test Controller

- Product GUID: a10ef011-41ab-4460-8c45-c1ad2dbf7ede
- Controls: axes 5, sliders 0, hat switches 1, buttons 10
- To use it: Add Plugin Device on the Devices tab adds one; map it to a controller like any device.

A pretend controller that moves by itself, so a mapping can be tried without hardware. By default, in every six seconds it moves for four and rests for two. The even axes (X, Z and Y Rotation) swing like a sine from the middle to each end and back, reversing every other cycle; the odd axes (Y and X Rotation) run from one end to the other and back; the hat turns once round and back. One button is held at a time, a new one each second. Its tab on the controller page sets the moving and resting times, the shape (Circle takes X and Y round), whether the axes, the hat and the buttons move, and how long each button is held; a square on the tab steers X and Y by hand.

### Its tab

A tab named Test Controller on the controller page the device is mapped to. Each device keeps its own values; Reset gives back the first ones.

- Move the axes (Movement, check box, starts at True): Moves the axes by themselves. Off, they rest: the even ones in the middle, the odd ones at their start.
- Turn the hat (Movement, check box, starts at True): Turns the hat once round and back while the axes move. Off, it stays released.
- Shape (Movement, one of: Swing and ramp, Circle, starts at Swing and ramp): Swing and ramp: the even axes swing like a sine, the odd ones run from one end to the other. Circle: X and Y go round, as a stick does when drawn in a circle.
- Moving time (s) (Movement, number, 0.5 to 20, starts at 4): How long the axes and the hat move before they rest.
- Resting time (s) (Movement, number, 0 to 20, starts at 2): How long the axes and the hat rest between movements.
- Press the buttons (Buttons, check box, starts at True): Holds one button at a time, the next one after each press. Off, none is pressed.
- Press time (ms) (Buttons, number, 50 to 5000, starts at 1000): How long each button is held before the next one.
- Reads per second (Status, read only): How many times x360ce read the controller in the last second.
- The plugin's own control, TestControllerPad, under the settings.
