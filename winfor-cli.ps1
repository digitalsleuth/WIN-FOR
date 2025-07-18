 <#
    .SYNOPSIS
        This script is used to install the Windows Forensic (Win-FOR) toolset into a Windows VM or Windows machine
        https://github.com/digitalsleuth/win-for
    .DESCRIPTION
        The Windows Forensic (Win-FOR) environment comes with a multitude of tools for use in conducting digital forensics using a Windows
        Operating System. Many useful tools for malware analysis, reverse engineering, and advanced digital forensics are designed
        to run in Windows. Instead of creating a list for manual download, this installer, as well as the SaltStack state files
        which are part of the package, will allow for an easy, automated install.
        Additionally, the Win-FOR states allow for the automated installation of the Windows Subsystem for Linux v2, and comes with
        the REMnux and SIFT toolsets, making Win-FOR a one-stop shop for forensics!
    .NOTES
        Version        : 6.0
        Author         : Corey Forman (https://github.com/digitalsleuth)
        Prerequisites  : Windows 10 1909 or later
                       : Set-ExecutionPolicy must allow for script execution
    .PARAMETER Install
        Used to initiate the install of the Win-FOR environment.
        Can be used in conjunction with -User and -Mode to customize the build process
		If already installed, this will update the current installation to the latest version.
    .PARAMETER User
        Choose the desired username to configure the installation for.
        If not selected, the currently logged in user will be selected.
    .PARAMETER Mode
        There are three modes to choose from for the installation of the Win-FOR Environment:
            addon: Installs all of the tools, but doesn't do any customization.
			       Leaves your config the way it is.
            dedicated: Assumes that you want a full installation, and will install all packages,
		              customize the layout and theme, and provide additional reference documents.
		    custom: You've already generated a custom state file, and want to install the items identified in that.
		            This requires the use of the -Custom flag as well, to point to the full file path of your custom state file.
        If no option is selected, 'addon' mode will be selected by default.
	.PARAMETER Custom
	    This is the path to the custom state file you've generated to install only the desired tools. Requires -Mode 'custom'.
	.PARAMETER Latest
	    Displays the latest version of Win-FOR available.
    .PARAMETER Version
        Print the current version of the installed Win-FOR environment
    .PARAMETER XUser
        The Username for the X-Ways portal - Required to download and install X-Ways
    .PARAMETER XPass
        The Password for the X-Ways portal - Required to download and install X-Ways - "QUOTES REQUIRED"
    .PARAMETER IncludeWsl
        When selected, will install the Windows Subsystem for Linux v2, and will install the SIFT and REMnux toolsets.
        This option assumes you also want the full Win-FOR suite, and will install that first, then WSL last
    .PARAMETER WslOnly
        If you wish to install only WSLv2 with SIFT and REMnux either separately (due to bandwidth / system limitations)
        or you only want that particular feature and nothing else, this option will do just that. It will not install the Win-FOR
        states.
    .Example
        winfor-cli -Install -User forensics -Mode dedicated -IncludeWsl -XUser forensics -XPass "your_password"
        winfor-cli -Install -StandalonesPath "C:\standalones"
        winfor-cli -WslOnly
        winfor-cli -Version
    #>

param (
  [string]$User = ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name -split ("\\"))[-1],
  [string]$Mode = "addon",
  [string]$XUser = "",
  [string]$XPass = "",
  [string]$StandalonesPath = "C:\standalone",
  [string]$Custom = "",
  [switch]$Install,
  [switch]$Version,
  [switch]$Latest,
  [switch]$IncludeWsl,
  [switch]$WslOnly,
  [switch]$Help
)
[string]$installerVersion = 'v6.0'
[string]$saltstackVersion = '3007.2'
[string]$saltstackFile = 'Salt-Minion-' + $saltstackVersion + '-Py3-AMD64-Setup.exe'
[string]$saltstackHash = "9d46f907cb744ec5c02bb0ee6ec79e9e7d8460b4d29cbe898c34610f747a3f43"
[string]$saltstackUrl = "https://packages.broadcom.com/artifactory/saltproject-generic/windows/" + $saltstackVersion + "/"
[string]$saltstackSource = $saltstackUrl + $saltstackFile
[string]$gitVersion = '2.49.0.windows.1'
[string]$coreVersion = ($gitVersion -split ".windows")[0]
[string]$winVersion = ($gitVersion -split ".windows")[1]
[string]$gitFile = 'Git-' + $coreVersion + '-64-bit.exe'
[string]$gitHash = "726056328967f242fe6e9afbfe7823903a928aff577dcf6f517f2fb6da6ce83c"
[string]$gitUrl = "https://github.com/git-for-windows/git/releases/download/v" + $coreVersion + ".windows" + $winVersion + "/" + $gitFile
[string]$versionFile = "C:\winfor-version"
[string]$apiUri = "https://api.github.com/repos/digitalsleuth/winfor-salt/releases/latest"

function Compare-Hash($FileName, $HashName) {
    $fileHash = (Get-FileHash $FileName -Algorithm SHA256).Hash
    if ($fileHash -eq $HashName) {
        Write-Host "[+] Hash values match" -ForegroundColor Green
        return $True
    } else {
        Write-Host "[!] Hash values do not match" -ForegroundColor Red
        return $False
    }
}

