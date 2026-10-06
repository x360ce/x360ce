# TODO and Known issues

## Requested features

- Record controller actions over time, so a player can see their actions per minute.
- Hide the real controller from games without leaving the program. Today HID Hide does it from its own window.
- A controller button combination that turns emulation on and off, for when the keyboard is out of reach.
- Install and update through winget.
- Publish a nightly build as a GitHub pre-release, made by a GitHub Actions workflow.
- Publish `docs/` as a website (MkDocs Material on GitHub Pages).

## Known issues

- An inverted D-Pad button (`d-N`, `IPOVButton`) is read from settings but the engine never applies it. No menu offers it either.
- After Repair, Remove Leftover Pads, a reorder or a virtual controller made again, a game's steady rumble stays off until the game sends it again.
- A crash inside Microsoft's XInput driver ends the program.
- A removed stale virtual controller record keeps its device open until the program closes, one handle per record.
- A pad whose force feedback keeps failing on one motor plays neither motor.
