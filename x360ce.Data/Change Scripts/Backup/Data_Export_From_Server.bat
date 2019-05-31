@echo off
::=============================================================
:: SET PROPERTIES
::-------------------------------------------------------------
TITLE=Export Data
SET srv=localhost
SET cat=x360ce
SET usr=
SET pwd=
SET compress=1
:: Note: You cannot pass Windows or Domain user name and password. It is not allowed. 
:: You can pass a SQL user name and password, or use the -T for trusted connection.
::-------------------------------------------------------------
CALL:ConfigTools
CALL:PromptComputer
CALL:PromptDatabase
CALL:PromptUsername
CALL:PromptPassword
::-------------------------------------------------------------
CALL:EXP aspnet_Applications
CALL:EXP aspnet_Membership
CALL:EXP aspnet_Roles
CALL:EXP aspnet_SchemaVersions
CALL:EXP aspnet_Users
CALL:EXP aspnet_UsersInRoles
CALL:EXP Tools_Obsolete_Objects
CALL:EXP x360ce_ChangeLogs
CALL:EXP x360ce_Layouts
CALL:EXP x360ce_PadSettings
CALL:EXP x360ce_Products
CALL:EXP x360ce_Programs
CALL:EXP x360ce_Settings
CALL:EXP x360ce_Summaries
CALL:EXP x360ce_UserComputers
CALL:EXP x360ce_UserDevices
CALL:EXP x360ce_UserGames
CALL:EXP x360ce_UserInstances
CALL:EXP x360ce_UserProfiles
CALL:EXP x360ce_Vendors
CALL:EXP x360ce_WebsiteLogs
echo.
pause
::=============================================================
:: Finalize
::-------------------------------------------------------------
::pause
::start notepad %log%
GOTO:EOF

::=============================================================
:ConfigTools :: CONFIGURE EXECUTABLE FILES
::-------------------------------------------------------------
:: Requires: "Microsoft SQL Native Client" 
:: Can be extracted from SQL 2005 SP2
:: ExtractedPath\hotfixsqlncli\files\Sqlncli.msi
:: or .\Tools folder
SET log=Logs\%~n0.log
IF NOT EXIST "Logs" MKDIR "Logs"
echo. > %log%
SET bcp=bcp.exe
SET val=Tools\%PROCESSOR_ARCHITECTURE%\bcp.exe
IF EXIST "%val%" SET bcp=%val%
SET sqlcmd=sqlcmd.exe
SET val=Tools\%PROCESSOR_ARCHITECTURE%\sqlcmd.exe
IF EXIST "%val%" SET sqlcmd=%val%
SET val=
GOTO:EOF
::=============================================================
:: USER INPUT
:: ------------------------------------------------------------
:PromptComputer
SET msg=Enter database server
IF NOT "%srv%" == "" SET msg=%msg% (%srv%)
SET val=
SET /P val=%msg%: 
IF NOT "%val%" == "" SET srv=%val%
GOTO:EOF
:: ------------------------------------------------------------
:PromptDatabase
SET msg=Enter database catalog
IF NOT "%cat%" == "" SET msg=%msg% (%cat%)
SET val=
SET /P val=%msg%: 
IF NOT "%val%" == "" SET cat=%val%
GOTO:EOF
:: ------------------------------------------------------------
:PromptUsername
SET msg=Enter database username
IF NOT "%usr%" == "" SET msg=%msg% (%usr%)
SET val=
SET /P val=%msg%: 
IF NOT "%val%" == "" SET usr=%val%
:: Use trusted connection by default.
SET bcpo=-S %srv% -T
SET slqo=-S %srv% -E
GOTO:EOF
::-------------------------------------------------------------
:PromptPassword
IF "%usr%" == "" GOTO:EOF
SET msg=Enter database password
IF NOT "%pwd%" == "" SET msg=%msg% (%pwd%)
SET val=
SET /P val=%msg%: 
IF NOT "%val%" == "" SET pwd=%val%
:: Configure security.
SET bcpo=-S %srv% -U %usr% -P %pwd%
SET slqo=-S %srv% -U %usr% -P %pwd%
GOTO:EOF

::=============================================================
:EXP :: EXPORT SQL DATA
::-------------------------------------------------------------
:: Parameters: TableName FileName
SET ep=%~2
IF "%ep%" == "" set ep=Data\%1.dat
echo Exporting: %1 to %ep%
echo ================================================= >> %log%
echo Exporting: %1 to %ep% >> %log%
echo ------------------------------------------------- >> %log%
:: Export file in native format.
SET pars=-N
:: Create missing folder.
IF NOT EXIST "Data" MKDIR "Data"
:: Export file in text format.
@echo "%ep%" | find ".txt" > nul
IF %ERRORLEVEL% == 0 SET pars=-c -t
:: Export file in XML format.
@echo "%ep%" | find ".xml" > nul
IF %ERRORLEVEL% == 0 SET pars=-x
:: Run Export command.
IF "%~3" == "" bcp "%cat%.dbo.%~1" out "%ep%" %pars% %bcpo% >> %log%
IF NOT "%~3" == "" bcp "%~3" queryout "%ep%" %pars% %bcpo% >> %log%
IF "%compress%" == "1" CALL:ComArchive "%ep%" >> %log%
IF "%compress%" == "1" del "%ep%" >> %log%
echo. >> %log%
GOTO:EOF

::=============================================================
:: Compress and Expand Archive.
::-------------------------------------------------------------
:ComArchive
ECHO Compress %~nx1
:: Use PowerShell if 7-Zip do not exists.
CD "%~p1"
SET arc=..\Tools\7za.exe
IF EXIST "%arc%" "%arc%" a -v100m "%~nx1.7z" "%~nx1"
IF NOT EXIST "%arc%" PowerShell.exe Compress-Archive -Path "%~nx1" -DestinationPath "%~nx1.zip" -Force
CD ..
GOTO:EOF

:ExpArchive
ECHO Expand %~nx1
:: Use PowerShell if 7-Zip do not exists.
CD "%~p1"
SET arc=..\Tools\7za.exe
IF EXIST "%arc%" "%arc%" e "%~nx1.7z.001" "*.*"
IF NOT EXIST "%arc%" PowerShell.exe Expand-Archive -Path "%~nx1.zip" -DestinationPath "." -Force
CD ..
GOTO:EOF
