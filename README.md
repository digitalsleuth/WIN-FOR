
# Win-FOR

Windows Forensics (Win-FOR) Customizer

![GitHub release (with filter)](https://img.shields.io/github/v/release/digitalsleuth/win-for?style=flat&label=Latest%20Win-FOR%20Release)

The design behind this is to use a barebones Windows VM or a Windows machine (preferably Windows 11 23H2 and higher to support WSLv2).
Once configured, and activated (to support customization features), then you can use one of the installers to
install all of the packages.  

The installer is a graphical interface to click and choose which items you want, and to enter the settings you need

Check out the [Releases](https://github.com/digitalsleuth/WIN-FOR/releases) section for the most up-to-date installers.  

## Win-FOR GUI

Why a GUI? Who doesn't like a good GUI!?
Not everyone enjoys Windows command line or PowerShell, especially when just starting out in Digital Forensics.
This makes it much easier to get your environment set up without having to worry about CMD or PS!

Win-FOR tool gives you the following features:

- Point and click to choose which tools you want installed in your distro (instead of just choosing them all)
- Checkboxes to choose if you want the WSLv2 with SIFT and REMnux, just SIFT, just REMnux, Kali, or all of them installed during the process, or click `WSL Only` to install it at a later date.
- Save your current selections in a custom SaltStack State file for your own purposes or record
- Identify the current version of the Win-FOR environment with a single click
- Check for updates to the Customizer
- Graphically enter any settings you need!

![screenshot-12 0 0](https://github.com/digitalsleuth/WIN-FOR/raw/main/images/screenshot-12.0.0.png)

![screenshot-12 0 0-debloat](https://github.com/digitalsleuth/WIN-FOR/raw/main/images/screenshot-12.0.0-debloat.png)

![screenshot-12 0 0-results](https://github.com/digitalsleuth/WIN-FOR/raw/main/images/screenshot-12.0.0-results.png)


## Now with offline mode!

1. In Win-FOR, select the apps and settings you want, then click Download.
2. Visit the [win-for-offline repo](https://github.com/digitalsleuth/win-for-offline) and download the latest release.
3. Transfer your downloads and the win-for-offline installer to your offline machine.
4. Launch the win-for-offline installer and select the path where your downloaded files are, where you want standalone applications to be placed, and the desired user
5. Click Install!

# Issues

All issues should be raised [here](https://github.com/digitalsleuth/WIN-FOR/Issues)
