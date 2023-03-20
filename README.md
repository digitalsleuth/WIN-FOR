# Win-FOR

Windows Forensics Environment (Win-FOR) Builder

The design behind this is to use a barebones Windows 10 VM or a Windows machine (preferably 1909 and higher to support WSLv2).
Once configured, and activated (to support customization features), then you can use one of the installers to
install all of the packages.

The installation methods are:

- Win-FOR Customizer **COMING SOON**: graphical interface to click and choose which items you want, and to enter the settings you need
- winfor-cli.exe: a command-line tool, requiring execution from an Administrator command prompt
- winfor-cli.ps1: a PowerShell based tool, exactly the same as the command-line exe, but requires an Administrator PowerShell prompt and to have the `Set-ExecutionPolicy Bypass` parameter set

Check out the [Releases](https://github.com/digitalsleuth/WIN-FOR/releases) section for the most up-to-date installers.

## Win-FOR Customizer

**FIRST OFF - Requires .NET 6.0 Desktop Runtime**  
**If you do not have it, you will be prompted to install at execution**

Why a GUI? Who doesn't like a good GUI!?
Not everyone enjoys Windows command line or PowerShell, especially when just starting out in Digital Forensics.
This makes it much easier to get your environment set up without having to worry about CMD or PS!

The customizer tool gives you the following features:

- Point and click to choose which tools you want installed in your distro (instead of just choosing them all)
- Checkboxes to choose if you want the WSLv2 with SIFT and REMnux installed during the process, or click `WSL Only` to install it at a later date
- Save your current selections in a custom SaltStack State file for your own purposes or record
- Identify the current version of the Win-FOR environment with a single click
- Check for updates to the Customizer
- Graphically enter any settings you need!

**The `Install` and `WSL Only` features are only accessible if the Customizer is run as Administrator (since these need Admin privileges to execute)**

![screenshot-v6 0 0 0](https://github.com/digitalsleuth/WIN-FOR/raw/main/images/screenshot-v6.0.0.0.png)

## PowerShell or CLI

If using the PowerShell script, the install requires that the `Set-ExecutionPolicy Bypass` feature be set to allow the script to run at least twice, depending on your choices.
This is best left to you to decide what is acceptable in your organization.

If using the standalone executable script, you can execute this from an Administrator Command Prompt.

### Usage of the CLI or PowerShell script

```text
Usage (.\winfor-cli.ps1 or winfor-cli.exe):
  winfor-cli -Install -User <user> -Mode <mode> -IncludeWsl -XUser forensics -XPass "<your_password>"
  winfor-cli -Install -StandalonesPath "C:\standalones"
  winfor-cli -Update
  winfor-cli -Upgrade
  winfor-cli -WslOnly
  winfor-cli -Version
  winfor-cli -Help

Usage:
    -Install          Installs the Win-FOR environment
    -User <user>      Choose the desired username for which to configure the installation
    -Mode <mode>      There are two modes to choose from for the installation:
                      addon: Install all of the tools, but don't do any customization
                      dedicated: Assumes you want the full meal-deal, will install all packages and customization
    -StandalonesPath  Choose the path for where the standalone executables will be downloaded
    -Update           Identifies the current version of Win-FOR and re-installs all states from that version
    -Upgrade          Identifies the latest version of Win-FOR and will install that version
    -Version          Displays the current version of Win-FOR (if installed) then exits
    -XUser            The Username for the X-Ways portal - Required to download and install X-Ways
    -XPass            The Password for the X-Ways portal - Required to download and install X-Ways - USE QUOTES
    -IncludeWsl       Will install the Windows Subsystem for Linux v2 with SIFT and REMnux toolsets
                      This option assumes you also want the full Win-FOR suite, install that first, then WSL
    -WslOnly          If you wish to only install WSLv2 with SIFT and REMnux separately, without the tools
```

## Issues

All issues should be raised [here](https://github.com/digitalsleuth/WIN-FOR/Issues)

The standalone executable installer converted from PowerShell to executable using [PS2EXE](https://github.com/MScholtes/PS2EXE)
