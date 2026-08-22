# UI drive & capture scripts

Tooling used to verify App.v3 / App.v4 rendering during the
`revert-to-4.17.0.0-reapply-bugfixes` work (see `docs/plans/README.md`).

- `Invoke-AppUiCapture.ps1` — raises the running app without stealing focus,
  selects tabs via native `TCM_SETCURFOCUS` messages, and captures the window
  with `PrintWindow(PW_RENDERFULLCONTENT)` (works while occluded). Output goes
  to `captures/` (git-ignored — screenshots are evidence, never committed).

Typical session:

```powershell
# Start the app first (App.v4):
./App.v4/bin/Debug/x360ce.exe

# Natural-size capture of the current tab:
./scripts/ui/Invoke-AppUiCapture.ps1 -NoResize -Capture pad1.png

# Select main tab strip item 6 (Devices), capture:
./scripts/ui/Invoke-AppUiCapture.ps1 -SelectTabs 0,6 -Capture devices.png
```

Each invocation needs a fresh PowerShell process (`Add-Type` types cannot be
redefined in-session).
