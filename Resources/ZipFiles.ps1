param (
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $sourceDir,

    [Parameter(Mandatory = $true, Position = 1)]
    [string] $destFile,
    
    # Optional. The search string to match against the names of files.
    [Parameter(Mandatory = $false, Position = 2)]
    [string] $searchPattern,

    # Optional. The pattern to exclude files from the zip.
    [Parameter(Mandatory = $false, Position = 3)]
    [string] $excludePattern,

    # Optional. Use shell zipper if this parameter is set to true.
    [Parameter(Mandatory = $false, Position = 4)]
    [bool] $UseShellToZipFiles = $false,

    # Optional. Use comment for console.
    [Parameter(Mandatory = $false, Position = 5)]
    [string] $LogPrefix = "",

    # Optional. Ignore empty folders when creating the zip file.
    [Parameter(Mandatory = $false, Position = 6)]
    $IgnoreEmptyFolders = $false
)

if (!(Test-Path -Path $sourceDir)) {
    return
}

Add-Type -Assembly "System.IO.Compression.FileSystem"

function Get-FileChecksum {
    param (
        [string] $filePath
    )
    $checksum = $null
    if (Test-Path -Path $filePath -PathType Leaf) {
        $hashAlgorithm = [System.Security.Cryptography.SHA256]::Create()
        try {
            $stream = [System.IO.File]::OpenRead($filePath)
            $hashBytes = $hashAlgorithm.ComputeHash($stream)
            $stream.Close()
            $checksum = -join ($hashBytes | ForEach-Object { $_.ToString("x2") })
        }
        finally {
            $hashAlgorithm.Dispose()
            if ($stream) {
                $stream.Dispose()
            }
        }
    } else {
        Write-Host "File does not exist: $filePath"
    }
    return $checksum
}

