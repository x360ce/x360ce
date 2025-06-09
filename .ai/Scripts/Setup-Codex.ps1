<#
    File:     Setup-Codex.ps1
    Purpose:  Install OpenAI Codex CLI and configure it to use either OpenAI or Azure OpenAI.
    Tested on: Windows 10/11, PowerShell 5.1 & 7+
#>

#==============================================================================
# Customizable Settings
#==============================================================================

# Azure OpenAI settings
$azureEndpoint = "https://jocycom-aicomp-dev.openai.azure.com"
$deploymentName = "gpt-4.1"
$apiVersion = "2025-01-01-preview"
$vaultName = "kv-jocyscom-aicomp-dev"
$secretName = "azure-openai-api-key"

# OpenAI settings
$baseUrl = "https://api.openai.com/v1"
$modelNameOpenAI = "codex-mini-latest"
$openaiSecretName = "openai-api-key"

#==============================================================================
# Utility Functions
#==============================================================================

function Initialize-SecretManagementModules {
    param([string]$VaultName)
    Write-Host "`n> Ensuring SecretManagement vault '$VaultName'..." -ForegroundColor Cyan
    if (-not (Get-Module -ListAvailable -Name Microsoft.PowerShell.SecretManagement)) {
        Install-Module Microsoft.PowerShell.SecretManagement -Force -Scope CurrentUser
    }
    Import-Module Microsoft.PowerShell.SecretManagement
    if (-not (Get-Module -ListAvailable -Name Microsoft.PowerShell.SecretStore)) {
        Install-Module Microsoft.PowerShell.SecretStore -Force -Scope CurrentUser
    }
    Import-Module Microsoft.PowerShell.SecretStore
    $vaults = Get-SecretVault -ErrorAction SilentlyContinue
    if (-not ($vaults.Name -contains $VaultName)) {
        Register-SecretVault -Name $VaultName -ModuleName Microsoft.PowerShell.SecretStore -DefaultVault
    }
}

function Initialize-AzureKeyVaultModules {
    Write-Host "`n> Ensuring Az.KeyVault module is available..." -ForegroundColor Cyan
    if (-not (Get-Module -ListAvailable -Name Az.KeyVault)) {
        Install-Module Az.KeyVault -Force -Scope CurrentUser
    }
    $oldWarning = $WarningPreference; $WarningPreference = 'SilentlyContinue'
    Import-Module Az.KeyVault; $WarningPreference = $oldWarning
}

function Connect-ToAzure {
    if (-not (Get-AzContext)) {
        Write-Host "`n> Logging in to Azure..." -ForegroundColor Cyan
        Connect-AzAccount | Out-Null
    }
}

function ConvertFrom-SecureString {
    param([SecureString]$SecureString)
    $plainText = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    )
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    )
    return $plainText
}

function Read-ApiKey {
    param([string]$PromptText)
    $apiKeySecure = Read-Host "`nEnter $PromptText" -AsSecureString
    return ConvertFrom-SecureString -SecureString $apiKeySecure
}

#==============================================================================
# Credential Functions
#==============================================================================

function Get-CredentialChoice {
    param([string]$ProviderName, [string]$EnvVarName)
    Write-Host "`nHow would you like Codex to obtain $ProviderName credentials?" -ForegroundColor Cyan
    Write-Host "  1) Environment variables ($EnvVarName) [default]" -ForegroundColor Cyan
    Write-Host "  2) PowerShell SecretManagement" -ForegroundColor Cyan
    Write-Host "  3) Azure Key Vault" -ForegroundColor Cyan
    do {
        $choice = Read-Host "Select 1, 2, or 3 [default: 1]"
        if ([string]::IsNullOrWhiteSpace($choice)) { $choice = '1' }
    } until ($choice -in '1', '2', '3')
    return $choice
}

function Get-CredentialFromEnvironment {
    param([string]$EnvVarName)
    return [Environment]::GetEnvironmentVariable($EnvVarName)
}

function Set-CredentialToEnvironment {
    param([string]$PromptText)
    return Read-ApiKey -PromptText $PromptText
}

function Get-CredentialFromSecretManagement {
    param([string]$VaultName, [string]$SecretName)
    Initialize-SecretManagementModules -VaultName $VaultName
    return Get-Secret -Name $SecretName -Vault $VaultName -AsPlainText -ErrorAction SilentlyContinue
}

function Set-CredentialToSecretManagement {
    param([string]$VaultName, [string]$SecretName, [string]$PromptText)
    Initialize-SecretManagementModules -VaultName $VaultName
    $secretValue = Read-Host "`nEnter $PromptText" -AsSecureString
    Set-Secret -Name $SecretName -Vault $VaultName -SecureStringSecret $secretValue
}

function Get-CredentialFromAzureKeyVault {
    param([string]$VaultName, [string]$SecretName)
    Initialize-AzureKeyVaultModules
    Connect-ToAzure
    Write-Host "`n> Retrieving secret from Azure Key Vault '$VaultName'..." -ForegroundColor Cyan
    return (Get-AzKeyVaultSecret -VaultName $VaultName -Name $SecretName -ErrorAction SilentlyContinue).SecretValueText
}