function Test-SaltStack {
    $InstalledSalt = (Get-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*','HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*' | Where-Object {$_.DisplayName -clike 'Salt Minion*' } | Select-Object DisplayName, DisplayVersion)
    if (($null -eq $InstalledSalt.DisplayName) -or ($null -ne $InstalledSalt.DisplayName -and $InstalledSalt.DisplayVersion -ne $saltstackVersion)) {
        return $False
    } elseif ($InstalledSalt.DisplayName -clike 'Salt Minion*' -and $InstalledSalt.DisplayVersion -eq $saltstackVersion) {
        return $True
    }
}

function Get-SaltStack {
    if (-Not (Test-Path C:\salt\tempdownload\$saltstackFile)) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Write-Host "[-] Downloading SaltStack v$saltstackVersion" -ForegroundColor Yellow
		Write-Host "$saltstackSource"
        Invoke-WebRequest -Uri $saltstackSource -OutFile "C:\salt\tempdownload\$saltstackFile" -ErrorAction Stop | Out-Null
        Write-Host "[-] Verifying Download" -ForegroundColor Yellow
        $SaltHashMatch = Compare-Hash -FileName C:\salt\tempdownload\$saltstackFile -HashName $saltstackHash
        if (-Not ($SaltHashMatch)) {
            Write-Host "[!] Exiting" -ForegroundColor Red
            exit
        } else {
            Write-Host "[-] Installing SaltStack v$saltstackVersion" -ForegroundColor Yellow
            Install-SaltStack
        }
    } else {
        Write-Host "[-] Found existing SaltStack installer - validating hash before installing" -ForegroundColor Yellow
        $SaltHashMatch = Compare-Hash -FileName C:\salt\tempdownload\$saltstackFile -HashName $saltstackHash
        if (-Not ($SaltHashMatch)) {
            Write-Host "[!] Exiting" -ForegroundColor Red
            exit
        } else {
            Write-Host "[-] Installing SaltStack v$saltstackVersion" -ForegroundColor Yellow
            Install-SaltStack
        }
    }
}

function Install-SaltStack {
    Start-Process -Wait -FilePath "C:\salt\tempdownload\$saltstackFile" -ArgumentList '/S /master=localhost /minion-name=WIN-FOR' -PassThru | Out-Null
    if ($?) {
        Write-Host "[+] SaltStack installed successfully" -ForegroundColor Green
    } else {
        Write-Host "[!] Installation of SaltStack failed. Please re-run the installer to try again" -ForegroundColor Red
        exit
    }
}

function Test-Git {
    $InstalledGit = (Get-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*','HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*' | Where-Object {$_.DisplayName -clike 'Git*' } | Select-Object DisplayName, DisplayVersion)
    if (($null -eq $InstalledGit.DisplayName) -or ($null -ne $InstalledGit.DisplayName -and $InstalledGit.DisplayVersion -ne $gitVersion)) {
        return $False
    } elseif ($InstalledGit.DisplayName -clike 'Git*' -and $InstalledGit.DisplayVersion -clike "$gitVersion*") {
        return $True
    }
}

function Get-Git {
    if (-Not (Test-Path C:\salt\tempdownload\$gitFile)) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Write-Host "[-] Downloading Git v$gitVersion" -ForegroundColor Yellow
        Start-BitsTransfer -Source $gitUrl -Destination "C:\salt\tempdownload\$gitFile" -Dynamic
        Write-Host "[-] Verifying Download" -ForegroundColor Yellow
        $GitHashMatch = Compare-Hash -FileName C:\salt\tempdownload\$gitFile -HashName $gitHash
        if (-Not ($GitHashMatch)) {
            Write-Host "[!] Exiting" -ForegroundColor Red
            exit
        } else {
            Write-Host "[-] Installing Git v$gitVersion" -ForegroundColor Yellow
            Install-Git
        }
    } else {
        Write-Host "[-] Found existing Git installer - validating hash before installing" -ForegroundColor Yellow
        $GitHashMatch = Compare-Hash -FileName C:\salt\tempdownload\$gitFile -HashName $gitHash
        if (-Not ($GitHashMatch)) {
            Write-Host "[!] Exiting" -ForegroundColor Red
            exit
        } else {
            Write-Host "[-] Installing Git v$gitVersion" -ForegroundColor Yellow
            Install-Git
        }
    }
}

function Install-Git {
    Start-Process -Wait -FilePath "C:\salt\tempdownload\$gitFile" -ArgumentList '/VERYSILENT /NORESTART /SP- /NOCANCEL /SUPPRESSMSGBOXES' -PassThru | Out-Null
    if ($?) {
        Write-Host "[+] Git installed successfully" -ForegroundColor Green
    } else {
        Write-Host "[!] Installation of Git failed. Please re-run the installer to try again" -ForegroundColor Red
        exit
    }
}
function Get-WinFORRelease($installVersion) {
    $zipFolder = 'winfor-salt-' + $installVersion.Split("v")[-1]
    Write-Host "[-] Downloading and unpacking $installVersion" -ForegroundColor Yellow
    Start-BitsTransfer -Source https://github.com/digitalsleuth/winfor-salt/archive/refs/tags/$installVersion.zip -Destination C:\salt\tempdownload -Dynamic
    Start-BitsTransfer -Source https://github.com/digitalsleuth/winfor-salt/releases/download/$installVersion/winfor-salt-$installVersion.zip.sha256 -Destination C:\salt\tempdownload -Dynamic
    $releaseHash = (Get-Content C:\salt\tempdownload\winfor-salt-$installVersion.zip.sha256).Split(" ")[0]
    Write-Host "[-] Validating hash for release file" -ForegroundColor Yellow
    $StateHashMatch = Compare-Hash -FileName C:\salt\tempdownload\$installVersion.zip -HashName $releaseHash
    if (-Not ($StateHashMatch)) {
        Write-host "[!] Exiting" -ForegroundColor Red
        exit
    } else {
        Expand-Archive -Path C:\salt\tempdownload\$installVersion.zip -Destination 'C:\ProgramData\Salt Project\Salt\srv\' -Force
    }
    if (Test-Path "C:\ProgramData\Salt Project\Salt\srv\salt") {
    Remove-Item -Force "C:\ProgramData\Salt Project\Salt\srv\salt" -Recurse
    }
    Move-Item "C:\ProgramData\Salt Project\Salt\srv\$zipFolder" 'C:\ProgramData\Salt Project\Salt\srv\salt' -Force
}

