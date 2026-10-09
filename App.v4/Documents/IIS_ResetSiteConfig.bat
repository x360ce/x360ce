@echo off
:: IIS Express configuration file.
:: \\fileserver1\home\<username>\IISExpress\config\applicationhost.config
:: IIS Express
::SET app=%ProgramFiles(x86)%\IIS Express\APPCMD.exe
::set binding=http://localhost:20360
:: IIS
SET app=%systemroot%\system32\inetsrv\APPCMD.exe
:: Set project folder.
SET prj=c:\Projects\Racer_S\x360ce
:: Alternative path to projects
IF NOT EXIST "%prj%" SET prj=D:\Projects\Racer_S\x360ce
SET domain=x360ce.com
SET siteName=x360ce
::"%app%" clear config /section:system.applicationHost/sites /commit:apphost
:: Delete old site.
"%app%" delete site "%domain%" > nul 2> nul
:: Add site.
"%app%" add site /id:20360 /name:"%domain%" /bindings:"http://%domain%:80,http://www.%domain%:80,http://localhost.%domain%:80" /physicalPath:"%prj%\x360ce.Web" /commit:apphost
:: Add application.
CALL:APP "" 
:: Add virtual applications:
::CALL:ADD "WebApplication1"     "%prj%\WebApplication1"
:: Start IIS Express.
::"%ProgramFiles(x86)%\IIS Express\iisexpress.exe" 
pause
GOTO:EOF

:ADD
set appName=%~1
set appPath=%~2
:: Add virtual folder.
"%app%" set config -section:system.applicationHost/sites /+"[name='%domain%'].[path='/%appName%']" /commit:apphost
:: Set physical path.
"%app%" set config -section:system.applicationHost/sites /+"[name='%domain%'].[path='/%appName%'].[path='/',physicalPath='%appPath%']" /commit:apphost
CALL:APP "%appNname%" 
GOTO:EOF

:APP
SET appName=%~1
SET appPool=%siteName%
IF NOT "%appName%" == "" SET appPool=%siteName%_%appName%
:: Remove old application pool.
"%app%" delete apppool "%appPool%" > nul 2> nul
:: Add new application pool.
"%app%" add apppool /name:%appPool% /managedRuntimeVersion:"v4.0" /managedPipelineMode:"Integrated" /autoStart:"true" /CLRConfigFile:"%%IIS_USER_HOME%%\config\aspnet.config"
:: Set application pool.
"%app%" set app "%domain%/%appName%" /applicationPool:%appPool% /commit:apphost
GOTO:EOF


pause