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

function Get-StreamChecksum {
    <#
    .SYNOPSIS
        SHA256 of whatever a stream yields.
    .DESCRIPTION
        Written against a stream rather than a path so the same hashing serves a file on disk
        and an entry being expanded out of an archive. Comparing those two is what proves an
        archive holds what it was asked to hold.
    #>
    param (
        [System.IO.Stream] $stream
    )
    $hashAlgorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $hashAlgorithm.ComputeHash($stream)
        return -join ($hashBytes | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        $hashAlgorithm.Dispose()
    }
}

function Wait-FileUnlocked {
    <#
    .SYNOPSIS
        Waits until nothing else holds the file open.
    .DESCRIPTION
        Explorer keeps the archive open while it is still writing, and an entry can be listed
        before its data has been flushed. Asking for the file exclusively is a direct answer to
        "has it finished", where counting entries is only an inference.
    #>
    param (
        [string] $path,
        [int] $timeoutSeconds = 600
    )
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $stream = [System.IO.File]::Open($path, "Open", "ReadWrite", "None")
            $stream.Dispose()
            return $true
        }
        catch [System.IO.IOException] {
            Start-Sleep -Milliseconds 200
        }
    }
    return $false
}

function Get-FileChecksum {
    param (
        [string] $filePath
    )
    if (-not (Test-Path -Path $filePath -PathType Leaf)) {
        Write-Host "File does not exist: $filePath"
        return $null
    }
    $stream = [System.IO.File]::OpenRead($filePath)
    try {
        return Get-StreamChecksum -stream $stream
    }
    finally {
        $stream.Dispose()
    }
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
    <#
    .SYNOPSIS
        Builds a zip with the compressor built into Windows Explorer.
    .DESCRIPTION
        Explorer and .NET compress the same bytes differently, and a scanner judges the
        compressed stream rather than the files inside it. Releases packed this way have not
        been flagged; releases packed by .NET have. Which stream a scanner will object to
        cannot be known in advance, so the one with the better record is the one used.

        Copying into a zip folder is asynchronous: the call returns immediately and Explorer
        keeps working in the background. The archive is therefore not finished when the call
        returns, and a caller that does not wait can publish a truncated file.
    #>
    param (
        [string] $sourceDir,
        [string] $destFile,
        [string] $searchPattern,
        [string] $excludePattern,
        $ignoreEmptyFolders = $false
    )

    $destDir = [System.IO.Path]::GetDirectoryName($destFile)
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir | Out-Null
    }
    if (Test-Path $destFile) {
        Remove-Item -LiteralPath $destFile -Force
    }

    # An empty archive is exactly the end-of-central-directory record: "PK", 5, 6, then 18
    # zero bytes. Written as bytes because a text write would add an encoding preamble or a
    # trailing newline, and Explorer would refuse the result.
    $empty = [byte[]](0x50, 0x4B, 0x05, 0x06) + (New-Object byte[] 18)
    [System.IO.File]::WriteAllBytes($destFile, $empty)

    $shellApplication = New-Object -ComObject Shell.Application
    $zipPackage = $shellApplication.NameSpace($destFile)
    if (-not $zipPackage) {
        throw "$($logPrefix)Explorer would not open $destFile as an archive."
    }

    $files = Get-ChildItem -Path $sourceDir -Recurse -File
    if (![string]::IsNullOrEmpty($searchPattern)) {
        $files = $files | Where-Object { $_.Name -like $searchPattern }
    }
    if (![string]::IsNullOrEmpty($excludePattern)) {
        $files = $files | Where-Object { $_.Name -notlike $excludePattern }
    }
    $files = @($files)

    $items = @($files)
    if (-not $ignoreEmptyFolders) {
        # Explorer cannot put an empty folder into an archive. Handed one it stops, says so
        # in a message box, and adds nothing at all - not even the files that were fine. On a
        # build nobody is watching, that is a wait for a button press that never comes, so the
        # folders are named here instead and the caller is told plainly.
        $empty = @(Get-ChildItem -Path $sourceDir -Recurse -Directory |
            Where-Object { (Get-ChildItem -Path $_.FullName -File -Recurse).Count -eq 0 })
        if ($empty.Count -gt 0) {
            throw ("$($logPrefix)Explorer cannot store empty folders, and these are empty: " +
                (($empty | ForEach-Object { $_.FullName }) -join ", ") +
                ". Pass -IgnoreEmptyFolders `$true to leave them out.")
        }
    }
    if ($items.Count -eq 0) {
        Write-Host "$($logPrefix)Nothing to add to $destFile."
        return
    }

    # Worked out before anything is copied. Explorer stores a file added on its own under its
    # own name with no folder in front of it, so two files with the same name from different
    # folders land on each other. Offered that, Explorer stops and asks which one to keep -
    # a question nobody is there to answer on a build machine. The checksums are needed for
    # the check at the end anyway, so they are taken here.
    $expected = @{}
    foreach ($file in $files) {
        if ($expected.ContainsKey($file.Name)) {
            throw ("$($logPrefix)Two files are named $($file.Name), and an archive built this " +
                "way keeps only one of them.")
        }
        $expected[$file.Name] = Get-FileChecksum -filePath $file.FullName
    }

    # Copied one at a time, each waited for before the next is offered. CopyHere returns
    # immediately and Explorer keeps working in the background, so a second call made while
    # the first is still running is refused - it reports a missing file or a permission it
    # does not have, stops, and leaves a part-built archive behind.
    $added = 0
    foreach ($item in $items) {
        $zipPackage.CopyHere($item.FullName)
        $added++
        $lastSize = -1
        $stalledFor = 0
        while ($true) {
            Start-Sleep -Milliseconds 500
            $count = @($shellApplication.NameSpace($destFile).Items()).Count
            if ($count -ge $added) { break }
            # Explorer reports no percentage, so growth of the archive is the only sign it is
            # still working. Waiting a fixed number of seconds would abandon a large file that
            # was progressing perfectly well.
            $size = (Get-Item -LiteralPath $destFile).Length
            if ($size -eq $lastSize) {
                $stalledFor++
                if ($stalledFor -ge 120) {
                    throw ("$($logPrefix)Explorer stopped while adding $($item.FullName): " +
                        "$count of $added entries are in $destFile.")
                }
            }
            else { $lastSize = $size; $stalledFor = 0 }
        }
    }

    # Nothing may still hold the archive open. An entry can be listed before its data has
    # been flushed, so reading it while Explorer is still writing would compare half a file.
    if (-not (Wait-FileUnlocked -path $destFile)) {
        throw "$($logPrefix)Explorer still has $destFile open."
    }

    Add-Type -Assembly "System.IO.Compression.FileSystem"
    $archive = [System.IO.Compression.ZipFile]::OpenRead($destFile)
    try {
        foreach ($name in $expected.Keys) {
            $entry = $archive.Entries | Where-Object {
                [System.IO.Path]::GetFileName($_.FullName) -eq $name
            } | Select-Object -First 1
            if (-not $entry) {
                throw "$($logPrefix)$destFile does not contain $name."
            }
            $stream = $entry.Open()
            try {
                $actual = Get-StreamChecksum -stream $stream
            }
            finally {
                $stream.Dispose()
            }
            if ($actual -ne $expected[$name]) {
                throw ("$($logPrefix)$name comes back out of $destFile with different " +
                    "contents than it went in with.")
            }
        }
        $written = $archive.Entries.Count
    }
    finally {
        $archive.Dispose()
    }
    Write-Host ("$($logPrefix)Packed $written file(s) into $destFile with Explorer, " +
        "contents verified.")
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
