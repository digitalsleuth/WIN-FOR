# WIN-FOR
Windows Forensics System Builder

The design behind this is to use a barebones Windows 10 VM or a Windows machine (preferably 1909 and higher to support WSLv2).
Once configured, and activated (to support customization features), then you can use the winfor-cli.ps1 file to
install all of the packages.

If using the PowerShell script, the install requires that the `Set-ExecutionPolicy Bypass` feature be set to allow the script to run at least twice, depending on your choices.
This is best left to you to decide what is acceptable in your organization.

If using the standalone executable script, you can execute this from and Administrator Command Prompt.

Check out the [Releases](https://github.com/digitalsleuth/WIN-FOR/releases) section at the [Win-FOR repo](https://github.com/digitalsleuth/WIN-FOR) for the most up-to-date installers.

## Usage
```text
Usage:
  .\winfor-cli.ps1 -Install -User <user> -Mode <mode> -IncludeWsl -XUser forensics -XPass "password123"
  .\winfor-cli.ps1 -Update
  .\winfor-cli.ps1 -Upgrade
  .\winfor-cli.ps1 -WslOnly
  .\winfor-cli.ps1 -Version
  .\winfor-cli.ps1 -Help

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
```

## Issues

All issues should be raised at the [WIN-FOR Repo](https://github.com/digitalsleuth/WIN-FOR)

## What the winfor-cli.ps1 and the winfor-cli.exe installers do:

Installs [Saltstack 3005.1-3](https://repo.saltproject.io/windows/Salt-Minion-3005.1-3-Py3-AMD64-Setup.exe) then installs
[Git](https://git-scm.com/download/win), then runs the following commands:
```
git clone https://github.com/digitalsleuth/winfor-salt/ "C:\ProgramData\Salt Project\Salt\srv\salt"
salt-call -l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.$mode pillar="{'winfor_user': '$user'}" --log-file="C:\winfor-saltstack.log" --log-file-level=debug --out-file="C:\winfor-saltstack.log" --out-file-append
```
