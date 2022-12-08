 <#
    .SYNOPSIS
        This script is used to install the Windows Forensic (Win-FOR) toolset into a Windows VM or Windows machine
        https://github.com/digitalsleuth/winfor-salt
    .DESCRIPTION
        The Windows Forensic (Win-FOR) environment comes with a multitude of tools for use in conducting digital forensics using a Windows
        Operating System. Many useful tools for malware analysis, reverse engineering, and advanced digital forensics are designed
        to run in Windows. Instead of creating a list for manual download, this installer, as well as the SaltStack state files
        which are part of the package, will allow for an easy, automated install.
        Additionally, the Win-FOR states allow for the automated installation of the Windows Subsystem for Linux v2, and comes with
        the REMnux and SIFT toolsets, making Win-FOR a one-stop shop for forensics!
    .NOTES
        Version        : 4.0
        Author         : Corey Forman (https://github.com/digitalsleuth)
        Prerequisites  : Windows 10 1909 or later
                       : Set-ExecutionPolicy must allow for script execution
    .PARAMETER Install
        Used to initiate the install of the Win-FOR environment.
        Can be used in conjunction with -User and -Mode to customize the build process
    .PARAMETER User
        Choose the desired username to configure the installation for.
        If not selected, the currently logged in user will be selected.
    .PARAMETER Mode
        There are two modes to choose from for the installation of the Win-FOR Environment:
            addon: Install all of the tools, but don't do any customization. Leaves your config the way it is
            dedicated: Assumes that you want the full meal-deal, and will install all packages, customize the layout, and provide
                       additional reference documents
        If neither option is selected, the addon mode will be selected.
    .PARAMETER Update
        Identifies the current version of the environment and re-installs all states from that version
    .PARAMETER Upgrade
        Identifies the latest version of Win-FOR and will install that version
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
        winfor -Install -User forensics -Mode dedicated -IncludeWsl -XUser forensics -XPass "password123"
        winfor -Install
        winfor -WslOnly
        winfor -Version
        winfor -Update
        winfor -Upgrade
    #>

param (
  [string]$User = "",
  [string]$Mode = "",
  [string]$XUser = "",
  [string]$XPass = "",
  [switch]$Install,
  [switch]$Update,
  [switch]$Upgrade,
  [switch]$Version,
  [switch]$IncludeWsl,
  [switch]$WslOnly,
  [switch]$Help
)
[string]$installerVersion = 'v4.0'
[string]$saltstackVersion = '3005.1-3'
[string]$saltstackFile = 'Salt-Minion-' + $saltstackVersion + '-Py3-AMD64-Setup.exe'
[string]$saltstackHash = "9899DE61DF2782BCA8A896EB814BF2EA0E92C0B18BF91F7C747B60EBF1EBF72D"
[string]$saltstackUrl = "https://repo.saltproject.io/windows/"
[string]$saltstackSource = $saltstackUrl + $saltstackFile
[string]$gitVersion = '2.38.1'
[string]$gitFile = 'Git-' + $gitVersion + '-64-bit.exe'
[string]$gitHash = "f3fe05e65cd7e9a9126784d4ad57fdf979d30d5987fe849af4348dbe3e284df6"
[string]$gitUrl = "https://github.com/git-for-windows/git/releases/download/v" + $gitVersion + ".windows.1/" + $gitFile
[string]$versionFile = "C:\ProgramData\Salt Project\Salt\srv\salt\winfor-version"

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

function Test-Saltstack {
    $InstalledSalt = (Get-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*','HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*' | Where-Object {$_.DisplayName -clike 'Salt Minion*' } | Select-Object DisplayName, DisplayVersion)
    if (($null -eq $InstalledSalt.DisplayName) -or ($null -ne $InstalledSalt.DisplayName -and $InstalledSalt.DisplayVersion -ne $saltstackVersion)) {
        return $False
    } elseif ($InstalledSalt.DisplayName -clike 'Salt Minion*' -and $InstalledSalt.DisplayVersion -eq $saltstackVersion) {
        return $True
    }
}

function Get-Saltstack {
    if (-Not (Test-Path C:\Windows\Temp\$saltstackFile)) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Write-Host "[-] Downloading SaltStack v$saltstackVersion" -ForegroundColor Yellow
        Start-BitsTransfer -Source $saltstackSource -Destination "C:\Windows\Temp\$saltstackFile" -Dynamic
        Write-Host "[-] Verifying Download" -ForegroundColor Yellow
        $SaltHashMatch = Compare-Hash -FileName C:\Windows\Temp\$saltstackFile -HashName $saltstackHash
        if (-Not ($SaltHashMatch)) {
            Write-Host "[!] Exiting" -ForegroundColor Red
            exit
        } else {
            Write-Host "[-] Installing SaltStack v$saltstackVersion" -ForegroundColor Yellow
            Install-Saltstack
        }
    } else {
        Write-Host "[-] Found existing SaltStack installer - validating hash before installing" -ForegroundColor Yellow
        $SaltHashMatch = Compare-Hash -FileName C:\Windows\Temp\$saltstackFile -HashName $saltstackHash
        if (-Not ($SaltHashMatch)) {
            Write-Host "[!] Exiting" -ForegroundColor Red
            exit
        } else {
            Write-Host "[-] Installing SaltStack v$saltstackVersion" -ForegroundColor Yellow
            Install-Saltstack
        }
    }
}

function Install-Saltstack {
    Start-Process -Wait -FilePath "C:\Windows\Temp\$saltstackFile" -ArgumentList '/S /master=localhost /minion-name=WIN-FOR' -PassThru | Out-Null
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
    if (-Not (Test-Path C:\Windows\Temp\$gitFile)) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Write-Host "[-] Downloading Git v$gitVersion" -ForegroundColor Yellow
        Start-BitsTransfer -Source $gitUrl -Destination "C:\Windows\Temp\$gitFile" -Dynamic
        Write-Host "[-] Verifying Download" -ForegroundColor Yellow
        $GitHashMatch = Compare-Hash -FileName C:\Windows\Temp\$gitFile -HashName $gitHash
        if (-Not ($GitHashMatch)) {
            Write-Host "[!] Exiting" -ForegroundColor Red
            exit
        } else {
            Write-Host "[-] Installing Git v$gitVersion" -ForegroundColor Yellow
            Install-Git
        }
    } else {
        Write-Host "[-] Found existing Git installer - validating hash before installing" -ForegroundColor Yellow
        $GitHashMatch = Compare-Hash -FileName C:\Windows\Temp\$gitFile -HashName $gitHash
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
    Start-Process -Wait -FilePath "C:\Windows\Temp\$gitFile" -ArgumentList '/VERYSILENT /NORESTART /SP- /NOCANCEL /SUPPRESSMSGBOXES' -PassThru | Out-Null
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
    Start-BitsTransfer -Source https://github.com/digitalsleuth/winfor-salt/archive/refs/tags/$installVersion.zip -Destination C:\Windows\Temp -Dynamic
    Start-BitsTransfer -Source https://github.com/digitalsleuth/winfor-salt/releases/download/$installVersion/winfor-salt-$installVersion.zip.sha256 -Destination C:\Windows\Temp -Dynamic
    $releaseHash = (Get-Content C:\Windows\Temp\winfor-salt-$installVersion.zip.sha256).Split(" ")[0]
    Write-Host "[-] Validating hash for release file" -ForegroundColor Yellow
    $StateHashMatch = Compare-Hash -FileName C:\Windows\Temp\$installVersion.zip -HashName $releaseHash
    if (-Not ($StateHashMatch)) {
        Write-host "[!] Exiting" -ForegroundColor Red
        exit
    } else {
        Expand-Archive -Path C:\Windows\Temp\$installVersion.zip -Destination 'C:\ProgramData\Salt Project\Salt\srv\' -Force
    }
    if (Test-Path "C:\ProgramData\Salt Project\Salt\srv\salt") {
    Remove-Item -Force "C:\ProgramData\Salt Project\Salt\srv\salt" -Recurse
    }
    Move-Item "C:\ProgramData\Salt Project\Salt\srv\$zipFolder" 'C:\ProgramData\Salt Project\Salt\srv\salt' -Force
}

function Install-WinFOR {
    $apiUri = "https://api.github.com/repos/digitalsleuth/winfor-salt/releases/latest"
    $latestVersion = ((((Invoke-WebRequest $apiUri -UseBasicParsing).Content) | ConvertFrom-Json).zipball_url).Split('/')[-1]
    $installVersion = $latestVersion
    $runningUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-Not $runningUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "[!] Not running as Administrator, please re-run this script as Administrator" -ForegroundColor Red
        exit 1
    }
    if ($User -eq "") {
        $User = [System.Environment]::UserName
        }
    if ($Mode -eq "") {
        $Mode = "addon"
        }
    if (($Mode -ne 'addon') -and ($Mode -ne 'dedicated')) {
        Write-Host "[!] The only valid modes are 'addon' or 'dedicated'." -ForegroundColor Red
        exit 1
    }
    if ($Update) {
       if(-Not (Test-Path $versionFile)) {
           $winforVersion = 'not installed'
           Write-Host "[!] Win-FOR is not installed. Try running the installer again before choosing the update option." -ForegroundColor Red
           exit
        } else {
           $winforVersion = (Get-Content $versionFile)
        }
        $installVersion = $winforVersion
	} elseif ($Upgrade) {
        if(-Not (Test-Path $versionFile)) {
           $winforVersion = 'not installed'
           Write-Host "[!] Win-FOR is not installed. Try running the installer again before choosing the upgrade option." -ForegroundColor Red
           exit
       } else {
        $installVersion = $latestVersion
        }
    }
    $saltStatus = Test-Saltstack
    if ($saltStatus -eq $False) {
        Write-Host "[-] SaltStack not installed" -ForegroundColor Yellow
        Get-Saltstack
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
    if (($XUser -ne "") -and ($XPass -ne "")) {
        $AuthToken = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($XUser + ":" + $XPass))
        ((Get-Content 'C:\ProgramData\Salt Project\Salt\srv\salt\winfor\standalones\x-ways.sls') -replace ' = "TOKENPLACEHOLDER"', (" = "+ '"' + $AuthToken + '"')) | Set-Content 'C:\ProgramData\Salt Project\Salt\srv\salt\winfor\standalones\x-ways.sls'
        }
    Write-Host "[+] The Win-FOR installer command is running, configuring for user $User - this will take a while... please be patient" -ForegroundColor Green
    Start-Process -Wait -FilePath "C:\Program Files\Salt Project\Salt\salt-call.bat" -ArgumentList ("-l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.$Mode pillar=`"{'winfor_user': '$User'}`" --log-file-level=debug --log-file=`"$logFile`" --out-file=`"$logFile`" --out-file-append") | Out-Null
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
        Invoke-WSLInstaller
    }
}

