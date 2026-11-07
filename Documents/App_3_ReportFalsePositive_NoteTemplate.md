> [!NOTE]
> If {ScanBrand} blocks or flags x360ce, it is a false positive. x360ce is open source and signed by its developer, and it contains no malware. We have reported the file to {ScanBrand}, and the detection goes away when {ScanBrand} next updates its definitions, which it does on its own.
>
> To get that update now, open {ScanBrand} and check for updates. Then download the file again, or scan the one you have again.
>
> In **Windows Security**, open **Virus & threat protection**, then **Protection updates**, and press **Check for updates**. Or run this in PowerShell:
>
> ```powershell
> & "C:\Program Files\Windows Defender\MpCmdRun.exe" -SignatureUpdate
> ```
>
> Then download the file again, or re-scan the one you have:
>
> ```powershell
> & "C:\Program Files\Windows Defender\MpCmdRun.exe" -Scan -ScanType 3 -File "$env:USERPROFILE\Downloads\x360ce.zip" -DisableRemediation
> ```
>
> Please don't turn off {ScanBrand} or add an exclusion for x360ce.
>
> You can check the file yourself on [VirusTotal]({VirusTotalUrl}).
