# Sub-project B — Test Database Scaffolding (implementation plan)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce a working set of PowerShell scripts under `scripts/db/` that build a DACPAC from `Data/x360ce.Data.sqlproj`, deploy it to a local SQL Server Developer Edition instance as database `x360ce_Tests`, seed minimal synthetic fixtures, drop it, and refresh it — with a hard guardrail that refuses to operate on the live `x360ce` database.

**Architecture:** Seven PowerShell scripts plus four SQL seed files. Each script is independently runnable from the repo root. The guardrail is a dot-sourced helper function `Assert-TestDbAllowed` shared by every write script. DACPAC is built via MSBuild against the existing sqlproj (canonical schema SSOT); deployed via SqlPackage.exe. No external test framework dependencies beyond Pester (in-box on PowerShell 5.1+) for the guardrail's unit tests.

**Tech Stack:** PowerShell 7.x (already present at 7.6.0), MSBuild via vswhere (VS 2026 Community present at `C:\Program Files\Microsoft Visual Studio\18\Community`), SqlPackage.exe (NOT currently installed — Task 0 installs it), Pester (in-box), SQL Server Developer Edition at `localhost` (already present), `Data/x360ce.Data.sqlproj` (canonical SSOT, schema-only, no CLR).

**Connection string (informational; consumed by sub-project C, not B):**
`data source=localhost;initial catalog=x360ce_Tests;persist security info=True;Integrated Security=True;multipleactiveresultsets=True`

---

## File structure (what gets created)

```
scripts/
└── db/
    ├── README.md                       # usage, prereqs, recovery
    ├── Verify-TestDbName.ps1           # guardrail helper (dot-sourced)
    ├── Find-MSBuild.ps1                # locates MSBuild via vswhere
    ├── Find-SqlPackage.ps1             # locates SqlPackage.exe
    ├── Build-TestDbDacpac.ps1          # MSBuild sqlproj -> DACPAC
    ├── Deploy-TestDb.ps1               # DACPAC -> x360ce_Tests; calls Seed
    ├── Drop-TestDb.ps1                 # DROP DATABASE x360ce_Tests (confirm)
    ├── Refresh-TestDb.ps1              # Drop -Force then Deploy
    ├── Seed-TestDb.ps1                 # runs scripts/db/seed/*.sql in order
    ├── Compare-TestDbToLive.ps1        # SqlPackage /Action:DeployReport vs x360ce (read-only)
    ├── seed/
    │   ├── 01_vendors.sql              # 3 synthetic vendor rows
    │   ├── 02_products.sql             # 3 synthetic product rows
    │   ├── 03_user_computer.sql        # 1 test computer row
    │   └── 04_programs.sql             # 3 synthetic program rows
    └── tests/
        └── Verify-TestDbName.Tests.ps1 # Pester tests for the guardrail
```

`Data/bin/Release/x360ce.Data.dacpac` is a build output (not a checked-in file).

---

## Task 0: One-time prerequisite — install SqlPackage

**Files:** none (environment install).

- [ ] **Step 1: Check if SqlPackage is already installed**

Run:
```powershell
Get-Command sqlpackage -ErrorAction SilentlyContinue | Select-Object Source
```
Expected: empty output (confirmed not installed via probe 2026-05-16). If a path is shown, skip Step 2.

- [ ] **Step 2: Install SqlPackage via winget**

Run:
```powershell
winget install Microsoft.SqlPackage
```
Expected: "Successfully installed".

- [ ] **Step 3: Verify installation**

Run:
```powershell
sqlpackage /version
```
Expected: version banner like `Microsoft SqlPackage 162.x.x.x`.

- [ ] **Step 4: No commit — environment-only**

---

## Task 1: Create folder structure and README skeleton

**Files:**
- Create: `scripts/db/README.md`

- [ ] **Step 1: Create directory tree**

Run:
```powershell
New-Item -ItemType Directory -Force -Path 'scripts\db\seed','scripts\db\tests' | Out-Null
```

- [ ] **Step 2: Write the README skeleton**

Create `scripts/db/README.md`:
```markdown
# scripts/db — Test Database Scaffolding

Scripts that manage the local `x360ce_Tests` SQL Server database. See `docs/plans/B-db/design.md` for full design.

## Prerequisites
- SQL Server Developer Edition (or higher) running on `localhost`.
- `SqlPackage.exe` in PATH (`winget install Microsoft.SqlPackage`).
- Visual Studio 2026 (or 2022) with SSDT for sqlproj build.
- Windows authentication with `dbcreator` on the local instance.

## Usage
```powershell
# Deploy the test DB (builds DACPAC, publishes, seeds)
.\scripts\db\Deploy-TestDb.ps1

# Drop the test DB
.\scripts\db\Drop-TestDb.ps1

# Refresh (drop + deploy)
.\scripts\db\Refresh-TestDb.ps1

# Read-only diff vs live
.\scripts\db\Compare-TestDbToLive.ps1
```

## Guardrail
All write scripts call `Assert-TestDbAllowed` (in `Verify-TestDbName.ps1`) which refuses any name not matching `^x360ce_Tests(_\w+)?$`. The live `x360ce` database is never written to.
```

- [ ] **Step 3: Commit (when user authorises)**

`git add scripts/db/README.md scripts/db/seed scripts/db/tests`
`git commit -m "scaffold: scripts/db folder + README"`

