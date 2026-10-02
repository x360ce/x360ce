# TODO and Known issues

## Planned for 4.24

- Virtual controllers are created even after Windows has been running for 25 days or more.
- The centering spring Auto run finishes even after Windows has been running for 25 days or more.
- The Issues tab says when the virtual driver is installed but not working, and repairs it.
- Installs and links the latest Visual C++ v14 Redistributable.
- Installing Visual C++ from the Issues tab says what the installer did: restart needed, a newer version already installed, cancelled, or failed.
- A hint when a game may see the controller twice, with a button that opens HID Hide.
- Inverted buttons work as inverted.
- Button 128 and POV 4 Left can be mapped.
- A switch mapped to a button does not flicker on and off at the press point.
- After recording a switch onto a button, the program says which position presses it and offers Invert.
- A controller tab whose place is taken says what is holding it.
- Help and a preset for flying with an RC transmitter.
- Adding a device that is already on another controller tab asks whether to move it or keep it there too, so a device can drive up to four controllers.
- The AI assistant can keep a device on several controller tabs.
- Buttons and force feedback follow only the current game's settings.
- A device on several controller tabs takes force feedback from one chosen tab, or from several merged.
- The Force Feedback page names the other controllers a device is on.
- Importing or exporting games settings shows a message instead of an error report.
- Scanning for games no longer closes the program.
- Error reports from released versions no longer say "(TEST)".
- The game settings panel no longer shows an error report after it closed.
- Installing, repairing or removing the virtual driver works when one of its files is in use.
- A controller that cannot be opened no longer slows the program down.
- A virtual controller that stops taking input is put back.
- A virtual controller whose vibration the driver refuses works.
- The guide button is no longer let go by another controller.
- The XInput view no longer shows an error report.
- The controller picture no longer turns into a red cross.
- Putting controllers in order stops and says so when XInput does not answer.
- A virtual controller the driver will not remove is made again.
- Clicking a POV in the mapping menu no longer maps the whole POV.
- A mouse mapped to a stick starts in the middle.
- A controller given vibration stops after Repair or reorder.
- A controller that Windows puts in the wrong place is no longer made again every few seconds.
- Putting controllers in order no longer stops part way at a virtual controller left behind by another run.
- A virtual controller left behind by another run with no place is no longer listed for ordering on the Devices page; the Issues tab lists it for removal.
- The Devices page says "Real (place not known)" for a real controller whose XInput place is not known, instead of leaving it blank.
- Remove Leftover Pads finds every virtual controller left behind by another run, never takes one for this program's own, and removes all of it in one go.
- A device unticked in a controller tab's list or on the Devices page is not read, held or sent force feedback, and x360ce no longer hides it, so another program can use it.
- A rumble sent just before a controller is taken away no longer comes back.
- A pad that refuses a force feedback effect no longer piles up old effects.
- A device whose force feedback keeps failing no longer slows the program down, and the Issues tab names the error.
- The minimum instances box on the Internet page shows the saved number and keeps the number you set.
- Putting controllers in order switches a whole Xbox One controller off and on, so it comes back with its XInput place.
- Putting controllers in order switches a controller off without storing anything, so one left off by an interrupted order comes back when it is plugged in again.
- A controller an interrupted reorder left switched off is reported as still off, with how to switch it on, not as switched back on.
- Putting controllers in order puts each controller in the XInput place it was asked for, using temporary virtual controllers to hold places on the way.
- The Issues tab says when a controller this program switched off to put controllers in order is still off.

## Requested features

- Record controller actions over time, so a player can see their actions per minute.
- Hide the real controller from games without leaving the program. Today HID Hide does it from its own window.
- A controller button combination that turns emulation on and off, for when the keyboard is out of reach.
- Install and update through winget.
- Publish a nightly build as a GitHub pre-release, made by a GitHub Actions workflow.
- Publish `docs/` as a website (MkDocs Material on GitHub Pages).

## Known issues

- The window is drawn at 96 DPI and scaled by Windows, so it looks soft on a high-DPI screen. A crisp relayout needs the controller page reworked for every scale.
- An inverted D-Pad button (`d-N`, `IPOVButton`) is read from settings but the engine never applies it. No menu offers it either.
- After Repair, Remove Leftover Pads, a reorder or a virtual controller made again, a game's steady rumble stays off until the game sends it again.
- A crash inside Microsoft's XInput driver ends the program.
- A removed stale virtual controller record keeps its device open until the program closes, one handle per record.
- A pad whose force feedback keeps failing on one motor plays neither motor.
