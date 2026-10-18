# TestController

An x360ce plugin. How plugins work, and how to write one: ../plugins.md.

- Source: embedded in x360ce.exe
- Signed by: part of x360ce
- State: loaded

## Test Controller

- Product GUID: a10ef011-41ab-4460-8c45-c1ad2dbf7ede
- Controls: axes 5, sliders 0, hat switches 1, buttons 10
- To use it: the list a controller tab's Add opens offers one until one is added, and Add Plugin Device on the Devices tab adds another; map it to a controller like any device. devices_list lists its tools, and device_call calls them.

A pretend controller that moves by itself, so a mapping can be tried without hardware. By default, in every six seconds it moves for four and rests for two. The even axes (X, Z and Y Rotation) swing like a sine from the middle to each end and back, reversing every other cycle; the odd axes (Y and X Rotation) run from one end to the other and back. Buttons 9 and 10 take turns, each held for a second. Its tab on the controller page sets the moving and resting times, the shape (Circle takes X and Y round), whether the axes, the hat and the buttons move, whether buttons 1 to 8 are pressed too, and how long each button is held; a square on the tab steers X and Y by hand.

Windows takes a controller x360ce makes for an Xbox controller, and sends what it does to the window in front as key presses. Windows apps navigate with them: A selects, B goes back, Menu opens a menu, the D-pad and the left stick move focus, and the triggers and bumpers turn pages. So, while this controller is mapped, it can act on the window in front too. It starts with the hat still and buttons 1 to 8 released, because x360ce's own mapping makes them the D-pad, A, B, X, Y, the bumpers, View and Menu. The axes still move the sticks and the triggers; turn off Move the axes to keep them still too.

### Its tab

A tab named Test Controller on the controller page the device is mapped to. Each device keeps its own values; Reset gives back the first ones.

- Move the axes (Movement, true or false, starts at True): Moves the axes by themselves. Off, they rest: the even ones in the middle, the odd ones at their start.
- Turn the hat (Movement, true or false, starts at False): Turns the hat once round and back while the axes move. Off, it stays released. x360ce's own mapping makes the hat the D-pad, which moves focus in the window in front.
- Shape (Movement, one of: Swing and ramp, Circle, starts at Swing and ramp): Swing and ramp: the even axes swing like a sine, the odd ones run from one end to the other. Circle: X and Y go round, as a stick does when drawn in a circle.
- Moving time (s) (Movement, a number from 0.5 to 20, starts at 4): How long the axes and the hat move before they rest.
- Resting time (s) (Movement, a number from 0 to 20, starts at 2): How long the axes and the hat rest between movements.
- Press the buttons (Buttons, true or false, starts at True): Holds one button at a time, the next one after each press. Off, none is pressed.
- Press the navigation buttons (Buttons, true or false, starts at False): Presses buttons 1 to 8 too, which x360ce's own mapping makes A, B, X, Y, the bumpers, View and Menu. Windows sends them to the window in front, where A selects, B goes back and Menu opens a menu. Off, only buttons 9 and 10, the stick clicks, are pressed.
- Press time (ms) (Buttons, a number from 50 to 5000, starts at 1000): How long each button is held before the next one.
- Reads per second (Status, read only): How many times x360ce read the controller in the last second.
- The plugin's own control, TestControllerPad, on the first page, before Properties.
