## Using the Template Zip file 

The template zip file initially contains three files:
- globalsettings.ptl
	+ This contains the basic color and organization scheme of the Portals application, and where to find the Portals which are created.
- portals.ptl
	+ This is essentially a blank template for the portals which will be created during the theme installation process, based on the `init.sls` file
- init.sls
	+ This file is the core of the template process, and will need to be modified to develop the desired appearance.
	
Once you have these files, you will also need to provide your own files including the wallpaper, profile pictures, and your license for Portals.

You can get your Portals license at https://portals-app.com/. The application is free and paying for a license is optional, however I encourage you to pay for a license (you get to set the price), as it is so far one of the best desktop organization applications I've seen in a long time.

All of these files together should be zipped, **WITHOUT** a folder structure, into a zip file, preferably one with no spaces in the file name. I would recommend something like `custom-theme.zip`.

Before you add these files into your zip file, you'll need to edit the `init.sls` file.


## init.sls

This is a SaltStack state file which contains variable you can customize to your desired style. The layout and purpose of these variables are as follows:

### profile_pictures

These are the filenames (no paths required) for each of the profile picture images you will be including. These will change the picture on the logon screen, the Start Menu, and within the Settings app.

For optimal design, it is recommended that your files include the following: 
- user.png (recommended at least 500x500)
- user.bmp (recommended at least 500x500)
- user-32.png (should be 32x32)
- user-40.png (should be 40x40)
- user-48.png (should be 48x48)
- user-192.png (should be 192x192)

These should be added to the zip, and when added, the init.sls can be updated by modifying the line (making sure to use single quotes):

```yaml
{% set profile_pictures = ['user.png', 'user.bmp', 'user-32.png', 'user-40.png', 'user-48.png', 'user-192.png'] %}
```


### portal_rows & portal_columns

When your Portals layout is finalized, it will be laid out in rows and columns, and sized based on the current resolution of your system. 

These should be integers, not surrounded by quotes. Default is:

```yaml
{% set portal_rows = 3 %}
{% set portal_columns = 6 %}
```

### wallpaper_name

Use this to identify the file name for the wallpaper you provide. It can be any name you choose, but it is recommended to not have any spaces in the name. Default is 'wallpaper.png'.

```yaml
{% set wallpaper_name = 'wallpaper.png' %}
```

### shortcuts

These shortcuts represent the Folder Name (Category) and the Shortcuts to the Applications which will be added to the Folder. These are used for both the start_folders and for Portals.

You only need to put the name of the link WITHOUT THE .LNK, and its path as it exists in PROGRAMDATA\Microsoft\Windows\Start Menu\Programs. For example "Magnet ACQUIRE.lnk" exists in PROGRAMDATA\Microsoft\Windows\Start Menu\Programs\Magnet ACQUIRE\. The entry in the list will then read "Magnet ACQUIRE\Magnet ACQUIRE"