function Install-WinFOR {
    $latestVersion = ((((Invoke-WebRequest $apiUri -UseBasicParsing).Content) | ConvertFrom-Json).zipball_url).Split('/')[-1]
    $installVersion = $latestVersion
    $runningUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-Not $runningUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "[!] Not running as Administrator, please re-run this script as Administrator" -ForegroundColor Red
        exit 1
    }
	if (-Not (Test-Path -Path 'C:\salt\tempdownload'))
	{
		New-Item -Path 'C:\salt\tempdownload' -ItemType Directory | Out-Null
	}
	if ($Mode) {$Mode = $Mode.ToLower()}
    if ($Mode -notin ('addon', 'dedicated', 'custom')) {
        Write-Host "[!] The only valid modes are 'addon', 'dedicated' or 'custom'." -ForegroundColor Red
        exit 1
    }
	if (($Mode -eq "custom") -and ($Custom -eq ""))
	{
		Write-Host "[!] When using 'custom' as a mode, you must provide the path to the custom state file using the '-Custom' argument." -ForegroundColor Red
		exit 1
	}
	elseif (($Mode -eq "custom") -and ($Custom -ne ""))
	{
		if (-Not (Test-Path $Custom))
		{
			Write-Host "[!] The file $Custom does not exist at the provided path. Please check your path and try again." -ForegroundColor Red
			exit 1
		}
	}
	elseif (($Custom) -and ($Mode -ne 'custom'))
	{
		Write-Host "[!] The -Custom argument requires that -Mode be 'custom'. Please either avoid setting -Custom or change your -Mode option to 'custom'." -ForegroundColor Red
		exit 1
	}
	$StandalonesPath = $StandalonesPath.TrimEnd('\')
    $saltStatus = Test-SaltStack
    if ($saltStatus -eq $False) {
        Write-Host "[-] SaltStack not installed" -ForegroundColor Yellow
        Get-SaltStack
    } elseif ($saltStatus -eq $True) {
        Write-Host "[+] SaltStack v$saltstackVersion already installed" -ForegroundColor Green
    }
	$gitStatus = Test-Git
    if ($gitStatus -eq $False) {
        Write-Host "[-] Git not installed" -ForegroundColor Yellow
        Get-Git
    } elseif ($gitStatus -eq $True) {
        Write-Host "[+] Git v$gitVersion already installed" -ForegroundColor Green
    }
    Write-Host "[-] Refreshing environment variables" -ForegroundColor Yellow
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine")
    $logFile = "C:\winfor-saltstack-$installVersion.log"
    Get-WinFORRelease $installVersion
	if ($Mode -eq 'custom')
	{
		Copy-Item $Custom -Destination 'C:\ProgramData\Salt Project\Salt\srv\salt\winfor\custom.sls'
	}
    if (($XUser -ne "") -and ($XPass -ne "")) {
        $AuthToken = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($XUser + ":" + $XPass))
        ((Get-Content 'C:\ProgramData\Salt Project\Salt\srv\salt\winfor\standalones\x-ways.sls') -replace ' = "TOKENPLACEHOLDER"', (" = "+ '"' + $AuthToken + '"')) | Set-Content 'C:\ProgramData\Salt Project\Salt\srv\salt\winfor\standalones\x-ways.sls'
        }
    Write-Host "[+] The Win-FOR installer command is running, configuring for user $User - this will take a while... please be patient" -ForegroundColor Green
    Start-Process -Wait -FilePath "C:\Program Files\Salt Project\Salt\salt-call.exe" -ArgumentList ("-l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.$Mode pillar=`"{'winfor_user': '$User', 'inpath': '$StandalonesPath'}`" --log-file-level=debug --log-file=`"$logFile`" --out-file=`"$logFile`" --out-file-append") | Out-Null
    if (-Not (Test-Path $logFile)) {
        $results=$failures=$errors=$null
	} else {
    $results = (Select-String -Path $logFile -Pattern 'Succeeded:' -Context 1 | ForEach-Object{"[!] " + $_.Line; "[!] " + $_.Context.PostContext} | Out-String).Trim()
    $failures = (Select-String -Path $logFile -Pattern 'Succeeded:' -Context 1 | ForEach-Object{$_.Context.PostContext}).split(':')[1].Trim()
    $errors = (Select-String -Path $logFile -Pattern '          ID:' -Context 0,6 | ForEach-Object{$_.Line; $_.Context.DisplayPostContext + "`r-------------"})
    }
    $errorLogFile = "C:\winfor-errors-$installVersion.log"
    if ($failures -ne 0 -and $null -ne $failures) {
        $errors | Out-File $errorLogFile -Append
        Write-Host $results -ForegroundColor Yellow
        Write-Host "[!] To determine the cause of the failures, review the log file $logFile and search for lines containing [ERROR   ] or review $errorLogFile for a less verbose listing." -ForegroundColor Yellow
        Write-Host "[!] In order to ensure all configuration changes are successful, it is recommended to reboot before first use." -ForegroundColor Yellow
    } else {
        Write-Host $results -ForegroundColor Green
        Write-Host "[!] In order to ensure all configuration changes are successful, it is recommended to reboot before first use." -ForegroundColor Green
    }
    if ($IncludeWsl) {
        if ($results) {
            $results | Out-File "C:\winfor-results-$installVersion.log" -Append
            }
        Invoke-WSLInstaller -User $User -StandalonesPath $StandalonesPath
    }
}

