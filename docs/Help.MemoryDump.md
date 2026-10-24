# Making a memory dump when x360ce crashes or freezes

When x360ce closes by itself or stops responding, a memory dump shows where it stopped. A small one, a mini dump, is
usually enough, and it holds little of your data.

When x360ce shows its own error window, it sends an error report itself, so no dump is needed. A dump helps when it
closes without one, or freezes.

## 1. Get ProcDump from Microsoft

1. Download ProcDump from Microsoft's page: https://learn.microsoft.com/sysinternals/downloads/procdump
   (the file is https://download.sysinternals.com/files/Procdump.zip).
2. Unzip it into a folder, for example `C:\ProcDump`.
3. Open that folder in File Explorer, click the address bar, type `cmd` and press Enter. A Command Prompt opens in
   the folder.

Microsoft lists Windows 11 as the version of Windows ProcDump supports. If it does not start on your PC, see
[Without ProcDump](#without-procdump).

## 2. Make the dump

Type the command for what happens, press Enter, and leave the window open.

| When x360ce | Command | What ProcDump does |
| --- | --- | --- |
| closes by itself | `procdump -accepteula -mm -e -w x360ce.exe` | Waits for x360ce, and writes the dump the moment it fails. Run it before you start x360ce, or while it runs. |
| freezes | `procdump -accepteula -mm -h -w x360ce.exe` | Writes the dump once x360ce's window has not answered for 5 seconds. |
| is frozen now | `procdump -accepteula -mm x360ce.exe` | Writes the dump at once. |

ProcDump writes the dump into its own folder, as a file whose name starts with `x360ce` and ends in `.dmp`.

**x360ce 3:** it runs inside the game, so when the game closes or freezes, put the game's program name in place of
`x360ce.exe`, for example `procdump -accepteula -mm -e -w Game.exe`.

## 3. Send it

1. Right-click the `.dmp` file and choose **Send to**, **Compressed (zipped) folder**.
2. Upload the zip through the private link the developer sends you.

Do not attach a dump to a GitHub issue. Anyone can download it there, and a dump can hold your Windows user name,
folder names and settings.

## Without ProcDump

While x360ce is frozen, open Task Manager, go to **Details**, right-click `x360ce.exe` and choose **Create memory dump
file**. Windows shows where it saved the file. This is a full dump: hundreds of MB, with more of your data in it than a
mini dump.