> **Do NOT commit without explicit user approval.** Park the staged changes and report.

---

## Task 2: Write Verify-TestDbName.ps1 (the guardrail)

**Files:**
- Create: `scripts/db/Verify-TestDbName.ps1`

- [ ] **Step 1: Write the guardrail**

Create `scripts/db/Verify-TestDbName.ps1`:
```powershell
<#
.SYNOPSIS
  Refuses to operate on the live x360ce database.
.DESCRIPTION
  Dot-sourced by every script in scripts/db that writes to a database.
  Throws if the database name is not in the allow-list (^x360ce_Tests(_\w+)?$).
#>

function Assert-TestDbAllowed {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Database)
    if ($Database -notmatch '^x360ce_Tests(_\w+)?$') {
        throw "REFUSED. Database name '$Database' is not in the test allow-list " +
              "(^x360ce_Tests(_\w+)?$). This guard prevents tests from writing " +
              "to the live x360ce database."
    }
}
```

- [ ] **Step 2: Smoke-check by dot-sourcing**

Run:
```powershell
. .\scripts\db\Verify-TestDbName.ps1
Assert-TestDbAllowed -Database 'x360ce_Tests'      # should succeed silently
Assert-TestDbAllowed -Database 'x360ce_Tests_dev'  # should succeed silently
try { Assert-TestDbAllowed -Database 'x360ce' } catch { Write-Host "OK refused: $_" }
```
Expected: first two return nothing; third prints `OK refused: REFUSED. ...`.

---

## Task 3: Write Pester tests for the guardrail

**Files:**
- Create: `scripts/db/tests/Verify-TestDbName.Tests.ps1`

- [ ] **Step 1: Check Pester is available**

Run:
```powershell
Get-Module -ListAvailable Pester | Select-Object -First 1 Name, Version
```
Expected: Pester v5.x present.
If missing: `Install-Module -Name Pester -Force -SkipPublisherCheck`.

- [ ] **Step 2: Write the test file**

Create `scripts/db/tests/Verify-TestDbName.Tests.ps1`:
```powershell
BeforeAll {
    . "$PSScriptRoot\..\Verify-TestDbName.ps1"
}

Describe 'Assert-TestDbAllowed' {

    Context 'allowed names' {
        It 'accepts x360ce_Tests' {
            { Assert-TestDbAllowed -Database 'x360ce_Tests' } | Should -Not -Throw
        }
        It 'accepts x360ce_Tests_dev' {
            { Assert-TestDbAllowed -Database 'x360ce_Tests_dev' } | Should -Not -Throw
        }
        It 'accepts x360ce_Tests_ci' {
            { Assert-TestDbAllowed -Database 'x360ce_Tests_ci' } | Should -Not -Throw
        }
    }

    Context 'refused names' {
        It 'refuses the live x360ce' {
            { Assert-TestDbAllowed -Database 'x360ce' } |
                Should -Throw -ExpectedMessage '*REFUSED*'
        }
        It 'refuses master' {
            { Assert-TestDbAllowed -Database 'master' } |
                Should -Throw -ExpectedMessage '*REFUSED*'
        }
        It 'refuses lowercase x360ce_tests' {
            { Assert-TestDbAllowed -Database 'x360ce_tests' } |
                Should -Throw -ExpectedMessage '*REFUSED*'
        }
        It 'refuses x360ce_TestSomething (no underscore)' {
            { Assert-TestDbAllowed -Database 'x360ce_TestSomething' } |
                Should -Throw -ExpectedMessage '*REFUSED*'
        }
        It 'refuses empty string' {
            { Assert-TestDbAllowed -Database '' } | Should -Throw
        }
    }
}
```

- [ ] **Step 3: Run the tests**

Run:
```powershell
Invoke-Pester .\scripts\db\tests\Verify-TestDbName.Tests.ps1 -Output Detailed
```
Expected: all 8 tests pass.

- [ ] **Step 4: Commit (when user authorises)**

`git add scripts/db/Verify-TestDbName.ps1 scripts/db/tests/Verify-TestDbName.Tests.ps1`
`git commit -m "feat(scripts/db): allow-list guardrail with Pester tests"`

---

## Task 4: Write Find-MSBuild.ps1 helper

**Files:**
- Create: `scripts/db/Find-MSBuild.ps1`

- [ ] **Step 1: Write the helper**

Create `scripts/db/Find-MSBuild.ps1`:
```powershell
<#
.SYNOPSIS
  Locates MSBuild.exe on the local machine.
.DESCRIPTION
  Searches PATH, then uses vswhere to find the latest VS installation's MSBuild.
.OUTPUTS
  String — full path to MSBuild.exe.
#>

function Find-MSBuild {
    [CmdletBinding()]
    param()

    # 1. Already in PATH?
    $cmd = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    # 2. Find via vswhere.
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) {
        $vswhere = "$env:ProgramFiles\Microsoft Visual Studio\Installer\vswhere.exe"
    }
    if (Test-Path $vswhere) {
        $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild `
                              -find 'MSBuild\**\Bin\MSBuild.exe' |
                   Select-Object -First 1
        if ($msbuild -and (Test-Path $msbuild)) { return $msbuild }
    }

    throw "MSBuild.exe not found. Install Visual Studio with the 'MSBuild' component, " +
          "or open a Developer PowerShell prompt before running this script."
}
```

- [ ] **Step 2: Smoke-check**

Run:
```powershell
. .\scripts\db\Find-MSBuild.ps1
Find-MSBuild
```
Expected: a path ending in `MSBuild.exe`. (Probe showed VS 2026 at `C:\Program Files\Microsoft Visual Studio\18\Community` — vswhere should find MSBuild under it.)

---

## Task 5: Write Find-SqlPackage.ps1 helper

**Files:**
- Create: `scripts/db/Find-SqlPackage.ps1`

- [ ] **Step 1: Write the helper**

Create `scripts/db/Find-SqlPackage.ps1`:
```powershell
<#
.SYNOPSIS
  Locates SqlPackage.exe on the local machine.