```yaml
{% set shortcuts = [('Acquisition and Analysis', ['FEX Imager','FIT','FTK Imager','Active@ Disk Editor\Active@ Disk Editor','Arsenal Image Mounter','Autopsy\Autopsy 4.21.0','Magnet AXIOM\AXIOM Examine','Magnet AXIOM\AXIOM Process','gkape','Magnet ACQUIRE\Magnet ACQUIRE','Magnet Chromebook Acquisition Assistant v1\Magnet Chromebook Acquisition Assistant v1','Magnet Web Page Saver Portable V3','OSFMount\OSFMount','Tableau\Tableau Imager\Tableau Imager','X-Ways']),
                    ('Browsers', ['Firefox','Google Chrome','Microsoft Edge']),
                    ('Databases', ['ADOQuery','DataEdit','DB Browser (SQLCipher)','DB Browser (SQLite)','DBeaver Community\DBeaver','SDBExplorer','SQLiteQuery','SQLiteStudio\SQLiteStudio','SysTools SQL MDF Viewer\SysTools SQL MDF Viewer']),
                    ('Document Analysis', ['ExifTool GUI','OffVis','PDFStreamDumper\PdfStreamDumper.exe','SSView']),
                    ('Document Viewers', ['Acrobat Reader','EZViewer','LibreOffice 7.6\LibreOffice Calc','LibreOffice 7.6\LibreOffice Impress','LibreOffice 7.6\LibreOffice Writer','LibreOffice 7.6\LibreOffice','Notepad++','Sublime Text','Visual Studio Code\Visual Studio Code']),
                    ('Email', ['Aid4Mail 5\Aid4Mail5','EHB','Email Header Analyzer - Web Based','Kernel Exchange EDB Viewer\Kernel Exchange EDB Viewer','Kernel OST Viewer\Kernel OST Viewer','Kernel Outlook PST Viewer\Kernel Outlook PST Viewer','MailView','PST Walker Software\MSG Viewer','SysTools Outlook PST Viewer\SysTools Outlook PST Viewer','BitRecover EML Viewer',"4n6 Software\\4n6 Email Forensics Wizard",'PST Walker Software\PST Walker']),
                    ('Executables', ['rohitab.com\API Monitor v2\API Monitor v2 (Alpha) 64-bit','Explorer Suite\CFF Explorer','BinText','Cutter','DIE','dotPeek64','ExeInfoPE','McAfee FileInsight\FileInsight','IDA Freeware 8.3\IDA Freeware 8.3','ILSpy','KsDumper11','Magnet Process Capture','MalCat','Explorer Suite\Tools\PE Detective','Process Hacker 2\PE Viewer','PE-Bear','PEiD','PEStudio','Portex Analyzer','PPEE','Process Hacker 2\Process Hacker 2','Regshot x64 Unicode','Rehex','Resource Hacker','Scylla x64','Explorer Suite\Signature Explorer','Explorer Suite\Task Explorer (64-bit)','Total PE 2','VB Decompiler Lite\VB Decompiler Lite','WinDbg','x64dbg','x32dbg']),
                    ('Installers', ['AutoIT Extractor','lessmsi','MSI Viewer','Py2ExeDecompiler','UniExtract']),
                    ('Logs', ['EventFinder','EZViewer','HttpLogBrowser\HttpLogBrowser','Log Parser 2.2\Log Parser 2.2','LogParser-Studio','LogViewer2']),
                    ('Mobile Analysis', ['ALEAPP-GUI','Android Studio\Android Studio','Bytecode Viewer','ILEAPP-GUI','iPhoneAnalyzer','JD-GUI','VLEAPP-GUI','VOW Software\plist Editor Pro\plist Editor Pro']),
                    ('Network', ['Burp Suite Community Edition\Burp Suite Community Edition','Fiddler Classic','IHB','NetScanner','NetworkMiner','PuTTY (64-bit)\PSFTP','PuTTY (64-bit)\PuTTY','WinSCP','Wireshark','Zui']),
                    ('Raw Parsers and Decoders', ['Bulk Extractor 1.5.5\BEViewer with Bulk Extractor 1.5.5 (64-bit)','CyberChef','Digital Detective\DataDump v2\DataDump v2.2','Digital Detective\DCode v5\DCode v5.5','HHD Hex Editor Neo\Hex Editor Neo','HEXEdit','HxD Hex Editor\HxD','JSONView','Passware\Encryption Analyzer 2023 v4\Passware Encryption Analyzer 2023 v4 (64-bit)','PhotoRec','TestDisk','Time Decode','Redline\Redline','XMLView','WinHex']),
                    ('Registry', ['RegistryExplorer','RegRipper','Regshot x64 ANSI']),
                    ('Terminals', ['Cygwin\Cygwin64 Terminal','MobaXterm\MobaXterm','Terminal','WSL','VcXsrv\XLaunch']),
                    ('Utilities', ['Agent Ransack\Agent Ransack','Aurora','Digital Detective\DCode v5\DCode v5.5','EZViewer','FastCopy','Glossary Generator','Google Earth Pro','Hasher','IrfanView\IrfanView 64 4.62','iTunes\iTunes','Monolith Notes',"Nuix\\Nuix Evidence Mover\\Nuix Evidence Mover",'Rufus','Sysinternals','Tableau\Tableau Firmware Update\Tableau Firmware Update','TeraCopy','USB Write Blocker','VeraCrypt 1.26.7\VeraCrypt','Oracle VM VirtualBox\Oracle VM VirtualBox','VideoLAN\VLC media player','CDSG\WriteBlocking Validation Utility\WriteBlocking Validation Utility','WinMerge\WinMerge']),
                    ('Windows Analysis', ['AutoRunner','Event Log Explorer','EXE','Hibernation Recon','JumpListExplorer','Live Response Collection - Cedarpelta','LogFileParser64','MFTBrowser','MFTExplorer','NirLauncher','NTFS Log Tracker','OneDriveExplorer-GUI','Redline\Redline','RegistryExplorer','RegRipper','SE','ShadowExplorer','ShellBagsExplorer','SRUM-DUMP2','ThumbCache Viewer','TimelineExplorer','USB Detective','Volatility Workbench','Windows Timeline','WLEAPP-GUI'])
                   ] %}
```


### start_folders

These will be set up in correlation with the `shortcuts` variable.
```yaml
{% set start_folders = [('01','Acquisition and Analysis'),
                        ('02','Browsers'),
                        ('03','Databases'),
                        ('04','Document Analysis'),
                        ('05','Document Viewers'),
                        ('06','Email'),
                        ('07','Executables'),
                        ('08','Installers'),
                        ('09','Logs'),
                        ('10','Mobile Analysis'),
                        ('11','Network'),
                        ('12','Raw Parsers and Decoders'),
                        ('13','Registry'),
                        ('14','Terminals'),
                        ('15','Utilities'),
                        ('16','Windows Analysis')
                       ] %}
```

The folders are set up with numerical values to ensure that, when they are created, they'll be at the top of the Start Menu. These folders will contain shortcuts to launch an application from the identified category, which are based on the shortcuts folder. 

### Final Steps

The remainder of the variables found within the State should stay as they are. Zip all of the files together as they are, and not in their own folder. Using Windows built-in zip will work fine, as will any other "zip" format.