function Get-FileChecksums {
    param (
        [string] $directory,
        [string] $searchPattern = "*",
        [string] $excludePattern = ""
    )
    $checksums = @{}
    $files = Get-ChildItem -Path $directory -Recurse -File -Filter $searchPattern
    
    # Apply exclude pattern if specified
    if (![string]::IsNullOrEmpty($excludePattern)) {
        $files = $files | Where-Object { $_.Name -notlike $excludePattern }
    }
    
    $files | ForEach-Object {
        $checksum = Get-FileChecksum -filePath $_.FullName
        if ($checksum) {
            [string]$key = $_.FullName.Replace($directory, "").TrimStart("\")
            $checksums[$key] = $checksum
        }
    }
    return $checksums
}

function CheckAndZipFiles {

    # Get file checksums...
    $sourceChecksums = Get-FileChecksums -directory $sourceDir -searchPattern $searchPattern -excludePattern $excludePattern

    $tempDir = $null
    $destChecksums = @{}
    if (Test-Path -Path $destFile) {
        $tempDir = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        [IO.Compression.ZipFile]::ExtractToDirectory($destFile, $tempDir)
        $destChecksums = Get-FileChecksums -directory $tempDir -searchPattern $searchPattern -excludePattern $excludePattern
    }

    $checksumsChanged = $false

    # 1. Compare files by checksums
    $allFileKeys = ($sourceChecksums.Keys + $destChecksums.Keys) | Sort-Object -Unique
    foreach ($key in $allFileKeys) {
        if (-not $sourceChecksums.ContainsKey($key)) {
            Write-Host "Zip-only file: $key"
            $checksumsChanged = $true
            break
        }
        if (-not $destChecksums.ContainsKey($key)) {
            Write-Host "New file on disk: $key"
            $checksumsChanged = $true
            break
        }
        if ($sourceChecksums[$key] -ne $destChecksums[$key]) {
            Write-Host "File changed: $key"
            $checksumsChanged = $true
            break
        }
    }

    # 2. Compare directories: any directory in zip but missing on disk (or vice versa) triggers a rewrite.
    # Skip this check if IgnoreEmptyFolders is true
    if ($tempDir -and -not $IgnoreEmptyFolders) {
        $sourceDirs = Get-ChildItem -Path $sourceDir -Recurse -Directory | 
                      ForEach-Object { $_.FullName.Replace($sourceDir, "").TrimStart("\") }
        $destDirs   = Get-ChildItem -Path $tempDir  -Recurse -Directory |
                      ForEach-Object { $_.FullName.Replace($tempDir, "").TrimStart("\") }

        $allDirKeys = ($sourceDirs + $destDirs) | Sort-Object -Unique

        foreach ($dirKey in $allDirKeys) {
            if (-not $sourceDirs.Contains($dirKey)) {
                Write-Host "Zip-only directory: $dirKey"
                $checksumsChanged = $true
                break
            }
            if (-not $destDirs.Contains($dirKey)) {
                Write-Host "New directory on disk: $dirKey"
                $checksumsChanged = $true
                break
            }
        }

        # Clean up extracted folder
        Remove-Item -Path $tempDir -Recurse -Force
    }

    # 3. Rezip if needed
    if ($checksumsChanged) {
        Write-Host "$($logPrefix)Source and destination checksums (or folders) do not match. Updating destination file..."
        if (Test-Path -Path $destFile) {
            Remove-Item -Path $destFile -Force
        }
        if ($UseShellToZipFiles) {
            Compress-ZipFileUsingShell -sourceDir $sourceDir -destFile $destFile -searchPattern $searchPattern -excludePattern $excludePattern -ignoreEmptyFolders $IgnoreEmptyFolders
        } else {
            Compress-ZipFileUsingCSharp -sourceDir $sourceDir -destFile $destFile -searchPattern $searchPattern -excludePattern $excludePattern -ignoreEmptyFolders $IgnoreEmptyFolders
        }
    } else {
        Write-Host "$($logPrefix)Source and destination checksums match. No update needed."
    }
}

function Compress-ZipFileUsingCSharp {
    param (
        [string] $sourceDir,
        [string] $destFile,
        [string] $searchPattern,
        [string] $excludePattern,
        $ignoreEmptyFolders = $false
    )
    # Create a temporary directory
    $tempSourceDir = New-Item -ItemType Directory -Path ([System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName()))

    # Resolve to the canonical filesystem path before computing relative paths below.
    # Get-ChildItem reports the on-disk casing (e.g. "D:\..."), but MSBuild may pass
    # "d:\..." depending on how the build was invoked. String.Replace is case-sensitive,
    # so an unresolved prefix would survive and produce an invalid path like
    # "C:\Temp\xyz\D:\Projects\...", which fails with NotSupportedException.
    $sourceRoot = (Resolve-Path -LiteralPath $sourceDir).Path.TrimEnd('\')

    $files = Get-ChildItem -Path $sourceDir -Recurse -File
    
    # Apply search pattern if specified
    if (![string]::IsNullOrEmpty($searchPattern)) {
        $files = $files | Where-Object { $_.Name -like $searchPattern }
    }
    
    # Apply exclude pattern if specified
    if (![string]::IsNullOrEmpty($excludePattern)) {
        $files = $files | Where-Object { $_.FullName -notmatch "\\Temp\\|\\Temp$" }
    }
    
    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($sourceRoot.Length).TrimStart("\")
        $targetPath = Join-Path -Path $tempSourceDir -ChildPath $relativePath
        
        # Ensure the directory structure exists
        $targetDir = [System.IO.Path]::GetDirectoryName($targetPath)
        if (!(Test-Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }
        
        # Copy the file
        Copy-Item -Path $file.FullName -Destination $targetPath -Force
    }
    
    # If not ignoring empty folders, copy the directory structure as well
    if (-not $ignoreEmptyFolders) {
        $directories = Get-ChildItem -Path $sourceDir -Recurse -Directory
        foreach ($dir in $directories) {
            $relativePath = $dir.FullName.Substring($sourceRoot.Length).TrimStart("\")
            $targetPath = Join-Path -Path $tempSourceDir -ChildPath $relativePath
            
            if (!(Test-Path $targetPath)) {
                New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
            }
        }
    }
    
    [IO.Compression.ZipFile]::CreateFromDirectory($tempSourceDir.FullName, $destFile)
    Remove-Item -Path $tempSourceDir -Recurse -Force
}

function Compress-ZipFileUsingShell {
    param (
        [string] $sourceDir,
        [string] $destFile,
        [string] $searchPattern,
        [string] $excludePattern,
        $ignoreEmptyFolders = $false
    )
    
    # Ensure the destination directory exists
    $destDir = [System.IO.Path]::GetDirectoryName($destFile)
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir | Out-Null
    }

    # Create an empty zip if it doesn't exist
    if (-not (Test-Path $destFile)) {
        $null = Set-Content -Path $destFile -Value ("PK" + [char]5 + [char]6 + ("$([char]0)" * 18))
    }

    # Use Shell Application to manipulate the zip file
    $shellApplication = New-Object -ComObject Shell.Application
    $zipPackage = $shellApplication.NameSpace($destFile)

    if (-not $zipPackage) {
        Write-Error "$($logPrefix)Failed to create a zip package COM object for the destination file. Check the path and permissions."
        return
    }

    # Get files first
    $files = Get-ChildItem -Path $sourceDir -Recurse -File
    
    # Apply search pattern if specified
    if (![string]::IsNullOrEmpty($searchPattern)) {
        $files = $files | Where-Object { $_.Name -like $searchPattern }
    }
    
    # Apply exclude pattern if specified
    if (![string]::IsNullOrEmpty($excludePattern)) {
        $files = $files | Where-Object { $_.Name -notlike $excludePattern }
    }

    # Process files
    foreach ($file in $files) {
        $path = $file.FullName
        $zipPackage.CopyHere($path)
        
        $maxRetries = 4
        $retryCount = 0
        Do {
            Start-Sleep -Seconds 2
            $retryCount++
            if ($retryCount -gt $maxRetries) {
                Write-Host "$($logPrefix)Max retries reached. Moving to next file..."
                break
            }
        } While (($shellApplication.NameSpace($destFile).Items() | Where-Object { $_.Path -eq $path }).Count -eq 0)
    }

    # Process directories if not ignoring empty folders
    if (-not $ignoreEmptyFolders) {
        $directories = Get-ChildItem -Path $sourceDir -Recurse -Directory
        
        # Filter directories to only include empty ones (since non-empty ones will be created when adding files)
        $emptyDirectories = $directories | Where-Object {
            (Get-ChildItem -Path $_.FullName -File -Recurse).Count -eq 0
        }
        
        foreach ($dir in $emptyDirectories) {
            $path = $dir.FullName
            $zipPackage.CopyHere($path)
            
            $maxRetries = 4
            $retryCount = 0
            Do {
                Start-Sleep -Seconds 2
                $retryCount++
                if ($retryCount -gt $maxRetries) {
                    Write-Host "$($logPrefix)Max retries reached. Moving to next directory..."
                    break
                }
            } While (($shellApplication.NameSpace($destFile).Items() | Where-Object { $_.Path -eq $path }).Count -eq 0)
        }
    }

    # Release COM objects
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($zipPackage) | Out-Null
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($shellApplication) | Out-Null
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}

$destName = [System.IO.Path]::GetFileName($destFile)
$logPrefix = "$($destName): $($LogPrefix)"

#==============================================================
# Ensure that only one instance of this script can run.
# Other instances wait for the previous one to complete.
#--------------------------------------------------------------
# Use the full script name with path as the lock name.
$scriptName = $MyInvocation.MyCommand.Name
$mutexName = "Global\$scriptName"
$mutexCreated = $false
$mutex = New-Object System.Threading.Mutex($true, $mutexName, [ref] $mutexCreated)
if (-not $mutexCreated) {
    # Set timeout (e.g., 5 minutes = 300,000 milliseconds)
    $timeout = 300000
    Write-Host "$($logPrefix)Another instance is running. Waiting..."
    $waitResult = $mutex.WaitOne($timeout)
}
try {
    # Main script logic goes here...
    CheckAndZipFiles
}
finally {
    # Release the mutex so that other instances can proceed.
    $mutex.ReleaseMutex()
    $mutex.Dispose()
}
#==============================================================
