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

## Requested features

- Record controller actions over time, so a player can see their actions per minute.
- Hide the real controller from games without leaving the program. Today HID Hide does it from its own window.
- A controller button combination that turns emulation on and off, for when the keyboard is out of reach.
- Formulas longer than 16 characters, so `clamp`, `deadzone`, `antideadzone` and `curve` fit. The column widening script is `Data/Change Scripts/2026-08-27_Widen_Mapping_Columns_To_128.sql`; `MapExpression.MaxLength` follows it.
- Install and update through winget.
- Publish `docs/` as a website (MkDocs Material on GitHub Pages).

## Known issues

- The window is drawn at 96 DPI and scaled by Windows, so it looks soft on a high-DPI screen. A crisp relayout needs the controller page reworked for every scale.
- An inverted D-Pad button (`d-N`, `IPOVButton`) is read from settings but the engine never applies it. No menu offers it either.
- A virtual controller the driver will not remove is not made again until Repair.
