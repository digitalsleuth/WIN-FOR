using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace WinFOR_Customizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TextBoxOutputter outputter;
        private static readonly string? appname = Assembly.GetExecutingAssembly().GetName().Name;
#pragma warning disable CS8602 // Deference of a possibly null reference.
        private static readonly Version? appversion = new(Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
#pragma warning restore CS8602 // Deference of a possibly null reference.
        readonly string git_version = "2.40.0";
        readonly string git_hash = "ff8954afb29814821e9e3759a761bdac49186085e916fa354bf8706e3c7fe7a2";
        readonly string salt_version = "3005.1-2";
        readonly string salt_hash = "fac148e51a7f0a8e836a6419102b9da93c7f16ab659f709e49e2e05713cf7cbc";
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            if (!IsAdministrator())
            {
                MessageBox.Show("In order to use the Install function of this application,\nit must be run as Administrator",
                                "Not running as Administrator - Install function disabled!",
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
                install_button.IsEnabled = false;
                install_wsl_button.IsEnabled = false;
                download_button.IsEnabled = false;
            }
            Version.Content = $"v{appversion}-rc8";
            outputter = new TextBoxOutputter(OutputConsole);
            Console.SetOut(outputter);
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.LoadFile, (sender, e) => { File_Load(); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.LoadFile, new KeyGesture(Key.L, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.SaveFile, (sender, e) => { File_Save(); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.SaveFile, new KeyGesture(Key.S, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ShowCPC, (sender, e) => { CPC_Default(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ShowCPC, new KeyGesture(Key.P, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ShowCRA, (sender, e) => { CRA_Default(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ShowCRA, new KeyGesture(Key.R, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ShowWIN, (sender, e) => { WINFOR_Default(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ShowWIN, new KeyGesture(Key.W, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.DownloadToolList, (sender, e) => { Download_ToolList(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.DownloadToolList, new KeyGesture(Key.T, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.CheckForUpdates, (sender, e) => { Check_Updates(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.CheckForUpdates, new KeyGesture(Key.U, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ShowLatest, (sender, e) => { Show_LatestRelease(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ShowLatest, new KeyGesture(Key.G, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.CheckDistroVersion, (sender, e) => { Check_DistroVersion(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.CheckDistroVersion, new KeyGesture(Key.V, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ShowAbout, (sender, e) => { Show_About(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ShowAbout, new KeyGesture(Key.A, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ToolList, (sender, e) => { Tool_List(); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ToolList, new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift)));
        }
        public static class KeyboardShortcuts
        // Setup bindings and RoutedCommands for Keyboard Shortcuts for the Menu
        {
            static KeyboardShortcuts()
            {
                LoadFile = new RoutedCommand("LoadFile", typeof(MainWindow));
                SaveFile = new RoutedCommand("SaveFile", typeof(MainWindow));
                ShowCPC = new RoutedCommand("ShowCPC", typeof(MainWindow));
                ShowCRA = new RoutedCommand("ShowCRA", typeof(MainWindow));
                ShowWIN = new RoutedCommand("ShowWIN", typeof(MainWindow));
                DownloadToolList = new RoutedCommand("DownloadToolList", typeof(MainWindow));
                CheckForUpdates = new RoutedCommand("CheckForUpdates", typeof(MainWindow));
                ShowLatest = new RoutedCommand("ShowLatest", typeof(MainWindow));
                CheckDistroVersion = new RoutedCommand("CheckDistroVersion", typeof(MainWindow));
                ShowAbout = new RoutedCommand("ShowAbout", typeof(MainWindow));
                ToolList = new RoutedCommand("ToolList", typeof(MainWindow));
            }
            public static RoutedCommand LoadFile { get; private set; }
            public static RoutedCommand SaveFile { get; private set; }
            public static RoutedCommand ShowCPC { get; private set; }
            public static RoutedCommand ShowCRA { get; private set; }
            public static RoutedCommand ShowWIN { get; private set; }
            public static RoutedCommand DownloadToolList { get; private set; }
            public static RoutedCommand CheckForUpdates { get; private set; }
            public static RoutedCommand ShowLatest { get; private set; }
            public static RoutedCommand CheckDistroVersion { get; private set; }
            public static RoutedCommand ShowAbout { get; private set; }
            public static RoutedCommand ToolList { get; private set; }
        }
        public class TextBoxOutputter : TextWriter
        // Idea for the TextBoxOutputter from https://social.technet.microsoft.com/wiki/contents/articles/12347.wpf-howto-add-a-debugoutput-console-to-your-application.aspx
        {
            TextBox textBox;
            public TextBoxOutputter(TextBox output)
            {
                textBox = output;
            }
            public override void Write(char value)
            {
                base.Write(value);
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.AppendText(value.ToString());
                    textBox.Focus();
                    textBox.CaretIndex = textBox.Text.Length;
                    textBox.ScrollToEnd();
                    textBox.IsReadOnly = true;
                }));
            }
            public override Encoding Encoding
            {
                get { return System.Text.Encoding.UTF8; }
            }
        }
        public static bool IsAdministrator()
        // Some functions require administrative privilege - Check to see if the application is run as Admin
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
            // Loop through Visual objects and find children of the item to perform operations
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
            }
        }
        public static List<T> GetLogicalChildCollection<T>(object parent) where T : DependencyObject
        {
            List<T> logicalCollection = new();
            GetLogicalChildCollection((DependencyObject)parent, logicalCollection);
            return logicalCollection;
        }
        private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
            // Loop through Logical objects (visible or not) to perform operations on them.
        {
            IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object child in children)
            {
                if (child is DependencyObject)
                {
                    DependencyObject? depChild = child as DependencyObject;
                    if (child is T t)
                    {
                        logicalCollection.Add(t);
                    }
                    GetLogicalChildCollection(depChild!, logicalCollection);
                }
            }
        }
        private void Visit_Github(object sender, RequestNavigateEventArgs e)
        // Visits the digitalsleuth/win-for GitHub Repo
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to launch process:\n{ex}");
            }
        }
        private void BtnExpand_All(object sender, RoutedEventArgs e)
        {
            Expand_All();
        }
        private void Expand_All()
        // Expands all TreeViewItems in the display
        {
            try
            {
                foreach (TreeViewItem ti in GetLogicalChildCollection<TreeViewItem>(AllTools))
                {
                    ti.IsExpanded = true;
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to Expand All:\n{ex}");
            }
        }
        private void BtnCollapse_All(object sender, RoutedEventArgs e)
        {
            Collapse_All();
        }
        private void Collapse_All()
        // Collapses all TreeViewItems in the display
        {
            try
            {
                foreach (TreeViewItem ti in GetLogicalChildCollection<TreeViewItem>(AllTools))
                {
                    ti.IsExpanded = false;
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to Collapse All:\n{ex}");
            }
        }
        private void BtnUncheck_All(object sender, RoutedEventArgs e)
        {
            UnCheck_All();
        }
        private void UnCheck_All()
        // Unchecks all CheckBoxes in the TreeView AllTools component
        {
            try
            {
                foreach (CheckBox cb in GetLogicalChildCollection<CheckBox>(AllTools))
                {
                    if (cb.IsEnabled == true)
                    {
                        cb.IsChecked = false;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to UnCheck All:\n{ex}");
            }
        }
        private void BtnCheck_All(object sender, RoutedEventArgs e)
        {
            Check_All();
        }
        private void Check_All()
        // Checks all CheckBoxes in the TreeView AllTools component
        {
            try
            {
                foreach (CheckBox cb in GetLogicalChildCollection<CheckBox>(AllTools))
                {
                    if (cb.IsEnabled == true)
                    {
                        cb.IsChecked = true;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to Check All:\n{ex}");
            }
        }
        private void CPC_Default(object sender, RoutedEventArgs e)
        // Display the default tools available in the CPC Theme
        {
            try
            {
                Expand_All();
                foreach (CheckBox cb in GetLogicalChildCollection<CheckBox>(AllTools))
                {
                    cb.IsChecked = false;
                    cb.IsEnabled = true;
                }
                List<CheckBox> cpc_items = new()
                {
                    installers_data_dump,
                    installers_dcode,
                    installers_fastcopy,
                    installers_hxd,
                    installers_irfanview_plugins,
                    installers_magnet_axiom,
                    installers_mobaxterm,
                    installers_systools_pst_viewer,
                    packages_active_disk_editor,
                    packages_apimonitor,
                    packages_autopsy,
                    packages_bulk_extractor,
                    packages_bulkrenameutility,
                    packages_burpsuite_community,
                    packages_cerbero_suite,
                    packages_chrome,
                    packages_db_browser_sqlite,
                    packages_dbeaver,
                    packages_elcomsoft_efdd,
                    packages_fiddler,
                    packages_fileinsight,
                    packages_firefox,
                    packages_free_hex_editor_neo,
                    packages_ftk_imager,
                    packages_google_earth_pro,
                    packages_hashcheck,
                    packages_httplogbrowser,
                    packages_irfanview,
                    packages_itunes,
                    packages_kernel_edb_viewer,
                    packages_kernel_ost_viewer,
                    packages_kernel_pst_viewer,
                    packages_libreoffice,
                    packages_logparser,
                    packages_magnet_acquire,
                    packages_magnet_chromebook_acquisition,
                    packages_mdf_viewer,
                    packages_monolith_notes,
                    packages_ms_powertoys,
                    packages_npp,
                    packages_nuix_evidence_mover,
                    packages_openhashtab,
                    packages_passware_encryption_analyzer,
                    packages_pdfstreamdumper,
                    packages_process_hacker,
                    packages_putty,
                    packages_sqlitestudio,
                    packages_sublime_text,
                    packages_tableau_firmware_update,
                    packages_tableau_imager,
                    packages_vcxsrv,
                    packages_veracrypt,
                    packages_virtualbox,
                    packages_vlc,
                    packages_vscode,
                    packages_wiebetech_writeblocking_validation_utility,
                    packages_wireshark,
                    python2_tools_volatility2,
                    python3_tools_aleapp,
                    python3_tools_amcache,
                    python3_tools_autotimeliner,
                    python3_tools_bitsparser,
                    python3_tools_ileapp,
                    python3_tools_iptools,
                    python3_tools_msoffcrypto_crack,
                    python3_tools_msoffcrypto_tool,
                    python3_tools_oledump,
                    python3_tools_olefile,
                    python3_tools_oletools,
                    python3_tools_pdf_parser,
                    python3_tools_pdfid,
                    python3_tools_rtfdump,
                    python3_tools_time_decode,
                    python3_tools_usbdeviceforensics,
                    python3_tools_usn_journal_parser,
                    python3_tools_vleapp,
                    python3_tools_volatility3,
                    python3_tools_wleapp,
                    python3_tools_xlmmacrodeobfuscator,
                    python3_tools_yara_python,
                    standalones_arsenal_image_mounter,
                    standalones_autorunner,
                    standalones_bintext,
                    standalones_bitrecover_eml_viewer,
                    standalones_bulkrenameutility_portable,
                    standalones_caine,
                    standalones_cyberchef,
                    standalones_eventfinder,
                    standalones_evtx_dump,
                    standalones_exiftool,
                    standalones_glossary_generator,
                    standalones_hayabusa,
                    standalones_hex2guid,
                    standalones_hibernation_recon,
                    standalones_hindsight,
                    standalones_iphone_analyzer,
                    standalones_kansa,
                    standalones_kape,
                    standalones_logfileparser,
                    standalones_logparser_studio,
                    standalones_logviewer2,
                    standalones_magnet_edd,
                    standalones_magnet_process_capture,
                    standalones_magnet_ram_capture,
                    standalones_magnet_response,
                    standalones_magnet_web_page_saver_portable,
                    standalones_megatools,
                    standalones_mftbrowser,
                    standalones_mimikatz,
                    standalones_mitec,
                    standalones_nirsoft,
                    standalones_ntcore,
                    standalones_ntfs_log_tracker,
                    standalones_offvis,
                    standalones_pilfer,
                    standalones_psdecode,
                    standalones_regripper,
                    standalones_rufus,
                    standalones_shadowexplorer,
                    standalones_silketw,
                    standalones_sleuthkit,
                    standalones_smi_parser,
                    standalones_srum_dump2,
                    standalones_sysinternals,
                    standalones_usb_write_blocker,
                    standalones_usbdetective,
                    standalones_velociraptor,
                    standalones_vssmount,
                    standalones_windowgrid,
                    standalones_winpmem,
                    standalones_wmi_parser,
                    standalones_x_ways,
                    standalones_zimmerman
                };
                foreach (CheckBox c in cpc_items)
                {
                    c.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to display CPC Default tools:\n{ex}");
            }
        }
        private void CRA_Default(object sender, RoutedEventArgs e)
        // Display the default tools available in the CRA Theme
        {
            try
            {
                Expand_All();
                foreach (CheckBox cb in GetLogicalChildCollection<CheckBox>(AllTools))
                {
                    cb.IsChecked = false;
                    cb.IsEnabled = true;
                }
                List<CheckBox> cra_items = new()
            {
                    installers_data_dump,
                    installers_dcode,
                    installers_fastcopy,
                    installers_fec,
                    installers_hxd,
                    installers_irfanview_plugins,
                    installers_magnet_axiom,
                    installers_mobaxterm,
                    installers_systools_pst_viewer,
                    packages_active_disk_editor,
                    packages_apimonitor,
                    packages_autopsy,
                    packages_bulk_extractor,
                    packages_burpsuite_community,
                    packages_cerbero_suite,
                    packages_chrome,
                    packages_db_browser_sqlite,
                    packages_dbeaver,
                    packages_fiddler,
                    packages_fileinsight,
                    packages_firefox,
                    packages_free_hex_editor_neo,
                    packages_ftk_imager,
                    packages_hashcheck,
                    packages_httplogbrowser,
                    packages_irfanview,
                    packages_kernel_edb_viewer,
                    packages_kernel_ost_viewer,
                    packages_kernel_pst_viewer,
                    packages_libreoffice,
                    packages_logparser,
                    packages_magnet_acquire,
                    packages_magnet_chromebook_acquisition,
                    packages_mdf_viewer,
                    packages_monolith_notes,
                    packages_npp,
                    packages_nuix_evidence_mover,
                    packages_openhashtab,
                    packages_passware_encryption_analyzer,
                    packages_pdfstreamdumper,
                    packages_process_hacker,
                    packages_pst_walker,
                    packages_putty,
                    packages_sqlitestudio,
                    packages_sublime_text,
                    packages_tableau_firmware_update,
                    packages_tableau_imager,
                    packages_vcxsrv,
                    packages_vlc,
                    packages_vscode,
                    packages_wireshark,
                    python2_tools_volatility2,
                    python3_tools_aleapp,
                    python3_tools_amcache,
                    python3_tools_autotimeliner,
                    python3_tools_bitsparser,
                    python3_tools_ileapp,
                    python3_tools_iptools,
                    python3_tools_msoffcrypto_crack,
                    python3_tools_msoffcrypto_tool,
                    python3_tools_oledump,
                    python3_tools_olefile,
                    python3_tools_oletools,
                    python3_tools_pdf_parser,
                    python3_tools_pdfid,
                    python3_tools_rtfdump,
                    python3_tools_time_decode,
                    python3_tools_usbdeviceforensics,
                    python3_tools_usn_journal_parser,
                    python3_tools_vleapp,
                    python3_tools_volatility3,
                    python3_tools_wleapp,
                    python3_tools_xlmmacrodeobfuscator,
                    python3_tools_yara_python,
                    standalones_arsenal_image_mounter,
                    standalones_autorunner,
                    standalones_bintext,
                    standalones_bytecode_viewer,
                    standalones_cyberchef,
                    standalones_eventfinder,
                    standalones_evtx_dump,
                    standalones_exiftool,
                    standalones_hayabusa,
                    standalones_hindsight,
                    standalones_iphone_analyzer,
                    standalones_kansa,
                    standalones_kape,
                    standalones_logfileparser,
                    standalones_logparser_studio,
                    standalones_logviewer2,
                    standalones_magnet_edd,
                    standalones_magnet_process_capture,
                    standalones_magnet_ram_capture,
                    standalones_magnet_response,
                    standalones_magnet_web_page_saver_portable,
                    standalones_megatools,
                    standalones_mftbrowser,
                    standalones_mimikatz,
                    standalones_mitec,
                    standalones_nirsoft,
                    standalones_ntfs_log_tracker,
                    standalones_officemalscanner,
                    standalones_offvis,
                    standalones_pilfer,
                    standalones_psdecode,
                    standalones_regripper,
                    standalones_rufus,
                    standalones_shadowexplorer,
                    standalones_silketw,
                    standalones_sleuthkit,
                    standalones_srum_dump2,
                    standalones_sysinternals,
                    standalones_usb_write_blocker,
                    standalones_velociraptor,
                    standalones_vssmount,
                    standalones_windowgrid,
                    standalones_winpmem,
                    standalones_wmi_parser,
                    standalones_x_ways,
                    standalones_zimmerman
            };
                foreach (CheckBox c in cra_items)
                {
                    c.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to display CRA Default tools:\n{ex}");
            }
        }
        private void WINFOR_Default(object sender, RoutedEventArgs e)
        // Since everything is available in the default Win-FOR theme, show and check everything.
        {
            try
            {
                Expand_All();
                foreach (CheckBox cb in GetLogicalChildCollection<CheckBox>(AllTools))
                {
                    cb.IsChecked = true;
                    cb.IsEnabled = true;
                }
                List<CheckBox> non_winfor_items = new()
                {

                };
                foreach (CheckBox c in non_winfor_items)
                {
                    c.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to display Win-FOR Default tools:\n{ex}");
            }
        }
        private void XWays_Checked(object sender, RoutedEventArgs e)
        // Determine if the X-Ways CheckBox is checked, and enables the user/pass boxes to enter credentials for the portal
        {
            if (XUser == null || XPass == null)
            {
                return;
            }
            XUser.IsEnabled = true;
            XPass.IsEnabled = true;
            XUserLabel.IsEnabled = true;
            XPassLabel.IsEnabled = true;
        }
        private void XWays_Unchecked(object sender, RoutedEventArgs e)
        // Determine if the X-Ways CheckBox is unchecked, and disables the user/pass boxes
        {
            if (XUser == null || XPass == null)
            {
                return;
            }
            XUser.IsEnabled = false;
            XPass.IsEnabled = false;
            XUserLabel.IsEnabled = false;
            XPassLabel.IsEnabled = false;
        }
        private void FileSave_Click(object sender, RoutedEventArgs e)
        {
            File_Save();
        }
        private void File_Save()
        // Save the selections as a Custom SaltStack state file for re-load or re-use later
        {
            try
            {
                bool is_themed = themed.IsChecked == true;
                string all_tools = Generate_State("install", is_themed);
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "SaltState File | *.sls"
                };
                if (saveFileDialog.ShowDialog() == true)
                    File.WriteAllText(saveFileDialog.FileName, all_tools);
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to launch the Save File dialog:\n{ex}");
            }
        }
        private void FileLoad_Click(object sender, RoutedEventArgs e)
        {
            File_Load();
        }
        private void File_Load()
        // Load a custom SaltStack state file (probably saved using the File_Save option) for easy install on multiple systems.
        {
            try
            {
                OpenFileDialog openFile = new()
                {
                    Filter = "SaltState File | *.sls"
                };
                if (openFile.ShowDialog() == true)
                {
                    OutputExpander.Visibility = Visibility.Visible;
                    OutputExpander.IsExpanded = true;
                    string[] custom_state = File.ReadAllLines(openFile.FileName);
                    string file = openFile.FileName;
                    Expand_All();
                    UnCheck_All();
                    List<string> listed_tools = new();
                    Console_Output($"Loading configuration from {file}");
                    int include_ln = Find_LineNumber(custom_state, "include:");
                    int nop_ln = Find_LineNumber(custom_state, "test.nop:");
                    for (int line_num = include_ln + 1; line_num < (nop_ln - 2); line_num++)
                    {
                        string line = custom_state[line_num];
                        line = line.Replace("  - winfor.", "");
                        if (line.Contains('.'))
                        {
                            line = line.Replace('.', '_');
                        }
                        if (line.Contains('-'))
                        {
                            line = line.Replace('-', '_');
                        }
                        if (line == "repos" || line == "config" || line == "cleanup")
                        {
                            continue;
                        }
                        else if (line.Contains("theme"))
                        {
                            string theme = line.Split("_")[1];
                            themed.IsChecked = true;
                            Theme.SelectedItem = FindName(theme) as ComboBoxItem;
                        }
                        else
                        {
                            listed_tools.Add(line);
                        }
                    }
                    foreach (string tool in listed_tools)
                    {
                        if (FindName(tool) is CheckBox cb)
                        {
                            cb.IsChecked = true;
                        }
                        else
                        {
                            OutputExpander.Visibility = Visibility.Visible;
                            OutputExpander.IsExpanded = true;
                            Console_Output($"{tool} is not an available option - please check your custom state and try again.");
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to display the Load File dialog:\n{ex}");
            }
        }
        private void Enable_Theme(object sender, RoutedEventArgs e)
        {
            Theme.IsEnabled = true;
        }
        private void Disable_Theme(object sender, RoutedEventArgs e)
        {
            Theme.IsEnabled = false;
            Theme.Text = "";
        }
        public string Generate_State(string state_type, bool themed_install)
        // Used to generate the data for the custom SaltStack state file
        {
            string all_tools = "";
            try
            {
                string repo = "winfor";
                foreach (TreeViewItem ti in GetLogicalChildCollection<TreeViewItem>(AllTools))
                {
                    ti.IsExpanded = true;
                }
                if (Theme.Text == "CPC-WIN")
                {
                    repo = "cpcwin";
                }
                else if (Theme.Text == "CRA-WIN")
                {
                    repo = "crawin";
                }
                else if (Theme.Text == "WIN-FOR")
                {
                    repo = "winfor";
                }
                (List<string> all_checked, _) = GetCheck_Status();
                all_checked.Sort();
                List<string> states = new();
                StringBuilder include_tool = new();
                StringBuilder require_tool = new();
                if (state_type == "install")
                {
                    include_tool.Append("include:\n");
                    include_tool.Append($"  - winfor.repos\n");
                    if (wsl.IsChecked == true || themed.IsChecked == true)
                    {
                        include_tool.Append($"  - winfor.config\n");
                    }
                    require_tool.Append($"{repo}-custom-states:\n");
                    require_tool.Append("  test.nop:\n");
                    require_tool.Append("    - require:\n");
                    require_tool.Append($"      - sls: winfor.repos\n");
                    if (wsl.IsChecked == true || themed.IsChecked == true)
                    {
                        require_tool.Append($"      - sls: winfor.config\n");
                    }
                }
                else if (state_type == "download")
                {
                    include_tool.Append("include:\n");
                    require_tool.Append("download-only-states:\n");
                    require_tool.Append("  test.nop:\n");
                    require_tool.Append("    - require:\n");
                }
                foreach (string tool in all_checked)
                {
                    int under_score = tool.IndexOf("_");
                    if (tool.Split("_")[0] == "python3" || tool.Split("_")[0] == "python2")
                    {
                        if (state_type == "install")
                        {
                            string python_tool = tool.Remove(under_score, "_".Length).Insert(under_score, "-");
                            int second_under_score = python_tool.IndexOf("_");
                            string python_val = python_tool.Remove(second_under_score, "_".Length).Insert(second_under_score, ".");
                            states.Add(python_val);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (tool == "themed" || tool == "wsl")
                    {
                        continue;
                    }
                    else
                    {
                        string not_python_val = tool.Remove(under_score, "_".Length).Insert(under_score, ".");
                        if (state_type == "download" && (not_python_val == "installers.windbg" || not_python_val == "installers.windows_sandbox"))
                        {
                            continue;
                        }
                        else
                        {
                            states.Add(not_python_val);
                        }
                    }
                }
                foreach (string selection in states)
                {
                    if (state_type == "install")
                    {
                        string result = selection.Replace("_", "-");
                        include_tool.Append($"  - winfor.{result}\n");
                        require_tool.Append($"      - sls: winfor.{result}\n");
                    }
                    else if (state_type == "download")
                    {
                        string result = selection.Replace("_", "-");
                        include_tool.Append($"  - winfor.downloads.{result}\n");
                        require_tool.Append($"      - sls: winfor.downloads.{result}\n");
                    }
                }
                if (themed_install)
                {
                    include_tool.Append($"  - winfor.theme.{repo}\n");
                    require_tool.Append($"      - sls: winfor.theme.{repo}\n");
                }
                if (state_type == "install")
                {
                    include_tool.Append($"  - winfor.cleanup\n");
                    require_tool.Append($"      - sls: winfor.cleanup\n");
                }
                string include_tools = include_tool.ToString() + "\n";
                string require_tools = require_tool.ToString().TrimEnd('\n');
                all_tools = include_tools + require_tools;
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to generate a state file:\n{ex}");

            }
            return all_tools;
        }
        public (List<string>, List<string>) GetCheck_Status()
        // Identify the status of all checkboxes - also adds the ability to grab the proper name for the tool
        // The checked_items_content is not yet in use, but is setup for future use under Tool_List
        {
            List<string> checked_items = new();
            List<string> checked_items_content = new();
            try
            {

                foreach (CheckBox cb in GetLogicalChildCollection<CheckBox>(AllTools))
                {
                    if (cb.IsChecked == true)
                    {
                        if (cb.Name.StartsWith("header_"))
                        {
                            continue;
                        }
                        else
                        {
                            checked_items.Add(cb.Name.ToString());
                            checked_items_content.Add(cb.Content.ToString()!);
                        }
                    }
                }
                if (wsl.IsChecked == true)
                {
                    checked_items.Add(wsl.Name.ToString());
                    checked_items_content.Add(wsl.Content.ToString()!);
                }
                if (themed.IsChecked == true)
                {
                    checked_items.Add(themed.Name.ToString());
                    checked_items_content.Add(themed.Content.ToString()!);
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to determine checkbox status:\n{ex}");
            }
            return (checked_items, checked_items_content);
        }
        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private async void Install_Click(object sender, RoutedEventArgs e)
        // The main function for determining the status of all fields and initiating the installation of the Win-FOR environment
        {
            try
            {
                OutputExpander.Visibility = Visibility.Visible;
                OutputExpander.IsExpanded = true;
                Console_Output($"{appname} v{appversion}");
                string drive_letter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
                string distro;
                bool is_themed;
                if (themed.IsChecked == true)
                {
                    is_themed = true;
                    if (Theme.Text == "")
                    {
                        distro = "WIN-FOR";
                    }
                    else
                    {
                        distro = Theme.Text;
                    }
                    Console_Output($"Selected theme is {distro}");
                }
                else
                {
                    is_themed = false;
                    Console_Output($"No theme has been selected.");
                }
                string current_user = Environment.UserName;
                string xways_data;
                string xways_token;
                bool xways_selected;
                string standalones_path;
                string user_name;
                bool wsl_selected;
                if (standalones_x_ways.IsChecked == true && (XUser.Text != "" || XPass.Text != ""))
                {
                    xways_data = $"{XUser.Text}:{XPass.Text}";
                    xways_token = Convert.ToBase64String(Encoding.UTF8.GetBytes(xways_data));
                    xways_selected = true;
                    Console_Output($"X-Ways is selected and credentials have been provided");
                }
                else if (standalones_x_ways.IsChecked == true && (XUser.Text == "" || XPass.Text == ""))
                {
                    MessageBox.Show("With X-Ways enabled, neither X-Ways Portal User nor X-Ways Portal Pass can be empty!",
                                    "X-Ways Portal Credentials Not Supplied",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    xways_selected = false;
                    xways_token = "TOKENPLACEHOLDER";
                    Console_Output("X-Ways is not selected and will not be downloaded / installed");
                }
                if (wsl.IsChecked == true)
                {
                    wsl_selected = true;
                }
                else
                {
                    wsl_selected = false;
                    Console_Output("WSL is not selected.");
                }
                if (UserName.Text == "")
                {
                    user_name = current_user;
                }
                else
                {
                    user_name = UserName.Text;
                }
                Console_Output($"Selected user is {user_name}");
                if (Standalones.Text != "")
                {
                    standalones_path = $@"{Standalones.Text}";
                    Console_Output($"Standalones path is {standalones_path}");
                }
                else
                {
                    standalones_path = @"C:\standalone";
                    Console_Output($"Standalones path box was empty - default will be used - {standalones_path}");
                }
                string temp_dir = $"{drive_letter}winfor-temp\\";
                List<string>? current_release_data = await Identify_Release();
                string release_version = current_release_data![0];
                string uri_zip = current_release_data[1];
                string uri_hash = current_release_data[2];
                Console_Output($"{temp_dir} is being created for temporary storage of required files");
                Create_TempDirectory(temp_dir);
                if (!Check_SaltStackInstalled(salt_version))
                {
                    Console_Output($"SaltStack {salt_version} is not installed");
                    await Download_SaltStack(temp_dir, salt_version, salt_hash);
                    await Install_Saltstack(temp_dir, salt_version);
                }
                else
                {
                    Console_Output($"SaltStack {salt_version} is already installed");
                }
                if (!Check_GitInstalled(git_version))
                {
                    Console_Output($"Git {git_version} is not installed");
                    await Download_Git(temp_dir, git_version, git_hash);
                    await Install_Git(temp_dir, git_version);
                }
                else
                {
                    Console_Output($"Git {git_version} is already installed");
                }
                string release_file = $"{temp_dir}{release_version}.zip";
                string provided_hash;
                bool hash_match;
                Console_Output($"Current release of WIN-FOR is {release_version}");
                FileInfo fi_release_file = new(release_file);
                FileInfo fi_release_hash = new($"{release_file}.sha256");
                if ((File.Exists(release_file) && File.Exists($"{release_file}.sha256")) && (fi_release_file.Length != 0 && fi_release_hash.Length != 0))
                {
                    Console_Output($"{release_file} and {release_file}.sha256 already exist and not zero-byte files.");
                    Console_Output("Comparing hashes...");
                    provided_hash = File.ReadAllText($"{temp_dir}{release_version}.zip.sha256").ToLower().Split(" ")[0];
                    hash_match = Compare_Hash(provided_hash, release_file);
                    if (hash_match)
                    {
                        Console_Output("Hashes match, continuing...");
                        Console_Output("Extracting archive...");
                    }
                    else
                    {
                        Console_Output("Hashes do not match, aborting");
                        return;
                    }
                }
                else
                {
                    Console_Output("Downloading release file and SHA256 to compare");
                    await Download_States(temp_dir, release_version, uri_zip, uri_hash);
                    Console_Output("Downloads complete...");
                    Console_Output("Comparing hashes...");
                    provided_hash = File.ReadAllText($"{temp_dir}{release_version}.zip.sha256").ToLower().Split(" ")[0];
                    hash_match = Compare_Hash(provided_hash, release_file);
                    if (hash_match)
                    {
                        Console_Output("Hashes match, continuing...");
                        Console_Output("Extracting archive...");
                    }
                    else
                    {
                        Console_Output("Hashes do not match, aborting");
                        return;
                    }
                }
                string state_file = Generate_State("install", is_themed);
                bool extracted = Extract_States(temp_dir, release_version);
                if (extracted)
                {
                    if (xways_selected)
                    {
                        Console_Output("Adding authentication token to X-Ways state");
                        Insert_XWaysToken(xways_token);
                    }
                    bool copied = Copy_CustomState(state_file);
                    if (!copied)
                    {
                        return;
                    }
                    else
                    {
                        await Execute_SaltStack(user_name, standalones_path, release_version);
                    }
                }
                if (wsl_selected)
                {
                    Console_Output("WSL is selected, and will be installed last as a system reboot is required.");
                    await Execute_Wsl(user_name, release_version, standalones_path, true);
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to complete the installation process:\n{ex}");
            }
            finally
            {
                Console_Output("SaltStack installation process completed.");
            }
        }
        public static void Create_TempDirectory(string temp_dir)
        // Creates a pre-defined temp directory to store required files
        {
            try
            {
                if (Directory.Exists(temp_dir))
                {
                    Console_Output($"Directory {temp_dir} already exists");
                    DirectoryInfo di_temp = new(temp_dir);
                    di_temp.Attributes &= ~FileAttributes.ReadOnly;
                    DirectorySecurity securityRules_temp = new();
                    securityRules_temp.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    di_temp.SetAccessControl(securityRules_temp);
                    return;
                }
                else
                {
                    DirectoryInfo di_temp = Directory.CreateDirectory(temp_dir);
                    di_temp.Attributes &= ~FileAttributes.ReadOnly;
                    DirectorySecurity securityRules_temp = new();
                    securityRules_temp.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    di_temp.SetAccessControl(securityRules_temp);
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to create temp directory {temp_dir}:\n{ex}");
                return;
            }
        }
        public static bool Check_SaltStackInstalled(string salt_version)
        // Checks if the pre-determined version of SaltStack is installed
        {
            bool salt_installed = false;
            try
            {
                const string localMachine = "HKEY_LOCAL_MACHINE";
                const string uninstallkey32 = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
                const string uninstallkey64 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
                string version32 = (string)Registry.GetValue($@"{localMachine}\{uninstallkey32}", "DisplayVersion", null)!;
                string version64 = (string)Registry.GetValue($@"{localMachine}\{uninstallkey64}", "DisplayVersion", null)!;
                if (version32 == salt_version.Split("-")[0] || version64 == salt_version.Split("-")[0])
                {
                    salt_installed = true;
                }
                else
                {
                    salt_installed = false;
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to determine if SaltStack is installed:\n{ex}");
            }
            return salt_installed;
        }
        private static bool Check_GitInstalled(string git_version)
        // Checks if the pre-determined version of Git is installed, required for the implementation of most of the Salt states
        {
            bool git_installed = false;
            try
            {
                const string localMachine = "HKEY_LOCAL_MACHINE";
                const string uninstallkey32 = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
                const string uninstallkey64 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
                string version32 = (string)Registry.GetValue($@"{localMachine}\{uninstallkey32}", "DisplayVersion", null)!;
                string version64 = (string)Registry.GetValue($@"{localMachine}\{uninstallkey64}", "DisplayVersion", null)!;
                if (version32 == git_version || version64 == git_version)
                {
                    git_installed = true;
                }
                else
                {
                    git_installed = false;
                }

            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to determine if Git is installed:\n{ex}");
            }
            return git_installed;
        }
        public static async Task File_Download(string uri, string download_location)
        // Used for standard download of the provided file (from the uri) to the provided download location
        {
            HttpClient httpClient = new();
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0");
            }
            byte[] fileBytes = await httpClient.GetByteArrayAsync(uri);
            File.WriteAllBytes(download_location, fileBytes);
        }
        private static async Task Download_SaltStack(string temp_dir, string salt_version, string salt_hash)
        // Downloads the pre-determined version of SaltStack
        {
            try
            {
                string salt_file = $"salt-{salt_version}-windows-amd64.exe";
                string uri = $"https://repo.saltproject.io/salt/py3/windows/{salt_version}/{salt_file}";
                if (!Directory.Exists(temp_dir))
                {
                    Console_Output($"{temp_dir} does not exist. Creating...");
                    Create_TempDirectory(temp_dir);
                    Console_Output($"{temp_dir} created");
                }

                if (File.Exists($"{temp_dir}{salt_file}"))
                {
                    Console_Output("Found previous download of SaltStack - comparing hash");
                    bool match = Compare_Hash(salt_hash, $"{temp_dir}{salt_file}");
                    if (match)
                    {
                        Console_Output($"Hash value for {temp_dir}{salt_file} is correct, continuing...");
                        return;
                    }
                    else
                    {
                        Console_Output("Hash values don't match, deleting existing file and downloading again...");
                        File.Delete($"{temp_dir}{salt_file}");
                    }
                }
                Console_Output($"Downloading SaltStack v{salt_version}");
                await File_Download(uri, $"{temp_dir}{salt_file}");
                Console_Output($"{salt_file} downloaded");
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to download SaltStack:\n{ex}");
                return;
            }
        }
        private static async Task Install_Saltstack(string temp_dir, string salt_version)
        // Installs the pre-determined version of SaltStack, provided it can be downloaded, or is already downloaded in the temp_dir
        {
            try
            {
                Console_Output($"Installing SaltStack v{salt_version}");
                ProcessStartInfo startInfo = new()
                {
                    FileName = $"{temp_dir}salt-{salt_version}-windows-amd64.exe",
                    Arguments = @$"/S /master=localhost /minion-name=WIN-FOR",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process process = new()
                {
                    StartInfo = startInfo
                };
                process.Start();
                Task readOutput = process.StandardOutput.ReadToEndAsync();
                Task readError = process.StandardError.ReadToEndAsync();
                await readOutput;
                await readError;
                Console_Output($"Installation of Saltstack v{salt_version} is complete");
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to install SaltStack:\n{ex}");
                return;
            }
        }
        private static async Task Download_Git(string temp_dir, string git_version, string git_hash)
        // Downloads the pre-determined version of Git
        {
            string git_file = $"Git-{git_version}-64-bit.exe";
            string uri = $"https://github.com/git-for-windows/git/releases/download/v{git_version}.windows.1/{git_file}";
            try
            {
                if (!Directory.Exists(temp_dir))
                {
                    Console_Output($"{temp_dir} does not exist. Creating...");
                    Create_TempDirectory(temp_dir);
                    Console_Output($"{temp_dir} created");
                }
                if (File.Exists($"{temp_dir}{git_file}"))
                {
                    Console_Output("Found previous download of Git - comparing hash");
                    bool match = Compare_Hash(git_hash, $"{temp_dir}{git_file}");
                    if (match)
                    {
                        Console_Output($"Hash value for {temp_dir}{git_file} is correct, continuing...");
                        return;
                    }
                    else
                    {
                        Console_Output("Hash values don't match, deleting existing file and downloading again...");
                        File.Delete($"{temp_dir}{git_file}");
                    }
                }
                Console_Output($"Downloading Git v{git_version}");
                await File_Download(uri, $"{temp_dir}{git_file}");
                Console_Output($"{git_file} downloaded");
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to download git:\n{ex}");
                return;
            }
        }
        private static async Task Install_Git(string temp_dir, string git_version)
        // Installs the pre-determined version of Git, provided it can be downloaded, or is available in the temp_dir
        {
            try
            {
                Console_Output($"Installing Git {git_version}");
                ProcessStartInfo startInfo = new()
                {
                    FileName = $"{temp_dir}Git-{git_version}-64-bit.exe",
                    Arguments = @"/VERYSILENT /NORESTART /SP- /NOCANCEL /SUPPRESSMSGBOXES",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                Process process = new()
                {
                    StartInfo = startInfo
                };
                process.Start();

                Task readOutput = process.StandardOutput.ReadToEndAsync();
                Task readError = process.StandardError.ReadToEndAsync();
                await readOutput;
                await readError;
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to install git:\n{ex}");
                return;
            }
        }
        private static async Task<List<string>> Identify_Release()
        // Identifies the most recent release of the winfor-salt states for installation
        {
            List<string> release_data = new();
            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0");
                string uri = $@"https://api.github.com/repos/digitalsleuth/winfor-salt/releases/latest";
                var result = await httpClient.GetAsync(uri);
                string data = result.Content.ReadAsStringAsync().Result;
                var json_data = JsonDocument.Parse(data);
                var release = (json_data.RootElement.GetProperty("tag_name")).ToString();
                string release_file = $"https://github.com/digitalsleuth/winfor-salt/archive/refs/tags/{release}.zip";
                string release_hash = $"https://github.com/digitalsleuth/winfor-salt/releases/download/{release}/winfor-salt-{release}.zip.sha256";
                release_data.Add(release);
                release_data.Add(release_file);
                release_data.Add(release_hash);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] Unable to identify release:\n{ex}");
            }
            return release_data;
        }
        private static async Task Download_States(string temp_dir, string current_release, string uri_zip, string uri_hash)
        // Downloads the latest winfor-salt states
        {
            try
            {
                if (!Directory.Exists(temp_dir))
                {
                    Console_Output($"Temp directory {temp_dir} does not exist, creating...");
                    Create_TempDirectory(temp_dir);
                }
                Console_Output($"Downloading {uri_zip}");
                await File_Download(uri_zip, @$"{temp_dir}\{current_release}.zip");
                Console_Output($"{uri_zip} downloaded.");
                Console_Output($"Downloading {uri_hash}");
                await File_Download(uri_hash, @$"{temp_dir}\{current_release}.zip.sha256");
                Console_Output($"{uri_hash} downloaded.");
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to download state files:\n{ex}");
                return;
            }
        }
        public static bool Compare_Hash(string hash_value, string file)
        // Used to calculate the SHA256 hash of a file, and compare it to a given hash
        {
            bool match = false;
            try
            {
                string file_hash;
                if (!File.Exists(file))
                {
                    MessageBox.Show($"{file} does not exist!");
                }
                var sha256_gen = SHA256.Create();
                var file_stream = File.OpenRead(file);
                var hash_output = sha256_gen.ComputeHash(file_stream);
                file_hash = BitConverter.ToString(hash_output).Replace("-", "").ToLowerInvariant();
                file_stream.Close();
                if (file_hash == hash_value)
                {
                    Console_Output($"File Hash: {file_hash}");
                    Console_Output($"Given Hash: {hash_value}");
                    match = true;
                }
                else
                {
                    Console_Output($"File Hash: {file_hash}");
                    Console_Output($"Given Hash: {hash_value}");
                    match = false;
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to Compare Hash:\n{ex}");
            }
            return match;
        }
        private static bool Extract_States(string temp_dir, string release)
        // Once downloaded, or available, this will extract the winfor-salt states to the required location in the Salt Project\Salt folder
        {
            bool extracted = false;
            try
            {
                string file = $"{temp_dir}{release}.zip";
                string salt_path = @"C:\ProgramData\Salt Project\Salt\";
                if (!Directory.Exists($"{salt_path}srv"))
                {
                    Directory.CreateDirectory($"{salt_path}srv");
                    Directory.CreateDirectory($@"{salt_path}srv\salt\");
                    salt_path = $@"{salt_path}srv\salt\";
                }
                else
                {
                    salt_path = @"C:\ProgramData\Salt Project\Salt\srv\salt\";
                }
                string short_release = release.TrimStart('v');
                string distro_folder = $@"{temp_dir}winfor-salt-{short_release}\winfor";
                string distro_dest = $@"{salt_path}winfor";
                Console_Output($"Extracting {file} to {temp_dir}");
                ZipFile.ExtractToDirectory(file, temp_dir, true);
                Console_Output($"Moving {distro_folder} folder to {distro_dest}");
                if (Directory.Exists(distro_dest))
                {
                    DirectoryInfo di_dest = new(distro_dest);
                    di_dest.Attributes &= ~FileAttributes.ReadOnly;
                    DirectorySecurity securityRules_dest = new();
                    securityRules_dest.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    di_dest.SetAccessControl(securityRules_dest);
                    Directory.Delete(distro_dest, true);
                }
                DirectoryInfo di_distro = new(distro_folder);
                di_distro.Attributes &= ~FileAttributes.ReadOnly;
                DirectorySecurity securityRules_distro = new();
                securityRules_distro.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                di_distro.SetAccessControl(securityRules_distro);
                Directory.Move(distro_folder, distro_dest);
                extracted = true;
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to extract states:\n{ex}");
            }
            return extracted;
        }
        private static void Insert_XWaysToken(string authtoken)
        // This function will take the provided authtoken and insert it in the required spot in the x-ways.sls State file once available.
        {
            string state_file = $@"C:\ProgramData\Salt Project\Salt\srv\salt\winfor\standalones\x-ways.sls";
            try
            {
                if (File.Exists(state_file))
                {
                    string all_text = File.ReadAllText(state_file);
                    all_text = all_text.Replace("{% set auth_token = \"TOKENPLACEHOLDER\" %}", $"{{% set auth_token = \"{authtoken}\" %}}");
                    File.WriteAllText(state_file, all_text);
                    Console_Output($"Authentication token written to {state_file}");
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to add X-Ways Token to {state_file}:\n{ex}");
                return;
            }
        }
        public static bool Copy_CustomState(string state_file)
        // A simple copy of the generated custom state_file (from the Generate_State function) to the proper location
        {
            bool copied = false;
            try
            {
                File.WriteAllText(@$"C:\ProgramData\Salt Project\Salt\srv\salt\winfor\custom.sls", state_file);
                Console_Output($"Custom state custom.sls copied to the SaltStack winfor directory");
                copied = true;
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to copy the custom state to the SaltStack winfor directory:\n{ex}");
            }
            return copied;
        }
        private async void Download_Only(object sender, RoutedEventArgs e)
        // This is used to generate a custom state for simply downloading the selected files, without any installation or modification
        {
            try
            {
                OutputExpander.Visibility = Visibility.Visible;
                OutputExpander.IsExpanded = true;
                Console_Output($"{appname} v{appversion}");
                string drive_letter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
                string state_list = Generate_State("download", false);
                string temp_dir = $"{drive_letter}winfor-temp\\";
                List<string>? current_release_data = await Identify_Release();
                string release_version = current_release_data![0];
                string uri_zip = current_release_data[1];
                string uri_hash = current_release_data[2];
                string download_path;
                if (DownloadsPath.Text == "")
                {
                    download_path = @"C:\winfor-downloads\";
                }
                else
                {
                    download_path = DownloadsPath.Text;
                }
                Console_Output($"{temp_dir} is being created for temporary storage of required files");
                Create_TempDirectory(temp_dir);
                if (!Check_SaltStackInstalled(salt_version))
                {
                    Console_Output($"SaltStack {salt_version} is not installed");
                    await Download_SaltStack(temp_dir, salt_version, salt_hash);
                    await Install_Saltstack(temp_dir, salt_version);
                }
                else
                {
                    Console_Output($"SaltStack {salt_version} is already installed");
                }
                if (!Check_GitInstalled(git_version))
                {
                    Console_Output($"Git {git_version} is not installed");
                    await Download_Git(temp_dir, git_version, git_hash);
                    await Install_Git(temp_dir, git_version);
                }
                else
                {
                    Console_Output($"Git {git_version} is already installed");
                }
                string release_file = $"{temp_dir}{release_version}.zip";
                string provided_hash;
                bool hash_match;
                Console_Output($"Current release of WIN-FOR is {release_version}");
                FileInfo fi_release_file = new(release_file);
                FileInfo fi_release_hash = new($"{release_file}.sha256");
                if ((File.Exists(release_file) && File.Exists($"{release_file}.sha256")) && (fi_release_file.Length != 0 && fi_release_hash.Length != 0))
                {
                    Console_Output($"{release_file} and {release_file}.sha256 already exist and not zero-byte files.");
                    provided_hash = File.ReadAllText($"{temp_dir}{release_version}.zip.sha256").ToLower().Split(" ")[0];
                    hash_match = Compare_Hash(provided_hash, release_file);
                    if (hash_match)
                    {
                        Console_Output("Hashes match, continuing...");
                        Console_Output("Extracting archive...");
                    }
                    else
                    {
                        Console_Output("Hashes do not match, aborting");
                        return;
                    }
                }
                else
                {
                    Console_Output("Downloading release file and SHA256 to compare");
                    await Download_States(temp_dir, release_version, uri_zip, uri_hash);
                    Console_Output("Downloads complete...");
                    Console_Output("Comparing hashes...");
                    provided_hash = File.ReadAllText($"{temp_dir}{release_version}.zip.sha256").ToLower().Split(" ")[0];
                    hash_match = Compare_Hash(provided_hash, release_file);
                    if (hash_match)
                    {
                        Console_Output("Hashes match, continuing...");
                        Console_Output("Extracting archive...");
                    }
                    else
                    {
                        Console_Output("Hashes do not match, aborting");
                        return;
                    }
                }
                bool extracted = Extract_States(temp_dir, release_version);
                if (extracted)
                {
                    Console_Output($@"State files extracted, writing winfor\downloads\init.sls");
                    File.WriteAllText($@"C:\ProgramData\Salt Project\Salt\srv\salt\winfor\downloads\init.sls", state_list);
                }
                if (File.Exists($@"C:\ProgramData\Salt Project\Salt\srv\salt\winfor\downloads\init.sls"))
                {
                    await Execute_SaltStackDownloads(release_version, download_path);
                }
                else
                {
                    Console_Output($@"winfor\downloads\init.sls not found, aborting!");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to complete the Download process:\n{ex}");
            }
        }

        private Process? saltproc;
        private Process? wslproc;
        private TaskCompletionSource<bool>? ProcHandled;
        private TaskCompletionSource<bool>? wslHandled;
        private async Task Execute_SaltStack(string username, string standalones_path, string release)
        // Generate a salt.exe process with the required arguments to install the custom salt states
        {
            ProcHandled = new TaskCompletionSource<bool>();
            string salt_exe = @"C:\Program Files\Salt Project\Salt\bin\salt.exe";
            string args = $"call -l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.custom pillar=\"{{ 'winfor_user': '{username}', 'inpath': '{standalones_path}'}}\" --out-file=\"C:\\winfor-saltstack-{release}.log\" --out-file-append --log-file=\"C:\\winfor-saltstack-{release}.log\" --log-file-level=debug";
            using (saltproc = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = salt_exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                },
            })
            {
                try
                {
                    if (File.Exists(salt_exe))
                    {
                        Console_Output(
                            $"Installing the selected states.\n" +
                            $"Executing: salt call with the following variables\n" +
                            $"  --local\n" +
                            $"  --retcode-passthrough\n" +
                            $"  --state-output=mixed\n" +
                            $"  state.sls\n" +
                            $"  winfor.custom\n" +
                            $"  pillar=\"{{ 'winfor_user': '{username}', 'inpath': '{standalones_path}'}}\"\n" +
                            $"  --out-file=\"C:\\winfor-saltstack-{release}.log\"\n" +
                            $"  --out-file-append\n" +
                            $"  --log-file=\"C:\\winfor-saltstack-{release}.log\"\n" +
                            $"  --log-file-level=debug\n"
                            );
                        saltproc.Exited += new EventHandler(Process_Exited);
                        saltproc.Start();
                        Task readOutput = saltproc.StandardOutput.ReadToEndAsync();
                        await readOutput;
                        if (saltproc.HasExited && saltproc.ExitCode != 0)
                        {
                            Console_Output("Installation has completed with errors.");
                        }
                        else if (saltproc.HasExited && saltproc.ExitCode == 0)
                        {
                            Console_Output("Installation has completed successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console_Output($"[ERROR] Unable to execute SaltStack install:\n{ex}");
                    return;
                }
                await Task.WhenAny(ProcHandled.Task, Task.Delay(3000));
            }
        }
        private async Task Execute_SaltStackDownloads(string release, string download_path)
        // Generate a salt.exe process with the required argument to simply download the selected files
        {
            ProcHandled = new TaskCompletionSource<bool>();
            string salt_exe = @"C:\Program Files\Salt Project\Salt\bin\salt.exe";
            string args = $"call -l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.downloads pillar=\"{{ 'downloads': '{download_path}'}}\" --out-file=\"C:\\winfor-saltstack-downloads-{release}.log\" --out-file-append --log-file=\"C:\\winfor-saltstack-downloads-{release}.log\" --log-file-level=debug";
            using (saltproc = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = salt_exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                },
            })
            {
                try
                {
                    if (File.Exists(salt_exe))
                    {
                        Console_Output(
                            $"Installing the selected states.\n" +
                            $"Executing: salt call with the following variables\n" +
                            $"  --local\n" +
                            $"  --retcode-passthrough\n" +
                            $"  --state-output=mixed\n" +
                            $"  state.sls\n" +
                            $"  winfor.downloads\n" +
                            $"  pillar=\"{{ 'downloads': '{download_path}'}}\"\n" +
                            $"  --out-file=\"C:\\winfor-saltstack-downloads-{release}.log\"\n" +
                            $"  --out-file-append\n" +
                            $"  --log-file=\"C:\\winfor-saltstack-downloads-{release}.log\"\n" +
                            $"  --log-file-level=debug"
                            );
                        saltproc.Exited += new EventHandler(Process_Exited);
                        saltproc.Start();
                        Task readOutput = saltproc.StandardOutput.ReadToEndAsync();
                        await readOutput;
                        if (saltproc.HasExited && saltproc.ExitCode != 0)
                        {
                            Console_Output("Download process has completed with errors.");
                        }
                        else if (saltproc.HasExited && saltproc.ExitCode == 0)
                        {
                            Console_Output("Download process has completed successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console_Output($"[ERROR] Unable to execute SaltStack download install:\n{ex}");
                    return;
                }
                await Task.WhenAny(ProcHandled.Task, Task.Delay(1000));
            }
        }
        private void Process_Exited(object sender, EventArgs e)
        // An Event Handler for tracking the Execute_SaltStack and Execute_SaltStackDownloads functions
        {
            if (sender is Process proc)
            {
                Console_Output(
                $"\nExited\t\t: {proc.ExitTime}\n" +
                $"Exit code \t: {proc.ExitCode}\n" +
                $"Elapsed time\t: {Math.Round((proc.ExitTime - proc.StartTime).TotalMilliseconds)}");
                ProcHandled?.TrySetResult(true);
            }
        }
        private async Task Execute_Wsl(string username, string release, string standalones_path, bool wait_for_salt)
        // A salt.exe process used for the installation of the Windows Subsystem for Linux v2 environment
        {
            wslHandled = new TaskCompletionSource<bool>();
            string salt_exe = @"C:\Program Files\Salt Project\Salt\bin\salt.exe";
            string args = $"call -l debug --local --retcode-passthrough --state-output=mixed state.sls winfor.wsl pillar=\"{{ 'winfor_user': '{username}', 'inpath': '{standalones_path}'}}\" --out-file=\"C:\\winfor-saltstack-{release}-wsl.log\" --out-file-append --log-file=\"C:\\winfor-saltstack-{release}-wsl.log\" --log-file-level=debug";
            using (wslproc = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = salt_exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                },
            })
            {
                try
                {
                    if (File.Exists(salt_exe))
                    {
                        if (wait_for_salt)
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        { await Task.WhenAny(ProcHandled.Task); }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        Console_Output(
                           $"Installing WSLv2.\n" +
                           $"Executing: salt call with the following variables\n" +
                           $"  --local\n" +
                           $"  --retcode-passthrough\n" +
                           $"  --state-output=mixed\n" +
                           $"  state.sls\n" +
                           $"  winfor.wsl\n" +
                           $"  pillar=\"{{ 'winfor_user': '{username}', 'inpath': '{standalones_path}'}}\"\n" +
                           $"  --out-file=\"C:\\winfor-saltstack-{release}-wsl.log\"\n" +
                           $"  --out-file-append\n" +
                           $"  --log-file=\"C:\\winfor-saltstack-{release}-wsl.log\"\n" +
                           $"  --log-file-level=debug\n"
                           );
                        wslproc.Exited += new EventHandler(WslProcess_Exited);
                        Save_ConsoleOutput("wsl", null);
                        wslproc.Start();
                        Task readOutput = wslproc.StandardOutput.ReadToEndAsync();
                        await readOutput;
                        if (wslproc.HasExited && wslproc.ExitCode != 0)
                        {
                            Console_Output("WSL installation has completed with errors.");
                        }
                        else if (wslproc.HasExited && wslproc.ExitCode == 0)
                        {
                            Console_Output("WSL installation has completed successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console_Output($"[ERROR] Unable to execute WSLv2 install:\n{ex}");
                    return;
                }
                await Task.WhenAny(wslHandled.Task, Task.Delay(3000));
            }
        }
        private void WslProcess_Exited(object sender, EventArgs e)
        // An Event Handler for tracking the Execute_Wsl process
        {
            if (sender is Process proc)
            {
                Console_Output(
                $"Exited\t\t: {proc.ExitTime}\n" +
                $"Exit code \t: {proc.ExitCode}\n" +
                $"Elapsed time\t: {Math.Round((proc.ExitTime - proc.StartTime).TotalMilliseconds)}");
                wslHandled?.TrySetResult(true);
            }
        }
        private async void Install_WslOnly(object sender, RoutedEventArgs e)
        // Used to install WSL without any other options selected.
        // This determines that all pre-reqs are met before beginning the Execute_Wsl function
        {
            try
            {
                OutputExpander.Visibility = Visibility.Visible;
                OutputExpander.IsExpanded = true;
                Console_Output($"{appname} v{appversion}");
                string drive_letter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
                string distro;
                string user_name;
                string standalones_path;
                string current_user = Environment.UserName;
                if (Theme.Text == "")
                {
                    distro = "WIN-FOR";
                }
                else
                {
                    distro = Theme.Text;
                }
                if (UserName.Text == "")
                {
                    user_name = current_user;
                }
                else
                {
                    user_name = UserName.Text;
                }
                Console_Output($"Selected user is {user_name}");
                if (Standalones.Text != "")
                {
                    standalones_path = $@"{Standalones.Text}";
                    Console_Output($"Standalones path is {standalones_path}");
                }
                else
                {
                    standalones_path = @"C:\standalone";
                    Console_Output($"Standalones path box was empty - default will be used - {standalones_path}");
                }
                string distro_file = distro.ToLower().Replace("-", "");
                string temp_dir = $"{drive_letter}winfor-temp\\";
                List<string>? current_release_data = await Identify_Release();
                string release_version = current_release_data![0];
                string uri_zip = current_release_data[1];
                string uri_hash = current_release_data[2];
                Console_Output($"{temp_dir} is being created for temporary storage of required files");
                Create_TempDirectory(temp_dir);
                if (!Check_SaltStackInstalled(salt_version))
                {
                    Console_Output($"SaltStack is being downloaded");
                    await Download_SaltStack(temp_dir, salt_version, salt_hash);
                    Console_Output("SaltStack is being installed...");
                    await Install_Saltstack(temp_dir, salt_version);
                }
                else
                {
                    Console_Output($"SaltStack {salt_version} is already installed");
                }
                if (!Check_GitInstalled(git_version))
                {
                    Console_Output($"Git is being downloaded.");
                    await Download_Git(temp_dir, git_version, git_hash);
                    Console_Output("Git is being installed...");
                    await Install_Git(temp_dir, git_version);
                }
                else
                {
                    Console_Output($"Git {git_version} is already installed");
                }
                string release_file = $"{temp_dir}{release_version}.zip";
                string provided_hash;
                bool hash_match;
                Console_Output($"Current release of {distro} is {release_version}");
                if (File.Exists(release_file) && File.Exists($"{release_file}.sha256"))
                {
                    Console_Output($"{release_file} and {release_file}.sha256 already exist!\nComparing hashes...");
                    provided_hash = File.ReadAllText($"{temp_dir}{release_version}.zip.sha256").ToLower().Split(" ")[0];
                    hash_match = Compare_Hash(provided_hash, release_file);
                    if (hash_match)
                    {
                        Console_Output("Hashes match, continuing...");
                        Console_Output("Extracting archive...");
                    }
                    else
                    {
                        Console_Output("Hashes do not match, aborting");
                        return;
                    }
                }
                else
                {
                    Console_Output("Downloading release file and SHA256 to compare");
                    await Download_States(temp_dir, release_version, uri_zip, uri_hash);
                    Console_Output("Downloads complete...");
                    Console_Output("Comparing hashes...");
                    provided_hash = File.ReadAllText($"{temp_dir}{release_version}.zip.sha256").ToLower().Split(" ")[0];
                    hash_match = Compare_Hash(provided_hash, release_file);
                    if (hash_match)
                    {
                        Console_Output("Hashes match, continuing...");
                        Console_Output("Extracting archive...");
                    }
                    else
                    {
                        Console_Output("Hashes do not match, aborting");
                        return;
                    }
                }
                bool extracted = Extract_States(temp_dir, release_version);
                if (extracted)
                {
                    await Execute_Wsl(user_name, release_version, standalones_path, false);
                }
                Console_Output("SaltStack WSL installation completed.");
            }
            catch (Exception ex)
            {
                Console_Output($"[ERROR] Unable to execute WSLv2 install:\n{ex}");
                return;
            }
        }
        private static T? FindVisualParent<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);
            if (parent == null) return null;
            var parentT = parent as T;
            return parentT ?? FindVisualParent<T>(parent);
        }
        private static T? FindLogicalParent<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = LogicalTreeHelper.GetParent(dependencyObject);
            if (parent == null) return null;
            var parentT = parent as T;
            return parentT ?? FindLogicalParent<T>(parent);
        }
        private void SectionUncheck_All(object sender, RoutedEventArgs e)
        // Used to identify the selected TreeViewItem and when its checkbox is unchecked, the CheckBox child items are also unchecked
        {
            CheckBox? header = sender as CheckBox;
            TreeViewItem tvi = FindLogicalParent<TreeViewItem>(header!)!;
            foreach (object item in tvi.Items)
            {
                if (item.GetType() == typeof(CheckBox))
                {
                    CheckBox cb = (CheckBox)item;
                    if (cb.Name.StartsWith("header_"))
                    {
                        continue;
                    }
                    else if (cb.IsEnabled == false)
                    {
                        continue;
                    }
                    else
                    {
                        cb.IsChecked = false;
                    }
                }
                else { continue; }
            }
        }
        private void SectionCheck_All(object sender, RoutedEventArgs e)
        // Used to identify the selected TreeViewItem and when its checkbox is checked, the CheckBox child items are also checked
        {
            CheckBox? header = (sender as CheckBox);
            TreeViewItem tvi = FindLogicalParent<TreeViewItem>(header!)!;
            foreach (object item in tvi.Items)
            {
                if (item.GetType() == typeof(CheckBox))
                {
                    CheckBox cb = (CheckBox)item;
                    if (cb.Name.StartsWith("header_"))
                    {
                        continue;
                    }
                    else if (cb.IsEnabled == false)
                    {
                        continue;
                    }
                    else
                    {
                        cb.IsChecked = true;
                    }
                }
                else { continue; }
            }
        }
        private async void Save_ConsoleOutput(object sender, RoutedEventArgs? e)
        // Saves the TextBox console Output for log analysis or review
        {
            OutputExpander.IsEnabled = true;
            OutputExpander.Visibility = Visibility.Visible;
            OutputExpander.IsExpanded = true;
            await Task.Delay(100);
            string dtnow = $"{DateTime.Now:yyyyMMdd-hhmmss}";
            string filename;
            if (sender.GetType() == typeof(Button))
            {
                if (OutputConsole.Text == "")
                {
                    MessageBox.Show($"Output Console contains no information - not saving output.", "Output Console Empty!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    OutputExpander.IsEnabled = false;
                    OutputExpander.Visibility = Visibility.Hidden;
                    OutputExpander.IsExpanded = false;
                    return;
                }
                else
                {
                    filename = $@"C:\winfor-customizer-output-{dtnow}.log";
                    File.WriteAllText(filename, OutputConsole.Text);
                    MessageBox.Show($"Output Console data saved to {filename}.", "Output Console Log Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (sender.GetType() == typeof(String))
            {
                if (OutputConsole.Text == "")
                {
                    OutputExpander.IsEnabled = false;
                    OutputExpander.Visibility = Visibility.Hidden;
                    OutputExpander.IsExpanded = false;
                    return;
                }
                else
                {
                    filename = $@"C:\winfor-customizer-output-{dtnow}-{sender}.log";
                    File.WriteAllText(filename, OutputConsole.Text);
                }
            }
            else
            {
                filename = $@"C:\winfor-customizer-output-{dtnow}.log";
                File.WriteAllText(filename, OutputConsole.Text);
            }
        }
        private async void Check_Updates(object sender, RoutedEventArgs e)
        // Checks for updates of the Win-FOR Customizer
        {
            try
            {
                List<string> release_data = new();
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0");
                string uri = $@"https://api.github.com/repos/digitalsleuth/win-for/releases/latest";
                var get_request = await httpClient.GetAsync(uri);
                string data = get_request.Content.ReadAsStringAsync().Result;
                var json_data = JsonDocument.Parse(data);
                string release = (json_data.RootElement.GetProperty("tag_name")).ToString();
                Version release_tag = new(release.Replace("v", ""));
                string new_release = $"https://github.com/digitalsleuth/win-for/releases/download/{release}/winfor-customizer-{release}.exe";
                string release_hash = $"https://github.com/digitalsleuth/win-for/releases/download/{release}/winfor-customizer-{release}.exe.sha256";
                if (release_tag > appversion)
                {
                    MessageBoxResult result = MessageBox.Show(
                       $"New version found: {release_tag}\n" +
                       $"Current version: {appversion}\n\n" +
                       $"Would you like to download the new version of Win-FOR Customizer?", $"New Version Found - {release_tag}", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo($"{new_release}") { UseShellExecute = true });
                    }
                }
                else if (release_tag <= appversion)
                {
                    MessageBox.Show($"No new release of Win-FOR Customizer found:\n{appversion} is the most recent release.", "No new release of Win-FOR Customizer found", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (appversion > release_tag)
                {
                    MessageBox.Show($"Lucky you! You're ahead of the times!\nVersion {appversion} is even newer than the current release!", "Where you're going, you don't need versions..", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] Unable to identify release:\n{ex}");
            }
        }
        private void Results_Button(object sender, RoutedEventArgs e)
        // Parses the available logs for the SaltStack and WSL installs to determine its summary
        {
            if (!File.Exists(@"C:\ProgramData\Salt Project\Salt\srv\salt\winfor\VERSION"))
            {
                MessageBox.Show(@"No recently downloaded release attempts found in C:\ProgramData\Salt Project\Salt\srv\salt\winfor\VERSION\", "No recent installation attempts found!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                string release_version = File.ReadAllText(@"C:\ProgramData\Salt Project\Salt\srv\salt\winfor\VERSION").TrimEnd();
                string status = "";
                string log_file = $@"C:\winfor-saltstack-{release_version}.log";
                string download_log = $@"C:\winfor-saltstack-downloads-{release_version}.log";
                string wsl_log = $@"C:\winfor-wsl.log";
                List<string> logfiles = new()
                {
                    log_file,
                    download_log,
                    wsl_log
                };
                foreach (string log in logfiles)
                {
                    if (File.Exists(log))
                    {
                        status += $"{log}\n";
                        status += "_________________________________________________\n\n";
                        status += Parse_Log(log, "Summary for");
                        status += "_________________________________________________\n\n";
                    }
                }
                if (status == "")
                {
                    status += $"No log files found for {release_version}";
                }
                MessageBox.Show($"Most recent downloaded version - {release_version}\n\n{status}", $"Results for {release_version}", MessageBoxButton.OK);
            }
        }
        private void Check_DistroVersion(object sender, RoutedEventArgs e)
        // Checks the current environment to see if Win-FOR is installed and provide its version
        {
            string drive_letter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
            string current_version;
            string msgbox_version;
            string version_file = drive_letter + "winfor-version";
            if (File.Exists(version_file))
            {
                current_version = File.ReadAllText(version_file);
                msgbox_version = current_version;
            }
            else
            {
                current_version = $"not installed.\n" +
                                  $"If you are expecting version information, you may have\n" +
                                  $"encountered errors during your last install attempt.\n\n" +
                                  $"You can check the log(s) labelled {drive_letter}winfor-saltstack-<version>.log";
                msgbox_version = "not installed.";
            }
            MessageBox.Show($"WIN-FOR is {current_version}", $"WIN-FOR {msgbox_version}", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void Show_About(object sender, RoutedEventArgs e)
        // Shows the About box
        {
            MessageBoxResult result = MessageBox.Show(
                $"{appname} v{appversion}\n" +
                $"Author: Corey Forman (digitalsleuth)\n" +
                $"Source: https://github.com/digitalsleuth/win-for\n\n" +
                $"Would you like to visit the source repo on GitHub?",
                $"{appname} v{appversion}", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo($"https://github.com/digitalsleuth/win-for") { UseShellExecute = true });
            }
        }
        public static int Find_LineNumber(string[] content, string search_term)
        // Used to identify the line number for a string of text identified in the provided content
        {
            return Array.FindIndex<string>(content, i => i.Contains(search_term));
        }
        public static List<int> Find_AllLineNumbers(string[] content, string search_term)
        // Used to identify all line numbers within the given content for the provided search_term
        {
            List<int> line_numbers = new();
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i].Contains(search_term))
                {
                    int number = Array.FindIndex(content, i, x => x == content[i]);
                    line_numbers.Add(number);
                }
            }
            return line_numbers;
        }
        private static string Parse_Log(string logfile, string search_text)
        // The function for actually parsing the log file provided and searching for the given text
        {
            StringBuilder summary = new();
            string output;
            try
            {
                if (File.Exists(logfile))
                {
                    string[] contents = File.ReadAllLines(logfile);
                    List<int> line_numbers = Find_AllLineNumbers(contents, search_text);
                    foreach (int number in line_numbers)
                    {
                        for (int line = number; line < (number + 7); line++)
                        {
                            summary.Append($"{contents[line]}\n");
                        }
                        summary.Append('\n');
                    }
                    output = summary.ToString();
                }
                else
                {
                    Console_Output($"The file {logfile} does not exist!\n");
                    output = $"The file {logfile} does not exist!\n";
                }
            }
            catch (IOException)
            {
                Console_Output($"{logfile} is being used by another process.");
                output = $"{logfile} is being used by another process.\n";
            }
            catch (Exception ex)
            {
                Console_Output($"Unable to parse the log file {logfile}:\n{ex}");
                output = $"Unable to parse the log file {logfile}\n";
            }
            return output;
        }
        private static void Console_Output(string message)
        // Function to output the given content with a date/time value in front of it for tracking events
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}");
        }
        private void Download_ToolList(object sender, RoutedEventArgs e)
        // Downloads the latest Win-FOR Tool List from GitHub which shows all tools and versions available.
        {
            Process.Start(new ProcessStartInfo($"https://github.com/digitalsleuth/WIN-FOR/raw/main/WIN-FOR-Tool-List.pdf") { UseShellExecute = true });
        }
        private void Tool_List()
        // Currently not 'implemented', but will provide the Proper Name for the tools selected
        {
            OutputExpander.IsEnabled = true;
            OutputExpander.Visibility = Visibility.Visible;
            OutputExpander.IsExpanded = true;
            (_, List<string> checked_content) = GetCheck_Status();
            //(List<string> checked_tools, List<string> checked_content) = GetCheck_Status();
            checked_content.Sort();
            //var result = checked_tools.Zip(checked_content, (a, b) => new { tool_name = a, tool_content = b });
            //Console.WriteLine($"{string.Join("\n", result)}");
            Console.WriteLine($"{string.Join("\n", checked_content)}");
        }
        private async void Show_LatestRelease(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> release_data = await Identify_Release();
                MessageBox.Show($"The latest version of Win-FOR is {release_data[0]}\nIf you wish to update, simply select your tools\nand click Install", $"{release_data[0]} is the latest version", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                OutputExpander.IsEnabled = true;
                OutputExpander.Visibility = Visibility.Visible;
                OutputExpander.IsExpanded = true;
                Console_Output($"[ERROR] Unable to determine the latest version:\n{ex}");
            }
        }
        public void Test_Button(object sender, RoutedEventArgs e)
        {
            
        }
        private void Clear_Console(object sender, RoutedEventArgs e)
        {
            OutputConsole.Clear();
            OutputExpander.IsEnabled = false;
            OutputExpander.Visibility = Visibility.Hidden;
            OutputExpander.IsExpanded = false;
        }
    }
}