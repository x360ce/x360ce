[CmdletBinding()]
param(
    [ValidateRange(1, 500)]
    [int]$Count = 50,

    [ValidateRange(2, 60)]
    [int]$ReadyTimeoutSeconds = 10,

    [switch]$AllowDesktopWindows,

    [string]$Executable = "..\x360ce.App.Beta\bin\Release\net48\x360ce.exe"
)

$ErrorActionPreference = "Stop"

if (-not $AllowDesktopWindows) {
    throw "This test visibly opens and closes x360ce. Run it only in a dedicated VM/CI desktop and pass -AllowDesktopWindows."
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$executablePath = [IO.Path]::GetFullPath((Join-Path $scriptRoot $Executable))
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Executable not found: $executablePath"
}

$results = [Collections.Generic.List[object]]::new()
$workingDirectory = Split-Path -Parent $executablePath

for ($launch = 1; $launch -le $Count; $launch++) {
    $process = $null
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $ready = $false
    $nonRespondingSamples = 0
    try {
        $process = Start-Process -FilePath $executablePath -WorkingDirectory $workingDirectory -PassThru
        while ($timer.Elapsed.TotalSeconds -lt $ReadyTimeoutSeconds -and -not $process.HasExited) {
            Start-Sleep -Milliseconds 100
            $process.Refresh()
            if (-not $process.Responding) {
                $nonRespondingSamples++
            }
            # The bootstrap title is "x360ce". The real mapping window uses the
            # full product title, so a longer title is the readiness signal.
            if ($process.Responding -and $process.MainWindowTitle.Length -gt 20) {
                $ready = $true
                break
            }
        }
    }
    finally {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id
            Wait-Process -Id $process.Id -ErrorAction SilentlyContinue
        }
    }

    $results.Add([pscustomobject]@{
        Launch = $launch
        Ready = $ready
        ElapsedMilliseconds = $timer.ElapsedMilliseconds
        NonRespondingSamples = $nonRespondingSamples
    })
}

$failed = @($results | Where-Object { -not $_.Ready -or $_.NonRespondingSamples -gt 0 })
$summary = [pscustomobject]@{
    Total = $results.Count
    Passed = $results.Count - $failed.Count
    Failed = $failed.Count
    AverageMilliseconds = [math]::Round(($results | Measure-Object ElapsedMilliseconds -Average).Average)
    MaximumMilliseconds = ($results | Measure-Object ElapsedMilliseconds -Maximum).Maximum
}

$summary
if ($failed.Count -gt 0) {
    $failed | Format-Table -AutoSize
    exit 1
}
