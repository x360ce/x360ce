# How to set up the x360ce website

Shared by both application versions. This describes hosting the **website**, not building the
application — the page name says otherwise and is wrong.

## Install IIS

Control Panel > Programs and Features > Turn Windows features on

Check:

- [v] Internet Information Services

Make sure that these options are also checked:

- [v] .NET Extensibility 4.5
- [v] ASP.NET 4.5
- [v] ISAPI Extensions
- [v] ISAPI Filters

![Windows Features dialog: Internet Information Services expanded, with .NET Extensibility 4.5, ASP.NET 4.5, ISAPI Extensions and ISAPI Filters ticked](.HowToBuild/windows-features-iis.png)

## Install URL Rewrite Module

Go to: <http://www.microsoft.com/web/downloads/platform.aspx>

Download and launch: Microsoft Web Platform Installer 4.5

Install `Server\URL Rewrite 2.0 Module`:

![Web Platform Installer 4.5, Products > Server, with URL Rewrite 2.0 selected and shown as Installed](.HowToBuild/web-platform-installer-url-rewrite.png)

To configure IIS to log rewritten URLs into its log files, instead of logging the original URLs requested by the HTTP client, run the following command from an elevated command prompt:

```
reg add HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\InetStp\Rewrite /v LogRewrittenUrl /t REG_DWORD /d 1
```

and

```
iisreset
```

Check help: <http://learn.iis.net/page.aspx/517/url-rewriting-for-aspnet-web-forms/>

You can repair a URL Rewrite installation by using the manual installers:
<http://www.iis.net/downloads/microsoft/url-rewrite#additionalDownloads>

## URL Rewrite intellisense for Visual Studio

```
Racer_S\x360ce\x360ce.Web\Resources\Rewrite_Intellisense_VS2012\Install.bat
```

## Configure Web Site

Control Panel > Administrative Tools:

1. Open Computer Management: `Services and Applications\Internet Information Services`
2. Expand the `[Computer]` name on the other side and select the `Sites` node
3. Add Website...

Point `Physical path` to the `x360ce.Web` folder on your computer:

![Add Website dialog: site name x360ce.com, physical path D:\Projects\Racer_S\x360ce\x360ce.Web, http binding on port 80](.HowToBuild/iis-add-website.png)

Select the `x360ce.com` website and update `Bindings...` by adding the `www.x360ce.com` and `localhost.x360ce.com` names:

![Site Bindings dialog listing x360ce.com, www.x360ce.com and localhost.x360ce.com, all http on port 80](.HowToBuild/iis-site-bindings.png)

`localhost.x360ce.com` will be pointed to the `127.0.0.1` IP address by default.

## Update Microsoft .NET 4.0 on IIS

1. Run the command prompt as administrator
2. Type the line in the command prompt below

On 32-bit Windows:

```
%windir%\Microsoft.NET\Framework\v4.0.30319\aspnet_regiis.exe -i
```

On 64-bit Windows:

```
%windir%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis.exe -i
```

## Enable 32-bit Applications

Control Panel > Administrative Tools:

1. Open Computer Management: `Services and Applications\Internet Information Services`
2. Expand the `[Computer]` name on the other side and select the `Application Pools` node
3. Select the `x360ce.com` application pool on the right side
4. Edit Advanced Settings...
5. Set `Enable 32-Bit Applications` to: `true`

![Application pool Advanced Settings with Enable 32-Bit Applications set to True](.HowToBuild/iis-app-pool-advanced-settings.png)

## Unlock web.config modules and handlers

IIS implements "Configuration Locking". This is to help with IIS administration. The IIS Administrator can lock down Configuration Sections, Section Elements and Attributes at the IIS level.

You have to unlock the `web.config` section by executing these two commands inside a Command Prompt with administrator rights:

```
%windir%\system32\inetsrv\appcmd.exe unlock config -section:system.webServer/handlers
%windir%\system32\inetsrv\appcmd.exe unlock config -section:system.webServer/modules
```

## Fix SQL Login after restoring database

Run this SQL stored procedure after restoring the database from backup:

```sql
USE [x360ce]
GO
-- This command will create and fix missing SQL Server Login and Database User.
-- You can change the 'localdev' password to something else.
EXEC [dbo].[Tools_FixUser] 'x360ceAdmin', 'localdev', 1
```

## Why the web project stays on System.Web

`Web/x360ce.Web.csproj` is the one C# project in the legacy format. Every other C# project uses
`Microsoft.NET.Sdk`.

The web project is ASP.NET Web Forms and `.asmx` on `System.Web`. `Microsoft.WebApplication.targets`,
`.aspx` designer generation and web publish are legacy-format features. v5 keeps Web and Data
legacy too.

Web is a separate IIS deployable and is not part of `x360ce.zip`. Its wire contract is frozen,
because deployed v3 and v4 programs call it.

Three routes exist:

1. **W1, leave it legacy.** It costs nothing and leaves one odd project in the solution. It still
   needs Visual Studio MSBuild. This is the choice.
2. **W2, the third-party `MSBuild.SDK.SystemWeb`.** An SDK-style file with the same runtime and
   IIS hosting; a companion SDK generates the Web Forms designer fields at compile time. It is
   one project-file rewrite, with a package outside Microsoft's own. Publishing still needs
   Visual Studio MSBuild. It is worth it only if a uniform solution matters more than avoiding a
   third-party SDK. Not tried here.
3. **W3, rewrite on ASP.NET Core.** SoapCore serves the `.asmx` contract on the same
   `/WebServices/x360ce.asmx` path. The `.aspx` admin pages and the `aspnet_*` membership are
   rebuilt or dropped. Nothing else in the repository depends on the web project. It is its own
   project and the only route that ends on a supported runtime. Plan it when the server is next
   worked on.

## How to Open *.SQLPROJ Projects

If your Visual Studio can't open `*.sqlproj` projects then you don't have "Microsoft SQL Server Data Tools" installed. Go here and download them for your version of Visual Studio:
<http://msdn.microsoft.com/en-us/data/tools.aspx>

You can download these tools as an ISO image (~1.7 GB).

`Data/x360ce.Data.sqlproj` stays in the legacy format on purpose (checked 2026-09-15). Visual Studio
2026 does not open SDK-style SQL projects at all. In Visual Studio 2022 the SDK-style form disables
Schema Compare, which is how the live database is synced into this project
(`Data/Schema Comparisons/*.scmp`). Revisit this when Visual Studio supports the SDK-style form
with Schema Compare.

The SDK-style form (`Microsoft.Build.Sql`) builds under `dotnet build`, and nothing references the
project, so the conversion itself is low risk.

## Changing a table

Make every schema change in this order:

1. The local database.
2. The Data project's table file, under `Data/dbo/Tables`.
3. The model: the `MaxLength` facets in the SSDL and CSDL of `Engine/Data/x360ceModel.edmx`, and
   the `.ssdl`, `.csdl` and `.msl` metadata files checked in beside it, which are what the build
   embeds.
4. Build.
5. Tests green.
6. Only then the live database.

Either half alone loses data silently. The model truncates what the database would take, or the
database refuses what the model passes. A round trip through the service (the `schema-4-23` tests,
see `Tests/ReadMe.md`) is the check.

Drift check: build the DACPAC with Visual Studio MSBuild and run `sqlpackage /Action:DeployReport`
against the local database with `DropObjectsNotInSource=False`. The `x360ce_*` tables must report
zero operations.