function Invoke-WSLInstaller($User, $StandalonesPath) {
	$StandalonesPath = $StandalonesPath.TrimEnd('\')
    $runningUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $runningUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "[!] Not running as administrator, please re-run this script as Administrator" -ForegroundColor Red
        exit 1
    }
    ### UAC and Defender settings based on https://github.com/Disassembler0/Win10-Initial-Setup-Script
    ### Required for unattended WSL setup
    Write-Output "[-] Lowering UAC level..."
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name "ConsentPromptBehaviorAdmin" -Type DWord -Value 0
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name "PromptOnSecureDesktop" -Type DWord -Value 0
    If (!(Test-Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender")) {
        New-Item -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender" -Force | Out-Null
    }
	Set-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender" -Name "DisableAntiSpyware" -Type DWord -Value 1
	If ([System.Environment]::OSVersion.Version.Build -eq 14393) {
	    Remove-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "WindowsDefender" -ErrorAction SilentlyContinue
	} ElseIf ([System.Environment]::OSVersion.Version.Build -ge 15063) {
        Remove-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "SecurityHealth" -ErrorAction SilentlyContinue
    }
    Add-MpPreference -ExclusionPath "$StandalonesPath\wsl"
    $wslLogFile = "C:\winfor-wsl.log"
    $wslErrorLog = "C:\winfor-wsl-errors.log"
    if (-Not (Test-Path "C:\ProgramData\Salt Project\Salt\srv\salt\winfor")) {
        $latestVersion = ((((Invoke-WebRequest $apiUri -UseBasicParsing).Content) | ConvertFrom-Json).zipball_url).Split('/')[-1]
        $installVersion = $latestVersion
        Get-WinFORRelease $installVersion
    }
    Write-Host "[+] Preparing for WSLv2 Installation" -ForegroundColor Green
    Write-Host "[-] This will process will automatically reboot the system and continue on the next login" -ForegroundColor Yellow
    Start-Process -Wait -FilePath "C:\Program Files\Salt Project\Salt\salt-call.exe" -ArgumentList ("-l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.repos pillar=`"{'winfor_user': '$User'}`" --log-file-level=debug --log-file=`"$wslLogFile`" --out-file=`"$wslLogFile`" --out-file-append") | Out-Null
    Start-Process -Wait -FilePath "C:\Program Files\Salt Project\Salt\salt-call.exe" -ArgumentList ("-l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.wsl pillar=`"{'winfor_user': '$User', 'inpath': '$StandalonesPath'}`" --log-file-level=debug --log-file=`"$wslLogFile`" --out-file=`"$wslLogFile`" --out-file-append") | Out-Null
    ### If the above is successful, the following lines have no effect, as a reboot will have occurred.
    ### However, if they are not successful, the following will log the errors in a separate file for examination.
    $wslErrors = (Select-String -Path $wslLogFile -Pattern '          ID:' -Context 0,6 | ForEach-Object{$_.Line; $_.Context.DisplayPostContext + "`r-------------"})
    if ($wslErrors -ne 0 -and $null -ne $wslErrors) {
        $wslErrors | Out-File $wslErrorLog -Append
    }
}

function Show-WinFORHelp {
    Write-Host -ForegroundColor Yellow @"
Windows Forensics Environment (Win-FOR) Installer $installerVersion

Usage:
  -Install                  Installs the Win-FOR environment, or if already installed, upgrades it.
  -User <user>              Choose the desired username for which to configure the installation. Default is $User.
  -Mode <mode>              There are three modes to choose from for the installation of the Win-FOR Environment:
                            addon: Installs all of the tools, but doesn't do any customization. Leaves your config the way it is.
                            dedicated: Assumes that you want a full installation, and will install all packages,
                                       customize the layout and theme, and provide additional reference documents.
                            custom: You've already generated a custom state file, and want to install the items identified in that.
                                    This requires the use of the -Custom flag as well, to point to the full file path of your custom state file.
                            If no option is selected, '$Mode' will be selected by default.
  -Custom <path>            This is the path to the custom state file you've generated to install only the desired tools. Requires -Mode 'custom'.
  -StandalonesPath <path>   Choose the path for where the standalone executables will be downloaded. Default is '$StandalonesPath'.
  -Latest                   Displays the latest version of Win-FOR available.
  -Version                  Displays the current version of Win-FOR (if installed) then exits.
  -XUser                    The Username for the X-Ways portal - Required to download and install X-Ways.
  -XPass                    The Password for the X-Ways portal - Required to download and install X-Ways - USE QUOTES.
  -IncludeWsl               Will install the Windows Subsystem for Linux v2 with SIFT and REMnux toolsets.
                            This option assumes you also want the full Win-FOR suite, install that first, then WSL.
  -WslOnly                  If you wish to only install WSLv2 with SIFT and REMnux separately, without the tools.

"@
}

function Get-WinFORVersion {
    if(-Not (Test-Path $versionFile)) {
        $winforVersion = 'not installed'
    } else {
        $winforVersion = (Get-Content $versionFile)
    }
    Write-Host "Win-FOR is $winforVersion" -ForegroundColor Green
    exit
}

function Get-LatestReleaseVersion {
	$ProgressPreference = 'SilentlyContinue'
	$latestVersion = ((((Invoke-WebRequest $apiUri -UseBasicParsing).Content) | ConvertFrom-Json).zipball_url).Split('/')[-1]
	Write-Host "The latest available release of Win-FOR is $latestVersion" -ForegroundColor Green
	exit
}

if ($WslOnly) {
    $saltStatus = Test-SaltStack
    $gitStatus = Test-Git
    if ($saltStatus -ne $True) {
        Write-Host "[-] SaltStack not installed" -ForegroundColor Yellow
        Get-SaltStack
    }
    if ($gitStatus -ne $True) {
        Write-Host "[-] Git not installed" -ForegroundColor Yellow
        Get-Git
    }
    Invoke-WSLInstaller $User $StandalonesPath
} elseif ($Help -and $PSBoundParameters.Count -eq 1) {
    Show-WinFORHelp
} elseif ($Version -and $PSBoundParameters.Count -eq 1) {
    Get-WinFORVersion
} elseif ($Latest -and $PSBoundParameters.Count -eq 1) {
	Get-LatestReleaseVersion
} elseif ($PSBoundParameters.Count -eq 0) {
    Show-WinFORHelp
} elseif ($Install) {
    Install-WinFOR
}

# SIG # Begin signature block
# MIIbvAYJKoZIhvcNAQcCoIIbrTCCG6kCAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUJl723jvyZnmMyv3uuQ7jZfyV
# fnygghYnMIIDHDCCAgSgAwIBAgIQcv98vmCQa6hHnZia/ZugPTANBgkqhkiG9w0B
# AQsFADAmMSQwIgYDVQQDDBtEaWdpdGFsIFNsZXV0aCBBdXRoZW50aWNvZGUwHhcN
# MjIxMjA5MTQwNTI5WhcNMjMxMjA5MTQyNTI5WjAmMSQwIgYDVQQDDBtEaWdpdGFs
# IFNsZXV0aCBBdXRoZW50aWNvZGUwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEK
# AoIBAQCyqdH/Mqw7OZLLqwFza0Ty4ONZ9O7dzt3qUriOZ7TxoZVWyyNmIEMhQiDW
# pUXWQ3epHLN094ZK3UGmj1Ctx0O/swbcqm8AwCUxtqCbye/5NR3RrVrZte/5r/LX
# +ztBtcE2b557jonOk+qKbp7O1ryOaif0clfMJHMubgXp/5Z2z5dHCqryj3mqIneM
# ShvIPo8YSte+AoMAlCLv+Us7jm4TiMtQF+TIqYX65yldiqqjROvPKQfLqpRpABa9
# RyaEDTxQxbaYW5M3rr8OvScsltvv93iihRpTFluBeWWAcLH/NNk3+XARrLOa/NFL
# NLFMqW1YEvIwhSfU4h00VYcPcjZVAgMBAAGjRjBEMA4GA1UdDwEB/wQEAwIHgDAT
# BgNVHSUEDDAKBggrBgEFBQcDAzAdBgNVHQ4EFgQUCpmScy/IvzSLE1odkleJMZh8
# aCswDQYJKoZIhvcNAQELBQADggEBAGFHQKA8F/tutHrFLiyLtuL5TkgiaxN9dojN
# oB41Jkczwilsnm7uRNp5UTr9ww8CKbk99uARMFEypeDfKaMrwAhCNPa2p5vlN0Uy
# iSuND28pnkDafFjl+Ec+KPCKIhythp6QDTYFkrPjnxFulQtzStSWVzxBsshZK83f
# 1mjRsOStPZD0XdPY4p/t7DRgBYYfeKXtaHabOPXjgb95D6MiN6kS7EmYCiadexxH
# GezIytN3svS2/r6dXJinmF619otyxYWHtr6IYtyxKqwunx87GdKHUV0Vyd7zq3K8
# 0RP4v480W2gHMu/InDxi6jVwoE+sJ4GpajdJ75Un7ibVOB7J4FswggWNMIIEdaAD
# AgECAhAOmxiO+dAt5+/bUOIIQBhaMA0GCSqGSIb3DQEBDAUAMGUxCzAJBgNVBAYT
# AlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2Vy
# dC5jb20xJDAiBgNVBAMTG0RpZ2lDZXJ0IEFzc3VyZWQgSUQgUm9vdCBDQTAeFw0y
# MjA4MDEwMDAwMDBaFw0zMTExMDkyMzU5NTlaMGIxCzAJBgNVBAYTAlVTMRUwEwYD
# VQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAf
# BgNVBAMTGERpZ2lDZXJ0IFRydXN0ZWQgUm9vdCBHNDCCAiIwDQYJKoZIhvcNAQEB
# BQADggIPADCCAgoCggIBAL/mkHNo3rvkXUo8MCIwaTPswqclLskhPfKK2FnC4Smn
# PVirdprNrnsbhA3EMB/zG6Q4FutWxpdtHauyefLKEdLkX9YFPFIPUh/GnhWlfr6f
# qVcWWVVyr2iTcMKyunWZanMylNEQRBAu34LzB4TmdDttceItDBvuINXJIB1jKS3O
# 7F5OyJP4IWGbNOsFxl7sWxq868nPzaw0QF+xembud8hIqGZXV59UWI4MK7dPpzDZ
# Vu7Ke13jrclPXuU15zHL2pNe3I6PgNq2kZhAkHnDeMe2scS1ahg4AxCN2NQ3pC4F
# fYj1gj4QkXCrVYJBMtfbBHMqbpEBfCFM1LyuGwN1XXhm2ToxRJozQL8I11pJpMLm
# qaBn3aQnvKFPObURWBf3JFxGj2T3wWmIdph2PVldQnaHiZdpekjw4KISG2aadMre
# Sx7nDmOu5tTvkpI6nj3cAORFJYm2mkQZK37AlLTSYW3rM9nF30sEAMx9HJXDj/ch
# srIRt7t/8tWMcCxBYKqxYxhElRp2Yn72gLD76GSmM9GJB+G9t+ZDpBi4pncB4Q+U
# DCEdslQpJYls5Q5SUUd0viastkF13nqsX40/ybzTQRESW+UQUOsxxcpyFiIJ33xM
# dT9j7CFfxCBRa2+xq4aLT8LWRV+dIPyhHsXAj6KxfgommfXkaS+YHS312amyHeUb
# AgMBAAGjggE6MIIBNjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTs1+OC0nFd
# ZEzfLmc/57qYrhwPTzAfBgNVHSMEGDAWgBRF66Kv9JLLgjEtUYunpyGd823IDzAO
# BgNVHQ8BAf8EBAMCAYYweQYIKwYBBQUHAQEEbTBrMCQGCCsGAQUFBzABhhhodHRw
# Oi8vb2NzcC5kaWdpY2VydC5jb20wQwYIKwYBBQUHMAKGN2h0dHA6Ly9jYWNlcnRz
# LmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydEFzc3VyZWRJRFJvb3RDQS5jcnQwRQYDVR0f
# BD4wPDA6oDigNoY0aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNz
# dXJlZElEUm9vdENBLmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQEM
# BQADggEBAHCgv0NcVec4X6CjdBs9thbX979XB72arKGHLOyFXqkauyL4hxppVCLt
# pIh3bb0aFPQTSnovLbc47/T/gLn4offyct4kvFIDyE7QKt76LVbP+fT3rDB6mouy
# XtTP0UNEm0Mh65ZyoUi0mcudT6cGAxN3J0TU53/oWajwvy8LpunyNDzs9wPHh6jS
# TEAZNUZqaVSwuKFWjuyk1T3osdz9HNj0d1pcVIxv76FQPfx2CWiEn2/K2yCNNWAc
# AgPLILCsWKAOQGPFmCLBsln1VWvPJ6tsds5vIy30fnFqI2si/xK4VC0nftg62fC2
# h5b9W9FcrBjDTZ9ztwGpn1eqXijiuZQwggauMIIElqADAgECAhAHNje3JFR82Ees
# /ShmKl5bMA0GCSqGSIb3DQEBCwUAMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxE
# aWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMT
# GERpZ2lDZXJ0IFRydXN0ZWQgUm9vdCBHNDAeFw0yMjAzMjMwMDAwMDBaFw0zNzAz
# MjIyMzU5NTlaMGMxCzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2VydCwgSW5j
# LjE7MDkGA1UEAxMyRGlnaUNlcnQgVHJ1c3RlZCBHNCBSU0E0MDk2IFNIQTI1NiBU
# aW1lU3RhbXBpbmcgQ0EwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQDG
# hjUGSbPBPXJJUVXHJQPE8pE3qZdRodbSg9GeTKJtoLDMg/la9hGhRBVCX6SI82j6
# ffOciQt/nR+eDzMfUBMLJnOWbfhXqAJ9/UO0hNoR8XOxs+4rgISKIhjf69o9xBd/
# qxkrPkLcZ47qUT3w1lbU5ygt69OxtXXnHwZljZQp09nsad/ZkIdGAHvbREGJ3Hxq
# V3rwN3mfXazL6IRktFLydkf3YYMZ3V+0VAshaG43IbtArF+y3kp9zvU5EmfvDqVj
# bOSmxR3NNg1c1eYbqMFkdECnwHLFuk4fsbVYTXn+149zk6wsOeKlSNbwsDETqVcp
# licu9Yemj052FVUmcJgmf6AaRyBD40NjgHt1biclkJg6OBGz9vae5jtb7IHeIhTZ
# girHkr+g3uM+onP65x9abJTyUpURK1h0QCirc0PO30qhHGs4xSnzyqqWc0Jon7ZG
# s506o9UD4L/wojzKQtwYSH8UNM/STKvvmz3+DrhkKvp1KCRB7UK/BZxmSVJQ9FHz
# NklNiyDSLFc1eSuo80VgvCONWPfcYd6T/jnA+bIwpUzX6ZhKWD7TA4j+s4/TXkt2
# ElGTyYwMO1uKIqjBJgj5FBASA31fI7tk42PgpuE+9sJ0sj8eCXbsq11GdeJgo1gJ
# ASgADoRU7s7pXcheMBK9Rp6103a50g5rmQzSM7TNsQIDAQABo4IBXTCCAVkwEgYD
# VR0TAQH/BAgwBgEB/wIBADAdBgNVHQ4EFgQUuhbZbU2FL3MpdpovdYxqII+eyG8w
# HwYDVR0jBBgwFoAU7NfjgtJxXWRM3y5nP+e6mK4cD08wDgYDVR0PAQH/BAQDAgGG
# MBMGA1UdJQQMMAoGCCsGAQUFBwMIMHcGCCsGAQUFBwEBBGswaTAkBggrBgEFBQcw
# AYYYaHR0cDovL29jc3AuZGlnaWNlcnQuY29tMEEGCCsGAQUFBzAChjVodHRwOi8v
# Y2FjZXJ0cy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9vdEc0LmNydDBD
# BgNVHR8EPDA6MDigNqA0hjJodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vRGlnaUNl
# cnRUcnVzdGVkUm9vdEc0LmNybDAgBgNVHSAEGTAXMAgGBmeBDAEEAjALBglghkgB
# hv1sBwEwDQYJKoZIhvcNAQELBQADggIBAH1ZjsCTtm+YqUQiAX5m1tghQuGwGC4Q
# TRPPMFPOvxj7x1Bd4ksp+3CKDaopafxpwc8dB+k+YMjYC+VcW9dth/qEICU0MWfN
# thKWb8RQTGIdDAiCqBa9qVbPFXONASIlzpVpP0d3+3J0FNf/q0+KLHqrhc1DX+1g
# tqpPkWaeLJ7giqzl/Yy8ZCaHbJK9nXzQcAp876i8dU+6WvepELJd6f8oVInw1Ypx
# dmXazPByoyP6wCeCRK6ZJxurJB4mwbfeKuv2nrF5mYGjVoarCkXJ38SNoOeY+/um
# nXKvxMfBwWpx2cYTgAnEtp/Nh4cku0+jSbl3ZpHxcpzpSwJSpzd+k1OsOx0ISQ+U
# zTl63f8lY5knLD0/a6fxZsNBzU+2QJshIUDQtxMkzdwdeDrknq3lNHGS1yZr5Dhz
# q6YBT70/O3itTK37xJV77QpfMzmHQXh6OOmc4d0j/R0o08f56PGYX/sr2H7yRp11
# LB4nLCbbbxV7HhmLNriT1ObyF5lZynDwN7+YAN8gFk8n+2BnFqFmut1VwDophrCY
# oCvtlUG3OtUVmDG0YgkPCr2B2RP+v6TR81fZvAT6gt4y3wSJ8ADNXcL50CN/AAvk
# dgIm2fBldkKmKYcJRyvmfxqkhQ/8mJb2VVQrH4D6wPIOK+XW+6kvRBVK5xMOHds3
# OBqhK/bt1nz8MIIGwDCCBKigAwIBAgIQDE1pckuU+jwqSj0pB4A9WjANBgkqhkiG
# 9w0BAQsFADBjMQswCQYDVQQGEwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4x
# OzA5BgNVBAMTMkRpZ2lDZXJ0IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGlt
# ZVN0YW1waW5nIENBMB4XDTIyMDkyMTAwMDAwMFoXDTMzMTEyMTIzNTk1OVowRjEL
# MAkGA1UEBhMCVVMxETAPBgNVBAoTCERpZ2lDZXJ0MSQwIgYDVQQDExtEaWdpQ2Vy
# dCBUaW1lc3RhbXAgMjAyMiAtIDIwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIK
# AoICAQDP7KUmOsap8mu7jcENmtuh6BSFdDMaJqzQHFUeHjZtvJJVDGH0nQl3PRWW
# CC9rZKT9BoMW15GSOBwxApb7crGXOlWvM+xhiummKNuQY1y9iVPgOi2Mh0KuJqTk
# u3h4uXoW4VbGwLpkU7sqFudQSLuIaQyIxvG+4C99O7HKU41Agx7ny3JJKB5MgB6F
# VueF7fJhvKo6B332q27lZt3iXPUv7Y3UTZWEaOOAy2p50dIQkUYp6z4m8rSMzUy5
# Zsi7qlA4DeWMlF0ZWr/1e0BubxaompyVR4aFeT4MXmaMGgokvpyq0py2909ueMQo
# P6McD1AGN7oI2TWmtR7aeFgdOej4TJEQln5N4d3CraV++C0bH+wrRhijGfY59/XB
# T3EuiQMRoku7mL/6T+R7Nu8GRORV/zbq5Xwx5/PCUsTmFntafqUlc9vAapkhLWPl
# WfVNL5AfJ7fSqxTlOGaHUQhr+1NDOdBk+lbP4PQK5hRtZHi7mP2Uw3Mh8y/CLiDX
# gazT8QfU4b3ZXUtuMZQpi+ZBpGWUwFjl5S4pkKa3YWT62SBsGFFguqaBDwklU/G/
# O+mrBw5qBzliGcnWhX8T2Y15z2LF7OF7ucxnEweawXjtxojIsG4yeccLWYONxu71
# LHx7jstkifGxxLjnU15fVdJ9GSlZA076XepFcxyEftfO4tQ6dwIDAQABo4IBizCC
# AYcwDgYDVR0PAQH/BAQDAgeAMAwGA1UdEwEB/wQCMAAwFgYDVR0lAQH/BAwwCgYI
# KwYBBQUHAwgwIAYDVR0gBBkwFzAIBgZngQwBBAIwCwYJYIZIAYb9bAcBMB8GA1Ud
# IwQYMBaAFLoW2W1NhS9zKXaaL3WMaiCPnshvMB0GA1UdDgQWBBRiit7QYfyPMRTt
# lwvNPSqUFN9SnDBaBgNVHR8EUzBRME+gTaBLhklodHRwOi8vY3JsMy5kaWdpY2Vy
# dC5jb20vRGlnaUNlcnRUcnVzdGVkRzRSU0E0MDk2U0hBMjU2VGltZVN0YW1waW5n
# Q0EuY3JsMIGQBggrBgEFBQcBAQSBgzCBgDAkBggrBgEFBQcwAYYYaHR0cDovL29j
# c3AuZGlnaWNlcnQuY29tMFgGCCsGAQUFBzAChkxodHRwOi8vY2FjZXJ0cy5kaWdp
# Y2VydC5jb20vRGlnaUNlcnRUcnVzdGVkRzRSU0E0MDk2U0hBMjU2VGltZVN0YW1w
# aW5nQ0EuY3J0MA0GCSqGSIb3DQEBCwUAA4ICAQBVqioa80bzeFc3MPx140/WhSPx
# /PmVOZsl5vdyipjDd9Rk/BX7NsJJUSx4iGNVCUY5APxp1MqbKfujP8DJAJsTHbCY
# idx48s18hc1Tna9i4mFmoxQqRYdKmEIrUPwbtZ4IMAn65C3XCYl5+QnmiM59G7hq
# opvBU2AJ6KO4ndetHxy47JhB8PYOgPvk/9+dEKfrALpfSo8aOlK06r8JSRU1Nlma
# D1TSsht/fl4JrXZUinRtytIFZyt26/+YsiaVOBmIRBTlClmia+ciPkQh0j8cwJvt
# fEiy2JIMkU88ZpSvXQJT657inuTTH4YBZJwAwuladHUNPeF5iL8cAZfJGSOA1zZa
# X5YWsWMMxkZAO85dNdRZPkOaGK7DycvD+5sTX2q1x+DzBcNZ3ydiK95ByVO5/zQQ
# Z/YmMph7/lxClIGUgp2sCovGSxVK05iQRWAzgOAj3vgDpPZFR+XOuANCR+hBNnF3
# rf2i6Jd0Ti7aHh2MWsgemtXC8MYiqE+bvdgcmlHEL5r2X6cnl7qWLoVXwGDneFZ/
# au/ClZpLEQLIgpzJGgV8unG1TnqZbPTontRamMifv427GFxD9dAq6OJi7ngE273R
# +1sKqHB+8JeEeOMIA11HLGOoJTiXAdI/Otrl5fbmm9x+LMz/F0xNAKLY1gEOuIvu
# 5uByVYksJxlh9ncBjDGCBP8wggT7AgEBMDowJjEkMCIGA1UEAwwbRGlnaXRhbCBT
# bGV1dGggQXV0aGVudGljb2RlAhBy/3y+YJBrqEedmJr9m6A9MAkGBSsOAwIaBQCg
# eDAYBgorBgEEAYI3AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJAzEMBgorBgEE
# AYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMCMGCSqGSIb3DQEJ
# BDEWBBSYhkYeCdzvadqlSza25CPxJNH/NDANBgkqhkiG9w0BAQEFAASCAQAsrr+n
# r/1oyndfYWgo7A2HgElYseiInMDo5ircDCf+4G1KD75zTjD7sDX8en8pK+ZqWDbh
# vwXwuy8mJbAvnd1DOwIpm2UFtlB+a+p26GFOgiSTJq48zaJXSwwxCMNfMMeVaKFf
# H7Nk7mOgRvvCTOEwu8q3Tft2+4ATB7OBSxs6HwEMgBXHi67HFacDBsTF6hUtx8+M
# +6Dt+TpwBaKq4TtgJr5EVBE5NsHmbfQChOJNu8l4Ab4J1KYj/oKpsUdNOxpidOfY
# 73wls3LW7q0dxR2TKVtR3zTRsFkJH4ucWfLENe4Dv7Wxe+7suZS4y25U2aNLMoVc
# 6pWtht8+AnIAmkjPoYIDIDCCAxwGCSqGSIb3DQEJBjGCAw0wggMJAgEBMHcwYzEL
# MAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMTswOQYDVQQDEzJE
# aWdpQ2VydCBUcnVzdGVkIEc0IFJTQTQwOTYgU0hBMjU2IFRpbWVTdGFtcGluZyBD
# QQIQDE1pckuU+jwqSj0pB4A9WjANBglghkgBZQMEAgEFAKBpMBgGCSqGSIb3DQEJ
# AzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkFMQ8XDTIyMTIyMDAzNTIxMVowLwYJ
# KoZIhvcNAQkEMSIEILRoeP7BmCzQwhnSToJ4ytvDoiGnZmtBwTQ+gHTDtYoBMA0G
# CSqGSIb3DQEBAQUABIICAFIjjJ4x+fxWrt/o6fTEJP8UGqSeD+F8PmbSTwct7tIQ
# tFPCiRDlv0eYd0s0L8GqDoUZKy4U88a98kRa+IxcgxheXZS5OWSoF9u7XP36oPao
# jyO3hjs6b2py+iyui/gAloRFu0c1lOgPrrARf/i+9/abOQiB0PE9oEu3yxcxgbp0
# 0JKNEgB2iBI3/Mcsec1CkpIEHCx5fjpLg4tXAmneYlooy53zFcDTgcS0qCcYpPl1
# 8Z0RIDvv+q2UksduYMkdfWvMh4E3vyf0aLD8tWUvntAQyyWN6behptEypgvlJDcL
# cVUrZdPmq2PFlVWB58aGS4JuyOAWWPw9xr3j6U58wpoNllPdK+WW2iLAqbDDlyjY
# 8pbCk0tDqbYByq8mIbECtQuTg1b+7dK+KRZwvZSpFwxV4sdHZkbZPnKBxr37yGPq
# VkHTY4YSSYhJxOxXwkza5I26dJwoDV8mwu6FoKcyhU4N8DnOqNY/R0/mCDBRFshC
# 7jqOth8UZxCSAYFzW3DYFWSL4hut9Qzql1wD0Xhd5SO22IPpOY1sYI3TqJ6zjQAd
# IZO2nOTkIDnDBnh5IW88RVy+St8FDJrJT+/kZxIG3fo7I6WahrtkOqieIPj5xMeZ
# U3BADsHkGkqM+si5ZXNUMf44vOcpYCLYnsns/ZY4+Vc1f/zYKdfHrZOe2JdAzYNa
# SIG # End signature block