.OUTPUTS
  String — full path to SqlPackage.exe.
#>

function Find-SqlPackage {
    [CmdletBinding()]
    param()

    # 1. Already in PATH?
    $cmd = Get-Command sqlpackage -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    # 2. Standalone DAC install paths.
    $candidates = @(
        "$env:ProgramFiles\Microsoft SQL Server\160\DAC\bin\sqlpackage.exe",
        "$env:ProgramFiles\Microsoft SQL Server\150\DAC\bin\sqlpackage.exe",
        "$env:ProgramFiles\Microsoft\SqlPackage\sqlpackage.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path $p) { return $p }
    }

    # 3. WinGet package location.
    $wingetGlob = "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Microsoft.SqlPackage*\sqlpackage.exe"
    $found = Get-ChildItem -Path $wingetGlob -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { return $found.FullName }

    # 4. VS install path.
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) {
        $vswhere = "$env:ProgramFiles\Microsoft Visual Studio\Installer\vswhere.exe"
    }
    if (Test-Path $vswhere) {
        $vsRoot = & $vswhere -latest -property installationPath 2>$null
        if ($vsRoot) {
            $sp = Get-ChildItem -Path $vsRoot -Recurse -Filter 'sqlpackage.exe' -ErrorAction SilentlyContinue |
                  Select-Object -First 1
            if ($sp) { return $sp.FullName }
        }
    }

    throw "SqlPackage.exe not found. Install via:  winget install Microsoft.SqlPackage"
}
```

- [ ] **Step 2: Smoke-check (after Task 0)**

Run:
```powershell
. .\scripts\db\Find-SqlPackage.ps1
Find-SqlPackage
```
Expected: a path ending in `sqlpackage.exe`.

- [ ] **Step 3: Commit (when user authorises)**

`git add scripts/db/Find-MSBuild.ps1 scripts/db/Find-SqlPackage.ps1`
`git commit -m "feat(scripts/db): MSBuild + SqlPackage locator helpers"`

---

## Task 6: Write Build-TestDbDacpac.ps1

**Files:**
- Create: `scripts/db/Build-TestDbDacpac.ps1`

- [ ] **Step 1: Write the build script**

Create `scripts/db/Build-TestDbDacpac.ps1`:
```powershell
<#
.SYNOPSIS
  Builds Data/x360ce.Data.sqlproj and returns the DACPAC path.
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\Find-MSBuild.ps1"

$repoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$sqlproj  = Join-Path $repoRoot 'Data\x360ce.Data.sqlproj'
if (-not (Test-Path $sqlproj)) {
    throw "Not found: $sqlproj"
}

$msbuild = Find-MSBuild
Write-Host "Using MSBuild: $msbuild"
Write-Host "Building $sqlproj ($Configuration)..."

& $msbuild $sqlproj "/p:Configuration=$Configuration" "/v:minimal" "/nologo"
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed (exit $LASTEXITCODE)"
}

$dacpac = Join-Path $repoRoot "Data\bin\$Configuration\x360ce.Data.dacpac"
if (-not (Test-Path $dacpac)) {
    throw "Build succeeded but DACPAC not found at $dacpac"
}

Write-Host "DACPAC: $dacpac" -ForegroundColor Green
$dacpac
```

- [ ] **Step 2: Run a clean build**

Run:
```powershell
.\scripts\db\Build-TestDbDacpac.ps1
```
Expected: prints `DACPAC: <path>\x360ce.Data.dacpac` and the file exists. If MSBuild fails, open `Data\x360ce.Data.sqlproj` in Visual Studio to inspect.

- [ ] **Step 3: Commit (when user authorises)**

`git add scripts/db/Build-TestDbDacpac.ps1`
`git commit -m "feat(scripts/db): build DACPAC from sqlproj"`

---

## Task 7: Write Deploy-TestDb.ps1 (without seed first)

**Files:**
- Create: `scripts/db/Deploy-TestDb.ps1`

- [ ] **Step 1: Write the deploy script (seed call commented out, added in Task 10)**

Create `scripts/db/Deploy-TestDb.ps1`:
```powershell
<#
.SYNOPSIS
  Publishes the test DACPAC to x360ce_Tests.
.PARAMETER Server
  Default 'localhost'.
.PARAMETER Database
  Default 'x360ce_Tests'. Refused by guardrail unless matches ^x360ce_Tests(_\w+)?$.
.PARAMETER Configuration
  Build configuration: Debug or Release. Default Release.
.PARAMETER SkipSeed
  If set, skips Seed-TestDb.ps1 call after publish.
#>

