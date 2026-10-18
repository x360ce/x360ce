# RemoteMcpController

An x360ce plugin. How plugins work, and how to write one: ../plugins.md.

- Source: embedded in x360ce.exe
- Signed by: part of x360ce
- State: loaded

## Remote MCP Controller

- Product GUID: 884e9137-a970-446b-962f-31ed8c2abb11
- Controls: axes 6, sliders 8, hat switches 4, buttons 128
- To use it: the list a controller tab's Add opens offers one until one is added, and Add Plugin Device on the Devices tab adds another; map it to a controller like any device. devices_list lists its tools, and device_call calls them.

A pretend controller whose controls are set from outside, so a mapping, a formula or an AutoHotkey script can be tried without hardware and without hands. Its State setting takes the controls to change as JSON, named and valued as in formulas: {"b1":1,"a1":-0.5,"s1":0.25,"p1":90} presses button 1, holds axis 1 a quarter of the way left, slider 1 a quarter of the way along and hat 1 right. A control the text does not name keeps its value; {"rest":1} puts every control at rest first. A program or an AI assistant sets State over MCP with set_state, and reads Now back with read: on the controller's own MCP server, which its tab opens, or through x360ce's door with device_call. State is kept with the other settings, so after x360ce starts again the controller holds what it held: send {"rest":1} to let go.

### Its tab

A tab named Remote MCP Controller on the controller page the device is mapped to. Each device keeps its own values; Reset gives back the first ones.

- MCP server: closed until the person opens it on the tab, as AI assistant access is opened on the AI page. It listens on the port x360ce's port plan gives the controller and place the device is mapped to, 371CD: 37141 for the first device on controller 4. It serves this device's tools alone: read, set_state.
- State (Remote, text, starts empty): The controls to change, as JSON named and valued as in formulas: b1 to b128 a button, 1 or 0; a1 to a6 an axis, -1 to 1; s1 to s8 a slider, 0 to 1; p1 to p4 a hat, degrees clockwise from up or -1 at rest. "rest":1 puts every control at rest before the others are set.
- Now (Remote, read only): Every control that is not at rest, as JSON in the form State takes.
- Error (Remote, read only): What in the last State could not be set, or empty.
