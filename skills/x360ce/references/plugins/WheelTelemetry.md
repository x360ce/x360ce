# WheelTelemetry

An x360ce plugin. How plugins work, and how to write one: ../plugins.md.

- Source: embedded in x360ce.exe
- Signed by: part of x360ce
- State: loaded

## Wheel Telemetry

- Kind: wheel telemetry provider (`IPluginWheelTelemetry`), active automatically while a wheel’s centering spring and telemetry are enabled.
- Dedicated tab: WheelTelemetryPage, hosted as the Wheel Telemetry tab on a steering wheel’s controller page.

Reads live vehicle telemetry from racing games over Windows Shared Memory (Assetto Corsa, ACC, AC Evo, iRacing) and standard localhost UDP formats (Forza Data Out, EA F1, Codemasters, OutSim, Project CARS). Listens only on 127.0.0.1:37150 by default so SimHub, CrewChief, Pit House and other telemetry apps keep their ports.
