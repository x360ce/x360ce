@echo off
SET wra="%ProgramFiles%\WinRAR\winrar.exe"
if NOT EXIST %wra% SET wra="%ProgramFiles(x86)%\WinRAR\winrar.exe"
if NOT EXIST %wra% SET wra="%ProgramW6432%\WinRAR\winrar.exe"
SET zip=%wra% a -ep
:: ---------------------------------------------------------------------------
SET x86r=Files\x360ce.zip
SET x86d=Files\x360ce_debug.zip
SET x64r=Files\x360ce_x64.zip
SET x64d=Files\x360ce_x64_debug.zip
echo --- Delete files
IF NOT EXIST Files\nul MKDIR Files
IF EXIST %x86r% DEL %x86r%
IF EXIST %x86d% DEL %x86d%
IF EXIST %x64r% DEL %x64r%
IF EXIST %x64d% DEL %x64d%
::-------------------------------------------------------------
:: Archive x86 application.
%zip% %x86r% ..\bin\Debug_x86\x360ce.exe
:: Archive x86 application with debug info.
%zip% %x86d% ..\bin\Debug_x86\x360ce.exe
%zip% %x86d% ..\bin\Debug_x86\x360ce.pdb
%zip% %x86d% ..\bin\Debug_x86\x360ce.Engine.dll
%zip% %x86d% ..\bin\Debug_x86\x360ce.Engine.pdb
%zip% %x86d% ..\..\x360ce\x360ce\bin\Debug\xinput1_3.dll
%zip% %x86d% ..\..\x360ce\x360ce\bin\Debug\xinput1_3.pdb
::-------------------------------------------------------------
:: Rename 64-bit file so it can be in same folder as 32-bit exe.
copy /Y ..\bin\Debug_x64\x360ce.exe ..\bin\Debug_x64\x360ce_x64.exe
copy /Y ..\bin\Debug_x64\x360ce.pdb ..\bin\Debug_x64\x360ce_x64.pdb
::-------------------------------------------------------------
:: Archive x64 application.
%zip% %x64r% ..\bin\Debug_x64\x360ce_x64.exe
:: Archive x64 application with debug info.
%zip% %x64d% ..\bin\Debug_x64\x360ce_x64.exe
%zip% %x64d% ..\bin\Debug_x64\x360ce_x64.pdb
%zip% %x64d% ..\bin\Debug_x64\x360ce.Engine.dll
%zip% %x64d% ..\bin\Debug_x64\x360ce.Engine.pdb
%zip% %x64d% ..\..\x360ce\x360ce\bin64\Debug\xinput1_3.dll
%zip% %x64d% ..\..\x360ce\x360ce\bin64\Debug\xinput1_3.pdb
pause
