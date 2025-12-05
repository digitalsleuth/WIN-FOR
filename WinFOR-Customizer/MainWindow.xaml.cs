using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Win32;
using Windows.Media.SpeechRecognition;

namespace WinFORCustomizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly DispatcherTimer? elapsedTimer = new();
        private static readonly Stopwatch? stopWatch = new();
        private static string customThemeZip = "";
        private static readonly string runningUser = WindowsIdentity.GetCurrent().Name.Split("\\")[1];
        private static readonly string currentHostname = Environment.MachineName;
        private static readonly string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0";
        private static readonly string saltPath = @$"C:\ProgramData\Salt Project\Salt\srv\salt";
        private static readonly string src = "winfor";
        private static readonly string displayName = "Win-FOR";
        private static readonly string githubApi = "https://api.github.com/repos/digitalsleuth/winfor-salt/releases/latest";
        private static readonly string githubTags = "https://github.com/digitalsleuth/winfor-salt/archive/refs/tags";
        private static readonly string githubReleaseDownload = "https://github.com/digitalsleuth/winfor-salt/releases/download";
        private static readonly string githubBinaryApi = $@"https://api.github.com/repos/digitalsleuth/win-for/releases/latest";
        private static readonly string githubBinaryReleaseDownload = "https://github.com/digitalsleuth/win-for/releases/download";
        private static readonly string githubBinaryRepo = "https://github.com/digitalsleuth/win-for";
        private static readonly string toolListPdf = "https://github.com/digitalsleuth/WIN-FOR/raw/main/WIN-FOR-Tool-List.pdf";
        private static readonly string configFile = $@"https://raw.githubusercontent.com/digitalsleuth/winfor-salt/main/.config";
        private static readonly string layoutFile = $@"https://raw.githubusercontent.com/digitalsleuth/winfor-salt/main/winfor/config/layout/layout.json";
        private static readonly string themeTemplateZip = "https://github.com/digitalsleuth/WIN-FOR/raw/refs/heads/main/blank-template.zip";
