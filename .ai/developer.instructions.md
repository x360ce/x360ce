# Developer-Provided Context

Use only C# language features up to and including version 7.3; do not use any features introduced in C# 8.0 or later.

## The engine and the interface are separate, and the engine never waits

The engine — `RefreshAllThread` in `App.v4/Common/DInput/DInputHelper.cs` and its six
`DInputHelper.Step*` files — reads hardware and produces controller state at **up to 1000
cycles a second**. The interface draws at about 10. They are separate on purpose, and the
separation is only real while the engine can complete a cycle without the interface's
permission.

**Rule: nothing on the engine path may wait for the interface thread, and nothing on it may
take a lock the interface thread also takes.** One blocked cycle at 1000 Hz is not a pause,
it is the whole budget.

This has been broken repeatedly, always the same way: a crash was fixed by adding a lock or
a marshalled call, and the rate fell from about 1000 Hz to single digits. Before changing
anything reachable from `RefreshAllThread`, or anything the engine calls, check every line
against this list:

- **No `lock` taken by both the engine and the interface.** `BindingListInvoked` holds
  `OneChangeAtTheTime` on the interface thread for the whole of each delivered notification.
  An engine-side `lock` on it blocks the engine for as long as the interface is drawing.
- **No synchronous marshalling.** `Control.Invoke`, `Task.RunSynchronously(uiScheduler)`,
  `Wait()`, `Result`, or anything else that blocks until the interface answers.
- **No work moved above the guard that used to skip it.** Hoisting a lookup out of an
  `if (hasForceFeedback)` or `if (mapped)` makes every device pay it on every cycle. A call
  that was cheap once a second is not cheap a thousand times a second.
- **No allocation or copy added per cycle.** `ItemsToArraySyncronized()` locks and copies the
  whole list. Calling it once per device per cycle is a cost; calling it for devices that do
  not need the answer is waste.
- **No exception thrown per cycle**, and no cost that grows with the number of devices.

### How to check it before saying it is done

1. `dotnet` cannot build this solution — use `Tests\Run-Tests.ps1`. Run
   `EngineFrequencyTest`, which measures the engine's rate with the interface idle and busy,
   over **both** the synchronous and the asynchronous marshalling paths. `UserDevices.Items`
   uses the asynchronous one (`SettingsManager.cs` sets `AsynchronousInvoke = true`), so a
   change that only slows that path must still fail a test.
2. Then run the program and read **HW Hz** in the status bar with a controller connected. It
   must sit near the polling rate chosen on the Options page. A drop to single or double
   digits means the engine is waiting on something. `EngineRateTest` does this and is tagged
   `ui-interactive`.

A measurement is not optional for a change on this path. Two of these regressions passed the
whole test suite and were caught by a person looking at the status bar.
