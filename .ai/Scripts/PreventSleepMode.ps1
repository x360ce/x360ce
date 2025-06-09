<#
.SYNOPSIS
    This script prevents a system from entering sleep mode or activating the screensaver
    while executing a long-running PowerShel or AI tasks. It uses a try-finally block to ensure that the
    power settings are restored to their default state even if the script is terminated with CTRL+C or
    CTRL+Break. This allows for better progress tracking during the execution of the script.
#>

# If PowerSettings type is not available, add it to the current session.
if (-not ([System.Management.Automation.PSTypeName]'PowerSettings').Type) {
    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        
        public class PowerSettings {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern uint SetThreadExecutionState(uint esFlags);
            
            public const uint ES_CONTINUOUS = 0x80000000;
            public const uint ES_SYSTEM_REQUIRED = 0x00000001;
            public const uint ES_DISPLAY_REQUIRED = 0x00000002;
        }
"@
}

try {

    Write-Host "Prevent sleep and screensaver"
    [void][PowerSettings]::SetThreadExecutionState([PowerSettings]::ES_CONTINUOUS -bor [PowerSettings]::ES_DISPLAY_REQUIRED)

	pause

}
finally {
 
	Write-Host "Reset the power settings to default"
    [void][PowerSettings]::SetThreadExecutionState([PowerSettings]::ES_CONTINUOUS)
}