function Set-CredentialToAzureKeyVault {
    param([string]$VaultName, [string]$SecretName, [string]$PromptText)
    Initialize-AzureKeyVaultModules
    Connect-ToAzure
    $secretValue = Read-Host "`nEnter $PromptText" -AsSecureString
    Set-AzKeyVaultSecret -VaultName $VaultName -Name $SecretName -SecretValue $secretValue | Out-Null
    return ConvertFrom-SecureString -SecureString $secretValue
}

function Write-CodexConfig {
    param(
        [string]$Model,
        [string]$Provider,
        [string]$Endpoint,
        [string]$ConfigType,
        [string]$EnvKey,
        [string]$VaultName = "",
        [string]$SecretName = "",
        [string]$ApiVersion = ""
    )
    Write-Host "`n> Writing ~/.codex/config.yaml ($Provider, $ConfigType)..." -ForegroundColor Cyan
    $codexDir = Join-Path $env:USERPROFILE ".codex"
    if (-not (Test-Path $codexDir)) { New-Item -ItemType Directory -Path $codexDir | Out-Null }
    $providerName = if ($Provider -eq 'openai') { 'OpenAI' } else { 'AzureOpenAI' }
    $baseURL = if ($Provider -eq 'azure') { $Endpoint.TrimEnd('/') + '/openai' } else { $Endpoint }
    $config = @"
model: $Model
provider: $Provider

providers:
  ${Provider}:
    name: $providerName
    baseURL: $baseURL
"@
    
    if ($ApiVersion) { $config += "`n    apiVersion: $ApiVersion" }
    
    if ($ConfigType -eq "envKey") {
        $config += "`n    envKey: $EnvKey"
    }
    else {
        $config += "`n    secretVault: $VaultName`n    secretName:  $SecretName"
    }
    
    $config | Set-Content -Encoding UTF8 (Join-Path $codexDir "config.yaml")
}

function Set-ProviderEnvironmentVariables {
    param(
        [string]$Provider,
        [string]$ApiKey,
        [string]$Endpoint = "",
        [string]$Model = "",
        [string]$ApiVersion = ""
    )
    if ($Provider -eq 'openai') {
        Write-Host "`n> Writing environment variable OPENAI_API_KEY..." -ForegroundColor Cyan
        $env:OPENAI_API_KEY = $ApiKey
    }
    else {
        Write-Host "`n> Writing Azure environment variables..." -ForegroundColor Cyan
        $env:AZURE_OPENAI_ENDPOINT = $Endpoint
        $env:AZURE_OPENAI_DEPLOYMENT_NAME = $Model
        $env:AZURE_OPENAI_API_KEY = $ApiKey
        $env:AZURE_OPENAI_API_VERSION = $ApiVersion
    }
}

function Get-OrSetCredential {
    param(
        [string]$Choice,
        [string]$VaultName,
        [string]$SecretName,
        [string]$EnvVarName,
        [string]$PromptText
    )
    switch ($Choice) {
        '1' {
            $existingKey = Get-CredentialFromEnvironment -EnvVarName $EnvVarName
            if (-not [string]::IsNullOrWhiteSpace($existingKey)) {
                $len = $existingKey.Length
                $pre = $existingKey.Substring(0, [Math]::Min(4, $len))
                $suf = $existingKey.Substring([Math]::Max(0, $len - 4))
                $masked = "$pre****$suf"
                $prompt = "`nEnter $PromptText [default: $masked]"
                $secureInput = Read-Host $prompt -AsSecureString
                $inputKey = ConvertFrom-SecureString -SecureString $secureInput
                if ([string]::IsNullOrWhiteSpace($inputKey)) {
                    $apiKey = $existingKey
                }
                else {
                    $apiKey = $inputKey
                }
            }
            else {
                Write-Host "`n> Environment variable `$EnvVarName is empty or whitespace; prompting for secure input..." -ForegroundColor Yellow
                $apiKey = Set-CredentialToEnvironment -PromptText $PromptText
            }
            return $apiKey
        }
        '2' {
            $apiKey = Get-CredentialFromSecretManagement -VaultName $VaultName -SecretName $SecretName
            if ([string]::IsNullOrWhiteSpace($apiKey)) {
                Write-Host "`n> SecretManagement entry '$SecretName' is empty or missing; prompting for secure input..." -ForegroundColor Yellow
                Set-CredentialToSecretManagement -VaultName $VaultName -SecretName $SecretName -PromptText $PromptText
                $apiKey = Get-CredentialFromSecretManagement -VaultName $VaultName -SecretName $SecretName
            }
            return $apiKey
        }
        '3' {
            $apiKey = Get-CredentialFromAzureKeyVault -VaultName $VaultName -SecretName $SecretName
            if ([string]::IsNullOrWhiteSpace($apiKey)) {
                Write-Host "`n> Azure Key Vault secret '$SecretName' is empty or missing; prompting for secure input..." -ForegroundColor Yellow
                $apiKey = Set-CredentialToAzureKeyVault -VaultName $VaultName -SecretName $SecretName -PromptText $PromptText
            }
            return $apiKey
        }
    }
}