#pragma warning disable CS8602 // Deference of a possibly null reference.
        private static readonly Version? appVersion = new(Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
#pragma warning restore CS8602 // Deference of a possibly null reference.
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            mainWindow.Title = $"{displayName} v{appVersion}";
            if (currentHostname.Length >= 15)
            {
                HostNamePlaceholder.FontSize = 10;
            }
            else if (currentHostname.Length >= 10 && currentHostname.Length < 15)
            {
                HostNamePlaceholder.FontSize = 11;
            }
            else
            {
                HostNamePlaceholder.FontSize = 12;
            }
            HostNamePlaceholder.Text = currentHostname;
            UserNamePlaceholder.Text = runningUser;
            Console.SetOut(new TextBoxOutputter(OutputConsole));
            elapsedTimer!.Tick += new EventHandler(ElapsedTime!);
            elapsedTimer.Interval = TimeSpan.FromSeconds(1);
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.LoadFile, (sender, e) => { FileLoad(); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.LoadFile, new KeyGesture(Key.L, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.SaveFile, (sender, e) => { FileSave(); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.SaveFile, new KeyGesture(Key.S, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.DownloadToolList, (sender, e) => { DownloadToolList(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.DownloadToolList, new KeyGesture(Key.T, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.CheckForUpdates, (sender, e) => { CheckUpdates(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.CheckForUpdates, new KeyGesture(Key.U, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ShowLatest, (sender, e) => { ShowLatestRelease(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ShowLatest, new KeyGesture(Key.G, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.CheckDistroVersion, (sender, e) => { CheckDistroVersion(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.CheckDistroVersion, new KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.FindButton, (sender, e) => { FindFunction(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.FindButton, new KeyGesture(Key.F, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ShowAbout, (sender, e) => { ShowAbout(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ShowAbout, new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ClearConsole, (sender, e) => { ClearConsole(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ClearConsole, new KeyGesture(Key.X, ModifierKeys.Control | ModifierKeys.Shift)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ToolList, (sender, e) => { ToolList(); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ToolList, new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.LocalLayout, (sender, e) => { _ = LocalLayout(); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.LocalLayout, new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Shift)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.SaveConsole, (sender, e) => { SaveConsoleOutput(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.SaveConsole, new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.ResultsOutput, (sender, e) => { ResultsButton_Click(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.ResultsOutput, new KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Shift)));
            OutputExpander.Visibility = Visibility.Visible;
            OutputExpander.IsEnabled = true;
            OutputExpander.IsExpanded = false;
            _ = GenerateTree();
        }

        public static class KeyboardShortcuts
        // Setup bindings and RoutedCommands for Keyboard Shortcuts for the Menu
        {
            static KeyboardShortcuts()
            {
                LoadFile = new RoutedCommand("LoadFile", typeof(MainWindow));
                SaveFile = new RoutedCommand("SaveFile", typeof(MainWindow));
                DownloadToolList = new RoutedCommand("DownloadToolList", typeof(MainWindow));
                CheckForUpdates = new RoutedCommand("CheckForUpdates", typeof(MainWindow));
                ShowLatest = new RoutedCommand("ShowLatest", typeof(MainWindow));
                CheckDistroVersion = new RoutedCommand("CheckDistroVersion", typeof(MainWindow));
                FindButton = new RoutedCommand("FindButton", typeof(MainWindow));
                ShowAbout = new RoutedCommand("ShowAbout", typeof(MainWindow));
                ClearConsole = new RoutedCommand("ClearConsole", typeof(MainWindow));
                ToolList = new RoutedCommand("ToolList", typeof(MainWindow));
                LocalLayout = new RoutedCommand("LocalLayout", typeof(MainWindow));
                SaveConsole = new RoutedCommand("SaveConsoleOutput", typeof(MainWindow));
                ResultsOutput = new RoutedCommand("ResultsOutput", typeof(MainWindow));
            }
            public static RoutedCommand LoadFile { get; private set; }
            public static RoutedCommand SaveFile { get; private set; }
            public static RoutedCommand DownloadToolList { get; private set; }
            public static RoutedCommand CheckForUpdates { get; private set; }
            public static RoutedCommand ShowLatest { get; private set; }
            public static RoutedCommand CheckDistroVersion { get; private set; }
            public static RoutedCommand FindButton { get; private set; }
            public static RoutedCommand ShowAbout { get; private set; }
            public static RoutedCommand ClearConsole { get; private set; }
            public static RoutedCommand ToolList { get; private set; }
            public static RoutedCommand LocalLayout { get; private set; }
            public static RoutedCommand SaveConsole { get; private set; }
            public static RoutedCommand ResultsOutput { get; private set; }
        }

    public partial class TextBoxOutputter(TextBox output) : TextWriter
        // Idea for the TextBoxOutputter from https://social.technet.microsoft.com/wiki/contents/articles/12347.wpf-howto-add-a-debugoutput-console-to-your-application.aspx
        {
            private readonly TextBox textBox = output;

            public override void Write(string? value)
            {
                if (string.IsNullOrEmpty(value)) return;

                textBox.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    textBox.AppendText(value);
                    textBox.CaretIndex = textBox.Text.Length;
                    textBox.ScrollToEnd();
                    textBox.IsReadOnly = true;
                }));
            }

            public override void WriteLine(string? value)
            {
                Write(value + Environment.NewLine);
            }

            public override Encoding Encoding => Encoding.UTF8;
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
            List<T> logicalCollection = [];
            GetLogicalChildCollection((DependencyObject)parent, logicalCollection);
            return logicalCollection;
        }
        private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
            // Loop through Logical objects (visible or not) to perform operations on them.
        {
            IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object child in children)
            {
                if (child is DependencyObject depChild)
                {
                    if (child is T t)
                    {
                        logicalCollection.Add(t);
                    }
                    GetLogicalChildCollection(depChild!, logicalCollection);
                }
            }
        }
        public static class CheckNetworkConnection
        {
            public static bool IsConnected()
            {
                // Return true if any network interface is up and not a loopback or tunnel
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Any(ni =>
                        ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
            }
        }
        private void ElapsedTime(object source, EventArgs e)
        {
            TimeSpan timeSpan = stopWatch!.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            TimerLabel.Content = elapsedTime;
        }
        private void VisitGithub(object sender, RequestNavigateEventArgs e)
        // Visits the digitalsleuth/win-for GitHub Repo
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to launch process:\n{ex}");
            }
        }

        public static HttpClient NewHttpClient()
        {
            HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            return httpClient;
        }
        private void ExpandAllBtn(object sender, RoutedEventArgs e)
        {
            ExpandAll();
        }
        public void ExpandAll()
        // Expands all TreeViewItems in the display
        {
            try
            {
                foreach (TreeViewItem ti in GetLogicalChildCollection<TreeViewItem>(AllTools))
                {
                    ti.IsExpanded = true;
                }
                ExpandCollapseTextBox.Text = "Collapse All";
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to Expand All:\n{ex}");
            }
        }
        private void CollapseAllBtn(object sender, RoutedEventArgs e)
        {
            CollapseAll();
        }
        private void CollapseAll()
        // Collapses all TreeViewItems in the display
        {
            try
            {
                foreach (TreeViewItem treeItem in GetLogicalChildCollection<TreeViewItem>(AllTools))
                {
                    treeItem.IsExpanded = false;
                }
                ExpandCollapseTextBox.Text = "Expand All";
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to Collapse All:\n{ex}");
            }
        }
        private void UncheckAllBtn(object sender, RoutedEventArgs e)
        {
            UncheckAll();
        }
        private void UncheckAll()
        // Unchecks all CheckBoxes in the TreeView AllTools component
        {
            try
            {
                foreach (CheckBox checkBox in GetLogicalChildCollection<CheckBox>(AllTools))
                {
                    if (checkBox.IsEnabled)
                    {
                        checkBox.IsChecked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to Uncheck All:\n{ex}");
            }
        }

        private void CheckUncheckAll_Click(object sender, RoutedEventArgs e)
        {
            if (CheckUncheckTextBox.Text == "Check All")
            {
                CheckAll();
                CheckUncheckTextBox.Text = "Uncheck All";
                CheckUncheckTextBox.Focus();
            }
            else if (CheckUncheckTextBox.Text == "Uncheck All")
            {
                UncheckAll();
                CheckUncheckTextBox.Text = "Check All";
                CheckUncheckTextBox.Focus();
            }
        }

        private void CheckAllBtn(object sender, RoutedEventArgs e)
        {
                CheckAll();
        }
        private void CheckAll()
        // Checks all CheckBoxes in the TreeView AllTools component
        {
            try
            {
                foreach (CheckBox checkBox in GetLogicalChildCollection<CheckBox>(AllTools))
                {
                    if (checkBox.IsEnabled)
                    {
                        checkBox.IsChecked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to Check All:\n{ex}");
            }
        }
        private void XwaysChecked(object sender, RoutedEventArgs e)
        // Determine if the X-Ways CheckBox is checked, and enable the user/pass boxes to enter credentials for the portal
        {
            if (XUser == null || XPass == null)
            {
                return;
            }
            List<CheckBox> toolList = GetLogicalChildCollection<CheckBox>(AllTools);
            CheckBox? xwaysCheckBox = toolList.FirstOrDefault(cb => cb.Name == "standalones_x_ways");
            CheckBox? xwaysTemplateCheckBox = toolList.FirstOrDefault(cb => cb.Name == "standalones_x_ways_templates");
            XUser.IsEnabled = true;
            XPass.IsEnabled = true;
            XUserLabel.IsEnabled = true;
            XPassLabel.IsEnabled = true;
            if (sender.GetType() == typeof(CheckBox))
            {
                CheckBox? obj = sender as CheckBox;
                string cbName = obj!.Name;
                if (cbName == "standalones_x_ways")
                {
                    XWays.IsChecked = true;
                    xwaysTemplateCheckBox!.IsChecked = true;
                }
                else if (cbName == "XWays")
                {
                    xwaysCheckBox!.IsChecked = true;
                    xwaysTemplateCheckBox!.IsChecked = true;
                }
            }
        }
        private void XwaysUnchecked(object sender, RoutedEventArgs e)
        // Determine if the X-Ways CheckBox is unchecked, and disable the user/pass boxes
        {
            if (XUser == null || XPass == null)
            {
                return;
            }
            List<CheckBox> toolList = GetLogicalChildCollection<CheckBox>(AllTools);
            CheckBox? xwaysCheckBox = toolList.FirstOrDefault(cb => cb.Name == "standalones_x_ways");
            CheckBox? xwaysTemplateCheckBox = toolList.FirstOrDefault(cb => cb.Name == "standalones_x_ways_templates");
            XUser.IsEnabled = false;
            XPass.IsEnabled = false;
            XUserLabel.IsEnabled = false;
            XPassLabel.IsEnabled = false;
            if (sender.GetType() == typeof(CheckBox))
            {
                CheckBox? obj = sender as CheckBox;
                string cbName = obj!.Name;
                if (cbName == "standalones_x_ways")
                {
                    XWays.IsChecked = false;
                    xwaysTemplateCheckBox!.IsChecked = false;
                }
                else
                {
                    xwaysCheckBox!.IsChecked = false;
                    xwaysTemplateCheckBox!.IsChecked = false;
                }
            }
        }
        private void FileSaveClick(object sender, RoutedEventArgs e)
        {
            FileSave();
        }
        private void FileSave()
        // Save the selections as a Custom SaltStack state file for re-load or re-use later
        {
            try
            {
                bool isThemed = themed.IsChecked == true;
                bool wslInstall = WSL.IsChecked == true;
                string allTools = GenerateState("install", isThemed, wslInstall);
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "SaltState File | *.sls"
                };
                if (saveFileDialog.ShowDialog() == true)
                    File.WriteAllText(saveFileDialog.FileName, allTools);
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to launch the Save File dialog:\n{ex}");
            }
        }
        private void FileLoadClick(object sender, RoutedEventArgs e)
        {
            FileLoad();
        }
        private void FileLoad()
        // Load a custom SaltStack state file (probably saved using the FileSave option) for easy install on multiple systems.
        {
            try
            {
                OpenFileDialog openFile = new()
                {
                    Filter = "SaltState File | *.sls"
                };
                if (openFile.ShowDialog() == true)
                {
                    string[] customState = File.ReadAllLines(openFile.FileName);
                    string file = openFile.FileName;
                    ExpandAll();
                    UncheckAll();
                    List<string> listedTools = [];
                    
                    OutputExpander.IsExpanded = true;
                    ConsoleOutput($"Loading configuration from {file}");
                    int includeLineNumber = FindLineNumber(customState, "include:");
                    int nopLineNumber = FindLineNumber(customState, "test.nop:");
                    for (int lineNumber = includeLineNumber + 1; lineNumber < (nopLineNumber - 2); lineNumber++)
                    {
                        string line = customState[lineNumber];
                        line = line.Replace($"  - {src}.", "");
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
                            listedTools.Add(line);
                        }
                    }
                    List<CheckBox> allCheckBoxes = GetLogicalChildCollection<CheckBox>(AllTools);
                    List<string> checkBoxNames = [];
                    foreach (CheckBox checkBox in allCheckBoxes)
                    {
                        checkBoxNames.Add(checkBox.Name);
                    }
                    foreach (string tool in listedTools)
                    {
                        foreach (CheckBox checkBox in allCheckBoxes)
                        {

                            if (checkBox.Name == tool)
                            {
                                checkBox.IsChecked = true;
                            }
                        }
                        if (!checkBoxNames.Contains(tool))
                        {
                            OutputExpander.IsExpanded = true;
                            ConsoleOutput($"{tool} is not, or is no longer, an available option - please check your custom state and try again. To continue using your custom state without this tool, simply remove the two lines containing that tool - one under the \"include\" heading, and one under the \"{src}-custom-states\" heading.");
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
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to display the Load File dialog:\n{ex}");
            }
        }
        private void EnableTheme(object sender, RoutedEventArgs e)
        {
            Theme.Text = "WIN-FOR";
            Theme.IsEnabled = true;
            HostName.Text = null;
            HostName.IsEnabled = true;
            HostNameLabel.IsEnabled = true;
        }
        private void DisableTheme(object sender, RoutedEventArgs e)
        {
            Theme.Text = null;
            Theme.IsEnabled = false;
            HostName.Text = null;
            HostName.IsEnabled = false;
            HostNameLabel.IsEnabled = false;
        }
        public static class ThemeChoices
        {
            public static string? SelectedTheme;
            public static string? ThemeZip;
        }
        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Theme.IsEnabled)
            {
                ThemeChoices.SelectedTheme = (Theme.SelectedItem as ComboBoxItem)?.Content.ToString();
                Theme.ToolTip = ThemeChoices.SelectedTheme;
                if (ThemeChoices.SelectedTheme == "Custom")
                {
                    ThemeChoices.ThemeZip = SelectThemeZip();
                    Theme.ToolTip = ThemeChoices.ThemeZip;
                    customThemeZip = ThemeChoices.ThemeZip;
                }
            }
        }

        private void ThemeComboBox_EnableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Theme.IsEnabled)
            {
                ThemeChoices.SelectedTheme = (Theme.SelectedItem as ComboBoxItem)?.Content.ToString();
                Theme.ToolTip = ThemeChoices.SelectedTheme;
                if (ThemeChoices.SelectedTheme == "Custom")
                {
                    ThemeChoices.ThemeZip = SelectThemeZip();
                    Theme.ToolTip = ThemeChoices.ThemeZip;
                    customThemeZip = ThemeChoices.ThemeZip;
                }
            }
        }

        private string SelectThemeZip()
        {
            string file = "";
            try
            {
                OpenFileDialog openFile = new()
                {
                    Filter = $"{displayName} Theme Zip | *.zip"
                };
                if (openFile.ShowDialog() == true)
                {
                    file = openFile.FileName;
                }
                else
                {
                    Theme.SelectedIndex = 1;
                    customThemeZip = "";
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to select the file: {ex}");
            }
            return file;
        }
        private static bool ExtractCustomTheme(string zipFile)
        {
            bool extractStatus = false;
            string themePath = @$"{saltPath}\{src}\theme\";
            string themeName = "custom-theme";
            string themeFolder = Path.Join(themePath, themeName);
            try
            {
                if (Directory.Exists($"{themeFolder}"))
                {
                    ManageDirectory(themeFolder, "delete");
                    ManageDirectory(themeFolder, "create");
                }
                else
                {
                    ManageDirectory($"{themeFolder}", "create");
                }
                ConsoleOutput($"Extracting {zipFile} to {themeFolder}");
                ZipFile.ExtractToDirectory(zipFile!, themeFolder, true);
                extractStatus = true;
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to extract custom zip file to {themeFolder}:\n{ex}");
            }
            return extractStatus;
        }
        public string GenerateState(string stateType, bool themedInstall, bool wslInstall)
        // Used to generate the data for the custom SaltStack state file
        {
            string allToolsString = "";
            try
            {
                string repo = "winfor";
                foreach (TreeViewItem treeItem in GetLogicalChildCollection<TreeViewItem>(AllTools))
                {
                    treeItem.IsExpanded = true;
                }
                if (themedInstall && ThemeChoices.SelectedTheme == "CPC-WIN")
                {
                    repo = "cpcwin";
                }
                else if (themedInstall && ThemeChoices.SelectedTheme == "WIN-FOR")
                {
                    repo = "winfor";
                }
                else if (themedInstall && ThemeChoices.SelectedTheme == "Custom")
                {
                    repo = "custom-theme";
                }
                (List<string> allChecked, _) = GetCheckStatus();
                allChecked.Sort();
                List<string> states = [];
                var order = new Dictionary<string, int>
                {
                    ["python3_"] = 0,
                    ["packages_"] = 1,
                    ["installers_"] = 2,
                    ["standalones_"] = 3,
                    ["themed"] = 4
                };

                int GetOrder(string s)
                {
                    foreach (var kv in order)
                        if (s.StartsWith(kv.Key))
                            return kv.Value;

                    return int.MaxValue;
                }
                allChecked = allChecked
                    .OrderBy(GetOrder)
                    .ThenBy(s => s)
                    .ToList();

                StringBuilder includeTool = new();
                StringBuilder requireTool = new();
                if (stateType == "install")
                {
                    includeTool.Append("include:\n");
                    includeTool.Append($"  - {src}.set-version\n");
                    includeTool.Append($"  - {src}.repos\n");
                    requireTool.Append($"{repo}-custom-states:\n");
                    requireTool.Append("  test.nop:\n");
                    requireTool.Append("    - require:\n");
                    requireTool.Append($"      - sls: {src}.set-version\n");
                    requireTool.Append($"      - sls: {src}.repos\n");
                    if (wslInstall || themedInstall)
                    {
                        includeTool.Append($"  - {src}.config\n");
                        includeTool.Append($"  - {src}.theme.{repo}.computer-name\n");
                        includeTool.Append($"  - {src}.config.debloat-windows\n");
                        requireTool.Append($"      - sls: {src}.config\n");
                        requireTool.Append($"      - sls: {src}.theme.{repo}.computer-name\n");
                        requireTool.Append($"      - sls: {src}.config.debloat-windows\n");
                    }
                }
                else if (stateType == "download")
                {
                    includeTool.Append("include:\n");
                    requireTool.Append("download-only-states:\n");
                    requireTool.Append("  test.nop:\n");
                    requireTool.Append("    - require:\n");
                }
                foreach (string tool in allChecked)
                {
                    int underScoreIndex = tool.IndexOf('_');
                    if (tool.Split("_")[0] == "python3")
                    {
                        if (stateType == "install")
                        {
                            string pythonTool = tool.Remove(underScoreIndex, "_".Length).Insert(underScoreIndex, "-");
                            int secondUnderScoreIndex = pythonTool.IndexOf('_');
                            string pythonVal = pythonTool.Remove(secondUnderScoreIndex, "_".Length).Insert(secondUnderScoreIndex, ".");
                            pythonVal = pythonVal.Replace('_', '-');
                            states.Add(pythonVal);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (tool == "themed" || tool == "WSL")
                    {
                        continue;
                    }
                    else
                    {
                        string notPythonVal = tool.Remove(underScoreIndex, "_".Length).Insert(underScoreIndex, ".");
                        notPythonVal = notPythonVal.Replace('_', '-');
                        if (stateType == "download" && (notPythonVal == "installers.windbg" || notPythonVal == "installers.windows-sandbox"))
                        {
                            continue;
                        }
                        else
                        {
                            states.Add(notPythonVal);
                        }
                    }
                }
                foreach (string selection in states)
                {
                    if (stateType == "install")
                    {
                        includeTool.Append($"  - {src}.{selection}\n");
                        requireTool.Append($"      - sls: {src}.{selection}\n");
                    }
                    else if (stateType == "download")
                    {
                        includeTool.Append($"  - {src}.downloads.{selection}\n");
                        requireTool.Append($"      - sls: {src}.downloads.{selection}\n");
                    }
                }
                if (themedInstall)
                {
                    includeTool.Append($"  - {src}.theme.{repo}\n");
                    requireTool.Append($"      - sls: {src}.theme.{repo}\n");
                }
                if (stateType == "install")
                {
                    includeTool.Append($"  - {src}.cleanup\n");
                    requireTool.Append($"      - sls: {src}.cleanup\n");
                }
                if (wslInstall && stateType == "install")
                {
                    includeTool.Append($"  - {src}.wsl\n");
                    requireTool.Append($"      - sls: {src}.wsl\n");
                }
                string include_tools = includeTool.ToString() + "\n";
                string require_tools = requireTool.ToString().TrimEnd('\n');
                allToolsString = include_tools + require_tools;
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to generate a state file:\n{ex}");

            }
            return allToolsString;
        }
        public (List<string>, List<string>) GetCheckStatus()
        // Identify the status of all checkboxes - also adds the ability to grab the proper name for the tool
        {
            List<string> checkedItems = [];
            List<string> checkedItemsContent = [];
            try
            {

                foreach (CheckBox checkBox in GetLogicalChildCollection<CheckBox>(AllTools))
                {
                    if (checkBox.IsChecked == true)
                    {
                        if (checkBox.Name.StartsWith("header_"))
                        {
                            continue;
                        }
                        else
                        {
                            checkedItems.Add(checkBox.Name.ToString());
                            checkedItemsContent.Add(checkBox.Content.ToString()!);
                        }
                    }
                }
                if (WSL.IsChecked == true)
                {
                    checkedItems.Add(WSL.Name.ToString());
                    checkedItemsContent.Add(WSL.Content.ToString()!);
                }
                if (themed.IsChecked == true)
                {
                    checkedItems.Add(themed.Name.ToString());
                    checkedItemsContent.Add(themed.Content.ToString()!);
                }
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to determine checkbox status:\n{ex}");
            }
            return (checkedItems, checkedItemsContent);
        }
        private void FileExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private static void ManageDirectory(string dirToManage, string dirOperation, string destDir = "")
        {
            var identity = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            try
            {
                if (dirOperation == "create")
                {
                    Directory.CreateDirectory(dirToManage);
                    DirectoryInfo manageDirInfo = new(dirToManage);
                    manageDirInfo.Attributes &= ~FileAttributes.ReadOnly;
                    DirectorySecurity destSecurityRules = new();
                    destSecurityRules.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    manageDirInfo.SetAccessControl(destSecurityRules);
                }
                else if (dirOperation == "delete")
                {
                    DirectoryInfo manageDirInfo = new(dirToManage);
                    manageDirInfo.Attributes &= ~FileAttributes.ReadOnly;
                    DirectorySecurity destSecurityRules = new();
                    destSecurityRules.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    manageDirInfo.SetAccessControl(destSecurityRules);
                    FileInfo[] fileNames = manageDirInfo.GetFiles("*", SearchOption.AllDirectories);
                    DirectoryInfo[] directoryNames = manageDirInfo.GetDirectories("*", SearchOption.AllDirectories);
                    foreach (DirectoryInfo directoryName in directoryNames)
                    {
                        directoryName.Attributes &= ~FileAttributes.ReadOnly;
                    }
                    foreach (FileInfo fileName in fileNames)
                    {
                        fileName.Attributes &= ~FileAttributes.ReadOnly;
                        fileName.Delete();
                    }
                    Directory.Delete(dirToManage, true);
                }
                else if (dirOperation == "perms")
                {
                    DirectoryInfo manageDirInfo = new(dirToManage);
                    manageDirInfo.Attributes &= ~FileAttributes.ReadOnly;
                    DirectorySecurity destSecurityRules = new();
                    destSecurityRules.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    manageDirInfo.SetAccessControl(destSecurityRules);
                }
                else if (dirOperation == "move" && destDir != "")
                {
                    DirectoryInfo manageDirInfo = new(dirToManage);
                    manageDirInfo.Attributes &= ~FileAttributes.ReadOnly;
                    DirectorySecurity destSecurityRules = new();
                    destSecurityRules.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    manageDirInfo.SetAccessControl(destSecurityRules);
                    Directory.Move(dirToManage, destDir);
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to manage directory {dirToManage}:\n{ex}");
                return;
            }

        }
        private async void InstallClick(object sender, RoutedEventArgs e)
        // The main function for determining the status of all fields and initiating the installation of the selected environment
        {
            try
            {
                stopWatch?.Reset();
                stopWatch?.Start();
                elapsedTimer?.Start();
                bool Connected = CheckNetworkConnection.IsConnected();
                if (!Connected)
                {
                    OutputExpander.IsExpanded = true;
                    ConsoleOutput("[ERROR] No network connection detected - Please check your network connection and try the Install process again.");
                    return;
                }
                else 
                {
                    OutputExpander.IsExpanded = true;
                    ConsoleOutput("Network connection detected, continuing...");
                }
                (List<string> checkedItems, _) = GetCheckStatus();
                if (checkedItems.Count == 0)
                {
                    MessageBox.Show("No items selected! Choose at least one item to install.", "No items selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else if ((checkedItems.Count == 1) && (checkedItems.Contains("themed") || checkedItems.Contains("wsl")))
                {
                    string item = char.ToUpper(checkedItems[0][0]) + checkedItems[0][1..];
                    if (item == "Wsl")
                    {
                        MessageBox.Show($"Only {item} checkbox was selected!\nChoose at least one item from the tool list to install.\nTo install WSL only, use the \"WSL Only\" button on the side.", $"Only {item} checkbox selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Only {item} checkbox was selected!\nChoose at least one item from the tool list to install.", $"Only {item} checkbox selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
                else if ((checkedItems.Count == 2) && (checkedItems.Contains("themed") && checkedItems.Contains("wsl")))
                {
                    (string item0, string item1) = (checkedItems[0], checkedItems[1]);
                    item0 = char.ToUpper(item0[0]) + item0[1..];
                    item1 = char.ToUpper(item1[0]) + item1[1..];
                    MessageBox.Show($"Only {item0} and {item1} were selected!\nChoose at least one item from the tool list to install.\nTo install WSL only, use the \"WSL Only\" button on the side.",
                                    $"Only {item0} and {item1} checkboxes selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"{displayName} v{appVersion}");
                string driveLetter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
                string distro;
                string repo;
                bool isThemed;
                string currentUser = runningUser;
                string xwaysData;
                string xwaysToken;
                bool xwaysSelected;
                string standalonesPath;
                string userName;
                bool wslSelected;
                string hostName;
                bool statesExtracted;
                List<string>? debloatOptions = DebloatWindow.DebloatSettings.Selections;
                List<ConfigItems> softwareConfig = await GetJsonConfig();
                string? gitVersion = softwareConfig[0].Software!["Git"].SoftwareVersion!;
                string? gitHash = softwareConfig[0].Software!["Git"].SoftwareHash!;
                string? saltVersion = softwareConfig[0].Software!["SaltStack"].SoftwareVersion!;
                string? saltHash = softwareConfig[0].Software!["SaltStack"].SoftwareHash!;
                if (XWays.IsChecked == true && (XUser.Text != "" || XPass.Text != ""))
                {
                    xwaysData = $"{XUser.Text}:{XPass.Text}";
                    xwaysToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(xwaysData));
                    xwaysSelected = true;
                    ConsoleOutput($"X-Ways is selected and credentials have been provided");
                }
                else if (XWays.IsChecked == true && (XUser.Text == "" || XPass.Text == ""))
                {
                    ConsoleOutput("With X-Ways enabled, neither X-Ways Portal User nor X-Ways Portal Pass can be empty!");
                    MessageBox.Show("With X-Ways enabled, neither X-Ways Portal User nor X-Ways Portal Pass can be empty!",
                                    "X-Ways Portal Credentials Not Supplied",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    xwaysSelected = false;
                    xwaysToken = "TOKENPLACEHOLDER";
                    ConsoleOutput("X-Ways is not selected and will not be downloaded / installed");
                }
                if (themed.IsChecked == true)
                {
                    isThemed = true;
                    distro = ThemeChoices.SelectedTheme!;
                    ConsoleOutput($"Selected theme is {distro}");
                }
                else
                {
                    isThemed = false;
                    ConsoleOutput($"No theme has been selected.");
                }
                if (WSL.IsChecked == true)
                {
                    wslSelected = true;
                    MessageBoxResult result = MessageBox.Show("WSL installation will require a reboot! Ensure that you save any open documents, then click OK to continue.","WSL requires a reboot!",MessageBoxButton.OKCancel,MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Cancel) 
                    {
                        return;
                    }
                }
                else
                {
                    wslSelected = false;
                    ConsoleOutput("WSL is not selected.");
                }
                if (UserName.Text == "")
                {
                    userName = currentUser;
                }
                else
                {
                    userName = UserName.Text;
                }
                ConsoleOutput($"Selected user is {userName}");
                if (StandalonesPath.Text != "")
                {
                    standalonesPath = $@"{StandalonesPath.Text}";
                    ConsoleOutput($"Standalones path is {standalonesPath}");
                }
                else
                {
                    standalonesPath = @$"C:\standalone";
                    ConsoleOutput($"Standalones path box was empty - default will be used - {standalonesPath}");
                }
                string tempDir = @$"{driveLetter}{src}-temp\";
                List<string>? currentReleaseData = await IdentifyRelease();
                string releaseVersion = currentReleaseData![0];
                string uriZip = currentReleaseData[1];
                string uriHash = currentReleaseData[2];
                ConsoleOutput($"{tempDir} is being created for temporary storage of required files");
                if (!Directory.Exists(tempDir))
                {
                    ManageDirectory(tempDir, "create");
                }
                else
                {
                    ManageDirectory(tempDir, "perms");
                }
                if (!CheckGitInstalled(gitVersion))
                {
                    ConsoleOutput($"Git {gitVersion} is not installed");
                    await DownloadGit(tempDir, gitVersion, gitHash);
                    await InstallGit(tempDir, gitVersion);
                }
                else
                {
                    ConsoleOutput($"Git {gitVersion} is already installed");
                }
                if (!CheckSaltStackInstalled(saltVersion))
                {
                    ConsoleOutput($"SaltStack {saltVersion} is not installed");
                    await DownloadSaltStack(tempDir, saltVersion, saltHash);
                    await InstallSaltStack(tempDir, saltVersion);
                }
                else
                {
                    ConsoleOutput($"SaltStack {saltVersion} is already installed");
                }
                string releaseFile = $"{tempDir}{releaseVersion}.zip";
                string providedHash;
                bool hashMatch;
                ConsoleOutput($"Current release of WIN-FOR is {releaseVersion}");
                FileInfo releaseFileFileInfo = new(releaseFile);
                FileInfo releaseHashFileInfo = new($"{releaseFile}.sha256");
                if ((File.Exists(releaseFile) && File.Exists($"{releaseFile}.sha256")) && (releaseFileFileInfo.Length != 0 && releaseHashFileInfo.Length != 0))
                {
                    ConsoleOutput($"{releaseFile} and {releaseFile}.sha256 already exist and not zero-byte files.");
                    ConsoleOutput("Comparing hashes...");
                    providedHash = (await File.ReadAllTextAsync($"{tempDir}{releaseVersion}.zip.sha256")).ToLower().Split(" ")[0];
                    hashMatch = CompareHash(providedHash, releaseFile);
                    if (hashMatch)
                    {
                        ConsoleOutput("Hashes match, continuing...");
                        ConsoleOutput("Extracting archive...");
                    }
                    else
                    {
                        ConsoleOutput("Hashes do not match, aborting");
                        return;
                    }
                }
                else
                {
                    ConsoleOutput("Downloading release file and SHA256 to compare");
                    bool status = await DownloadStates(tempDir, releaseVersion, uriZip, uriHash);
                    if (!status)
                    {
                        ConsoleOutput("[ERROR] Unable to download required Salt states.");
                        return;
                    }
                    ConsoleOutput("Downloads complete...");
                    ConsoleOutput("Comparing hashes...");
                    providedHash = (await File.ReadAllTextAsync($"{tempDir}{releaseVersion}.zip.sha256")).ToLower().Split(" ")[0];
                    hashMatch = CompareHash(providedHash, releaseFile);
                    if (hashMatch)
                    {
                        ConsoleOutput("Hashes match, continuing...");
                        ConsoleOutput("Extracting archive...");
                    }
                    else
                    {
                        ConsoleOutput("Hashes do not match, aborting");
                        return;
                    }
                }
                ConsoleOutput("Checking for and removing previous repo folder");
                if (Directory.Exists(@$"{saltPath}\win\"))
                {
                    ManageDirectory(@$"{saltPath}\win\", "delete");
                }
                string stateFile = GenerateState("install", isThemed, wslSelected);
                statesExtracted = ExtractStates(tempDir, releaseVersion);
                if (statesExtracted)
                {
                    if (debloatOptions is not null)
                    {
                        await File.WriteAllTextAsync(@$"{saltPath}\{src}\config\debloat.preset", string.Join("\n", debloatOptions));
                    }
                    if (xwaysSelected)
                    {
                        ConsoleOutput("Adding authentication token to X-Ways state");
                        InsertXwaysToken(xwaysToken, true);
                    }
                    if (isThemed)
                    {
                        string layout = await GenerateLayout(standalonesPath);
                        await File.WriteAllTextAsync(@$"{saltPath}\{src}\config\layout\WIN-FOR-StartLayout.xml", layout);
                        if (ThemeChoices.SelectedTheme == "Custom" && customThemeZip != "") 
                        {
                            ExtractCustomTheme(customThemeZip);
                        }
                        if (HostName.Text != "")
                        {
                            hostName = HostName.Text;
                            repo = Theme.Name;
                            InsertHostName(hostName, repo);
                        }
                        else
                        {
                            hostName = currentHostname;
                            repo = Theme.Name;
                            InsertHostName(hostName, repo);
                        }
                    }
                    bool copied = CopyCustomState(stateFile);
                    if (!copied)
                    {
                        return;
                    }
                    else
                    {
                        await ExecuteSaltStack(userName, standalonesPath, releaseVersion);
                        await File.WriteAllTextAsync(@$"C:\{src}-version", releaseVersion);
                    }
                }
                if (wslSelected)
                {
                    ConsoleOutput("WSL is selected, and will be installed last as a system reboot is required.");
                    await ExecuteWsl(userName, releaseVersion, standalonesPath, true);
                }
                stopWatch?.Stop();
                elapsedTimer?.Stop();
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to complete the installation process:\n{ex}");
            }
        }
        private static bool CheckSaltStackInstalled(string saltVersion)
        // Checks if the pre-determined version of SaltStack is installed
        {
            bool saltInstalled = false;
            try
            {
                const string localMachine = "HKEY_LOCAL_MACHINE";
                const string uninstallkey32 = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
                const string uninstallkey64 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
                string version32 = (string)Registry.GetValue($@"{localMachine}\{uninstallkey32}", "DisplayVersion", null)!;
                string version64 = (string)Registry.GetValue($@"{localMachine}\{uninstallkey64}", "DisplayVersion", null)!;
                if (version32 == saltVersion.Split("-")[0] || version64 == saltVersion.Split("-")[0])
                {
                    saltInstalled = true;
                }
                else
                {
                    saltInstalled = false;
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to determine if SaltStack is installed:\n{ex}");
            }
            return saltInstalled;
        }
        private static bool CheckGitInstalled(string gitVersion)
        // Checks if the pre-determined version of Git is installed, required for the implementation of most of the Salt states
        {
            bool gitInstalled = false;
            try
            {
                const string localMachine = "HKEY_LOCAL_MACHINE";
                const string uninstallkey32 = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
                const string uninstallkey64 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
                string version32 = (string)Registry.GetValue($@"{localMachine}\{uninstallkey32}", "DisplayVersion", null)!;
                string version64 = (string)Registry.GetValue($@"{localMachine}\{uninstallkey64}", "DisplayVersion", null)!;
                gitVersion = gitVersion.Split(".windows")[0];
                if (version32 == gitVersion || version64 == gitVersion)
                {
                    gitInstalled = true;
                }
                else
                {
                    gitInstalled = false;
                }

            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to determine if Git is installed:\n{ex}");
            }
            return gitInstalled;
        }
        public static async Task<bool> FileDownload(string uri, string downloadLocation)
        // Used for standard download of the provided file (from the uri) to the provided download location
        {
            try
            {
                HttpClient httpClient = NewHttpClient();
                HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                ConsoleOutput($"Response code {(int)response.StatusCode} - {response.StatusCode}");
                response.EnsureSuccessStatusCode();
                HttpContent content = response.Content;
                var fileBytes = await content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(downloadLocation, fileBytes);
                return true;
            }
            catch (HttpRequestException requestException)
            {
                ConsoleOutput($"[ERROR] Unable to access {uri}: Received response - {requestException.StatusCode}: {requestException.Message}");
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                ConsoleOutput($"[ERROR] There was no response from the server or the download was canceled: Check your connection and try again.");
                return false;
            }
        }
        private static async Task DownloadSaltStack(string tempDir, string saltVersion, string saltHash)
        // Downloads the pre-determined version of SaltStack
        {
            try
            {
                string saltFile = $"Salt-Minion-{saltVersion}-Py3-AMD64-Setup.exe";
                string uri = $"https://packages.broadcom.com/artifactory/saltproject-generic/windows/{saltVersion}/{saltFile}";
                if (!Directory.Exists(tempDir))
                {
                    ConsoleOutput($"{tempDir} does not exist. Creating...");
                    ManageDirectory(tempDir, "create");
                    ConsoleOutput($"{tempDir} created");
                }
                else
                {
                    ManageDirectory(tempDir, "perms");
                }
                if (File.Exists($"{tempDir}{saltFile}"))
                {
                    ConsoleOutput("Found previous download of SaltStack - comparing hash");
                    bool matchExisting = CompareHash(saltHash, $"{tempDir}{saltFile}");
                    if (matchExisting)
                    {
                        ConsoleOutput($"Hash value for {tempDir}{saltFile} is correct, continuing...");
                        return;
                    }
                    else
                    {
                        ConsoleOutput("Hash values don't match, deleting existing file and downloading again...");
                        File.Delete($"{tempDir}{saltFile}");
                    }
                }
                ConsoleOutput($"Downloading SaltStack v{saltVersion}");
                bool status = await FileDownload(uri, $"{tempDir}{saltFile}");
                if (!status)
                {
                    ConsoleOutput("[ERROR] Unable to download SaltStack - an issue occurred with internet connectivity. Check your connection and try again.");
                    return;
                }
                ConsoleOutput($"{saltFile} downloaded");
                bool matchDownloaded = CompareHash(saltHash, $"{tempDir}{saltFile}");
                if (matchDownloaded)
                {
                    ConsoleOutput($"Hash value for {tempDir}{saltFile} is correct, continuing...");
                    return;
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to download SaltStack:\n{ex}");
            }
        }
        private static async Task InstallSaltStack(string tempDir, string saltVersion)
        // Installs the pre-determined version of SaltStack, provided it can be downloaded, or is already downloaded in the tempDir
        {
            try
            {
                ConsoleOutput($"Installing SaltStack v{saltVersion}");
                ProcessStartInfo startInfo = new()
                {
                    FileName = $"{tempDir}Salt-Minion-{saltVersion}-Py3-AMD64-Setup.exe",
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
                ConsoleOutput($"Installation of Saltstack v{saltVersion} is complete");
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to install SaltStack:\n{ex}");
                return;
            }
        }
        private static async Task DownloadGit(string tempDir, string gitVersion, string gitHash)
        // Downloads the pre-determined version of Git
        {
            string coreVersion = gitVersion.Split(".windows")[0];
            string winVersion = gitVersion.Split(".windows")[1];
            string gitFile = $"Git-{coreVersion}-64-bit.exe";
            string uri = $"https://github.com/git-for-windows/git/releases/download/v{coreVersion}.windows{winVersion}/{gitFile}";
            try
            {
                if (!Directory.Exists(tempDir))
                {
                    ConsoleOutput($"{tempDir} does not exist. Creating...");
                    ManageDirectory(tempDir, "create");
                    ConsoleOutput($"{tempDir} created");
                }
                else
                {
                    ManageDirectory(tempDir, "perms");
                }
                if (File.Exists($"{tempDir}{gitFile}"))
                {
                    ConsoleOutput("Found previous download of Git - comparing hash");
                    bool matchExisting = CompareHash(gitHash, $"{tempDir}{gitFile}");
                    if (matchExisting)
                    {
                        ConsoleOutput($"Hash value for {tempDir}{gitFile} is correct, continuing...");
                        return;
                    }
                    else
                    {
                        ConsoleOutput("Hash values don't match, deleting existing file and downloading again...");
                        File.Delete($"{tempDir}{gitFile}");
                    }
                }
                ConsoleOutput($"Downloading Git v{coreVersion}");
                bool status = await FileDownload(uri, $"{tempDir}{gitFile}");
                if (!status)
                {
                    ConsoleOutput("[ERROR] Unable to download Git - an issue occurred with internet connectivity. Check your connection and try again.");
                    return;
                }
                ConsoleOutput($"{gitFile} downloaded");
                bool matchDownload = CompareHash(gitHash, $"{tempDir}{gitFile}");
                if (matchDownload)
                {
                    ConsoleOutput($"Hash value for {tempDir}{gitFile} is correct, continuing...");
                    return;
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to download git:\n{ex}");
            }
        }
        private static async Task InstallGit(string tempDir, string gitVersion)
        // Installs the pre-determined version of Git, provided it can be downloaded, or is available in the tempDir
        {
            gitVersion = gitVersion.Split(".windows")[0];
            try
            {
                ConsoleOutput($"Installing Git {gitVersion}");
                ProcessStartInfo startInfo = new()
                {
                    FileName = $"{tempDir}Git-{gitVersion}-64-bit.exe",
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
                ConsoleOutput($"Installation of Git v{gitVersion} is complete");
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to install git:\n{ex}");
                return;
            }
        }
        private static async Task<List<string>> IdentifyRelease()
        // Identifies the most recent release of the salt states for installation
        {
            List<string> releaseData = [];
            try
            {
                CancellationTokenSource cancellationToken = new(new TimeSpan(0, 0, 200));
                HttpClient httpClient = NewHttpClient();
                string uri = $@"{githubApi}";
                var result = await httpClient.GetAsync(uri, cancellationToken.Token);
                string data = result.Content.ReadAsStringAsync().Result;
                var jsonData = JsonDocument.Parse(data);
                var release = (jsonData.RootElement.GetProperty("tag_name")).ToString();
                string releaseFile = $"{githubTags}/{release}.zip";
                string releaseHash = $"{githubReleaseDownload}/{release}/{src}-salt-{release}.zip.sha256";
                releaseData.Add(release);
                releaseData.Add(releaseFile);
                releaseData.Add(releaseHash);
                cancellationToken.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] Unable to identify release:\n{ex}");
            }
            return releaseData;
        }
        private static async Task<bool> DownloadStates(string tempDir, string currentRelease, string uriZip, string uriHash)
        // Downloads the latest salt states
        {
            try
            {
                if (!Directory.Exists(tempDir))
                {
                    ConsoleOutput($"Temp directory {tempDir} does not exist, creating...");
                    ManageDirectory(tempDir, "create");
                }
                else
                {
                    ManageDirectory(tempDir, "perms");
                }
                ConsoleOutput($"Downloading {uriZip}");
                bool zipStatus = await FileDownload(uriZip, @$"{tempDir}\{currentRelease}.zip");
                if (!zipStatus)
                {
                    return false;
                }
                ConsoleOutput($"{uriZip} downloaded.");
                ConsoleOutput($"Downloading {uriHash}");
                bool hashStatus = await FileDownload(uriHash, @$"{tempDir}\{currentRelease}.zip.sha256");
                if (!hashStatus)
                {
                    return false;
                }
                ConsoleOutput($"{uriHash} downloaded.");
                return true;
            }
            catch (HttpRequestException)
            {
                ConsoleOutput("[ERROR] A network connectivity error occurred during download. Please check your connection and try again.");
                return false;
            }
            catch (IOException)
            {
                ConsoleOutput("[ERROR] A network connectivity error occurred during download. Please check your connection and try again.");
                return false;
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to download state files:\n{ex}");
                return false;
            }
        }
        private static bool CompareHash(string hashValue, string file)
        // Used to calculate the SHA256 hash of a file, and compare it to a given hash
        {
            bool match = false;
            try
            {
                string fileHash;
                if (!File.Exists(file))
                {
                    MessageBox.Show($"{file} does not exist!");
                }
                var sha256Gen = SHA256.Create();
                var fileStream = File.OpenRead(file);
                var hashOutput = sha256Gen.ComputeHash(fileStream);
                fileHash = BitConverter.ToString(hashOutput).Replace("-", "").ToLowerInvariant();
                fileStream.Close();
                if (fileHash == hashValue)
                {
                    ConsoleOutput($"File Hash: {fileHash}");
                    ConsoleOutput($"Given Hash: {hashValue}");
                    match = true;
                }
                else
                {
                    ConsoleOutput($"File Hash: {fileHash}");
                    ConsoleOutput($"Given Hash: {hashValue}");
                    match = false;
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to Compare Hash:\n{ex}");
            }
            return match;
        }
        private static bool ExtractStates(string tempDir, string release)
        // Once downloaded, or available, this will extract the salt states to the required location in the Salt Project\Salt folder
        {
            bool extracted = false;
            try
            {
                string file = $"{tempDir}{release}.zip";
                string saltBase = @"C:\ProgramData\Salt Project\Salt\";
                if (!Directory.Exists($"{saltBase}srv"))
                {
                    ManageDirectory($"{saltBase}srv", "create");
                    ManageDirectory($@"{saltBase}srv\salt\", "create");
                    saltBase = $@"{saltBase}srv\salt\";
                }
                else
                {
                    saltBase = @$"{saltPath}\";
                }
                string shortRelease = release.TrimStart('v');
                string distroFolder = $@"{tempDir}{src}-salt-{shortRelease}\{src}";
                string distroDest = $@"{saltBase}{src}";
                ConsoleOutput($"Extracting {file} to {tempDir}");
                ZipFile.ExtractToDirectory(file, tempDir, true);
                ConsoleOutput($"Moving {distroFolder} folder to {distroDest}");
                if (Directory.Exists(distroDest))
                {
                    ManageDirectory(distroDest, "delete");
                }
                ManageDirectory(distroFolder, "move", distroDest);
                extracted = true;
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to extract states:\n{ex}");
            }
            return extracted;
        }
        private static void InsertXwaysToken(string authtoken, bool install)
        // This function will take the provided authtoken and insert it in the required spot in the x-ways.sls State file once available.
        {
            string stateFile;
            if (install)
            {
                stateFile = $@"{saltPath}\{src}\standalones\x-ways.sls";
            }
            else
            {
                stateFile = $@"{saltPath}\{src}\downloads\standalones\x-ways.sls";
            }
            try
            {
                if (File.Exists(stateFile))
                {
                    string allText = File.ReadAllText(stateFile);
                    allText = allText.Replace("{% set auth_token = \"TOKENPLACEHOLDER\" %}", $"{{% set auth_token = \"{authtoken}\" %}}");
                    File.WriteAllText(stateFile, allText);
                    ConsoleOutput($"Authentication token written to {stateFile}");
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to add X-Ways Token to {stateFile}:\n{ex}");
            }
        }

        private static void InsertHostName(string hostName, string repo)
        {
            string hostnameState = $@"{saltPath}\{src}\theme\{repo}\computer-name.sls";
            try
            {
                if (File.Exists(hostnameState))
                {
                    string allText = File.ReadAllText(hostnameState);
                    allText = allText.Replace("WIN-FOR", hostName);
                    File.WriteAllText(hostnameState, allText);
                    ConsoleOutput($"Chosen hostname {hostName} written to computer-name.sls");
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to write the selected hostname to computer-name.sls:\n{ex}");
            }
        }
        private static bool CopyCustomState(string stateFile)
        // A simple copy of the generated custom stateFile (from the GenerateState function) to the proper location
        {
            bool copied = false;
            try
            {
                File.WriteAllText(@$"{saltPath}\{src}\custom.sls", stateFile);
                ConsoleOutput($"Custom state custom.sls copied to the SaltStack {src} directory");
                copied = true;
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to copy the custom state to the SaltStack {src} directory:\n{ex}");
            }
            return copied;
        }
        private async void DownloadOnly(object? sender, RoutedEventArgs e)
        // This is used to generate a custom state for simply downloading the selected files, without any installation or modification
        {
            try
            {
                stopWatch?.Reset();
                stopWatch?.Start();
                elapsedTimer?.Start();
                bool Connected = CheckNetworkConnection.IsConnected();
                if (!Connected)
                {
                    OutputExpander.IsExpanded = true;
                    ConsoleOutput("[ERROR] No network connection detected - Please check your network connection and try the Download function again.");
                    return;
                }
                (List<string> checkedItems, _) = GetCheckStatus();
                if (checkedItems.Count == 0)
                {
                    MessageBox.Show("No items selected! Choose at least one item to download.", "No items selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if ((checkedItems.Count == 1) && (checkedItems.Contains("themed") || checkedItems.Contains("wsl")))
                {
                    string item = char.ToUpper(checkedItems[0][0]) + checkedItems[0][1..];
                    if (item == "Wsl")
                    {
                        MessageBox.Show($"Only {item} checkbox was selected!\nChoose at least one item from the tool list to download.\nTo install WSL only, use the \"WSL Only\" button on the side.\nWSL is not currently downloadable.", $"Only {item} checkbox selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Only {item} checkbox was selected!\nChoose at least one item from the tool list to download.\nThe \"Theme\" itself is not downloadable.", $"Only {item} checkbox selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
                if ((checkedItems.Count == 2) && (checkedItems.Contains("themed") && checkedItems.Contains("wsl")))
                {
                    (string item0, string item1) = (checkedItems[0], checkedItems[1]);
                    item0 = char.ToUpper(item0[0]) + item0[1..];
                    item1 = char.ToUpper(item1[0]) + item1[1..];
                    MessageBox.Show($"Only {item0} and {item1} were selected!\nChoose at least one item from the tool list to download.\nTo install WSL only, use the \"WSL Only\" button on the side.\nWSL is not currently downloadable.\nThe \"Theme\" itself is also not downloadable.",
                                    $"Only {item0} and {item1} checkboxes selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"{displayName} v{appVersion}");
                string driveLetter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
                string stateList = GenerateState("download", false, false);
                string tempDir = $@"{driveLetter}{src}-temp\";
                List<string>? currentReleaseData = await IdentifyRelease();
                string releaseVersion = currentReleaseData![0];
                string uriZip = currentReleaseData[1];
                string uriHash = currentReleaseData[2];
                string downloadPath;
                List<ConfigItems> softwareConfig = await GetJsonConfig();
                string? gitVersion = softwareConfig[0].Software!["Git"].SoftwareVersion!;
                string? gitHash = softwareConfig[0].Software!["Git"].SoftwareHash!;
                string? saltVersion = softwareConfig[0].Software!["SaltStack"].SoftwareVersion!;
                string? saltHash = softwareConfig[0].Software!["SaltStack"].SoftwareHash!;
                string xwaysData;
                string xwaysToken;
                bool xwaysSelected;
                if (XWays.IsChecked == true && (XUser.Text != "" || XPass.Text != ""))
                {
                    xwaysData = $"{XUser.Text}:{XPass.Text}";
                    xwaysToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(xwaysData));
                    xwaysSelected = true;
                    ConsoleOutput($"X-Ways is selected and credentials have been provided");
                }
                else if (XWays.IsChecked == true && (XUser.Text == "" || XPass.Text == ""))
                {
                    ConsoleOutput("With X-Ways enabled, neither X-Ways Portal User nor X-Ways Portal Pass can be empty!");
                    MessageBox.Show("With X-Ways enabled, neither X-Ways Portal User nor X-Ways Portal Pass can be empty!",
                                    "X-Ways Portal Credentials Not Supplied",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    xwaysSelected = false;
                    xwaysToken = "TOKENPLACEHOLDER";
                    ConsoleOutput("X-Ways is not selected and will not be downloaded / installed");
                }
                if (DownloadsPath.Text == "")
                {
                    downloadPath = @$"C:\{src}-downloads\";
                }
                else
                {
                    downloadPath = DownloadsPath.Text;
                }
                ConsoleOutput($"{tempDir} is being created for temporary storage of required files");
                if (!Directory.Exists(tempDir))
                {
                    ManageDirectory(tempDir, "create");
                }
                else
                {
                    ManageDirectory(tempDir, "perms");
                }
                if (!CheckGitInstalled(gitVersion))
                {
                    ConsoleOutput($"Git {gitVersion} is not installed");
                    await DownloadGit(tempDir, gitVersion, gitHash);
                    await InstallGit(tempDir, gitVersion);
                }
                else
                {
                    ConsoleOutput($"Git {gitVersion} is already installed");
                }
                if (!CheckSaltStackInstalled(saltVersion))
                {
                    ConsoleOutput($"SaltStack {saltVersion} is not installed");
                    await DownloadSaltStack(tempDir, saltVersion, saltHash);
                    await InstallSaltStack(tempDir, saltVersion);
                }
                else
                {
                    ConsoleOutput($"SaltStack {saltVersion} is already installed");
                }
                string releaseFile = $"{tempDir}{releaseVersion}.zip";
                string providedHash;
                bool hashMatch;
                Environment.GetEnvironmentVariable("PATH");
                ConsoleOutput($"Current release of WIN-FOR is {releaseVersion}");
                FileInfo releaseFileFileInfo = new(releaseFile);
                FileInfo releaseHashFileInfo = new($"{releaseFile}.sha256");
                if ((File.Exists(releaseFile) && File.Exists($"{releaseFile}.sha256")) && (releaseFileFileInfo.Length != 0 && releaseHashFileInfo.Length != 0))
                {
                    ConsoleOutput($"{releaseFile} and {releaseFile}.sha256 already exist and not zero-byte files.");
                    providedHash = (await File.ReadAllTextAsync($"{tempDir}{releaseVersion}.zip.sha256")).ToLower().Split(" ")[0];
                    hashMatch = CompareHash(providedHash, releaseFile);
                    if (hashMatch)
                    {
                        ConsoleOutput("Hashes match, continuing...");
                        ConsoleOutput("Extracting archive...");
                    }
                    else
                    {
                        ConsoleOutput("Hashes do not match, aborting");
                        return;
                    }
                }
                else
                {
                    ConsoleOutput("Downloading release file and SHA256 to compare");
                    bool status = await DownloadStates(tempDir, releaseVersion, uriZip, uriHash);
                    if (!status)
                    {
                        ConsoleOutput("[ERROR] Unable to download Salt states - an issue occurred with internet connectivity. Check your connection and try again.");
                        return;
                    }
                    ConsoleOutput("Downloads complete...");
                    ConsoleOutput("Comparing hashes...");
                    providedHash = (await File.ReadAllTextAsync($"{tempDir}{releaseVersion}.zip.sha256")).ToLower().Split(" ")[0];
                    hashMatch = CompareHash(providedHash, releaseFile);
                    if (hashMatch)
                    {
                        ConsoleOutput("Hashes match, continuing...");
                        ConsoleOutput("Extracting archive...");
                    }
                    else
                    {
                        ConsoleOutput("Hashes do not match, aborting");
                        return;
                    }
                }
                bool extracted = ExtractStates(tempDir, releaseVersion);
                if (extracted)
                {
                    ConsoleOutput($@"State files extracted, writing {src}\downloads\init.sls");
                    await File.WriteAllTextAsync(@$"{saltPath}\{src}\downloads\init.sls", stateList);
                }
                if (xwaysSelected)
                {
                    ConsoleOutput("Adding authentication token to X-Ways state");
                    InsertXwaysToken(xwaysToken, false);
                }
                if (File.Exists(@$"{saltPath}\{src}\downloads\init.sls"))
                {
                    await ExecuteSaltStackDownloads(releaseVersion, downloadPath);
                }
                else
                {
                    ConsoleOutput($@"{src}\downloads\init.sls not found, aborting!");
                    return;
                }
                stopWatch?.Stop();
                elapsedTimer?.Stop();
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to complete the Download process:\n{ex}");
            }
        }

        private static void HandleProcessOutput(string? data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                Application.Current.Dispatcher.Invoke(() => ConsoleOutput(data));
            }
        }

        private Process? saltproc;
        private Process? wslproc;
        private TaskCompletionSource<bool>? ProcHandled;
        private TaskCompletionSource<bool>? wslHandled;
        private async Task ExecuteSaltStack(string userName, string standalonesPath, string release)
        // Generate a salt-call.exe process with the required arguments to install the custom salt states
        {
            string envPath = Environment.GetEnvironmentVariable("Path")!;
            string gitPath = $@"{envPath};C:\Program Files\Git\cmd";
            ProcHandled = new TaskCompletionSource<bool>();
            string saltExe = @"C:\Program Files\Salt Project\Salt\salt-call.exe";
            string args = $"-l info --local --retcode-passthrough --state-output=mixed state.sls {src}.custom pillar=\"{{ '{src}_user': '{userName}', 'inpath': '{standalonesPath}'}}\" --out-file=\"C:\\{src}-saltstack-{release}.log\" --out-file-append --log-file=\"C:\\{src}-saltstack-{release}.log\" --log-file-level=debug";
            using (saltproc = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = saltExe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
            })
            {
                try
                {
                    if (File.Exists(saltExe))
                    {
                        ConsoleOutput(
                            $"Installing the selected states.\n" +
                            $"Log File: C:\\{src}-saltstack-{release}.log\n" +
                            $"Executing: salt call with the following variables\n" +
                            $"  {src}.custom\n" +
                            $"  {{ '{src}_user': '{userName}', 'inpath': '{standalonesPath}'}}\n"
                            );
                        saltproc.Exited += new EventHandler(ProcessExited);
                        if (!envPath.Contains(@"C:\Program Files\Git\cmd"))
                        {
                            saltproc.StartInfo.EnvironmentVariables["PATH"] = gitPath;
                        }
                        saltproc.OutputDataReceived += (s, e) => HandleProcessOutput(e.Data);
                        saltproc.ErrorDataReceived += (s, e) => HandleProcessOutput(e.Data);
                        saltproc.EnableRaisingEvents = true;
                        StopButton.IsEnabled = true;
                        StopButton.Visibility = Visibility.Visible;
                        saltproc.Start();
                        saltproc.BeginOutputReadLine();
                        saltproc.BeginErrorReadLine();
                        await saltproc.WaitForExitAsync();
                        if (saltproc.ExitCode != 0)
                        {
                            ConsoleOutput("Installation has ended with errors.");
                        }
                        else
                        {
                            ConsoleOutput("Installation has completed successfully.");
                        }
                        StopButton.Visibility = Visibility.Hidden;
                        StopButton.IsEnabled = false;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleOutput($"[ERROR] Unable to execute SaltStack install:\n{ex}");
                    return;
                }
                await Task.WhenAny(ProcHandled.Task, Task.Delay(10000));
            }
        }
        public void StopSaltStack_Click(object sender, RoutedEventArgs e)
        {
            if (saltproc != null && !saltproc.HasExited)
            {
                try
                {
                    saltproc.Kill(true);
                    saltproc.WaitForExit();
                    ConsoleOutput("SaltStack was terminated.");
                    StopButton.Visibility = Visibility.Hidden;
                }
                catch (Exception ex)
                {
                    ConsoleOutput($"[ERROR] Failed to kill SaltStack: {ex.Message}");
                }
            }
            if (wslproc != null && !wslproc.HasExited)
            {
                try
                {
                    wslproc.Kill(true);
                    wslproc.WaitForExit();
                    ConsoleOutput("SaltStack was terminated.");
                    StopButton.Visibility = Visibility.Hidden;
                }
                catch (Exception ex)
                {
                    ConsoleOutput($"[ERROR] Failed to kill SaltStack: {ex.Message}");
                }
            }
        }
        private async Task ExecuteSaltStackDownloads(string release, string downloadPath)
        // Generate a salt-call.exe process with the required argument to simply download the selected files
        {
            ProcHandled = new TaskCompletionSource<bool>();
            string envPath = Environment.GetEnvironmentVariable("Path")!;
            string gitPath = $@"{envPath};C:\Program Files\Git\cmd";
            string saltExe = @"C:\Program Files\Salt Project\Salt\salt-call.exe";
            string args = $"-l info --local --retcode-passthrough --state-output=mixed state.sls {src}.downloads pillar=\"{{ 'downloads': '{downloadPath}'}}\" --out-file=\"C:\\{src}-saltstack-{release}-downloads.log\" --out-file-append --log-file=\"C:\\{src}-saltstack-{release}-downloads.log\" --log-file-level=debug";
            using (saltproc = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = saltExe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
            })
            {
                try
                {
                    if (File.Exists(saltExe))
                    {
                        ConsoleOutput(
                            $"Installing the selected states.\n" +
                            $"Log File: C:\\{src}-saltstack-{release}-downloads.log\n" +
                            $"Executing: salt call with the following variables\n" +
                            $"  {src}.downloads\n" +
                            $"  {{ 'downloads': '{downloadPath}'}}\n"
                            );
                        saltproc.Exited += new EventHandler(ProcessExited);
                        if (!envPath.Contains(@"C:\Program Files\Git\cmd"))
                        {
                            saltproc.StartInfo.EnvironmentVariables["PATH"] = gitPath;
                        }
                        saltproc.OutputDataReceived += (s, e) => HandleProcessOutput(e.Data);
                        saltproc.ErrorDataReceived += (s, e) => HandleProcessOutput(e.Data);
                        saltproc.EnableRaisingEvents = true;
                        StopButton.IsEnabled = true;
                        StopButton.Visibility = Visibility.Visible;
                        saltproc.Start();
                        saltproc.BeginOutputReadLine();
                        saltproc.BeginErrorReadLine();
                        await saltproc.WaitForExitAsync();
                        if (saltproc.ExitCode != 0)
                        {
                            ConsoleOutput("Installation has ended with errors.");
                        }
                        else
                        {
                            ConsoleOutput("Installation has completed successfully.");
                        }
                        StopButton.Visibility = Visibility.Hidden;
                        StopButton.IsEnabled = false;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleOutput($"[ERROR] Unable to execute SaltStack download install:\n{ex}");
                    return;
                }
                await Task.WhenAny(ProcHandled.Task, Task.Delay(10000));
            }
        }
        private async void ProcessExited(object? sender, EventArgs e)
        // An Event Handler for tracking the ExecuteSaltStack and ExecuteSaltStackDownloads functions
        {
            if (sender is Process proc)
            {
                try
                {
                    await proc.WaitForExitAsync();
                    ConsoleOutput(
                    $"\nExited\t\t: {proc.ExitTime}\n" +
                    $"Exit code \t: {proc.ExitCode}\n" +
                    $"Elapsed time\t: {Math.Round((proc.ExitTime - proc.StartTime).TotalMilliseconds)}");
                    ProcHandled?.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to display exit details: {ex}","Unable to display exit details", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExecuteWsl(string userName, string release, string standalonesPath, bool waitForSalt)
        // A salt-call.exe process used for the installation of the Windows Subsystem for Linux v2 environment
        {
            wslHandled = new TaskCompletionSource<bool>();
            string envPath = Environment.GetEnvironmentVariable("Path")!;
            string gitPath = $@"{envPath};C:\Program Files\Git\cmd";
            string saltExe = @"C:\Program Files\Salt Project\Salt\salt-call.exe";
            string args = $"-l info --local --retcode-passthrough --state-output=mixed state.sls {src}.wsl pillar=\"{{ '{src}_user': '{userName}', 'inpath': '{standalonesPath}'}}\" --out-file=\"C:\\{src}-saltstack-{release}-wsl.log\" --out-file-append --log-file=\"C:\\{src}-saltstack-{release}-wsl.log\" --log-file-level=debug";
            using (wslproc = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = saltExe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
            })
            {
                try
                {
                    if (File.Exists(saltExe))
                    {
                        if (waitForSalt)
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        { 
                            await Task.WhenAny(ProcHandled.Task);
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        ConsoleOutput(
                           $"Installing WSL.\n" +
                           $"Log File: C:\\{src}-saltstack-{release}-wsl.log\n" +
                           $"Executing: salt call with the following variables\n" +
                           $"  {src}.wsl\n" +
                           $"  {{ '{src}_user': '{userName}', 'inpath': '{standalonesPath}'}}\n"
                           );
                        wslproc.Exited += new EventHandler(WslProcessExited);
                        if (!envPath.Contains(@"C:\Program Files\Git\cmd"))
                        {
                            wslproc.StartInfo.EnvironmentVariables["PATH"] = gitPath;
                        }
                        ConsoleOutput("Saving Console Output before beginning WSL installation");
                        SaveConsoleOutput("wsl", null);
                        wslproc.OutputDataReceived += (s, e) => HandleProcessOutput(e.Data);
                        wslproc.ErrorDataReceived += (s, e) => HandleProcessOutput(e.Data);
                        wslproc.EnableRaisingEvents = true;
                        StopButton.IsEnabled = true;
                        StopButton.Visibility = Visibility.Visible;
                        wslproc.Start();
                        wslproc.BeginOutputReadLine();
                        wslproc.BeginErrorReadLine();
                        await wslproc.WaitForExitAsync();
                        if (wslproc.ExitCode != 0)
                        {
                            ConsoleOutput("Installation has ended with errors.");
                        }
                        else
                        {
                            ConsoleOutput("Installation has completed successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleOutput($"[ERROR] Unable to execute WSL install:\n{ex}");
                    return;
                }
                await Task.WhenAny(wslHandled.Task, Task.Delay(10000));
            }
        }
        private async void WslProcessExited(object? sender, EventArgs e)
        // An Event Handler for tracking the ExecuteWsl process
        {
            if (sender is Process proc)
            {
                try
                {
                    await proc.WaitForExitAsync();
                    ConsoleOutput(
                    $"Exited\t\t: {proc.ExitTime}\n" +
                    $"Exit code \t: {proc.ExitCode}\n" +
                    $"Elapsed time\t: {Math.Round((proc.ExitTime - proc.StartTime).TotalMilliseconds)}");
                    wslHandled?.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to display out exit details: {ex}", "Unable to display exit details", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async void InstallWslOnly(object sender, RoutedEventArgs e)
        // Used to install WSL without any other options selected.
        // This determines that all pre-reqs are met before beginning the ExecuteWsl function
        {
            try
            {
                stopWatch?.Reset();
                stopWatch?.Start();
                elapsedTimer?.Start();
                bool Connected = CheckNetworkConnection.IsConnected();
                if (!Connected)
                {
                    OutputExpander.IsExpanded = true;
                    ConsoleOutput("[ERROR] No network connection detected - Please check your network connection and try the WSL Only option again.");
                    return;
                }
                MessageBoxResult result = MessageBox.Show("WSL installation will require a reboot! Ensure that you save any open documents, then click OK to continue.", "WSL requires a reboot!", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"{displayName} v{appVersion}");
                string driveLetter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
                string distro;
                string userName;
                string standalonesPath;
                string currentUser = Environment.UserName;
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
                    userName = currentUser;
                }
                else
                {
                    userName = UserName.Text;
                }
                ConsoleOutput($"Selected user is {userName}");
                if (StandalonesPath.Text != "")
                {
                    standalonesPath = $@"{StandalonesPath.Text}";
                    ConsoleOutput($"Standalones path is {standalonesPath}");
                }
                else
                {
                    standalonesPath = @$"C:\standalone";
                    ConsoleOutput($"Standalones path box was empty - default will be used - {standalonesPath}");
                }
                string tempDir = @$"{driveLetter}{src}-temp\";
                List<string>? currentReleaseData = await IdentifyRelease();
                string releaseVersion = currentReleaseData![0];
                string uriZip = currentReleaseData[1];
                string uriHash = currentReleaseData[2];
                List<ConfigItems> softwareConfig = await GetJsonConfig();
                string? gitVersion = softwareConfig[0].Software!["Git"].SoftwareVersion!;
                string? gitHash = softwareConfig[0].Software!["Git"].SoftwareHash!;
                string? saltVersion = softwareConfig[0].Software!["SaltStack"].SoftwareVersion!;
                string? saltHash = softwareConfig[0].Software!["SaltStack"].SoftwareHash!;
                ConsoleOutput($"{tempDir} is being created for temporary storage of required files");
                if (!Directory.Exists(tempDir))
                {
                    ManageDirectory(tempDir, "create");
                }
                else
                {
                    ManageDirectory(tempDir, "perms");
                }
                if (!CheckGitInstalled(gitVersion))
                {
                    ConsoleOutput($"Git is being downloaded.");
                    await DownloadGit(tempDir, gitVersion, gitHash);
                    ConsoleOutput("Git is being installed...");
                    await InstallGit(tempDir, gitVersion);
                }
                else
                {
                    ConsoleOutput($"Git {gitVersion} is already installed");
                }
                if (!CheckSaltStackInstalled(saltVersion))
                {
                    ConsoleOutput($"SaltStack is being downloaded");
                    await DownloadSaltStack(tempDir, saltVersion, saltHash);
                    ConsoleOutput("SaltStack is being installed...");
                    await InstallSaltStack(tempDir, saltVersion);
                }
                else
                {
                    ConsoleOutput($"SaltStack {saltVersion} is already installed");
                }
                string releaseFile = $"{tempDir}{releaseVersion}.zip";
                string providedHash;
                bool hashMatch;
                ConsoleOutput($"Current release of {distro} is {releaseVersion}");
                if (File.Exists(releaseFile) && File.Exists($"{releaseFile}.sha256"))
                {
                    ConsoleOutput($"{releaseFile} and {releaseFile}.sha256 already exist!\nComparing hashes...");
                    providedHash = (await File.ReadAllTextAsync($"{tempDir}{releaseVersion}.zip.sha256")).ToLower().Split(" ")[0];
                    hashMatch = CompareHash(providedHash, releaseFile);
                    if (hashMatch)
                    {
                        ConsoleOutput("Hashes match, continuing...");
                        ConsoleOutput("Extracting archive...");
                    }
                    else
                    {
                        ConsoleOutput("Hashes do not match, aborting");
                        return;
                    }
                }
                else
                {
                    ConsoleOutput("Downloading release file and SHA256 to compare");
                    bool status = await DownloadStates(tempDir, releaseVersion, uriZip, uriHash);
                    if (!status)
                    {
                        ConsoleOutput("[ERROR] Unable to download required Salt states.");
                        return;
                    }
                    ConsoleOutput("Downloads complete...");
                    ConsoleOutput("Comparing hashes...");
                    providedHash = (await File.ReadAllTextAsync($"{tempDir}{releaseVersion}.zip.sha256")).ToLower().Split(" ")[0];
                    hashMatch = CompareHash(providedHash, releaseFile);
                    if (hashMatch)
                    {
                        ConsoleOutput("Hashes match, continuing...");
                        ConsoleOutput("Extracting archive...");
                    }
                    else
                    {
                        ConsoleOutput("Hashes do not match, aborting");
                        return;
                    }
                }
                bool extracted = ExtractStates(tempDir, releaseVersion);
                if (extracted)
                {
                    await ExecuteWsl(userName, releaseVersion, standalonesPath, false);
                }
                ConsoleOutput("SaltStack WSL installation completed.");
                stopWatch?.Stop();
                elapsedTimer?.Stop();
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to execute WSL install:\n{ex}");
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
        private void SectionUncheckAll(object sender, RoutedEventArgs e)
        // Used to identify the selected TreeViewItem and when its checkbox is unchecked, the CheckBox child items are also unchecked
        {
            CheckBox? header = sender as CheckBox;
            TreeViewItem treeViewItem = FindLogicalParent<TreeViewItem>(header!)!;
            foreach (object item in treeViewItem.Items)
            {
                if (item.GetType() == typeof(CheckBox))
                {
                    CheckBox checkBox = (CheckBox)item;
                    if (checkBox.Name.StartsWith("header_"))
                    {
                        continue;
                    }
                    else if (!checkBox.IsEnabled)
                    {
                        continue;
                    }
                    else
                    {
                        checkBox.IsChecked = false;
                    }
                }
                else
                {
                    continue;
                }
            }
        }
        private void SectionCheckAll(object sender, RoutedEventArgs e)
        // Used to identify the selected TreeViewItem and when its checkbox is checked, the CheckBox child items are also checked
        {
            CheckBox? header = (sender as CheckBox);
            TreeViewItem treeViewItem = FindLogicalParent<TreeViewItem>(header!)!;
            foreach (object item in treeViewItem.Items)
            {
                if (item.GetType() == typeof(CheckBox))
                {
                    CheckBox checkBox = (CheckBox)item;
                    if (checkBox.Name.StartsWith("header_"))
                    {
                        continue;
                    }
                    else if (!checkBox.IsEnabled)
                    {
                        continue;
                    }
                    else
                    {
                        checkBox.IsChecked = true;
                    }
                }
                else
                { 
                    continue;
                }
            }
        }
        private void SaveConsoleOutput(object sender, RoutedEventArgs? e)
        // Saves the TextBox console Output for log analysis or review
        {
            string dateTimeNow = $"{DateTime.Now:yyyyMMdd-hhmmss}";
            string fileName;
            if ((sender.GetType() == typeof(Button)) || (sender.GetType() == typeof(MenuItem)))
            {
                if (OutputConsole.Text == "")
                {
                    MessageBox.Show($"Output Console contains no information - not saving output.", "Output Console Empty!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    OutputExpander.IsExpanded = false;
                }
                else
                {
                    fileName = $@"C:\{src}-output-{dateTimeNow}.log";
                    File.WriteAllText(fileName, OutputConsole.Text);
                    MessageBox.Show($"Output Console data saved to {fileName}.", "Output Console Log Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (sender is string)
            {
                if (OutputConsole.Text == "")
                {
                    OutputExpander.IsExpanded = false;
                }
                else
                {
                    fileName = $@"C:\{src}-output-{dateTimeNow}-{sender}.log";
                    File.WriteAllText(fileName, OutputConsole.Text);
                }
            }
            else
            {
                fileName = $@"C:\{src}-output-{dateTimeNow}.log";
                File.WriteAllText(fileName, OutputConsole.Text);
            }
        }
        private async void CheckUpdates(object sender, RoutedEventArgs e)
        // Checks for updates of the application
        {
            try
            {
                CancellationTokenSource cancellationToken = new(new TimeSpan(0, 0, 200));
                HttpClient httpClient = NewHttpClient();
                string uri = $@"{githubBinaryApi}";
                var getRequest = await httpClient.GetAsync(uri, cancellationToken.Token);
                string data = getRequest.Content.ReadAsStringAsync().Result;
                var jsonData = JsonDocument.Parse(data);
                string release = (jsonData.RootElement.GetProperty("tag_name")).ToString();
                Version releaseTag = new(release.Replace("v", ""));
                string newRelease = $"{githubBinaryReleaseDownload}/{release}/{src}-{release}.exe";
                if (releaseTag > appVersion)
                {
                    MessageBoxResult result = MessageBox.Show(
                       $"New version found: {releaseTag}\n" +
                       $"Current version: {appVersion}\n\n" +
                       $"Would you like to download the new version of {displayName}?", $"New Version Found - {releaseTag}", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo($"{newRelease}") { UseShellExecute = true });
                    }
                }
                else if (releaseTag <= appVersion)
                {
                    MessageBox.Show($"No new release of {displayName} found:\n{appVersion} is the most recent release.", $"No new release of {displayName} found", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (appVersion > releaseTag)
                {
                    MessageBox.Show($"Lucky you! You're ahead of the times!\nVersion {appVersion} is even newer than the current release!", "Where you're going, you don't need versions..", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
                cancellationToken.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] Unable to identify release:\n{ex}");
            }
        }

        private void ResultsButton_Click(object sender, RoutedEventArgs e)
        // Parses the available logs for the SaltStack and WSL installs to determine its summary
        {
            string versionFileSalt = @$"{saltPath}\{src}\VERSION";
            string versionFileLocal = @$"C:\{src}-version";
            try
            {
                string releaseVersion = "";
                if (File.Exists(versionFileLocal))
                {
                    releaseVersion = File.ReadAllText($"{versionFileLocal}").TrimEnd();
                }
                else if (File.Exists(versionFileSalt))
                {
                    releaseVersion = File.ReadAllText($"{versionFileSalt}").TrimEnd();
                }

                else
                {
                    throw new FileNotFoundException("VERSION files not found");
                }
                if (!File.Exists(@$"C:\{src}-saltstack-{releaseVersion}.log") && !File.Exists(@$"C:\{src}-saltstack-{releaseVersion}-downloads.log") && !File.Exists(@$"C:\{src}-saltstack-{releaseVersion}-wsl.log"))
                {
                    MessageBox.Show($"The most recent attempt at installation\nwas for version {releaseVersion}.\n\nNo results were found in the log files,\n or no log file was found for {releaseVersion}.\nIt may have been canceled prematurely.\n\nTry reviewing the log files manually,\n and reach out on GitHub to let us know.", $"No results found for {releaseVersion}", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                else
                {
                    ResultsWindow resultsWindow = new(releaseVersion)
                    {
                        Owner = this
                    };
                    resultsWindow.Show();
                }
            }
            catch (FileNotFoundException)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"Unable to determine recent installation attempt:\n{versionFileSalt} and {versionFileLocal} are not present.");
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"Unable to display results:\n{ex}");
            }
        }
        public void FindFunction(object sender, RoutedEventArgs e)
        {
            SearchBox.Focus();
        }
 
        private void CheckDistroVersion(object sender, RoutedEventArgs e)
        // Checks the system to see if the chosen environment is installed and provide its version
        {
            string driveLetter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
            string currentVersion;
            string msgBoxVersion;
            string versionFile = driveLetter + $"{src}-version";
            if (File.Exists(versionFile))
            {
                currentVersion = File.ReadAllText(versionFile);
                msgBoxVersion = currentVersion;
            }
            else
            {
                currentVersion = $"not installed.\n" +
                                  $"If you are expecting version information, you may have\n" +
                                  $"encountered errors during your last install attempt.\n\n" +
                                  $"You can check the log(s) labelled {driveLetter}{src}-saltstack-<version>.log";
                msgBoxVersion = "not installed.";
            }
            MessageBox.Show($"{displayName} is {currentVersion}", $"{displayName} {msgBoxVersion}", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ShowAbout(object sender, RoutedEventArgs e)
        // Shows the About box
        {
            MessageBoxResult result = MessageBox.Show(
                $"{displayName} v{appVersion}\n" +
                $"Author: Corey Forman (digitalsleuth)\n" +
                $"Source: {githubBinaryRepo}\n\n" +
                $"Would you like to visit the repo on GitHub?",
                $"{displayName} v{appVersion}", MessageBoxButton.YesNoCancel, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo($"{githubBinaryRepo}") { UseShellExecute = true });
            }
        }
        public static int FindLineNumber(string[] content, string searchTerm)
        // Used to identify the line number for a string of text identified in the provided content
        {
            return Array.FindIndex<string>(content, i => i.Contains(searchTerm));
        }
        public static List<int> Find_AllLineNumbers(string[] content, string searchTerm)
        // Used to identify all line numbers within the given content for the provided searchTerm
        {
            List<int> lineNumbers = [];
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i].Contains(searchTerm))
                {
                    int number = Array.FindIndex(content, i, x => x == content[i]);
                    lineNumbers.Add(number);
                }
            }
            return lineNumbers;
        }

        private static void ConsoleOutput(string message)
        // Function to output the given content with a date/time value in front of it for tracking events
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
        private void DownloadToolList(object sender, RoutedEventArgs e)
        // Downloads the latest Tool List from GitHub which shows all tools and versions available.
        {
            Process.Start(new ProcessStartInfo($"{toolListPdf}") { UseShellExecute = true });
        }
        private void DownloadThemeTemplate(object sender, RoutedEventArgs e)
        // Downloads the latest Tool List from GitHub which shows all tools and versions available.
        {
            Process.Start(new ProcessStartInfo($"{themeTemplateZip}") { UseShellExecute = true });
        }
        private void ToolList()
        // Currently not 'implemented', but will provide the Proper Name for the tools selected
        {
            OutputExpander.IsExpanded = true;
            (_, List<string> checkedContent) = GetCheckStatus();
            checkedContent.Sort();
            Console.WriteLine($"{string.Join("\n", checkedContent)}");
        }
        private async void ShowLatestRelease(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> releaseData = await IdentifyRelease();
                MessageBox.Show($"The latest version of {displayName} is {releaseData[0]}\nIf you wish to update, simply select your tools\nand click Install.", $"{releaseData[0]} is the latest version", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to determine the latest version:\n{ex}");
            }
        }

        public class TreeItems
        {
            public string? TVI { get; set; }
            public string? HeaderName { get; set; }
            public string? HeaderContent { get; set; }
            public Dictionary<string, ToolsList>? Tools { get; set; }
        }
        public class ToolsList
        {
            public string? CbName { get; set; }
            public string? CbContent { get; set; }
            public string? DAID { get; set; }
            public bool AUMID { get; set; }
            public bool DALP { get; set; }
            public bool StartMenu { get; set; }
            public bool Checked { get; set; }
            public bool Enabled { get; set; }
            public string[]? ExtraTiles { get; set; }
        }
        public async Task<string> GenerateLayout(string selectedPath)
        {
            StringBuilder xmlOutput = new();
            string xmlHeader = @"<LayoutModificationTemplate xmlns:defaultlayout=""http://schemas.microsoft.com/Start/2014/FullDefaultLayout"" xmlns:start=""http://schemas.microsoft.com/Start/2014/StartLayout"" Version=""1"" xmlns=""http://schemas.microsoft.com/Start/2014/LayoutModification"">" + '\n';
            xmlHeader += @"  <LayoutOptions StartTileGroupCellWidth=""6"" StartTileGroupsColumnCount=""1""/>" + '\n';
            xmlHeader += @"  <DefaultLayoutOverride>" + '\n';
            xmlHeader += @"    <StartLayoutCollection>" + '\n';
            xmlHeader += @"      <defaultlayout:StartLayout GroupCellWidth=""6"">" + '\n';
            xmlOutput.Append(xmlHeader);
            string groupName;
            List<TreeItems>? jsonQuery = await GetJsonLayout();
            int count = jsonQuery!.Count;
            (List<string> checkedItems, _) = GetCheckStatus();
            for (int i = 0; i < count; i++)
            {
                int row = 0;
                int column = 0;
                string sectionTitle = jsonQuery[i].HeaderContent!;
                List<TreeViewItem> treeViewItem = GetLogicalChildCollection<TreeViewItem>(AllTools);
                TreeViewItem treeViewItemLabel = treeViewItem[i];
                int checkedCount = 0;
                foreach (object item in treeViewItemLabel!.Items)
                {
                    if (item.GetType() == typeof(CheckBox))
                    {
                        CheckBox checkBox = (CheckBox)item;
                        if (checkBox.IsChecked == true)
                        {
                            checkedCount++;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else { continue; }
                }
                if (checkedCount > 0)
                {
                    groupName = $@"        <start:Group Name=""{sectionTitle}"">" + '\n';
                    xmlOutput.Append(groupName);
                    var tools = jsonQuery[i].Tools;
                    foreach (var item in tools!)
                    {
                        var value = item.Value;
                        string cbName = value.CbName!;
                        if (!checkedItems.Contains(cbName) || !value.StartMenu)
                            continue;

                        string? ReplacePath(string? input) => input?.Replace("PLACEHOLDER_PATH", selectedPath);

                        string? BuildTile(string id)
                        {
                            if (value.AUMID)
                                return $@"          <start:Tile Size=""1x1"" Column=""{column}"" Row=""{row}"" AppUserModelID=""{id}"" />" + '\n';
                            if (value.DALP)
                                return $@"          <start:DesktopApplicationTile Size=""1x1"" Column=""{column}"" Row=""{row}"" DesktopApplicationLinkPath=""{id}"" />" + '\n';
                            return $@"          <start:DesktopApplicationTile Size=""1x1"" Column=""{column}"" Row=""{row}"" DesktopApplicationID=""{id}"" />" + '\n';
                        }
                        string? mainTile = BuildTile(ReplacePath(value.DAID)!);
                        if (mainTile != null)
                        {
                            xmlOutput.Append(mainTile);
                            column++;
                            if (column > 5) { column = 0; row++; }
                        }
                        if (value.ExtraTiles?.Length > 0)
                        {
                            foreach (var extra in value.ExtraTiles)
                            {
                                if (column > 5) { column = 0; row++; }

                                string? extraTile = BuildTile(ReplacePath(extra)!);
                                if (extraTile != null)
                                {
                                    xmlOutput.Append(extraTile);
                                    column++;
                                }
                            }
                        }
                    }

                }
                else
                {
                    continue;
                }
                xmlOutput.Append("        </start:Group>\n");
            }
            xmlOutput.Append("      </defaultlayout:StartLayout>\n");
            xmlOutput.Append("    </StartLayoutCollection>\n");
            xmlOutput.Append("  </DefaultLayoutOverride>\n");
            xmlOutput.Append("</LayoutModificationTemplate>");
            string xmlLayout = xmlOutput.ToString();
            return xmlLayout;
        }
        public class ConfigItems
        {
            public Dictionary<string, SoftwareList>? Software { get; set; }
        }
        public class SoftwareList
        {
            public string? SoftwareHash { get; set; }
            public string? SoftwareVersion { get; set; }
        }
        private async Task<List<ConfigItems>> GetJsonConfig()
        {
            List<ConfigItems>? jsonConfig = [];
            try
            {
                CancellationTokenSource cancellationToken = new(new TimeSpan(0, 0, 200));
                HttpClient httpClient = NewHttpClient();
                jsonConfig = await httpClient.GetFromJsonAsync<List<ConfigItems>>(configFile, cancellationToken.Token);
                cancellationToken.Dispose();
            }
            catch (HttpRequestException)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to get JSON Layout - check your network connectivity and try again");
            }
            return jsonConfig!;
        }
        private async Task<List<TreeItems>> GetJsonLayout()
        {
            List<TreeItems>? jsonData = [];
            try
            {
                CancellationTokenSource cancellationToken = new(new TimeSpan(0, 0, 200));
                HttpClient httpClient = NewHttpClient();
                jsonData = await httpClient.GetFromJsonAsync<List<TreeItems>>(layoutFile, cancellationToken.Token);
                cancellationToken.Dispose();
            }
            catch (HttpRequestException)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to get JSON Layout - check your network connectivity and try again");
            }
            return jsonData!;
        }
        private readonly Dictionary<TreeViewItem, List<CheckBox>> originalChildOrder = [];
        private async Task GenerateTree()
        {
            bool Connected = CheckNetworkConnection.IsConnected();
            if (!Connected)
            {
                OutputExpander.IsExpanded= true;
                ConsoleOutput("[ERROR] No network connection detected - Please check your network connection and try launching the application again.");
                return;
            }
            List<TreeItems>? jsonQuery = await GetJsonLayout();
            int count = jsonQuery!.Count;
            for (int i = 0; i < count; i++)
            {   
                string TVI = jsonQuery[i].TVI!;
                string HEADER = jsonQuery[i].HeaderContent!;
                string HEADERNAME = jsonQuery[i].HeaderName!;
                CheckBox checkBox = new()
                {
                    Name = HEADERNAME,
                    Content = HEADER,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    IsTabStop = false,
                    IsChecked = true
                };
                checkBox.Checked += SectionCheckAll;
                checkBox.Unchecked += SectionUncheckAll;
                TreeViewItem newChild = new()
                {
                    Name = TVI,
                    Header = checkBox,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top
                };
                AllTools.Items.Add(newChild);
                var tools = jsonQuery[i].Tools;
                foreach (var tool in tools!)
                {
                    CheckBox toolCheckBox = new()
                    {
                        Name = tool.Value.CbName,
                        Content = tool.Value.CbContent,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Top,
                        IsChecked = tool.Value.Checked,
                        IsEnabled = tool.Value.Enabled
                    };
                    toolCheckBox.Checked += ChildCheckChanged;
                    toolCheckBox.Unchecked += ChildCheckChanged;
                    if (!toolCheckBox.IsEnabled)
                    {
                        continue;
                    }
                    else
                    {
                        newChild.Items.Add(toolCheckBox);
                    }
                    if (toolCheckBox.IsChecked is false)
                    {
                        checkBox.IsChecked = null;
                    }
                    if (!originalChildOrder.ContainsKey(newChild))
                        originalChildOrder[newChild] = [];
                    originalChildOrder[newChild].Add(toolCheckBox);
                }
            }
        }

        private void ChildCheckChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox childCheckBox)
                return;

            TreeViewItem? parentItem = FindLogicalParent<TreeViewItem>(childCheckBox);
            if (parentItem?.Header is not CheckBox parentCheckBox)
                return;
            var childCheckBoxes = parentItem.Items.OfType<CheckBox>().ToList();
            if (childCheckBoxes.Count == 0)
            {
                childCheckBoxes = parentItem.Items
                    .OfType<object>()
                    .Select(item => item as CheckBox)
                    .Where(cb => cb != null)
                    .ToList()!;
            }

            int total = childCheckBoxes.Count;
            int checkedCount = childCheckBoxes.Count(cb => cb.IsChecked == true);

            if (checkedCount == total)
            {
                parentCheckBox.IsThreeState = false;
                parentCheckBox.IsChecked = true;
            }
            else if (checkedCount == 0)
            {
                parentCheckBox.IsThreeState = false;
                parentCheckBox.IsChecked = false;
            }
            else
            {
                parentCheckBox.IsThreeState = true;
                parentCheckBox.IsChecked = null;
            }
        }

        private async Task LocalLayout()
        {
            MessageBoxResult dlgResult = MessageBox.Show("On the following Dialog Box, please select where your Standalone Executables are stored from your previous installation.", "Important - Please Read!", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
            if (dlgResult != MessageBoxResult.OK)
            {
                return;
            }
            string folderPath = "";
            System.Windows.Forms.FolderBrowserDialog folderDlg = new()
            {
                Description = "Select the directory where your standalone programs are stored...",
                ShowNewFolderButton = false,
                UseDescriptionForTitle = true,
                RootFolder = Environment.SpecialFolder.Desktop,
                InitialDirectory = Environment.CurrentDirectory
            };
            System.Windows.Forms.DialogResult result = folderDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                folderPath = folderDlg.SelectedPath;
            }
            if (folderPath != "")
            {
                string layout = await GenerateLayout(folderPath);
                await File.WriteAllTextAsync($@"{folderPath}\{displayName}-StartLayout.xml", layout);
                ConsoleOutput(@$"Customized Start Layout written to {folderPath}\{displayName}-StartLayout.xml");
                MessageBox.Show(@$"Customized Start Layout written to {folderPath}\{displayName}-StartLayout.xml", "Customized Start Layout Saved!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private async void LocalLayoutClick(object sender, RoutedEventArgs e)
        { 
            await LocalLayout();
        }
        private void StandalonesPicker(object sender, RoutedEventArgs e)
        {
            string selectedPath = "";
            System.Windows.Forms.FolderBrowserDialog folderDlg = new()
            {
                Description = "Select the directory where you would like to store your standalone files",
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true,
                RootFolder = Environment.SpecialFolder.Desktop,
                InitialDirectory = Environment.CurrentDirectory
            };
            System.Windows.Forms.DialogResult result = folderDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                selectedPath = folderDlg.SelectedPath;
            }
            if (selectedPath != "")
            {
                StandalonesPath.Text = selectedPath;
            }
        }
        private void DownloadsPicker(object sender, RoutedEventArgs e)
        {
            string selectedPath = "";
            System.Windows.Forms.FolderBrowserDialog folderDlg = new()
            {
                Description = "Select the directory where you would like to store your standalone files",
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true,
                RootFolder = Environment.SpecialFolder.Desktop,
                InitialDirectory = Environment.CurrentDirectory
            };
            System.Windows.Forms.DialogResult result = folderDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                selectedPath = folderDlg.SelectedPath;
            }
            if (selectedPath != "")
            {
                DownloadsPath.Text = selectedPath;
            }
        }
        private void ShowDebloatOptions(object sender, RoutedEventArgs e)
        {
            DebloatWindow debloatWindow = new()
            {
                Owner = this
            };
            debloatWindow.Show();
        }

        private readonly List<CheckBox> searchResults = [];
        private int searchIndex;

        public void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if ((SearchBox.Text == string.Empty) && (SearchBox.IsFocused))
            {
                SearchBoxPlaceholder.Visibility = Visibility.Hidden;
            }
            ExpandAll();
            searchResults.Clear();
            searchIndex = -1;

            string searchText = SearchBox.Text.ToLower();

            foreach (TreeViewItem parent in AllTools.Items.OfType<TreeViewItem>())
            {
                if (parent.Header is not CheckBox) continue;
                if (!originalChildOrder.TryGetValue(parent, out var originalChildren))
                    continue;
                parent.Items.Clear();

                List<CheckBox> matchingChildren = [];
                List<CheckBox> nonMatchingChildren = [];

                foreach (CheckBox child in originalChildren)
                {
                    string content = child.Content.ToString()!.ToLower();

                    if (!string.IsNullOrWhiteSpace(searchText) && content.Contains(searchText))
                    {
                        child.Visibility = Visibility.Visible;
                        child.Foreground = Brushes.Red;
                        matchingChildren.Add(child);
                        searchResults.Add(child);
                    }
                    else
                    {
                        child.Visibility = string.IsNullOrWhiteSpace(searchText) ? Visibility.Visible : Visibility.Collapsed;
                        child.Foreground = Brushes.Black;
                        if (string.IsNullOrWhiteSpace(searchText))
                            nonMatchingChildren.Add(child);
                    }
                }

                foreach (var match in matchingChildren)
                {
                    parent.Items.Add(match);
                }
                foreach (var nonMatch in nonMatchingChildren)
                {
                    parent.Items.Add(nonMatch);
                }

                if (matchingChildren.Count > 0 || string.IsNullOrWhiteSpace(searchText))
                {
                    parent.Visibility = Visibility.Visible;
                    parent.IsExpanded = matchingChildren.Count > 0;
                }
                else
                {
                    parent.Visibility = Visibility.Collapsed;
                }
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                SearchBoxPlaceholder.Visibility = Visibility.Hidden;
                searchResults.Clear();
                searchIndex = -1;
                ClearSearch(this, new RoutedEventArgs());
            }
            else
            {
                SearchBox.Background = null;
                if (searchResults.Count > 0)
                {
                    searchIndex = 0;
                    searchResults[0].BringIntoView();
                    searchResults[0].FontWeight = FontWeights.Bold;
                }
            }
        }

        public void NextResult(object sender, RoutedEventArgs e)
        {
            if (searchResults.Count == 0)
            {
                return;
            }
            foreach (CheckBox checkBox in searchResults)
            {
                checkBox.FontWeight = FontWeights.Normal;
            }
            searchIndex = (searchIndex + 1) % searchResults.Count;
            CheckBox current = searchResults[searchIndex];
            current.BringIntoView();
            current.FontWeight = FontWeights.Bold;
        }

        public void PreviousResult(object sender, RoutedEventArgs e)
        {
            if (searchResults.Count == 0)
            {
                return;
            }
            foreach (CheckBox checkBox in searchResults)
            {
                checkBox.FontWeight = FontWeights.Normal;
            }
            searchIndex = (searchIndex - 1 + searchResults.Count) % searchResults.Count;
            CheckBox current = searchResults[searchIndex];
            current.BringIntoView();
            current.FontWeight = FontWeights.Bold;
        }
        public void ClearSearch(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            if (!SearchBox.IsFocused)
            {
                SearchBoxPlaceholder.Visibility = Visibility.Visible;
            }

            foreach (TreeViewItem parent in AllTools.Items.OfType<TreeViewItem>())
            {
                parent.Visibility = Visibility.Visible;
                parent.IsExpanded = false;

                foreach (CheckBox childCheckBox in parent.Items.OfType<CheckBox>())
                {
                    childCheckBox.Visibility = Visibility.Visible;
                    childCheckBox.Foreground = Brushes.Black;
                    childCheckBox.FontWeight = FontWeights.Normal;
                }
            }
            searchResults.Clear();
            searchIndex = 0;
            CollapseAll();
        }


        private void ClearConsole(object sender, RoutedEventArgs e)
        {
            OutputConsole.Clear();
            OutputExpander.IsExpanded = false;
        }
        //private void TestButton_Click(object sender, RoutedEventArgs e)
        //Used simply for testing

        private void StandalonesPath_GotFocus(object sender, RoutedEventArgs e)
        {
            StandalonesPlaceholder.Visibility = Visibility.Hidden;
        }

        private void StandalonesPath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StandalonesPath.Text))
            {
                StandalonesPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void DownloadsPath_GotFocus(object sender, RoutedEventArgs e)
        {
            DownloadsPlaceholder.Visibility = Visibility.Hidden;
        }

        private void DownloadsPath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DownloadsPath.Text))
            {
                DownloadsPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchBoxPlaceholder.Visibility = Visibility.Hidden;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBoxPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void UserName_GotFocus(object sender, RoutedEventArgs e)
        {
            UserNamePlaceholder.Visibility = Visibility.Hidden;
        }

        private void UserName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserName.Text))
            {
                UserNamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void HostName_GotFocus(object sender, RoutedEventArgs e)
        {
            HostNamePlaceholder.Visibility = Visibility.Hidden;
        }

        private void HostName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HostName.Text))
            {
                HostNamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void XUser_GotFocus(object sender, RoutedEventArgs e)
        {
            XUserPlaceholder.Visibility = Visibility.Hidden;
        }

        private void XUser_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(XUser.Text))
            {
                XUserPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void XPass_GotFocus(object sender, RoutedEventArgs e)
        {
            XPassPlaceholder.Visibility = Visibility.Hidden;
        }

        private void XPass_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(XPass.Text))
            {
                XPassPlaceholder.Visibility = Visibility.Visible;
            }
        }
        private void ExpandCollapse_Click(object sender, RoutedEventArgs e)
        {
            if (ExpandCollapseTextBox.Text == "Expand All")
            {
                ExpandAll();
                ExpandCollapseTextBox.Text = "Collapse All";
            }
            else if (ExpandCollapseTextBox.Text == "Collapse All")
            {
                CollapseAll();
                ExpandCollapseTextBox.Text = "Expand All";
            }
        }
    }
}