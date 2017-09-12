:: Remove config files only if they have [Dev|Test|Live].config alternatives.
:: <PreBuildEvent>if NOT EXIST "$(ProjectDir)web.config" copy "$(ProjectDir)web.Dev.config" "$(ProjectDir)web.config"</PreBuildEvent>
:: <PreBuildEvent>if NOT EXIST "$(ProjectDir)app.config" copy "$(ProjectDir)app.Dev.config" "$(ProjectDir)app.config"</PreBuildEvent>
@ECHO OFF

DEL /Q "*.suo"
FOR /D %%X IN (*) DO CALL:SUB "%%X"
pause
GOTO:EOF

:SUB
IF "%~1" == "Resources" GOTO:EOF
IF "%~1" == "Releases" GOTO:EOF
FOR /D %%Y IN ("%~1\*") DO CALL:DEL "%%Y"
GOTO:EOF

:DEL
DEL /Q "%~1\*.dbmdl"
DEL /Q "%~1\*.user"
DEL /Q "%~1\*.suo"
GOTO:EOF