#==============================================================================
# Main Script
#==============================================================================

# Ensure correct working directory: repository root
Set-Location (Resolve-Path "$PSScriptRoot\..\..")

# ---------- 1. Verify prerequisites ----------
Write-Host "`n> Checking for Node.js ..." -ForegroundColor Cyan
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Warning "Node.js not found. Install from https://nodejs.org then rerun."
    exit 1
}

# ---------- 2. Install or update Codex CLI ----------
Write-Host "`n> Checking for existing Codex CLI installation..." -ForegroundColor Cyan
if (Get-Command codex -ErrorAction SilentlyContinue) {
    Write-Host "Codex CLI is already installed." -ForegroundColor Yellow
    $updateChoice = Read-Host "Reinstall/update Codex [y/N]"
    if ($updateChoice -match '^[Yy]') {
        Write-Host "`n> Updating Codex CLI globally with npm ..." -ForegroundColor Cyan
        npm install -g @openai/codex
        if ($LASTEXITCODE -ne 0) {
            Write-Error "npm update failed; Codex not updated."
            exit 1
        }
    }
    else {
        Write-Host "Skipping Codex CLI installation/update." -ForegroundColor Green
    }
}
else {
    Write-Host "`n> Installing Codex CLI globally with npm ..." -ForegroundColor Cyan
    npm install -g @openai/codex
    if ($LASTEXITCODE -ne 0) {
        Write-Error "npm install failed; Codex not installed."
        exit 1
    }
}

# ---------- 3. Select provider ----------
Write-Host "`nSelect provider:" -ForegroundColor Cyan
Write-Host "  1) OpenAI (default, model: $modelNameOpenAI)" -ForegroundColor Cyan
Write-Host "  2) Azure OpenAI (default model: $deploymentName)" -ForegroundColor Cyan
$provChoice = Read-Host "Enter 1 or 2 [default: 1]"

if (-not $provChoice -or $provChoice -eq '1') {
    $provider = 'openai'
    $endpoint = $baseUrl
    $model = $modelNameOpenAI
    $currentSecretName = $openaiSecretName
    $envVarName = 'OPENAI_API_KEY'
    $promptText = 'OpenAI API Key'
}
else {
    $provider = 'azure'
    $endpoint = $azureEndpoint
    $model = $deploymentName
    $currentSecretName = $secretName
    $envVarName = 'AZURE_OPENAI_API_KEY'
    $promptText = 'Azure OpenAI API Key'
    
    # Azure-specific prompts
    $endpointRead = Read-Host "`nEnter Azure OpenAI *Endpoint* [default: $endpoint]"
    if ($endpointRead) { $endpoint = $endpointRead }
    $deploymentRead = Read-Host "Enter *Deployment Name* [default: $model]"
    if ($deploymentRead) { $model = $deploymentRead }
    $apiVersionRead = Read-Host "Enter API version [default: $apiVersion]"
    if ($apiVersionRead) { $apiVersion = $apiVersionRead }
}

# ---------- 4. Get credentials ----------
$providerName = if ($provider -eq 'openai') { 'OpenAI' } else { 'Azure OpenAI' }
$choice = Get-CredentialChoice -ProviderName $providerName -EnvVarName $envVarName

$choiceVaultName = if ($choice -eq '2') { 'SecretManagement' } else { 'Azure Key Vault' }
if ($choice -ne '1') {
    $vaultNameRead = Read-Host "`nEnter $($choiceVaultName) vault name [default: $vaultName]"
    if ($vaultNameRead) { $vaultName = $vaultNameRead }
    $secretNameRead = Read-Host "Enter secret name [default: $currentSecretName]"
    if ($secretNameRead) { $currentSecretName = $secretNameRead }
}

$apiKey = Get-OrSetCredential -Choice $choice -VaultName $vaultName -SecretName $currentSecretName -EnvVarName $envVarName -PromptText $promptText

if ($apiKey) {
    $len = $apiKey.Length
    $pre = $apiKey.Substring(0, [Math]::Min(4, $len))
    $suf = $apiKey.Substring([Math]::Max(0, $len - 4))
    Write-Host "`n> Using API key: $pre****$suf" -ForegroundColor Cyan
}

# ---------- 5. Configure environment and Codex ----------
Set-ProviderEnvironmentVariables -Provider $provider -ApiKey $apiKey -Endpoint $endpoint -Model $model -ApiVersion $apiVersion

$configType = @{ '1' = 'envKey'; '2' = 'SecretManagement'; '3' = 'Azure Key Vault' }[$choice]
Write-CodexConfig -Model $model -Provider $provider -Endpoint $endpoint -ConfigType $configType -EnvKey $envVarName -VaultName $vaultName -SecretName $currentSecretName -ApiVersion $apiVersion

# ---------- 6. Completion message ----------
Write-Host "`n[OK] Setup complete for provider '$provider' and model '$model'." -ForegroundColor Green
Write-Host @"
Open a new PowerShell window and run codex:

    codex --provider $provider -m $model --project-doc '.github\copilot-instructions.md'

"@
codex --provider $provider -m $model --project-doc '.github\copilot-instructions.md'
pause
