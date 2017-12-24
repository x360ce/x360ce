@echo off
SET wra="%ProgramFiles%\WinRAR\winrar.exe"
if NOT EXIST %wra% SET wra="%ProgramFiles(x86)%\WinRAR\winrar.exe"
if NOT EXIST %wra% SET wra="%ProgramW6432%\WinRAR\winrar.exe"
SET zip=%wra% a -ep
:: ---------------------------------------------------------------------------
echo --- Delete files
IF NOT EXIST Files\nul MKDIR Files
IF EXIST Files\x360ce.zip DEL x360ce.zip
IF EXIST Files\x360ce_debug.zip DEL x360ce_debug.zip
IF EXIST Files\x360ce_x64.zip DEL x360ce_x64.zip
IF EXIST Files\x360ce_x64_debug.zip DEL x360ce_x64_debug.zip
:: Archive x86 application.
%zip% Files\x360ce.zip "..\bin\Debug_x86\x360ce.exe"
:: Archive x86 application with debug info.
%zip% Files\x360ce_debug.zip "..\bin\Debug_x86\x360ce.exe"
::%zip% Files\x360ce_debug.zip "..\bin\Debug_x86\x360ce.pdb"
%zip% Files\x360ce_debug.zip "..\..\x360ce.Engine\bin\Debug_x86\x360ce.Engine.dll"
::%zip% Files\x360ce_debug.zip "..\..\x360ce.Engine\bin\Debug_x86\x360ce.Engine.pdb"
%zip% Files\x360ce_debug.zip "..\..\x360ce\x360ce\bin\Debug\xinput1_3.dll"
%zip% Files\x360ce_debug.zip "..\..\x360ce\x360ce\bin\Debug\xinput1_3.pdb"
:: Rename x64 application.
copy /Y "..\bin\Debug_x64\x360ce.exe" "..\bin\Debug_x64\x360ce_x64.exe"
::copy /Y "..\bin\Debug_x64\x360ce.pdb" "..\bin\Debug_x64\x360ce_x64.pdb"
:: Archive x64 application.
%zip% Files\x360ce_x64.zip "..\bin\Debug_x64\x360ce_x64.exe"
:: Archive x64 application with debug info.
%zip% Files\x360ce_x64_debug.zip "..\bin\Debug_x64\x360ce_x64.exe"
::%zip% Files\x360ce_x64_debug.zip "..\bin\Debug_x64\x360ce_x64.pdb"
%zip% Files\x360ce_x64_debug.zip "..\..\x360ce.Engine\bin\Debug_x64\x360ce.Engine.dll"
::%zip% Files\x360ce_x64_debug.zip "..\..\x360ce.Engine\bin\Debug_x64\x360ce.Engine.pdb"
%zip% Files\x360ce_x64_debug.zip "..\..\x360ce\x360ce\bin64\Debug\xinput1_3.dll"
%zip% Files\x360ce_x64_debug.zip "..\..\x360ce\x360ce\bin64\Debug\xinput1_3.pdb"
pause