[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [string]$Database = 'x360ce_Tests',
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipSeed
)
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\Verify-TestDbName.ps1"
. "$PSScriptRoot\Find-SqlPackage.ps1"

Assert-TestDbAllowed -Database $Database

$dacpac     = & "$PSScriptRoot\Build-TestDbDacpac.ps1" -Configuration $Configuration
$sqlPackage = Find-SqlPackage

Write-Host "Deploying $dacpac" -ForegroundColor Cyan
Write-Host "    to $Server\$Database" -ForegroundColor Cyan

& $sqlPackage `
    /Action:Publish `
    /SourceFile:$dacpac `
    /TargetServerName:$Server `
    /TargetDatabaseName:$Database `
    /TargetTrustServerCertificate:True `
    /Quiet:False

if ($LASTEXITCODE -ne 0) {
    throw "SqlPackage Publish failed (exit $LASTEXITCODE)"
}

# Task 10 will uncomment the Seed call.
# if (-not $SkipSeed) {
#     & "$PSScriptRoot\Seed-TestDb.ps1" -Server $Server -Database $Database
# }

Write-Host "Deployed $Database at $(Get-Date -Format 'o')" -ForegroundColor Green
```

- [ ] **Step 2: Run the deploy**

Run:
```powershell
.\scripts\db\Deploy-TestDb.ps1
```
Expected: SqlPackage logs schema operations and finishes with "Update complete." or similar.

- [ ] **Step 3: Verify the database exists**

Run:
```powershell
Invoke-Sqlcmd -ServerInstance 'localhost' `
              -Query "SELECT name FROM sys.databases WHERE name = 'x360ce_Tests'"
```
Expected: one row with `name = x360ce_Tests`.

If `Invoke-Sqlcmd` is missing, install: `Install-Module SqlServer -Scope CurrentUser`.

- [ ] **Step 4: Verify schema deployed**

Run:
```powershell
Invoke-Sqlcmd -ServerInstance 'localhost' -Database 'x360ce_Tests' `
              -Query "SELECT TOP 5 name FROM sys.tables ORDER BY name"
```
Expected: at least 5 table names, including `x360ce_Vendors`.

- [ ] **Step 5: Verify guardrail refuses live db**

Run:
```powershell
try {
    .\scripts\db\Deploy-TestDb.ps1 -Database 'x360ce'
    Write-Error "GUARDRAIL FAILED — deploy ran against x360ce!"
} catch {
    Write-Host "OK guardrail refused: $($_.Exception.Message)" -ForegroundColor Green
}
```
Expected: prints `OK guardrail refused: REFUSED. ...`.

- [ ] **Step 6: Commit (when user authorises)**

`git add scripts/db/Deploy-TestDb.ps1`
`git commit -m "feat(scripts/db): deploy DACPAC to x360ce_Tests (no seed yet)"`

---

## Task 8: Write seed SQL files

**Files:**
- Create: `scripts/db/seed/01_vendors.sql`
- Create: `scripts/db/seed/02_products.sql`
- Create: `scripts/db/seed/03_user_computer.sql`
- Create: `scripts/db/seed/04_programs.sql`

- [ ] **Step 1: Write 01_vendors.sql**

The `x360ce_Vendors` table has columns (per `Data/dbo/Tables/x360ce_Vendors.sql`):
`VendorId INT PK, VendorName NVARCHAR(256), ShortName NVARCHAR(32), WebSite NVARCHAR(256)`.

Create `scripts/db/seed/01_vendors.sql`:
```sql
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.x360ce_Vendors WHERE VendorId = 65533)
    INSERT INTO dbo.x360ce_Vendors (VendorId, VendorName, ShortName, WebSite)
    VALUES (65533, 'Test Vendor A', 'TVA', '');

IF NOT EXISTS (SELECT 1 FROM dbo.x360ce_Vendors WHERE VendorId = 65534)
    INSERT INTO dbo.x360ce_Vendors (VendorId, VendorName, ShortName, WebSite)
    VALUES (65534, 'Test Vendor B', 'TVB', '');

IF NOT EXISTS (SELECT 1 FROM dbo.x360ce_Vendors WHERE VendorId = 65535)
    INSERT INTO dbo.x360ce_Vendors (VendorId, VendorName, ShortName, WebSite)
    VALUES (65535, 'Test Vendor C', 'TVC', '');

PRINT '01_vendors.sql: 3 test vendors ensured (IDs 65533/65534/65535).';
```

- [ ] **Step 2: Write 02_products.sql**

`x360ce_Products` has `ProductGuid UNIQUEIDENTIFIER PK, ProductName NVARCHAR(256), InstanceCount INT, VendorId AS (computed from chars 5-8 of ProductGuid), ProductId AS (computed from chars 1-4)`.

We craft GUIDs whose first segment encodes ProductId (chars 1-4) + VendorId (chars 5-8) so the computed columns are predictable:

Create `scripts/db/seed/02_products.sql`:
```sql
SET NOCOUNT ON;

