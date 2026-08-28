# x360ce Documentation

Xbox 360 Controller Emulator. These pages are the source the application itself shows — the
help inside the program is rendered from the same Markdown, so there is one copy of every
document and no second version to keep in step.

## Using the application

| Page | For |
| --- | --- |
| [Help.v4](Help.v4.md) | Version 4.x, which creates a virtual controller through ViGEmBus. Includes the full expressions reference. |
| [Help.v3](Help.v3.md) | Version 3.x, which replaces the game's XInput library and runs from the game folder. |
| [Help.HidGuardian](Help.HidGuardian.md) | Hiding original controllers from games, and how to recover if keyboard and mouse stop working. |

## For the project

| Page | For |
| --- | --- |
| [HowToBuild](HowToBuild.md) | Setting up IIS to host the x360ce website. |
| [TODO](TODO.md) | Known issues and requested features. |

## Which version do I want?

Version 4 is the current one. It creates a virtual Xbox 360 controller that Windows and every
game can see, so it does not have to sit in the game folder and does not replace any file
belonging to the game.

Version 3 is the older approach: it puts its own XInput library beside the game executable so
the game loads that instead of the real one. It is still useful where the virtual bus driver
cannot be installed.
