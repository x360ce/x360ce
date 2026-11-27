# Best settings for driving games

A game made for an Xbox controller reads your wheel as the left stick and your pedals as the two
triggers, and adds a dead zone of its own on top: a part of the travel around the centre that does
nothing. Out of the box the wheel then feels numb at the centre and the pedals do nothing for the
first part of their travel. These steps take the game's dead zone away, so the car answers the
smallest turn and the lightest press.

Do them once for each game, with the game selected in the `[Game]` list at the top, then click
`[Save All]`.

## 1. Before you start

1. Map the wheel and the pedals on a controller tab, as described in [Help.v4](Help.v4.md).
2. In the game's own settings, choose an Xbox or XInput controller.
3. If the gas and brake move one axis together, separate them first: see
   [Gas and brake pedals are combined](Help.v4.md#gas-and-brake-pedals-are-combined-how-can-i-separate-them).

## 2. Steering

1. Open the controller tab with your wheel, then its `[Left Stick]` page.
2. In the `[X - Horizontal Axis]` group, open the `[Apply Preset]` list and choose
   `[1% DeadZone, 100% Controller Anti-DeadZone]`.
3. Drive slowly and turn the wheel a little each way. The car must answer the smallest turn.
4. If the car drifts while the wheel is centred, the game's dead zone is smaller than the preset
   expects: choose `[80% Controller Anti-DeadZone]`, or a lower one, instead.

The 1% dead zone hides the small noise every wheel makes at rest. `100% Controller Anti-DeadZone`
skips the dead zone Microsoft recommends to games for the left stick, about a quarter of the way
from the centre to the edge.

## 3. Wheel range

1. Open the `[Wheel]` page.
2. In `[Wheel range]`, choose `[900° - Road and sports cars]`.
3. Open the `[Force Feedback]` page and make sure force feedback is on: x360ce sends the range to the
   wheel with its force feedback.

x360ce sets the range of a Logitech G25, G27, G29 or Driving Force GT. On any other wheel, set
900° in the wheel's own software. For quicker steering choose a shorter range: see
[What are real life steering wheel degrees?](Help.v4.md#what-are-real-life-steering-wheel-degrees)

## 4. Pedals

1. Open the `[Triggers]` page.
2. In the `[Right Trigger]` group (usually the gas) and the `[Left Trigger]` group (usually the
   brake), raise `[Anti-dead zone]` until the lightest press of the pedal moves the car. The table
   below gives a starting value for each game listed.
3. If the car creeps with the pedal released, raise `[Dead zone]` by 1%.

## 5. Games

Values found by testing each game with a wheel and pedals. A game not listed starts from the steps
above.

| Game | Steering preset | Pedals: anti-dead zone | Wheel range |
| --- | --- | --- | --- |
| Grand Theft Auto V | `1% DeadZone, 100% Controller Anti-DeadZone` | 24% (61 in the box) | 900° |

The whole setup for GTA V, road feel included: [GTA V Enhanced with a wheel and road feel](Help.GtaV.md).
For force feedback that follows the road in Forza Horizon 6 and F1 25, see
[Wheel Telemetry Games](Help.Plugins.md#wheel-telemetry-games).