-- GUID '0001FFFD-0000-0000-0000-000000000001' -> ProductId=0x0001, VendorId=0xFFFD (65533)
IF NOT EXISTS (SELECT 1 FROM dbo.x360ce_Products
               WHERE ProductGuid = '0001FFFD-0000-0000-0000-000000000001')
    INSERT INTO dbo.x360ce_Products (ProductGuid, ProductName, InstanceCount)
    VALUES ('0001FFFD-0000-0000-0000-000000000001', 'Test Product A', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.x360ce_Products
               WHERE ProductGuid = '0001FFFE-0000-0000-0000-000000000002')
    INSERT INTO dbo.x360ce_Products (ProductGuid, ProductName, InstanceCount)
    VALUES ('0001FFFE-0000-0000-0000-000000000002', 'Test Product B', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.x360ce_Products
               WHERE ProductGuid = '0001FFFF-0000-0000-0000-000000000003')
    INSERT INTO dbo.x360ce_Products (ProductGuid, ProductName, InstanceCount)
    VALUES ('0001FFFF-0000-0000-0000-000000000003', 'Test Product C', 0);

PRINT '02_products.sql: 3 test products ensured.';
```

- [ ] **Step 3: Write 03_user_computer.sql**

`x360ce_UserComputers` has `Id, ApplicationId, UserId, ComputerId, ComputerName, DateCreated, DateUpdated, Checksum` — all GUIDs, ComputerName NVARCHAR.

Create `scripts/db/seed/03_user_computer.sql`:
```sql
SET NOCOUNT ON;

DECLARE @TestUserId      UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @TestComputerId  UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000002';
DECLARE @TestAppId       UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000003';

IF NOT EXISTS (
    SELECT 1 FROM dbo.x360ce_UserComputers
    WHERE UserId = @TestUserId AND ComputerId = @TestComputerId
)
    INSERT INTO dbo.x360ce_UserComputers
        (Id, ApplicationId, UserId, ComputerId, ComputerName, Checksum)
    VALUES
        (NEWID(), @TestAppId, @TestUserId, @TestComputerId,
         'TestComputer', '00000000-0000-0000-0000-000000000000');

PRINT '03_user_computer.sql: 1 test user-computer row ensured.';
```

- [ ] **Step 4: Write 04_programs.sql**

`x360ce_Programs` has many columns with sensible defaults — we only set the non-default ones.

Create `scripts/db/seed/04_programs.sql`:
```sql
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.x360ce_Programs WHERE FileName = 'TestGameA.exe')
    INSERT INTO dbo.x360ce_Programs (ProgramId, FileName, FileProductName, FileVersion, IsEnabled)
    VALUES (NEWID(), 'TestGameA.exe', 'Test Game A', '1.0.0.0', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.x360ce_Programs WHERE FileName = 'TestGameB.exe')
    INSERT INTO dbo.x360ce_Programs (ProgramId, FileName, FileProductName, FileVersion, IsEnabled)
    VALUES (NEWID(), 'TestGameB.exe', 'Test Game B', '1.0.0.0', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.x360ce_Programs WHERE FileName = 'TestGameC.exe')
    INSERT INTO dbo.x360ce_Programs (ProgramId, FileName, FileProductName, FileVersion, IsEnabled)
    VALUES (NEWID(), 'TestGameC.exe', 'Test Game C', '1.0.0.0', 1);

