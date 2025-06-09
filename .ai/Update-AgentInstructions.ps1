# Script: Update-AgentInstructions.ps1
# Location: .ai/Update-AgentInstructions.ps1
# Description: Updates AI agent instruction files from master copies in the .ai folder,
#              processing only files matching '*instructions.md'.

# Strict mode
Set-StrictMode -Version Latest
# Error handling: Stop on first error
$ErrorActionPreference = "Stop"

# Function to compare content and write file if different
function Test-AndWriteFile {
    param(
        [Parameter(Mandatory=$true)]
        [string]$TargetPath,
        [Parameter(Mandatory=$true)]
        [string]$NewContent,
        [string]$FileDescription = "File" # Optional description for messages
    )

    try {
        $TargetDir = Split-Path -Path $TargetPath -Parent
        if (-not (Test-Path -Path $TargetDir)) {
            Write-Host "Creating directory: $TargetDir"
            New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
        }

        # Ensure the new content ends with a newline, typical for markdown files
        $ContentToWrite = $NewContent.TrimEnd() + "`n"

        if (Test-Path -Path $TargetPath) {
            $existingContent = Get-Content -Path $TargetPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
            
            # Direct comparison as requested by user
            if ($existingContent -eq $ContentToWrite) {
                Write-Host "$($FileDescription): Content is identical (direct comparison). No update needed for '$TargetPath'."
                return $false # Indicate no update was made
            } else {
                Write-Host "$($FileDescription): Updating '$TargetPath' (direct comparison showed difference)."
                Set-Content -Path $TargetPath -Value $ContentToWrite -Encoding UTF8 -Force
                return $true # Indicate update was made
            }
        } else {
            Write-Host "$($FileDescription): Creating '$TargetPath'."
            Set-Content -Path $TargetPath -Value $ContentToWrite -Encoding UTF8 -Force
            return $true # Indicate update was made
        }
    } catch {
        Write-Error "Error processing file '$TargetPath': $($_.Exception.Message)"
        throw # Re-throw to be caught by main try-catch
    }
}

# --- Main Script ---
try {
    $scriptDir = $PSScriptRoot # Directory where the script itself is located (.ai)
    $repoRoot = Join-Path -Path $scriptDir -ChildPath ".." | Resolve-Path # Absolute path to the repository root

    # Discover source files matching *instructions.md in the .ai folder
    $sourceInstructionFiles = Get-ChildItem -Path $scriptDir -Filter "*instructions.md" -File
    if ($null -eq $sourceInstructionFiles -or $sourceInstructionFiles.Count -eq 0) {
        Write-Warning "No '*instructions.md' files found in '$scriptDir'. Nothing to process."
        exit 0
    }
    Write-Host "Found the following source instruction files in '$scriptDir':"
    $sourceInstructionFiles | ForEach-Object { Write-Host "- $($_.Name)" }

    # User prompt for agent selection
    $choiceCline = New-Object System.Management.Automation.Host.ChoiceDescription "&CLINE", "Update instructions for CLINE."
    $choiceCopilot = New-Object System.Management.Automation.Host.ChoiceDescription "&GitHub CoPilot", "Update instructions for GitHub CoPilot."
    $choiceAll = New-Object System.Management.Automation.Host.ChoiceDescription "&ALL", "Update instructions for both CLINE and GitHub CoPilot."
    $choiceCancel = New-Object System.Management.Automation.Host.ChoiceDescription "&Cancel", "Exit the script."
    
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]@($choiceCline, $choiceCopilot, $choiceAll, $choiceCancel)
    $title = "Select Agent Instruction Set to Update"
    $message = "Which agent's instructions do you want to update from the '*instructions.md' files in '$scriptDir'?"
    
    $userChoiceResult = $Host.UI.PromptForChoice($title, $message, $choices, 0) # Default to CLINE (index 0)

    $updateCline = $false
    $updateCopilot = $false

    switch ($userChoiceResult) {
        0 { $updateCline = $true; Write-Host "Selected: CLINE" }
        1 { $updateCopilot = $true; Write-Host "Selected: GitHub CoPilot" }
        2 { $updateCline = $true; $updateCopilot = $true; Write-Host "Selected: ALL" }
        3 { Write-Host "Operation cancelled by user."; exit 0 }
        default { throw "Invalid selection. Exiting." }
    }

    # --- CLINE Update Logic ---
    if ($updateCline) {
        Write-Host "`n--- Updating CLINE Instructions ---"
        $clineRulesDir = Join-Path $repoRoot ".clinerules"
        
        foreach ($sourceFile in $sourceInstructionFiles) {
            $clineTargetFile = Join-Path $clineRulesDir $sourceFile.Name
            $sourceContent = Get-Content $sourceFile.FullName -Raw -Encoding UTF8
            Test-AndWriteFile -TargetPath $clineTargetFile -NewContent $sourceContent -FileDescription "CLINE instruction file ($($sourceFile.Name))"
        }
        Write-Host "CLINE instruction update process complete."
    }

    # --- GitHub CoPilot Update Logic ---
    if ($updateCopilot) {
        Write-Host "`n--- Updating GitHub CoPilot Instructions ---"
        $githubDir = Join-Path $repoRoot ".github"
        $copilotTargetInstructionsFile = Join-Path $githubDir "copilot-instructions.md"
        
        $allInstructionsContent = New-Object System.Text.StringBuilder
        $firstFile = $true

        foreach ($sourceFile in $sourceInstructionFiles) {
            if (-not $firstFile) {
                $allInstructionsContent.AppendLine("") # Add a blank line separator before the next section
            }
            
            $allInstructionsContent.AppendLine("==== START OF INSTRUCTIONS FROM: $($sourceFile.Name) ====")
            $allInstructionsContent.AppendLine("") # Blank line after START marker
            
            $allInstructionsContent.AppendLine("# Instructions from: $($sourceFile.Name)")
            $allInstructionsContent.AppendLine("") # Blank line after header
            
            $sourceContent = Get-Content $sourceFile.FullName -Raw -Encoding UTF8
            $allInstructionsContent.AppendLine($sourceContent.Trim())
            
            $allInstructionsContent.AppendLine("") # Blank line before END marker
            $allInstructionsContent.AppendLine("==== END OF INSTRUCTIONS FROM: $($sourceFile.Name) ====")
            
            $firstFile = $false # Set to false after processing the first file
        }

        # No need to remove leading newline with this new structure as each block is self-contained.
        # The first block will start directly with "==== START..."
        $finalCopilotContent = $allInstructionsContent.ToString()
        Test-AndWriteFile -TargetPath $copilotTargetInstructionsFile -NewContent $finalCopilotContent -FileDescription "GitHub CoPilot main instructions"
        Write-Host "GitHub CoPilot instruction update process complete."
    }

    Write-Host "`nAll selected operations completed successfully."

} catch {
    Write-Error "An unexpected error occurred: $($_.Exception.Message)"
    Write-Error "Script Stack Trace: $($_.ScriptStackTrace)"
    # For more detailed error info, you might want to access $_.Exception.ToString()
    exit 1
}
pause