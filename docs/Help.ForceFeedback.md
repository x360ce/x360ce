## Which motor a game uses for what

| Motor | Weight | Frequency | What games put on it |
| --- | --- | --- | --- |
| Left | big | low | the car and what hits it: collisions, curbs, explosions, gunfire |
| Right | small | high | the fine and the ambient: engine, road texture, phone rings, menus |

On a wheel the right motor's small effects can feel out of place; `Right Motor Strength` at 0 % silences them.

## The Xbox controller's motors

Both motors are the same kind; the left carries the heavier weight, so it spins slower. Speed rises with drive once the motor has started. `Period` is the period at full drive; as the drive falls, the period played stretches along the same line.

| Motor | Spins from | Slowest | Period | Full drive | Period |
| --- | --- | --- | --- | --- | --- |
| Left | 8 % | 13 Hz | 76 ms | 25 Hz | 40 ms |
| Right | 16 % | 42 Hz | 24 ms | 62 Hz | 16 ms |

## Wheels: Motor Periods presets

A wheel has to move its rim, so its swing falls with the square of frequency: a gear-driven wheel follows up to 12-16 Hz and only rattles above that. `Motor Periods` plays both motors that many times slower, which keeps them apart and inside what the wheel can move. `4x` is the smallest that keeps the right motor inside a gear-driven wheel's band; `1x` and `2x` suit direct drive; any other pair of periods shows as `Custom`. A gamepad with motors of its own cannot follow a period and plays the `Constant` type, magnitude only.

| Preset | Wheel | Left | Left | Right | Right |
| --- | --- | --- | --- | --- | --- |
| 1x | Direct drive | 40 ms | 25.0 Hz | 16 ms | 62.5 Hz |
| 2x | Direct drive, heavy rim | 80 ms | 12.5 Hz | 32 ms | 31.3 Hz |
| 3x | Belt drive | 120 ms | 8.3 Hz | 48 ms | 20.8 Hz |
| 4x | Gear drive | 160 ms | 6.3 Hz | 64 ms | 15.6 Hz |
| 5x | Gear drive, heavy rim | 200 ms | 5.0 Hz | 80 ms | 12.5 Hz |
| 8x | Slow wheel | 320 ms | 3.1 Hz | 128 ms | 7.8 Hz |
