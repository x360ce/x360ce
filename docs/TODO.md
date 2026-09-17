# TODO and Known issues

## Requested features

- Record controller actions over time, so a player can see their actions per minute.
- Hide the real controller from games without leaving the program. Today HID Hide does it from its own window.
- A controller button combination that turns emulation on and off, for when the keyboard is out of reach.
- Formulas longer than 16 characters, so `clamp`, `deadzone`, `antideadzone` and `curve` fit. The column widening script is `Data/Change Scripts/2026-08-27_Widen_Mapping_Columns_To_128.sql`; `MapExpression.MaxLength` follows it.
- Install and update through winget.
- Publish `docs/` as a website (MkDocs Material on GitHub Pages).

## Known issues

- The window is drawn at 96 DPI and scaled by Windows, so it looks soft on a high-DPI screen. A crisp relayout needs the controller page reworked for every scale.