PRINT '04_programs.sql: 3 test programs ensured.';
```

---

## Task 9: Write Seed-TestDb.ps1

**Files:**
- Create: `scripts/db/Seed-TestDb.ps1`

- [ ] **Step 1: Write the script**

Create `scripts/db/Seed-TestDb.ps1`:
```powershell
<#
.SYNOPSIS
  Runs every scripts/db/seed/*.sql against x360ce_Tests in lexical order.
.PARAMETER Server
  Default 'localhost'.
.PARAMETER Database
  Default 'x360ce_Tests'. Refused by guardrail unless allow-listed.
#>

[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [string]$Database = 'x360ce_Tests'
)
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\Verify-TestDbName.ps1"
Assert-TestDbAllowed -Database $Database

$seedDir = Join-Path $PSScriptRoot 'seed'
$files = Get-ChildItem -Path $seedDir -Filter '*.sql' | Sort-Object Name
if (-not $files) { throw "No seed files found in $seedDir" }

foreach ($f in $files) {
    Write-Host "Seeding: $($f.Name)" -ForegroundColor Cyan
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -InputFile $f.FullName
}

Write-Host "Seed complete for $Database." -ForegroundColor Green
```

- [ ] **Step 2: Run the seed standalone (Deploy must have already run)**

Run:
```powershell
.\scripts\db\Seed-TestDb.ps1
```
Expected: each filename printed, plus `Seed complete for x360ce_Tests.`.

- [ ] **Step 3: Verify seed rows**

Run:
```powershell
Invoke-Sqlcmd -ServerInstance 'localhost' -Database 'x360ce_Tests' `
              -Query "SELECT (SELECT COUNT(*) FROM dbo.x360ce_Vendors  WHERE VendorId BETWEEN 65533 AND 65535) AS VendorCount,
                              (SELECT COUNT(*) FROM dbo.x360ce_Products WHERE ProductName LIKE 'Test Product%') AS ProductCount,
                              (SELECT COUNT(*) FROM dbo.x360ce_Programs WHERE FileName LIKE 'TestGame%')         AS ProgramCount,
                              (SELECT COUNT(*) FROM dbo.x360ce_UserComputers WHERE ComputerName = 'TestComputer') AS UserComputerCount"
```
Expected: `3, 3, 3, 1`.

- [ ] **Step 4: Verify idempotency — run seed twice, counts unchanged**

Run:
```powershell
.\scripts\db\Seed-TestDb.ps1
# repeat the SELECT above; expect identical counts
```
Expected: same `3, 3, 3, 1`.

---

## Task 10: Wire Seed call into Deploy-TestDb.ps1

**Files:**
- Modify: `scripts/db/Deploy-TestDb.ps1`

- [ ] **Step 1: Uncomment the Seed call**

Open `scripts/db/Deploy-TestDb.ps1`. Replace the commented block:
```powershell
# Task 10 will uncomment the Seed call.
# if (-not $SkipSeed) {
#     & "$PSScriptRoot\Seed-TestDb.ps1" -Server $Server -Database $Database
# }
```
with:
```powershell
if (-not $SkipSeed) {
    & "$PSScriptRoot\Seed-TestDb.ps1" -Server $Server -Database $Database
}
```

- [ ] **Step 2: Run Deploy and confirm seed runs**

Run:
```powershell
.\scripts\db\Deploy-TestDb.ps1
```
Expected: SqlPackage publish output, followed by `Seeding: 01_vendors.sql` etc., then `Seed complete for x360ce_Tests.`.

- [ ] **Step 3: Commit (when user authorises)**

`git add scripts/db/seed scripts/db/Seed-TestDb.ps1 scripts/db/Deploy-TestDb.ps1`
`git commit -m "feat(scripts/db): synthetic seed (vendors/products/programs/user-computer) wired into Deploy"`

---

## Task 11: Write Drop-TestDb.ps1

**Files:**
- Create: `scripts/db/Drop-TestDb.ps1`

- [ ] **Step 1: Write the script**

Create `scripts/db/Drop-TestDb.ps1`:
```powershell
<#
.SYNOPSIS
  Drops the test database (x360ce_Tests by default).
.PARAMETER Server
  Default 'localhost'.
.PARAMETER Database
  Default 'x360ce_Tests'. Refused by guardrail unless allow-listed.
.PARAMETER Force
  Skip the confirmation prompt.
#>

[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [string]$Database = 'x360ce_Tests',
    [switch]$Force
)
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\Verify-TestDbName.ps1"
Assert-TestDbAllowed -Database $Database

if (-not $Force) {
    $reply = Read-Host "Drop database '$Database' on '$Server'? (yes/NO)"
    if ($reply -ne 'yes') {
        Write-Host "Aborted." -ForegroundColor Yellow
        return
    }
}

$exists = Invoke-Sqlcmd -ServerInstance $Server `
                       -Query "SELECT name FROM sys.databases WHERE name = '$Database'"
if (-not $exists) {
    Write-Host "$Database does not exist on $Server. Nothing to drop." -ForegroundColor Yellow
    return
}

$sql = @"
ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [$Database];
"@

Invoke-Sqlcmd -ServerInstance $Server -Query $sql
Write-Host "Dropped $Database." -ForegroundColor Green
```

- [ ] **Step 2: Test the prompt path**

Run:
```powershell
.\scripts\db\Drop-TestDb.ps1
# When prompted, type 'no' and press Enter.
```
Expected: prints `Aborted.` and `x360ce_Tests` still exists.

- [ ] **Step 3: Test the Force path**

Run:
```powershell
.\scripts\db\Drop-TestDb.ps1 -Force
```
Expected: prints `Dropped x360ce_Tests.`. Verify gone:
```powershell
Invoke-Sqlcmd -ServerInstance 'localhost' `
              -Query "SELECT name FROM sys.databases WHERE name = 'x360ce_Tests'"
```
Expected: empty result.

- [ ] **Step 4: Test the guardrail**

Run:
```powershell
try {
    .\scripts\db\Drop-TestDb.ps1 -Database 'x360ce' -Force
    Write-Error "GUARDRAIL FAILED — drop ran against x360ce!"
} catch {
    Write-Host "OK guardrail refused: $($_.Exception.Message)" -ForegroundColor Green
}
```
Expected: prints `OK guardrail refused: REFUSED. ...`.

- [ ] **Step 5: Commit (when user authorises)**

`git add scripts/db/Drop-TestDb.ps1`
`git commit -m "feat(scripts/db): drop x360ce_Tests with confirmation + guardrail"`

---

## Task 12: Write Refresh-TestDb.ps1

**Files:**
- Create: `scripts/db/Refresh-TestDb.ps1`

- [ ] **Step 1: Write the script**

Create `scripts/db/Refresh-TestDb.ps1`:
```powershell
<#
.SYNOPSIS
  Drops then re-deploys + re-seeds the test database.
#>

[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [string]$Database = 'x360ce_Tests',
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\Verify-TestDbName.ps1"
Assert-TestDbAllowed -Database $Database

& "$PSScriptRoot\Drop-TestDb.ps1"   -Server $Server -Database $Database -Force
& "$PSScriptRoot\Deploy-TestDb.ps1" -Server $Server -Database $Database -Configuration $Configuration

Write-Host "Refreshed $Database." -ForegroundColor Green
```

- [ ] **Step 2: Test refresh cycle**

Run:
```powershell
.\scripts\db\Refresh-TestDb.ps1
```
Expected: drop output (or "does not exist" if currently dropped), then publish output, then seed output, then `Refreshed x360ce_Tests.`.

- [ ] **Step 3: Commit (when user authorises)**

`git add scripts/db/Refresh-TestDb.ps1`
`git commit -m "feat(scripts/db): refresh = drop + deploy + seed"`

---

## Task 13: Write Compare-TestDbToLive.ps1 (read-only on live)

**Files:**
- Create: `scripts/db/Compare-TestDbToLive.ps1`

- [ ] **Step 1: Write the script**

Create `scripts/db/Compare-TestDbToLive.ps1`:
```powershell
<#
.SYNOPSIS
  Read-only schema diff: would the test DACPAC need any operations to bring the live
  x360ce database to its current schema state?
.DESCRIPTION
  Runs SqlPackage /Action:DeployReport (NOT /Action:Publish) so nothing is written.
  Writes the XML report to scripts/db/reports/drift-<timestamp>.xml and prints a
  one-line summary.
#>

[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [string]$LiveDatabase = 'x360ce',
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\Find-SqlPackage.ps1"

$dacpac     = & "$PSScriptRoot\Build-TestDbDacpac.ps1" -Configuration $Configuration
$sqlPackage = Find-SqlPackage

$repoRoot   = (Resolve-Path "$PSScriptRoot\..\..").Path
$reportsDir = Join-Path $repoRoot 'scripts\db\reports'
if (-not (Test-Path $reportsDir)) { New-Item -ItemType Directory -Path $reportsDir | Out-Null }

$stamp  = Get-Date -Format 'yyyyMMdd-HHmmss'
$report = Join-Path $reportsDir "drift-$stamp.xml"

Write-Host "Running read-only DeployReport against $Server\$LiveDatabase ..." -ForegroundColor Cyan
& $sqlPackage `
    /Action:DeployReport `
    /SourceFile:$dacpac `
    /TargetServerName:$Server `
    /TargetDatabaseName:$LiveDatabase `
    /TargetTrustServerCertificate:True `
    /OutputPath:$report `
    /Quiet:True

if ($LASTEXITCODE -ne 0) {
    throw "DeployReport failed (SqlPackage exit $LASTEXITCODE). Note: this script does NOT modify $LiveDatabase."
}

[xml]$xml = Get-Content $report
$ops = $xml.SelectNodes('//*[local-name()="Operation"]')
$total  = $ops.Count
$creates = ($ops | Where-Object { $_.Name -eq 'Create' }).Count
$alters  = ($ops | Where-Object { $_.Name -eq 'Alter'  }).Count
$drops   = ($ops | Where-Object { $_.Name -eq 'Drop'   }).Count

Write-Host "Drift report: $report" -ForegroundColor Green
Write-Host "Operations: $total (Create: $creates, Alter: $alters, Drop: $drops)" -ForegroundColor $( if ($total -eq 0) { 'Green' } else { 'Yellow' } )
if ($total -gt 0) {
    Write-Host "Drift detected. Open the XML to inspect. Update Data/x360ce.Data.sqlproj to match, then re-run." -ForegroundColor Yellow
}
```

- [ ] **Step 2: Run against live**

Run:
```powershell
.\scripts\db\Compare-TestDbToLive.ps1
```
Expected: prints `Drift report: <path>` and `Operations: N (Create: ..., Alter: ..., Drop: ...)`. The live `x360ce` database is unaffected.

- [ ] **Step 3: Verify live is unchanged**

Run:
```powershell
Invoke-Sqlcmd -ServerInstance 'localhost' -Database 'x360ce' `
              -Query "SELECT COUNT(*) AS TableCount FROM sys.tables"
```
Expected: same count as before running Compare (sanity — Compare cannot have changed live).

- [ ] **Step 4: Commit (when user authorises)**

`git add scripts/db/Compare-TestDbToLive.ps1`
`git commit -m "feat(scripts/db): read-only schema drift report vs live x360ce"`

---

## Task 14: Final README and end-to-end smoke

**Files:**
- Modify: `scripts/db/README.md` (replace skeleton with final)

- [ ] **Step 1: Update README with the full usage matrix**

Replace `scripts/db/README.md` with:
```markdown
# scripts/db — Test Database Scaffolding

Manages the local **x360ce_Tests** SQL Server database used by `Web.Tests` (sub-project C). The live `x360ce` database is **read-only** to every script here — hard guardrail.

See `docs/plans/B-db/design.md` for the full design.

## Prerequisites
- **SQL Server Developer Edition** (or higher) at `localhost`. Express won't work (no CLR — currently not used but reserved).
- **SqlPackage.exe** — `winget install Microsoft.SqlPackage`.
- **Visual Studio 2022 or 2026** with SSDT (already required by the repo).
- Windows authentication, with **`dbcreator`** server role on the local instance.

## Script reference

| Script | Purpose | Modifies live `x360ce`? |
|---|---|---|
| `Build-TestDbDacpac.ps1` | MSBuilds the sqlproj, returns DACPAC path | No |
| `Deploy-TestDb.ps1` | Build + publish DACPAC to `x360ce_Tests` + seed | No (guardrail) |
| `Drop-TestDb.ps1` | DROP DATABASE `x360ce_Tests` (confirm or `-Force`) | No (guardrail) |
| `Refresh-TestDb.ps1` | Drop + Deploy | No (guardrail) |
| `Seed-TestDb.ps1` | Run all `seed/*.sql` against `x360ce_Tests` | No (guardrail) |
| `Compare-TestDbToLive.ps1` | Read-only diff of DACPAC vs `x360ce` (DeployReport) | **No — read-only** |
| `Verify-TestDbName.ps1` | Internal helper: `Assert-TestDbAllowed` allow-list | No |
| `Find-SqlPackage.ps1` | Locates SqlPackage.exe | No |
| `Find-MSBuild.ps1` | Locates MSBuild via vswhere | No |

## Usage

```powershell
# Day 1 — first deploy:
.\scripts\db\Deploy-TestDb.ps1                  # builds, publishes, seeds

# Iteration:
.\scripts\db\Refresh-TestDb.ps1                 # drop + redeploy + reseed

# Inspect schema drift (read-only on live):
.\scripts\db\Compare-TestDbToLive.ps1

# Teardown:
.\scripts\db\Drop-TestDb.ps1                    # prompts; add -Force to skip
```

## Connection string used by sub-project C tests
```
data source=localhost;initial catalog=x360ce_Tests;persist security info=True;Integrated Security=True;multipleactiveresultsets=True
```

## Seed strategy

**Interim** — synthetic-only fixtures live under `scripts/db/seed/*.sql`. When `Data/x360ce.Data.sqlproj` gets post-deploy content (shared with the installer), these files are deprecated; `Seed-TestDb.ps1` becomes a no-op.

## Failure recovery

| Error | Recovery |
|---|---|
| `SqlPackage.exe not found` | `winget install Microsoft.SqlPackage` |
| `MSBuild.exe not found` | Install VS with the MSBuild component; reopen shell |
| `Cannot open database 'x360ce_Tests'` (from C tests) | `.\scripts\db\Deploy-TestDb.ps1` |
| `Compare-TestDbToLive.ps1` shows drift | Open the XML report; update `Data/x360ce.Data.sqlproj` to match; rebuild; redeploy |
| `REFUSED. Database name 'x360ce'` | Working as designed — never run write scripts on live |
```

- [ ] **Step 2: End-to-end smoke (the B sub-project acceptance test)**

Run the full cycle from a clean state and time it:
```powershell
# Confirm no test DB present
.\scripts\db\Drop-TestDb.ps1 -Force   # safe to call when missing

# Full deploy
Measure-Command { .\scripts\db\Deploy-TestDb.ps1 } | Select TotalSeconds

# Verify seed rows
Invoke-Sqlcmd -ServerInstance 'localhost' -Database 'x360ce_Tests' `
  -Query "SELECT (SELECT COUNT(*) FROM dbo.x360ce_Vendors  WHERE VendorId BETWEEN 65533 AND 65535) AS V,
                  (SELECT COUNT(*) FROM dbo.x360ce_Products WHERE ProductName LIKE 'Test Product%') AS P,
                  (SELECT COUNT(*) FROM dbo.x360ce_Programs WHERE FileName LIKE 'TestGame%') AS G,
                  (SELECT COUNT(*) FROM dbo.x360ce_UserComputers WHERE ComputerName = 'TestComputer') AS U"

# Refresh (drop + redeploy + reseed)
Measure-Command { .\scripts\db\Refresh-TestDb.ps1 } | Select TotalSeconds

# Read-only drift report vs live
.\scripts\db\Compare-TestDbToLive.ps1

# Teardown
.\scripts\db\Drop-TestDb.ps1 -Force
```

Expected:
- Deploy completes in <60 s.
- Seed counts: `V=3, P=3, G=3, U=1`.
- Refresh completes in <90 s.
- Compare prints a drift summary; live `x360ce` table count unchanged.
- Drop removes `x360ce_Tests`.

- [ ] **Step 3: Pester tests still pass**

Run:
```powershell
Invoke-Pester .\scripts\db\tests\Verify-TestDbName.Tests.ps1 -Output Detailed
```
Expected: 8/8 pass.

- [ ] **Step 4: Final commit (when user authorises)**

`git add scripts/db/README.md`
`git commit -m "docs(scripts/db): final usage README + smoke verification"`

---

## Spec coverage self-review

| `docs/plans/B-db/design.md` section | Task(s) |
|---|---|
| §3 tooling (DACPAC, SqlPackage, MSBuild) | Tasks 4, 5, 6 |
| §4 seven scripts | Tasks 2, 4, 5, 6, 7, 9, 11, 12, 13 |
| §4.1 guardrail `Assert-TestDbAllowed` | Tasks 2, 3 |
| §4.2 connection string | README in Task 14 |
| §5 synthetic seed (interim) | Tasks 8, 9 |
| §6 script behaviours | Tasks 6, 7, 9, 11, 12, 13 |
| §7 CLR caveat | Confirmed sqlproj has no CLR (probe 2026-05-16) — not a Task line item, just confirmed during planning |
| §8 prereqs | Task 0 + README in Task 14 |
| §10 failure modes | README failure-recovery table (Task 14) |

Placeholder scan: none. Type consistency: all script signatures use `[CmdletBinding()]` with named parameters; `Assert-TestDbAllowed` takes `-Database` consistently across all callers.

---

## When this plan is complete

Sub-project B is done when:
- All 14 tasks are checked.
- `Invoke-Pester` on the guardrail test passes 8/8.
- A clean cycle (Drop → Deploy → seed verify → Refresh → Compare → Drop) runs without manual intervention.
- The user has reviewed and approved the staged commits, and they have been committed (per the per-task gate).

After B: invoke `writing-plans` for sub-project C (test harness, starting with C-M1 Engine.Tests).
