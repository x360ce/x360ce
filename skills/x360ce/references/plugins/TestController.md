# TestController

An x360ce plugin. How plugins work, and how to write one: ../plugins.md.

- Source: embedded in x360ce.exe
- Signed by: part of x360ce
- State: loaded

## Test Controller

- Product GUID: a10ef011-41ab-4460-8c45-c1ad2dbf7ede
- Controls: axes 5, sliders 0, hat switches 1, buttons 10
- To use it: Add Plugin Device on the Devices tab adds one; map it to a controller like any device.

A pretend controller that moves by itself, so a mapping can be tried without hardware. In every six seconds it moves for four and rests for two. The even axes (X, Z and Y Rotation) swing like a sine from the middle to each end and back, reversing every other cycle; the odd axes (Y and X Rotation) run from one end to the other and back; the hat turns once round and back. One button is held at a time, a new one each second.