function Invoke-WSLInstaller {
    if ($User -eq "") {
        $User = [System.Environment]::UserName
    }
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
    Add-MpPreference -ExclusionPath "C:\standalone\wsl"
    $wslLogFile = "C:\winfor-wsl.log"
    $wslErrorLog = "C:\winfor-wsl-errors.log"
    if (-Not (Test-Path "C:\ProgramData\Salt Project\Salt\srv\salt\winfor")) {
        $apiUri = "https://api.github.com/repos/digitalsleuth/winfor-salt/releases/latest"
        $latestVersion = ((((Invoke-WebRequest $apiUri -UseBasicParsing).Content) | ConvertFrom-Json).zipball_url).Split('/')[-1]
        $installVersion = $latestVersion
        Get-WinFORRelease $installVersion
    }
    Write-Host "[+] Preparing for WSLv2 Installation" -ForegroundColor Green
    Write-Host "[-] This will process will automatically reboot the system and continue on the next login" -ForegroundColor Yellow
    Start-Process -Wait -FilePath "C:\Program Files\Salt Project\Salt\salt-call.bat" -ArgumentList ("-l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.repos pillar=`"{'winfor_user': '$User'}`" --log-file-level=debug --log-file=`"$wslLogFile`" --out-file=`"$wslLogFile`" --out-file-append") | Out-Null
    Start-Process -Wait -FilePath "C:\Program Files\Salt Project\Salt\salt-call.bat" -ArgumentList ("-l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.wsl pillar=`"{'winfor_user': '$User'}`" --log-file-level=debug --log-file=`"$wslLogFile`" --out-file=`"$wslLogFile`" --out-file-append") | Out-Null
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
    -Install      Installs the Win-FOR environment
    -User <user>  Choose the desired username for which to configure the installation
    -Mode <mode>  There are two modes to choose from for the installation:
                  addon: Install all of the tools, but don't do any customization
                  dedicated: Assumes you want the full meal-deal, will install all packages and customization
    -Update       Identifies the current version of Win-FOR and re-installs all states from that version
    -Upgrade      Identifies the latest version of Win-FOR and will install that version
    -Version      Displays the current version of Win-FOR (if installed) then exits
    -XUser        The Username for the X-Ways portal - Required to download and install X-Ways
    -XPass        The Password for the X-Ways portal - Required to download and install X-Ways - USE QUOTES
    -IncludeWsl   Will install the Windows Subsystem for Linux v2 with SIFT and REMnux toolsets
                  This option assumes you also want the full Win-FOR suite, install that first, then WSL
    -WslOnly      If you wish to only install WSLv2 with SIFT and REMnux separately, without the tools
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

if ($WslOnly) {
    $saltStatus = Test-Saltstack
    $gitStatus = Test-Git
    if ($saltStatus -ne $True) {
        Write-Host "[-] SaltStack not installed" -ForegroundColor Yellow
        Get-Saltstack
    }
    if ($gitStatus -ne $True) {
        Write-Host "[-] Git not installed" -ForegroundColor Yellow
        Get-Git
    }
    Invoke-WSLInstaller
} elseif ($Help -and $PSBoundParameters.Count -eq 1) {
    Show-WinFORHelp
} elseif ($Version -and $PSBoundParameters.Count -eq 1) {
    Get-WinFORVersion
} elseif ($PSBoundParameters.Count -eq 0) {
    Show-WinFORHelp
} elseif ($Install -or $Update -or $Upgrade) {
    Install-WinFOR
}
