# Support Mail — Crash Report Analysis

First batch pulled from `support@x360ce.com` through the n8n workflow
`Support - Ingest Mail`, 2026-08-22.

| | |
|---|---|
| Messages | 200 |
| Date range | 2026-07-03 to 2026-07-06 |
| Distinct machines | **125** |
| Versions | 4.17.15.0 (187), 4.16.8.0 (4), unknown (9) |
| Distinct crash signatures | 31 |

125 distinct machines means these are real users, not one person's repeats. The
whole batch lands in four days, so it is a burst rather than a steady trickle —
most likely the days after a release.

Reports are clustered by exception type plus the first application stack frame.
Raw mail is never committed; it is fetched to the git-ignored `.tmp/` folder and
analysed there.

## Ranked issues

| Reports | Exception | Site | State in restored tree |
|---|---|---|---|
| **51** | AggregateException / InvalidOperationException | `PadControl.ShowHideAndSelectGridRows` via `UserSettings_Items_ListChanged` | **present** |
| 36 | InvalidOperationException | `SettingsManager.GetMappedDevices` | needs checking |
| 31 | DInputException | `DInputHelper.UpdateDiStates` (Step2, several lines) | needs checking |
| 13 | ArgumentException | `ForegroundWindowHook.GetActiveProcess` | **present, fix known** |
| **11** | ViGEmException | `DInputHelper.FeedDevice` | **fixed 2026-08-22** |
| 7 | AggregateException | `PadControl.SendVibration`, `MainForm`, `SettingsGridUserControl` | needs checking |
| 5 | IOException / UnauthorizedAccessException | `XInputMaskScanner.GetMask(s)` | needs checking |
| 4 | DllNotFoundException | `ViGEmClient` construction | related to VC++ runtime work |
| 4 | NullReferenceException | `Program.StartApp`, `SettingsManager.ApplyAllSettingsToXML` | needs checking |

### 1. Grid rebuild during settings change — 51 reports, the dominant issue

Two signatures, one bug. `UserSettings_Items_ListChanged` (42 reports) marshals
to the interface thread and calls `ShowHideAndSelectGridRows`, which throws
(9 reports report the inner exception directly). Whether it surfaces as
`AggregateException` or `InvalidOperationException` depends only on where the
wrapper caught it, so the true count is the sum.

The failing statement is the selection restore at the end of the rebuild:

```csharp
// PadControl.cs — inside the suspend/resume binding block
cm.SuspendBinding();
foreach (var item in itemsToRemove) mappedItems.Remove(item);
foreach (var item in itemsToInsert) mappedItems.Add(item);
if (bound) cm.ResumeBinding();
grid.ResumeLayout();
ControlsHelper.RestoreSelection(grid, nameof(UserSetting.InstanceGuid), selection);   // throws here
```

The settings read is already safe — `ItemsToArraySyncronized()` plus
`DevicesToMapDataGridViewLock`. What is not protected is the grid's own binding
state: rows are added and removed, binding is resumed, and the selection is then
restored against a `CurrencyManager` whose position may no longer be valid. A
device arriving or leaving mid-rebuild is enough. That matches the reported
control paths, which are spread across the interface rather than concentrated on
one screen.

A quarter of all reports come from this one defect. It deserves the next fix.

### 2. Foreground process lookup races — 13 reports, small and certain

`Engine/Common/ForegroundWindowHook.cs`:

```csharp
public static Process GetActiveProcess(IntPtr? hWnd = null)
{
    if (!hWnd.HasValue)
        hWnd = NativeMethods.GetForegroundWindow();
    var _ = NativeMethods.GetWindowThreadProcessId(hWnd.Value, out var processId);
    var process = Process.GetProcessById((int)processId);   // ArgumentException
    return process;
}
```

`Process.GetProcessById` throws `ArgumentException` when the process is gone.
Between reading the foreground window and resolving its id, the user can close
that window — and the application then crashes because someone alt-tabbed away
from something that was closing. Unguarded in the restored tree.

The fix is small and safe: catch and return null, and confirm the callers of
`OnActivate` tolerate a null process.

### 3. Already fixed today — 11 reports

`ViGEmException` escaping `DInputHelper.FeedDevice` is the unprotected
`SendReport` call, wrapped in try/catch in 4.17.46.0 (PR 1604, ykondury). These
reports are direct evidence that the defect happens in the field, not just in
theory.

Also worth noting: several stack traces end at `DInputHelper.ThreadAction()` with
nothing above them, which is the controller worker thread dying on an unhandled
exception — the other defect fixed in that batch. The exception still needs
fixing at its source, but it no longer kills input.

## Next steps

1. Fix the grid rebuild race (51 reports).
2. Fix `GetActiveProcess` (13 reports).
3. Check `GetMappedDevices` and the `UpdateDiStates` family against the restored
   tree; the locking there was partly reworked during the reapply, so some of
   these may already be closed.
4. Pull further batches. This one covers four days in July; the mailbox holds
   far more.
