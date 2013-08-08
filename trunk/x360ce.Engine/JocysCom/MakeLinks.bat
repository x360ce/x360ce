@echo off
::-------------------------------------------------------------
:: Main
::-------------------------------------------------------------

:: Use this file to include source code from other projects.

:: List   symbolic links: dir /A:L
:: Remote symbolic links: rmdir Skype
SET upr=d:\Projects\Jocys.com\WebSites\Engine
CALL:MKL Common\LinkItem.cs
CALL:MKL Common\ItemType.cs
CALL:MKL Common\GuidValueAttribute.cs
CALL:MKL Common\GuidEnum.cs
CALL:MKL Common\DataContextFactoryT.cs
CALL:MKL Security\BuildInRoles.cs
CALL:MKL Security\BuiltInUsers.cs
CALL:MKL Security\Check.cs
CALL:MKL Security\RoleQueryName.cs
CALL:MKL Security\UserQueryName.cs
CALL:MKL Security\Data\Role.cs
CALL:MKL Security\Data\SecurityEntities.cs
CALL:MKL Security\Data\SecurityModel.edmx
CALL:MKL Security\Data\SecurityModel.Designer.cs
CALL:MKL Security\Data\User.cs
SET upr=D:\Projects\Jocys.com\Class Library

pause
GOTO:EOF

::=============================================================
:MKL
::-------------------------------------------------------------

IF NOT EXIST "%~p1" mkdir "%~p1"
IF EXIST "%~1" (
  echo Already exists: %~1
) ELSE (
  echo Map: %~1
  fsutil hardlink create "%~1" "%upr%\%~1" > nul
)
GOTO:EOF
