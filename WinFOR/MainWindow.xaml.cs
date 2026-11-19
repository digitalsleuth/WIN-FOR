using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace WinFOR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly DispatcherTimer? elapsedTimer = new();
        private static readonly Stopwatch? stopWatch = new();
        private static string customThemeZip = "";
        private string mode = "install";
        private static readonly string runningUser = Environment.UserName;
        private static readonly string currentHostname = Environment.MachineName;
        // Branding
        internal const string src = "winfor";
        private const string displayName = "Win-FOR";
        private const string authorName = "Corey Forman (digitalsleuth)";
        private const string authorUrl = "https://github.com/digitalsleuth";
        private const string logoToolTip = $"Visit me on GitHub!\n{authorUrl}";
        private const string hostNameToolTip = $"Enter the hostname you would like for your installation.\nIf nothing is selected, the default of {defaultHostName} will be used.";
        private const string minionName = "WIN-FOR";
        private const string defaultHostName = "WIN-FOR";
        private const string defaultTheme = "WIN-FOR";
        private const string defaultThemeFolder = "winfor";
        private const string startLayoutFile = "WIN-FOR-StartLayout.xml";
        private static readonly string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:159.0) Gecko/20100101 Firefox/159.0";
        // Local paths and file names
        internal const string localRoot = @"C:\";
        internal const string defaultStandalonesPath = @"C:\standalone";
        internal const string defaultDownloadsPath = $@"{localRoot}{src}-downloads";
        internal const string debloatTempDir = @"C:\salt\tempdownload";
        internal const string standalonesToolTip = $"Choose where the standalone tools will be installed on your system.\nIf left blank, the default is {defaultStandalonesPath}";
        private const string downloadsToolTip = $"If selecting the 'Download' option,\nuse this to choose where the files will be downloaded.\nIf left blank, the default is {defaultDownloadsPath}";
        private const string saltDataRoot = @"C:\ProgramData\Salt Project\Salt";
        internal const string saltPath = $@"{saltDataRoot}\srv\salt";
        private const string saltInstallDir = @"C:\Program Files\Salt Project\Salt";
        private const string saltExe = $@"{saltInstallDir}\salt-call.exe";
        private const string gitCmdDir = @"C:\Program Files\Git\cmd";
        private const string gitExe = $@"{gitCmdDir}\git.exe";
        internal const string saltLogPrefix = $"{src}-saltstack-";
        private const string outputLogPrefix = $"{src}-output-";
        private const string versionFileName = $"{src}-version";
        private const string tempFolderName = $"{src}-temp";
        private static readonly string envPath = Environment.GetEnvironmentVariable("Path")!;
        private static readonly string gitPath = $@"{envPath};{gitCmdDir}";
        // URLs
        private static readonly string saltUri = "https://packages.broadcom.com/artifactory/saltproject-generic/windows/";
        private static readonly string gitUri = "https://github.com/git-for-windows/git/releases/download/v";
        private static readonly string githubSaltRepo = "https://github.com/digitalsleuth/winfor-salt";
        private static readonly string githubApi = "https://api.github.com/repos/digitalsleuth/winfor-salt/releases/latest";
        private static readonly string githubTags = "https://github.com/digitalsleuth/winfor-salt/archive/refs/tags";
        private static readonly string githubReleaseDownload = "https://github.com/digitalsleuth/winfor-salt/releases/download";
        private static readonly string githubBinaryApi = "https://api.github.com/repos/digitalsleuth/win-for/releases/latest";
        private static readonly string githubBinaryReleaseDownload = "https://github.com/digitalsleuth/win-for/releases/download";
        private static readonly string githubBinaryRepo = "https://github.com/digitalsleuth/win-for";
        private static readonly string toolListPdf = "https://github.com/digitalsleuth/WIN-FOR/raw/main/WIN-FOR-Tool-List.pdf";
        internal const string githubRaw = "https://raw.githubusercontent.com/digitalsleuth/winfor-salt";
        private static readonly string configFile = $"{githubRaw}/main/.config";
        private static readonly string layoutFile = $"{githubRaw}/main/{src}/config/layout/layout.json";
        private static readonly string localLayoutFile = Path.Combine(AppContext.BaseDirectory, "layout.json");
        private static readonly string localConfigFile = Path.Combine(AppContext.BaseDirectory, ".config");
        private static readonly bool debugMode = Environment.GetCommandLineArgs().Skip(1).Any(arg => arg.Equals("--debug", StringComparison.OrdinalIgnoreCase));
        private static readonly string themeTemplateZip = "https://github.com/digitalsleuth/WIN-FOR/raw/refs/heads/main/blank-template.zip";
        private const int searchDelayMs = 150;
        private const double treeSlack = 9;
        private const double minConsoleHeight = 80;
        private const double treeFrame = 10;
        private const double treeCornerRadius = 6;
        private double consoleHeight = 220;
        private double collapsedMinHeight;
        private static readonly HashSet<string> stateInfrastructure = ["repos", "config", "cleanup", "set_version", "config_debloat_windows"];
        private static readonly Regex hostNameRegex = new("^[A-Za-z0-9-]{1,15}$");
        private static readonly Regex releaseNameRegex = new(@"^v?[0-9A-Za-z.]+$");
        private static readonly SolidColorBrush errorBrush = new(Color.FromRgb(0xCB, 0x39, 0x3B));
        private static readonly SolidColorBrush countBrush = new(Color.FromRgb(0x77, 0x77, 0x77));
        private static readonly SolidColorBrush onlineBackground = new(Color.FromRgb(0xDF, 0xF6, 0xDD));
        private static readonly SolidColorBrush onlineForeground = new(Color.FromRgb(0x0F, 0x5B, 0x1C));
        private static readonly SolidColorBrush offlineBackground = new(Color.FromRgb(0xFD, 0xE7, 0xE9));
        private static readonly SolidColorBrush offlineForeground = new(Color.FromRgb(0x9E, 0x1B, 0x24));
        internal static readonly HttpClient httpClient = CreateHttpClient(true);
        private static readonly HttpClient redirectClient = CreateHttpClient(false);
        private readonly TextBoxOutputter consoleWriter;
        private readonly DispatcherTimer searchTimer = new();
        private bool bulkUpdating;
        private ResultsWindow? resultsWindow;
        private DebloatWindow? debloatWindow;
        private List<TreeItems> layoutItems = [];
        private readonly Dictionary<string, CheckBox> toolBoxes = [];
        private readonly Dictionary<Control, object?> fieldTips = [];
        private readonly Dictionary<TreeViewItem, TextBlock> sectionCounts = [];
#pragma warning disable CS8602 // Deference of a possibly null reference.
        private static readonly Version? appVersion = new(Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
#pragma warning restore CS8602 // Deference of a possibly null reference.
        // Optional X-Ways feature - implemented in MainWindow.XWays.cs, and skipped entirely when that file is deleted
        partial void XwaysCheckInputs(ref bool cancel, ref bool selected, ref string token);
        partial void XwaysInsertToken(bool selected, string token, bool install);
        partial void XwaysScrubToken(bool selected, bool install);
        partial void XwaysValidateField(Control field, string text, bool leaving, ref bool skipQuoteCheck, ref string? error);
        partial void XwaysWireTool(string cbName, CheckBox toolCheckBox);
        partial void XwaysReset();
        public MainWindow()
        {
            InitializeComponent();
            collapsedMinHeight = MinHeight;
            DataContext = this;
            mainWindow.Title = $"{displayName} v{appVersion}{(debugMode ? " [DEBUG]" : "")}";
            Placeholder.SetText(HostName, currentHostname);
            Placeholder.SetText(UserName, runningUser);
            Placeholder.SetText(StandalonesPath, defaultStandalonesPath);
            Placeholder.SetText(DownloadsPath, defaultDownloadsPath);
            StandalonesLabel.ToolTip = StandalonesPath.ToolTip = standalonesToolTip;
            DownloadsLabel.ToolTip = DownloadsPath.ToolTip = downloadsToolTip;
            HostNameLabel.ToolTip = hostNameToolTip;
            DS_FingerPrint.ToolTip = logoToolTip;
            consoleWriter = new TextBoxOutputter(OutputConsole);
            Console.SetOut(consoleWriter);
            elapsedTimer!.Tick += new EventHandler(ElapsedTime!);
            elapsedTimer.Interval = TimeSpan.FromSeconds(1);
            searchTimer.Interval = TimeSpan.FromMilliseconds(searchDelayMs);
            searchTimer.Tick += (sender, e) => { searchTimer.Stop(); ApplySearch(); };
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.LoadFile, (sender, e) => { FileLoad(); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.LoadFile, new KeyGesture(Key.L, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.SaveFile, (sender, e) => { FileSave(); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.SaveFile, new KeyGesture(Key.S, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(KeyboardShortcuts.SaveOffline, (sender, e) => { FileSaveClickOffline(sender, e); }, (sender, e) => { e.CanExecute = true; }));
            InputBindings.Add(new KeyBinding(KeyboardShortcuts.SaveOffline, new KeyGesture(Key.O, ModifierKeys.Control)));
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
            if (debugMode)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[DEBUG] Debug mode is on - the software configuration and layout come from {localConfigFile} and {localLayoutFile} instead of GitHub.");
            }
            UpdateNetworkState();
            NetworkChange.NetworkAddressChanged += (sender, e) => Dispatcher.BeginInvoke(UpdateNetworkState);
            _ = GenerateTree();
        }

        public static class KeyboardShortcuts
        // Setup bindings and RoutedCommands for Keyboard Shortcuts for the Menu
        {
            static KeyboardShortcuts()
            {
                LoadFile = new RoutedCommand("LoadFile", typeof(MainWindow));
                SaveFile = new RoutedCommand("SaveFile", typeof(MainWindow));
                SaveOffline = new RoutedCommand("SaveOffline", typeof(MainWindow));
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
            public static RoutedCommand SaveOffline { get; private set; }
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
            private readonly ConcurrentQueue<string> pending = new();
            private int flushScheduled;

            public override void Write(string? value)
            {
                if (string.IsNullOrEmpty(value)) return;

                pending.Enqueue(value);
                if (Interlocked.Exchange(ref flushScheduled, 1) == 0)
                {
                    textBox.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(FlushPending));
                }
            }

            public void FlushPending()
            {
                Interlocked.Exchange(ref flushScheduled, 0);
                if (pending.IsEmpty) return;
                StringBuilder batch = new();
                while (pending.TryDequeue(out string? text))
                {
                    batch.Append(text);
                }
                textBox.AppendText(batch.ToString());
                textBox.ScrollToEnd();
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
        private bool? networkConnected;
        private bool manualOffline;
        private bool layoutLoading;
        private bool layoutReloadPending;
        private bool layoutLoadedOffline;
        private bool OfflineMode => manualOffline || networkConnected == false;
        private void UpdateNetworkState()
        // Enables the buttons that need internet access only while a network connection is present, and switches to Offline mode while there is none
        {
            bool connected = CheckNetworkConnection.IsConnected();
            if (connected == networkConnected)
            {
                return;
            }
            bool first = networkConnected is null;
            bool wasOffline = OfflineMode;
            bool wasDisconnected = networkConnected == false;
            networkConnected = connected;
            ApplyMode();
            if (!connected)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput("[WARNING] No network connection detected. The Download, Offline, and WSL Only functions have been disabled, and Install has switched to Install Offline.");
            }
            else if (wasDisconnected)
            {
                ConsoleOutput("Network connection detected. The Download, Offline, and WSL Only functions have been enabled.");
            }
            if (!first && OfflineMode != wasOffline)
            {
                _ = ReloadLayout();
            }
        }
        private void ApplyMode()
        // Updates the Install button, the network chip and the menu to match the current mode
        {
            bool connected = networkConnected == true;
            bool busy = elapsedTimer!.IsEnabled;
            InstallButton.Content = OfflineMode ? "Install Offline" : "Install";
            InstallButton.ToolTip = OfflineMode ? "Will install the downloaded tools / environment on this system without network access" : "Will install the chosen tools / environment";
            DownloadButton.IsEnabled = connected;
            OfflineButton.IsEnabled = connected;
            InstallWslButton.IsEnabled = connected;
            NetworkChip.Background = OfflineMode ? offlineBackground : onlineBackground;
            NetworkChipText.Foreground = OfflineMode ? offlineForeground : onlineForeground;
            NetworkChipText.Text = !connected ? "Offline" : manualOffline ? "Offline mode" : "Online";
            NetworkChip.IsEnabled = !busy;
            NetworkChip.Opacity = busy ? 0.5 : 1;
            NetworkChip.Cursor = busy ? Cursors.Arrow : Cursors.Hand;
            NetworkChip.ToolTip = busy ? "The mode cannot be changed while a task is running" : !connected ? "No network connection - Offline mode stays on until one is available" : manualOffline ? "Offline mode - click to switch back to Online mode" : "Online mode - click to switch to Offline mode";
            OfflineModeOption.Header = manualOffline ? "Switch to _Online mode" : "Switch to _Offline mode";
            OfflineModeOption.IsEnabled = connected && !busy;
        }
        private void StartTimer()
        // Starts the elapsed timer and locks the mode switch until the task ends
        {
            elapsedTimer!.Start();
            ApplyMode();
        }
        private void StopTimer()
        {
            elapsedTimer!.Stop();
            ApplyMode();
            if (OfflineMode != layoutLoadedOffline)
            {
                _ = ReloadLayout();
            }
        }
        private void ToggleOfflineMode(object sender, RoutedEventArgs e)
        // Switches between Online and Offline mode without touching the network connection
        {
            if (networkConnected != true)
            {
                ConsoleOutput("[INFO] There is no network connection, so Offline mode stays on until one is available.");
                return;
            }
            manualOffline = !manualOffline;
            ApplyMode();
            ConsoleOutput(manualOffline ? "Offline mode is on. Install has switched to Install Offline." : "Offline mode is off. Install has switched back to Install.");
            _ = ReloadLayout();
        }
        private async Task ReloadLayout()
        // Loads the layout that matches the current mode, keeping the displayed one when that layout is unavailable
        {
            if (layoutLoading)
            {
                layoutReloadPending = true;
                return;
            }
            layoutLoading = true;
            try
            {
                do
                {
                    layoutReloadPending = false;
                    if (elapsedTimer!.IsEnabled)
                    {
                        ConsoleOutput("[INFO] A task is running, so the layout will be reloaded when it finishes.");
                    }
                    else if (OfflineMode && !File.Exists(localLayoutFile))
                    {
                        layoutLoadedOffline = true;
                        LayoutSourceText.Text = layoutItems.Count > 0 ?"Layout Source: Local (GitHub copy)" : LayoutSourceText.Text;
                        ConsoleOutput($"[INFO] No layout.json was found in {AppContext.BaseDirectory}, so the layout already loaded from GitHub is being used offline.");
                    }
                    else
                    {
                        await GenerateTree();
                    }
                } while (layoutReloadPending);
            }
            finally
            {
                layoutLoading = false;
            }
        }
        private void ElapsedTime(object source, EventArgs e)
        {
            TimeSpan timeSpan = stopWatch!.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            TimerLabel.Content = elapsedTime;
        }

        private void Logo_Click(object sender, MouseButtonEventArgs e)
        {
            OpenUrl(authorUrl);
        }
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to launch process:\n{ex}");
            }
        }


        private static HttpClient CreateHttpClient(bool followRedirects)
        {
            HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = followRedirects });
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            return client;
        }
        internal static Task<string?> LatestSaltRelease()
        {
            return LatestReleaseTag(githubSaltRepo, githubApi);
        }
        private static async Task<string?> LatestReleaseTag(string repoUrl, string apiUrl)
        // The github.com redirect has no API rate limit, so the API is only a fallback
        {
            try
            {
                using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(30));
                using HttpResponseMessage redirect = await redirectClient.GetAsync($"{repoUrl}/releases/latest", HttpCompletionOption.ResponseHeadersRead, cancellation.Token);
                string location = redirect.Headers.Location?.ToString() ?? "";
                int tagIndex = location.LastIndexOf("/tag/");
                if (tagIndex >= 0)
                {
                    return Uri.UnescapeDataString(location[(tagIndex + 5)..]);
                }
                using JsonDocument api = JsonDocument.Parse(await httpClient.GetStringAsync(apiUrl, cancellation.Token));
                return api.RootElement.GetProperty("tag_name").ToString();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
            {
                ConsoleOutput($"[ERROR] Unable to look up the latest release of {repoUrl}: {ex.Message}");
                return null;
            }
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
                foreach (TreeViewItem ti in AllTools.Items.OfType<TreeViewItem>())
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
                foreach (TreeViewItem treeItem in AllTools.Items.OfType<TreeViewItem>())
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
        // Unchecks all tool CheckBoxes, including any which a search has detached from the TreeView
        {
            try
            {
                SetAllTools(false);
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to Uncheck All:\n{ex}");
            }
        }
        private void SetAllTools(bool isChecked)
        {
            bulkUpdating = true;
            try
            {
                foreach (CheckBox checkBox in originalChildOrder.Values.SelectMany(children => children))
                {
                    checkBox.IsChecked = isChecked;
                }
            }
            finally
            {
                bulkUpdating = false;
            }
            RefreshSectionStates();
        }
        private void UpdateSectionState(TreeViewItem section)
        {
            if (section.Header is not CheckBox sectionBox || !originalChildOrder.TryGetValue(section, out List<CheckBox>? children))
            {
                return;
            }
            int checkedCount = children.Count(checkBox => checkBox.IsChecked == true);
            bulkUpdating = true;
            try
            {
                sectionBox.IsThreeState = checkedCount > 0 && checkedCount < children.Count;
                sectionBox.IsChecked = checkedCount == children.Count ? true : checkedCount == 0 ? false : null;
            }
            finally
            {
                bulkUpdating = false;
            }
            if (sectionCounts.TryGetValue(section, out TextBlock? count))
            {
                count.Text = $"{checkedCount} / {children.Count}";
            }
            SelectionSummary.Text = $"{originalChildOrder.Values.Sum(sectionChildren => sectionChildren.Count(checkBox => checkBox.IsChecked == true))} of {originalChildOrder.Values.Sum(sectionChildren => sectionChildren.Count)} tools selected";
        }
        private void RefreshSectionStates()
        {
            foreach (TreeViewItem section in originalChildOrder.Keys)
            {
                UpdateSectionState(section);
            }
        }
        private void SetTool(string name, bool isChecked)
        {
            if (toolBoxes.TryGetValue(name, out CheckBox? checkBox))
            {
                checkBox.IsChecked = isChecked;
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

        private void CheckAll()
        // Checks all tool CheckBoxes, including any which a search has detached from the TreeView
        {
            try
            {
                SetAllTools(true);
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to Check All:\n{ex}");
            }
        }
        private void FileSaveClickCustom(object sender, RoutedEventArgs e)
        {
            mode = "install";
            FileSave();
        }
        private void FileSaveClickOffline(object sender, RoutedEventArgs e)
        {
            mode = "offline";
            FileSave();
        }
        private void FileSave()
        // Save the selections as a Custom SaltStack state file for re-load or re-use later
        {
            try
            {
                string fileName = "custom";
                bool isThemed = themed.IsChecked == true;
                bool wslInstall = WSL.IsChecked == true;
                string allTools = GenerateState(mode, isThemed, wslInstall);
                if (mode == "offline")
                {
                    fileName = "init";
                }
                SaveFileDialog saveFileDialog = new()
                {
                    FileName = fileName,
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
                        if (stateInfrastructure.Contains(line))
                        {
                            continue;
                        }
                        else if (line == "wsl")
                        {
                            WSL.IsChecked = true;
                        }
                        else if (line.StartsWith("theme_"))
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
                    bulkUpdating = true;
                    try
                    {
                        foreach (string tool in listedTools)
                        {
                            if (toolBoxes.TryGetValue(tool, out CheckBox? checkBox))
                            {
                                checkBox.IsChecked = true;
                            }
                            else
                            {
                                OutputExpander.IsExpanded = true;
                                ConsoleOutput($"{tool} is not, or is no longer, an available option - please check your custom state and try again. To continue using your custom state without this tool, simply remove the two lines containing that tool - one under the \"include\" heading, and one under the \"{src}-custom-states\" heading.");
                            }
                        }
                    }
                    finally
                    {
                        bulkUpdating = false;
                    }
                    RefreshSectionStates();
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
            Theme.Text = defaultTheme;
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

        public static class WslChoices
        {
            public static string? WslSelection;
        }
        private void WslComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WSLChoice.IsEnabled)
            {
                WslChoices.WslSelection = (WSLChoice.SelectedItem as ComboBoxItem)?.Name.ToString();
                WSLChoice.ToolTip = (WSLChoice.SelectedItem as ComboBoxItem)?.Content.ToString();
            }
        }

        private void WslComboBox_EnableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (WSLChoice.IsEnabled)
            {
                WslChoices.WslSelection = (WSLChoice.SelectedItem as ComboBoxItem)?.Name.ToString();
                WSLChoice.ToolTip = (WSLChoice.SelectedItem as ComboBoxItem)?.Content.ToString();
            }
        }

        private void EnableWsl(object sender, RoutedEventArgs e)
        {
            WSLChoice.Text = "SIFT and REMnux";
            WSLChoice.IsEnabled = true;
        }
        private void DisableWsl(object sender, RoutedEventArgs e)
        {
            WSLChoice.Text = null;
            WSLChoice.IsEnabled = false;
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
        private static string ThemeFolder(bool themed)
        {
            if (!themed)
            {
                return defaultThemeFolder;
            }
            return ThemeChoices.SelectedTheme switch
            {
                "CPC-WIN" => "cpcwin",
                "Custom" => "custom-theme",
                _ => defaultThemeFolder
            };
        }
        public string GenerateState(string stateType, bool themedInstall, bool wslInstall)
        // Used to generate the data for the custom SaltStack state file
        {
            string allToolsString = "";
            try
            {
                string repo = ThemeFolder(themedInstall);
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
                else if (stateType == "download" || stateType == "offline")
                {
                    includeTool.Append("include:\n");
                    requireTool.Append($"{stateType}-only-states:\n");
                    requireTool.Append("  test.nop:\n");
                    requireTool.Append("    - require:\n");
                    if (stateType == "offline")
                    {
                        includeTool.Append($"  - {src}.set-version\n");
                        requireTool.Append($"      - sls: {src}.set-version\n");
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
                }
                foreach (string tool in allChecked)
                {
                    int underScoreIndex = tool.IndexOf('_');
                    if (tool.Split("_")[0] == "python3")
                    {
                        string pythonTool = tool.Remove(underScoreIndex, "_".Length).Insert(underScoreIndex, "-");
                        int secondUnderScoreIndex = pythonTool.IndexOf('_');
                        string pythonVal = pythonTool.Remove(secondUnderScoreIndex, "_".Length).Insert(secondUnderScoreIndex, ".");
                        pythonVal = pythonVal.Replace('_', '-');
                        states.Add(pythonVal);
                    }
                    else if (tool == "themed" || tool == "WSL")
                    {
                        continue;
                    }
                    else
                    {
                        string notPythonVal = tool.Remove(underScoreIndex, "_".Length).Insert(underScoreIndex, ".");
                        notPythonVal = notPythonVal.Replace('_', '-');
                        states.Add(notPythonVal);
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
                    else if (stateType == "offline")
                    {
                        includeTool.Append($"  - {src}.offline.{selection}\n");
                        requireTool.Append($"      - sls: {src}.offline.{selection}\n");
                    }
                }
                if (themedInstall)
                {
                    string themeState = stateType == "download" ? "downloads.theme" : $"theme.{repo}";
                    includeTool.Append($"  - {src}.{themeState}\n");
                    requireTool.Append($"      - sls: {src}.{themeState}\n");
                }
                if (stateType == "install" || stateType == "offline")
                {
                    includeTool.Append($"  - {src}.cleanup\n");
                    requireTool.Append($"      - sls: {src}.cleanup\n");
                }
                if (stateType == "download")
                {
                    includeTool.Append($"  - {src}.downloads.offline-staging\n");
                    requireTool.Append($"      - sls: {src}.downloads.offline-staging\n");
                }
                if (wslInstall && stateType == "install")
                {
                    includeTool.Append($"  - {src}.wsl\n");
                    requireTool.Append($"      - sls: {src}.wsl\n");
                }
                if (wslInstall && stateType == "download")
                {
                    includeTool.Append($"  - {src}.downloads.wsl\n");
                    requireTool.Append($"      - sls: {src}.downloads.wsl\n");
                }
                if (wslInstall && stateType == "offline")
                {
                    includeTool.Append($"  - {src}.offline.wsl\n");
                    requireTool.Append($"      - sls: {src}.offline.wsl\n");
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

                foreach (CheckBox checkBox in originalChildOrder.Values.SelectMany(children => children))
                {
                    if (checkBox.IsChecked == true)
                    {
                        checkedItems.Add(checkBox.Name);
                        checkedItemsContent.Add(checkBox.Content.ToString()!);
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
            if (OfflineMode)
            {
                await InstallOffline();
                return;
            }
            try
            {
                mode = "install";
                stopWatch?.Reset();
                stopWatch?.Start();
                StartTimer();
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
                else if ((checkedItems.Count == 1) && (checkedItems.Contains("themed") || checkedItems.Contains("WSL")))
                {
                    string item = char.ToUpper(checkedItems[0][0]) + checkedItems[0][1..];
                    if (item == "WSL")
                    {
                        MessageBox.Show($"Only {item} checkbox was selected!\nChoose at least one item from the tool list to install.\nTo install WSL only, use the \"WSL Only\" button on the side.", $"Only {item} checkbox selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Only {item} checkbox was selected!\nChoose at least one item from the tool list to install.", $"Only {item} checkbox selected!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
                else if ((checkedItems.Count == 2) && (checkedItems.Contains("themed") && checkedItems.Contains("WSL")))
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
                bool xwaysCancel = false;
                bool xwaysSelected = false;
                string xwaysToken = "";
                bool isThemed;
                bool wslSelected;
                string userName;
                string standalonesPath;
                XwaysCheckInputs(ref xwaysCancel, ref xwaysSelected, ref xwaysToken);
                if (xwaysCancel)
                {
                    return;
                }
                if (themed.IsChecked == true)
                {
                    isThemed = true;
                    ConsoleOutput($"Selected theme is {ThemeChoices.SelectedTheme}");
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
                userName = UserName.Text == "" ? runningUser : UserName.Text;
                ConsoleOutput($"Selected user is {userName}");
                if (StandalonesPath.Text != "")
                {
                    standalonesPath = $@"{StandalonesPath.Text}";
                    ConsoleOutput($"Standalones path is {standalonesPath}");
                }
                else
                {
                    standalonesPath = defaultStandalonesPath;
                    ConsoleOutput($"Standalones path box was empty - default will be used - {standalonesPath}");
                }
                if (!InputsAreValid(userName, standalonesPath) || (isThemed && !HostNameIsValid()))
                {
                    return;
                }
                string tempDir = $@"{driveLetter}{tempFolderName}\";
                string? releaseVersion = await PrepareEnvironment(tempDir);
                if (releaseVersion is null)
                {
                    return;
                }
                ConsoleOutput("Extracting archive...");
                ConsoleOutput("Checking for and removing previous repo folder");
                if (Directory.Exists(@$"{saltPath}\win\"))
                {
                    ManageDirectory(@$"{saltPath}\win\", "delete");
                }
                string stateFile = GenerateState(mode, isThemed, wslSelected);
                if (!ExtractStates(tempDir, releaseVersion))
                {
                    return;
                }
                await WriteDebloatPreset(saltPath);
                XwaysInsertToken(xwaysSelected, xwaysToken, true);
                if (isThemed)
                {
                    await PrepareTheme(standalonesPath);
                }
                if (!CopyCustomState(@$"{saltPath}\{src}\custom.sls", stateFile))
                {
                    return;
                }
                await ExecuteSaltStack(userName, standalonesPath, releaseVersion);
                await File.WriteAllTextAsync($@"{localRoot}{versionFileName}", releaseVersion);
                XwaysScrubToken(xwaysSelected, true);
                if (wslSelected)
                {
                    ConsoleOutput("WSL is selected, and will be installed last as a system reboot is required.");
                    await ExecuteWsl(userName, releaseVersion, standalonesPath);
                }
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to complete the installation process:\n{ex}");
            }
            finally
            {
                stopWatch?.Stop();
                StopTimer();
            }
        }
        private static string OfflineRelease(string offlineRoot)
        // The staged states carry their release in a VERSION file, which names the log the same way the online installs do
        {
            string versionFile = $@"{offlineRoot}\{src}\VERSION";
            string release = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "";
            return releaseNameRegex.IsMatch(release) ? release : "unversioned";
        }
        private async Task InstallOffline()
        // Asks for the offline paths, then installs from the downloaded files with the salt-call.exe that was downloaded alongside them
        {
            OfflineWindow dialog = new(StandalonesPath.Text, DownloadsPath.Text, UserName.Text) { Owner = this };
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            try
            {
                stopWatch?.Reset();
                stopWatch?.Start();
                StartTimer();
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"{displayName} v{appVersion} - Offline install");
                ConsoleOutput($"Downloads path is {dialog.SelectedDownloads}");
                ConsoleOutput($"Standalones path is {dialog.SelectedStandalones}");
                ConsoleOutput($"Selected user is {dialog.SelectedUser}");
                string offlineRoot = $@"{dialog.SelectedDownloads}\offline";
                string offlineSalt = $@"{offlineRoot}\Salt\salt-call.exe";
                string pillar = BuildPillar($"{src}_user", dialog.SelectedUser, dialog.SelectedStandalones, false)
                    + $", 'offline': '{dialog.SelectedDownloads.Replace("'", "''")}', 'saltpath': '{offlineSalt.Replace("'", "''")}'";
                await WriteDebloatPreset(offlineRoot);
                await RunSaltCall($"{src}.offline", pillar, $"{OfflineRelease(offlineRoot)}-offline", "Installing the selected states.", false, offlineSalt, offlineRoot);
            }
            catch (Exception ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to complete the offline installation process:\n{ex}");
            }
            finally
            {
                stopWatch?.Stop();
                StopTimer();
            }
        }
        internal static bool InputsAreValid(params string[] values)
        // Values are placed inside a salt-call pillar argument, so a double quote or line break would corrupt the command
        {
            foreach (string value in values)
            {
                if (value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                {
                    MessageBox.Show($"The value \"{value}\" contains a double quote or a line break, which cannot be used. Please correct it and try again.", "Invalid value", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            return true;
        }
        private void Field_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Control field)
            {
                ValidateField(field);
            }
        }
        private void ValidateField(Control field, bool leaving = false)
        // Marks a field with a red border and an explanatory tooltip as soon as its content is not usable
        {
            string text = field is PasswordBox passwordBox ? passwordBox.Password : ((TextBox)field).Text;
            bool skipQuoteCheck = false;
            string? error = null;
            XwaysValidateField(field, text, leaving, ref skipQuoteCheck, ref error);
            if (field == HostName && text != "" && !hostNameRegex.IsMatch(text))
            {
                error = "A hostname may only contain letters, numbers and hyphens, and be at most 15 characters long.";
            }
            else if (!skipQuoteCheck && text.Contains('"'))
            {
                error = "A double quote cannot be used here.";
            }
            SetFieldError(field, error);
        }
        private void SetFieldError(Control field, string? message)
        {
            if (message is null)
            {
                field.ClearValue(Control.BorderBrushProperty);
                if (fieldTips.TryGetValue(field, out object? tip))
                {
                    field.ToolTip = tip;
                }
                return;
            }
            fieldTips.TryAdd(field, field.ToolTip);
            field.BorderBrush = errorBrush;
            field.ToolTip = message;
        }
        private bool HostNameIsValid()
        {
            if (HostName.Text == "" || hostNameRegex.IsMatch(HostName.Text))
            {
                return true;
            }
            MessageBox.Show("A hostname may only contain letters, numbers and hyphens, and be at most 15 characters long.", "Invalid hostname", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
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
        private static bool GitReportsVersion(string gitVersion)
        // git --version prints the exact release tag
        {
            if (!File.Exists(gitExe))
            {
                return false;
            }
            try
            {
                using Process process = Process.Start(new ProcessStartInfo(gitExe, "--version") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true })!;
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return output == $"git version {gitVersion}";
            }
            catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception)
            {
                return false;
            }
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
                string installerVersion = GitInstallerVersion(gitVersion);
                if (version32 == installerVersion || version64 == installerVersion || GitReportsVersion(gitVersion))
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
                string partialFile = $"{downloadLocation}.part";
                using HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                ConsoleOutput($"Response code {(int)response.StatusCode} - {response.StatusCode} ({Path.GetFileName(downloadLocation)})");
                response.EnsureSuccessStatusCode();
                await using (Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                await using (FileStream destination = new(partialFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    await source.CopyToAsync(destination).ConfigureAwait(false);
                }
                File.Move(partialFile, downloadLocation, true);
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
        private sealed record Tooling(string GitVersion, string GitHash, string? GitFile, string SaltVersion, string SaltHash);
        private static (string Name, string Uri, string File) SaltInstaller(string tempDir, string saltVersion)
        {
            string saltFile = $"Salt-Minion-{saltVersion}-Py3-AMD64-Setup.exe";
            return ($"SaltStack v{saltVersion}", $"{saltUri}{saltVersion}/{saltFile}", $"{tempDir}{saltFile}");
        }
        private static (string Name, string Uri, string File) GitInstaller(string tempDir, string gitVersion, string? configuredFile)
        {
            string gitFile = GitInstallerFile(gitVersion, configuredFile);
            return ($"Git v{GitInstallerVersion(gitVersion)}", $"{gitUri}{gitVersion}/{gitFile}", $"{tempDir}{gitFile}");
        }
        private static async Task<bool> InstallerAvailable((string Name, string Uri, string File) installer)
        // Confirms an installer can be downloaded before anything else is fetched, unless a copy is already in the temp folder
        {
            if (File.Exists(installer.File))
            {
                return true;
            }
            try
            {
                HttpStatusCode status = await ProbeStatus(HttpMethod.Head, installer.Uri);
                if (status is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotImplemented)
                {
                    status = await ProbeStatus(HttpMethod.Get, installer.Uri);
                }
                if ((int)status is >= 200 and < 300)
                {
                    return true;
                }
                ConsoleOutput($"[ERROR] {installer.Name} is not available at {installer.Uri} - the server responded {(int)status} ({status}).");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ConsoleOutput($"[ERROR] Unable to reach {installer.Uri} to confirm that {installer.Name} is available ({ex.Message}).");
            }
            return false;
        }
        private static async Task<HttpStatusCode> ProbeStatus(HttpMethod method, string uri)
        {
            using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(30));
            using HttpRequestMessage request = new(method, uri);
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation.Token);
            return response.StatusCode;
        }
        private static Task<bool> DownloadSaltStack(string tempDir, string saltVersion, string saltHash)
        // Downloads the pre-determined version of SaltStack
        {
            (string name, string uri, string file) = SaltInstaller(tempDir, saltVersion);
            return DownloadInstaller(name, uri, file, saltHash);
        }
        private static string GitInstallerVersion(string gitVersion)
        // Git for Windows names the first build of a release Git-2.49.0-64-bit.exe, and every later build Git-2.56.0.4-64-bit.exe
        {
            string[] parts = gitVersion.Split(".windows.");
            return parts.Length == 2 && parts[1] != "1" ? $"{parts[0]}.{parts[1]}" : parts[0];
        }
        private static string GitInstallerFile(string gitVersion, string? configuredFile)
        // The installer name given in the .config file wins, otherwise it is worked out from the release tag
        {
            return string.IsNullOrWhiteSpace(configuredFile) ? $"Git-{GitInstallerVersion(gitVersion)}-64-bit.exe" : Path.GetFileName(configuredFile.Trim());
        }
        private static Task<bool> DownloadGit(string tempDir, string gitVersion, string gitHash, string? configuredFile)
        // Downloads the pre-determined version of Git
        {
            (string name, string uri, string file) = GitInstaller(tempDir, gitVersion, configuredFile);
            return DownloadInstaller(name, uri, file, gitHash);
        }
        private static async Task<bool> DownloadInstaller(string name, string uri, string file, string hash)
        // Downloads an installer, and only reports success once its SHA256 hash matches the expected value
        {
            try
            {
                if (File.Exists(file))
                {
                    ConsoleOutput($"Found previous download of {name} - comparing hash");
                    if (await Task.Run(() => CompareHash(hash, file)))
                    {
                        ConsoleOutput($"Hash value for {file} is correct, continuing...");
                        return true;
                    }
                    ConsoleOutput("Hash values don't match, deleting existing file and downloading again...");
                    File.Delete(file);
                }
                ConsoleOutput($"Downloading {name}");
                if (!await FileDownload(uri, file))
                {
                    ConsoleOutput($"[ERROR] Unable to download {name} - an issue occurred with internet connectivity. Check your connection and try again.");
                    return false;
                }
                ConsoleOutput($"{Path.GetFileName(file)} downloaded");
                if (await Task.Run(() => CompareHash(hash, file)))
                {
                    ConsoleOutput($"Hash value for {file} is correct, continuing...");
                    return true;
                }
                File.Delete(file);
                ConsoleOutput($"[ERROR] The hash of the downloaded {name} does not match the expected value, so it will not be installed.");
                return false;
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to download {name}:\n{ex}");
                return false;
            }
        }
        private static Task<bool> InstallSaltStack(string tempDir, string saltVersion)
        // Installs the pre-determined version of SaltStack, provided it can be downloaded, or is already downloaded in the tempDir
        {
            return RunInstaller($"SaltStack v{saltVersion}", $"{tempDir}Salt-Minion-{saltVersion}-Py3-AMD64-Setup.exe", $"/S /master=localhost /minion-name={minionName}");
        }
        private static Task<bool> InstallGit(string tempDir, string gitVersion, string? configuredFile)
        // Installs the pre-determined version of Git, provided it can be downloaded, or is available in the tempDir
        {
            return RunInstaller($"Git {GitInstallerVersion(gitVersion)}", $"{tempDir}{GitInstallerFile(gitVersion, configuredFile)}", "/VERYSILENT /NORESTART /SP- /NOCANCEL /SUPPRESSMSGBOXES");
        }
        private static async Task<bool> RunInstaller(string name, string file, string arguments)
        {
            try
            {
                ConsoleOutput($"Installing {name}");
                using Process process = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = file,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                await Task.WhenAll(process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync());
                await process.WaitForExitAsync();
                if (process.ExitCode is not (0 or 3010))
                {
                    ConsoleOutput($"[ERROR] The {name} installer ended with exit code {process.ExitCode}.");
                    return false;
                }
                ConsoleOutput($"Installation of {name} is complete");
                return true;
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to install {name}:\n{ex}");
                return false;
            }
        }
        private static async Task<List<string>> IdentifyRelease()
        // Identifies the most recent release of the salt states for installation
        {
            string? release = await LatestReleaseTag(githubSaltRepo, githubApi);
            if (release is null)
            {
                MessageBox.Show("Unable to identify the latest release - check your network connectivity and try again.", "Unable to identify release", MessageBoxButton.OK, MessageBoxImage.Error);
                return [];
            }
            return [release, $"{githubTags}/{release}.zip", $"{githubReleaseDownload}/{release}/{src}-salt-{release}.zip.sha256"];
        }
        private async Task<string?> PrepareEnvironment(string tempDir)
        // Identifies the release, installs Git and SaltStack when they are missing, and leaves a verified copy of the states in tempDir
        {
            Task<List<string>> releaseTask = IdentifyRelease();
            Task<Tooling?> toolingTask = LoadTooling();
            await Task.WhenAll(releaseTask, toolingTask);
            List<string> release = await releaseTask;
            Tooling? tooling = await toolingTask;
            if (release.Count == 0 || tooling is null)
            {
                return null;
            }
            ConsoleOutput($"{tempDir} is being created for temporary storage of required files");
            ManageDirectory(tempDir, Directory.Exists(tempDir) ? "perms" : "create");
            bool gitMissing = !CheckGitInstalled(tooling.GitVersion);
            bool saltMissing = !CheckSaltStackInstalled(tooling.SaltVersion);
            ConsoleOutput(gitMissing ? $"Git {tooling.GitVersion} is not installed" : $"Git {tooling.GitVersion} is already installed");
            ConsoleOutput(saltMissing ? $"SaltStack {tooling.SaltVersion} is not installed" : $"SaltStack {tooling.SaltVersion} is already installed");
            ConsoleOutput($"Current release of {displayName} is {release[0]}");
            bool[] available = await Task.WhenAll(
                gitMissing ? InstallerAvailable(GitInstaller(tempDir, tooling.GitVersion, tooling.GitFile)) : Task.FromResult(true),
                saltMissing ? InstallerAvailable(SaltInstaller(tempDir, tooling.SaltVersion)) : Task.FromResult(true));
            if (available.Contains(false))
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput("[ERROR] A required installer is not available, so the process has been stopped. Nothing was downloaded or installed.");
                return null;
            }
            bool[] downloaded = await Task.WhenAll(
                gitMissing ? DownloadGit(tempDir, tooling.GitVersion, tooling.GitHash, tooling.GitFile) : Task.FromResult(true),
                saltMissing ? DownloadSaltStack(tempDir, tooling.SaltVersion, tooling.SaltHash) : Task.FromResult(true),
                EnsureReleaseZip(tempDir, release[0], release[1], release[2]));
            if (downloaded.Contains(false))
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput("[ERROR] A required download failed, so the process has been stopped. Nothing was installed.");
                return null;
            }
            if (gitMissing && !await InstallGit(tempDir, tooling.GitVersion, tooling.GitFile))
            {
                return null;
            }
            if (saltMissing && !await InstallSaltStack(tempDir, tooling.SaltVersion))
            {
                return null;
            }
            return release[0];
        }
        private static async Task<bool> EnsureReleaseZip(string tempDir, string release, string uriZip, string uriHash)
        // Makes sure a copy of the states zip whose hash matches the published SHA256 is in tempDir, downloading it when needed
        {
            string zipFile = $"{tempDir}{release}.zip";
            string hashFile = $"{zipFile}.sha256";
            if (File.Exists(zipFile) && File.Exists(hashFile) && new FileInfo(zipFile).Length != 0 && new FileInfo(hashFile).Length != 0)
            {
                ConsoleOutput($"{zipFile} and {hashFile} already exist and are not zero-byte files.");
                if (await ReleaseHashMatches(zipFile, hashFile))
                {
                    return true;
                }
                ConsoleOutput("Hashes do not match, downloading the release file again...");
            }
            ConsoleOutput("Downloading release file and SHA256 to compare");
            if (!await DownloadStates(zipFile, hashFile, uriZip, uriHash))
            {
                ConsoleOutput("[ERROR] Unable to download required Salt states.");
                return false;
            }
            ConsoleOutput("Downloads complete...");
            if (await ReleaseHashMatches(zipFile, hashFile))
            {
                return true;
            }
            ConsoleOutput("Hashes do not match, aborting");
            return false;
        }
        private static async Task<bool> ReleaseHashMatches(string zipFile, string hashFile)
        {
            ConsoleOutput("Comparing hashes...");
            string providedHash = (await File.ReadAllTextAsync(hashFile)).Trim().ToLowerInvariant().Split(' ')[0];
            bool match = await Task.Run(() => CompareHash(providedHash, zipFile));
            if (match)
            {
                ConsoleOutput("Hashes match, continuing...");
            }
            return match;
        }
        private async Task<Tooling?> LoadTooling()
        // Reads the pinned Git and SaltStack versions and hashes from the .config file
        {
            List<ConfigItems> softwareConfig = await GetJsonConfig();
            if (softwareConfig.Count == 0 || softwareConfig[0].Software is not Dictionary<string, SoftwareList> software
                || !software.TryGetValue("Git", out SoftwareList? git) || !software.TryGetValue("SaltStack", out SoftwareList? salt)
                || git.SoftwareVersion is null || git.SoftwareHash is null || salt.SoftwareVersion is null || salt.SoftwareHash is null)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput("[ERROR] Unable to read the software configuration - check your network connectivity and try again.");
                return null;
            }
            return new Tooling(git.SoftwareVersion, git.SoftwareHash, git.SoftwareFile, salt.SoftwareVersion, salt.SoftwareHash);
        }
        private static async Task<bool> DownloadStates(string zipFile, string hashFile, string uriZip, string uriHash)
        // Downloads the latest salt states and their SHA256 hash
        {
            try
            {
                ConsoleOutput($"Downloading {uriZip}");
                ConsoleOutput($"Downloading {uriHash}");
                bool[] downloaded = await Task.WhenAll(FileDownload(uriZip, zipFile), FileDownload(uriHash, hashFile));
                if (downloaded.Contains(false))
                {
                    return false;
                }
                ConsoleOutput($"{uriZip} downloaded.");
                ConsoleOutput($"{uriHash} downloaded.");
                return true;
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
            try
            {
                if (!File.Exists(file))
                {
                    ConsoleOutput($"[ERROR] {file} does not exist!");
                    return false;
                }
                using FileStream fileStream = File.OpenRead(file);
                string fileHash = Convert.ToHexString(SHA256.HashData(fileStream)).ToLowerInvariant();
                ConsoleOutput($"File Hash: {fileHash}");
                ConsoleOutput($"Given Hash: {hashValue}");
                return string.Equals(fileHash, hashValue.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to Compare Hash:\n{ex}");
                return false;
            }
        }
        private static bool ExtractStates(string tempDir, string release)
        // Once downloaded, or available, this will extract only the distro folder of the salt states to the required location in the Salt Project\Salt folder
        {
            try
            {
                string saltBase = $@"{saltDataRoot}\";
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
                string file = $"{tempDir}{release}.zip";
                string archiveFolder = $"{src}-salt-{release.TrimStart('v')}/{src}/";
                string distroDest = Path.GetFullPath($@"{saltBase}{src}");
                using ZipArchive archive = ZipFile.OpenRead(file);
                List<ZipArchiveEntry> entries = archive.Entries.Where(entry => entry.FullName.StartsWith(archiveFolder, StringComparison.Ordinal)).ToList();
                if (entries.Count == 0)
                {
                    ConsoleOutput($"[ERROR] Unable to extract states: {archiveFolder} was not found in {file}");
                    return false;
                }
                ConsoleOutput($"Extracting {archiveFolder} from {file} to {distroDest}");
                if (Directory.Exists(distroDest))
                {
                    ManageDirectory(distroDest, "delete");
                }
                ManageDirectory(distroDest, "create");
                foreach (ZipArchiveEntry entry in entries)
                {
                    string target = Path.GetFullPath(Path.Combine(distroDest, entry.FullName[archiveFolder.Length..]));
                    if (!target.StartsWith(distroDest + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (entry.FullName.EndsWith('/'))
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    entry.ExtractToFile(target, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to extract states:\n{ex}");
                return false;
            }
        }
        private static async Task WriteDebloatPreset(string stateRoot)
        // Writes the saved Debloat selections to debloat.preset, which is left empty (zero bytes) when none were chosen so that no debloat is done
        {
            List<string> selections = DebloatWindow.DebloatSettings.Selections;
            await File.WriteAllTextAsync($@"{stateRoot}\{src}\config\debloat.preset", string.Join("\n", selections));
            ConsoleOutput(selections.Count == 0 ? "No debloat options selected - debloat.preset has been emptied, so no debloat will be done" : $"{selections.Count} debloat options written to debloat.preset");
        }
        private async Task PrepareTheme(string standalonesPath)
        // Writes the generated start layout, the custom theme (if chosen) and the host name into the extracted states
        {
            string layout = await GenerateLayout(standalonesPath);
            await File.WriteAllTextAsync(@$"{saltPath}\{src}\config\layout\{startLayoutFile}", layout);
            if (ThemeChoices.SelectedTheme == "Custom" && customThemeZip != "")
            {
                ExtractCustomTheme(customThemeZip);
            }
            InsertHostName(HostName.Text != "" ? HostName.Text : currentHostname, ThemeFolder(true));
        }
        private static void InsertHostName(string hostName, string repo)
        {
            string hostnameState = $@"{saltPath}\{src}\theme\{repo}\computer-name.sls";
            try
            {
                if (File.Exists(hostnameState))
                {
                    string allText = File.ReadAllText(hostnameState);
                    allText = allText.Replace(defaultHostName, hostName);
                    File.WriteAllText(hostnameState, allText);
                    ConsoleOutput($"Chosen hostname {hostName} written to computer-name.sls");
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to write the selected hostname to computer-name.sls:\n{ex}");
            }
        }
        private static bool CopyCustomState(string location, string stateFile)
        // A simple copy of the generated custom stateFile (from the GenerateState function) to the proper location
        {
            bool copied = false;
            try
            {
                File.WriteAllText(location, stateFile);
                ConsoleOutput($"Custom state copied to the {location} directory");
                copied = true;
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to copy the custom state to the {location} directory:\n{ex}");
            }
            return copied;
        }
        private async void DownloadOnly(object? sender, RoutedEventArgs e)
        {
            await DownloadSelected(false);
        }
        private async void OfflineDownload(object? sender, RoutedEventArgs e)
        {
            await DownloadSelected(true);
        }
        private async Task DownloadSelected(bool offline)
        // This is used to generate a custom state for simply downloading the selected files, without any installation or modification
        // With offline set, an offline state is also generated so the downloads can be installed on a system without network access
        {
            try
            {
                mode = "download";
                stopWatch?.Reset();
                stopWatch?.Start();
                StartTimer();
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
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"{displayName} v{appVersion}");
                string driveLetter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
                bool addWSL = checkedItems.Contains("WSL");
                bool addTheme = checkedItems.Contains("themed");
                string stateList = GenerateState(mode, addTheme, addWSL);
                string tempDir = $@"{driveLetter}{tempFolderName}\";
                bool xwaysCancel = false;
                bool xwaysSelected = false;
                string xwaysToken = "";
                XwaysCheckInputs(ref xwaysCancel, ref xwaysSelected, ref xwaysToken);
                if (xwaysCancel)
                {
                    return;
                }
                string downloadPath = DownloadsPath.Text == "" ? $@"{defaultDownloadsPath}\" : DownloadsPath.Text;
                string standalonesPath;
                if (StandalonesPath.Text != "")
                {
                    standalonesPath = $@"{StandalonesPath.Text}";
                    ConsoleOutput($"Standalones path is {standalonesPath}");
                }
                else
                {
                    standalonesPath = defaultStandalonesPath;
                    ConsoleOutput($"Standalones path box was empty - default will be used - {standalonesPath}");
                }
                if (!InputsAreValid(downloadPath, standalonesPath) || (offline && addTheme && !HostNameIsValid()))
                {
                    return;
                }
                string? releaseVersion = await PrepareEnvironment(tempDir);
                if (releaseVersion is null)
                {
                    return;
                }
                ConsoleOutput("Extracting archive...");
                if (!ExtractStates(tempDir, releaseVersion))
                {
                    return;
                }
                await WriteDebloatPreset(saltPath);
                ConsoleOutput($@"State files extracted, writing {src}\downloads\init.sls");
                await File.WriteAllTextAsync(@$"{saltPath}\{src}\downloads\init.sls", stateList);
                XwaysInsertToken(xwaysSelected, xwaysToken, false);
                if (offline)
                {
                    ConsoleOutput($@"Offline staging selected, writing {src}\offline\init.sls");
                    string offlineStates = GenerateState("offline", addTheme, addWSL);
                    await File.WriteAllTextAsync(@$"{saltPath}\{src}\offline\init.sls", offlineStates);
                    if (addTheme)
                    {
                        // The theme is applied from the staged states, so its layout, custom theme and host name are written before they are copied
                        await PrepareTheme(standalonesPath);
                    }
                }
                await ExecuteSaltStackDownloads(releaseVersion, downloadPath, standalonesPath);
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to complete the Download process:\n{ex}");
            }
            finally
            {
                stopWatch?.Stop();
                StopTimer();
            }
        }

        private static void HandleProcessOutput(string? data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                ConsoleOutput(data);
            }
        }

        private Process? saltproc;
        private Process? wslproc;
        private static string BuildPillar(string key, string value, string standalonesPath, bool includeWsl = true)
        // Single quotes are doubled so they cannot end the YAML string early
        {
            string pillar = $"'{key}': '{value.Replace("'", "''")}', 'inpath': '{standalonesPath.Replace("'", "''")}'";
            if (includeWsl && WslChoices.WslSelection != null)
            {
                pillar += $", 'wsl_choice': '{WslChoices.WslSelection}'";
            }
            return pillar;
        }
        private Task ExecuteSaltStack(string userName, string standalonesPath, string release)
        // Generate a salt-call.exe process with the required arguments to install the custom salt states
        {
            return RunSaltCall($"{src}.custom", BuildPillar($"{src}_user", userName, standalonesPath), release, "Installing the selected states.", false);
        }
        private Task ExecuteSaltStackDownloads(string release, string downloadPath, string standalonesPath)
        // Generate a salt-call.exe process with the required argument to simply download the selected files
        {
            return RunSaltCall($"{src}.downloads", BuildPillar("downloads", downloadPath, standalonesPath), $"{release}-downloads", "Installing the selected states.", false);
        }
        private Task ExecuteWsl(string userName, string release, string standalonesPath)
        // A salt-call.exe process used for the installation of the Windows Subsystem for Linux v2 environment
        {
            return RunSaltCall($"{src}.wsl", BuildPillar($"{src}_user", userName, standalonesPath), $"{release}-wsl", "Installing WSL.", true);
        }
        private async Task RunSaltCall(string state, string pillar, string logName, string heading, bool isWsl, string? exePath = null, string? fileRoot = null)
        // An offline install passes the salt-call.exe and file root that were downloaded alongside the states
        {
            string logFile = $@"{localRoot}{saltLogPrefix}{logName}.log";
            string exe = exePath ?? saltExe;
            if (!File.Exists(exe))
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] {exe} was not found - {(fileRoot is null ? "SaltStack may not have been installed correctly." : "check that the downloaded files are complete.")}");
                return;
            }
            using Process process = new()
            {
                StartInfo =
                {
                    FileName = exe,
                    Arguments = "-l info --local --retcode-passthrough --state-output=mixed " + (fileRoot is null ? "" : $"--file-root=\"{fileRoot}\" ") + $"state.sls {state} pillar=\"{{ {pillar} }}\" --out-file=\"{logFile}\" --out-file-append --log-file=\"{logFile}\" --log-file-level=debug",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            try
            {
                ConsoleOutput(
                    $"{heading}\n" +
                    $"Log File: {logFile}\n" +
                    $"Executing: salt call with the following variables\n" +
                    $"  {state}\n" +
                    $"  {{ {pillar} }}\n"
                    );
                if (fileRoot is null && !envPath.Contains(gitCmdDir))
                {
                    process.StartInfo.EnvironmentVariables["PATH"] = gitPath;
                }
                if (isWsl)
                {
                    ConsoleOutput("Saving Console Output before beginning WSL installation");
                    SaveConsoleOutput("wsl", null);
                    wslproc = process;
                }
                else
                {
                    saltproc = process;
                }
                process.OutputDataReceived += (s, e) => HandleProcessOutput(e.Data);
                process.ErrorDataReceived += (s, e) => HandleProcessOutput(e.Data);
                StopButton.IsEnabled = true;
                StopButton.Visibility = Visibility.Visible;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();
                TimeSpan elapsed = process.ExitTime - process.StartTime;
                ConsoleOutput(
                    $"\nExited\t\t: {process.ExitTime}\n" +
                    $"Exit code \t: {process.ExitCode}\n" +
                    $"Elapsed time\t: {(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}");
                ConsoleOutput(process.ExitCode != 0 ? "Installation has ended with errors." : "Installation has completed successfully.");
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to execute SaltStack ({state}):\n{ex}");
            }
            finally
            {
                if (isWsl)
                {
                    wslproc = null;
                }
                else
                {
                    saltproc = null;
                }
                StopButton.Visibility = Visibility.Hidden;
                StopButton.IsEnabled = false;
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
                    ConsoleOutput($"[ERROR] Failed to kill SaltStack process: {ex.Message}");
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
                    ConsoleOutput($"[ERROR] Failed to kill SaltStack WSL process: {ex.Message}");
                }
            }
        }
        private async void InstallWslOnly(object sender, RoutedEventArgs e)
        // Used to install WSL without any other options selected.
        // This determines that all pre-reqs are met before beginning the ExecuteWsl function
        {
            try
            {
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
                stopWatch?.Reset();
                stopWatch?.Start();
                StartTimer();
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"{displayName} v{appVersion}");
                string driveLetter = Path.GetPathRoot(path: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;
                string userName = UserName.Text == "" ? runningUser : UserName.Text;
                string standalonesPath;
                ConsoleOutput($"Selected user is {userName}");
                if (StandalonesPath.Text != "")
                {
                    standalonesPath = $@"{StandalonesPath.Text}";
                    ConsoleOutput($"Standalones path is {standalonesPath}");
                }
                else
                {
                    standalonesPath = defaultStandalonesPath;
                    ConsoleOutput($"Standalones path box was empty - default will be used - {standalonesPath}");
                }
                if (!InputsAreValid(userName, standalonesPath))
                {
                    return;
                }
                string tempDir = $@"{driveLetter}{tempFolderName}\";
                string? releaseVersion = await PrepareEnvironment(tempDir);
                if (releaseVersion is null)
                {
                    return;
                }
                ConsoleOutput("Extracting archive...");
                if (!ExtractStates(tempDir, releaseVersion))
                {
                    return;
                }
                await WriteDebloatPreset(saltPath);
                await ExecuteWsl(userName, releaseVersion, standalonesPath);
                ConsoleOutput("SaltStack WSL installation completed.");
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to execute WSL install:\n{ex}");
            }
            finally
            {
                stopWatch?.Stop();
                StopTimer();
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
            SetSection(sender, false);
        }
        private void SectionCheckAll(object sender, RoutedEventArgs e)
        // Used to identify the selected TreeViewItem and when its checkbox is checked, the CheckBox child items are also checked
        {
            SetSection(sender, true);
        }
        private void SetSection(object sender, bool isChecked)
        {
            if (bulkUpdating || sender is not CheckBox header || FindLogicalParent<TreeViewItem>(header) is not TreeViewItem treeViewItem)
            {
                return;
            }
            bulkUpdating = true;
            try
            {
                foreach (CheckBox checkBox in treeViewItem.Items.OfType<CheckBox>())
                {
                    checkBox.IsChecked = isChecked;
                }
            }
            finally
            {
                bulkUpdating = false;
            }
            UpdateSectionState(treeViewItem);
        }
        private void SaveConsoleOutput(object sender, RoutedEventArgs? e)
        // Saves the TextBox console Output for log analysis or review
        {
            consoleWriter.FlushPending();
            string dateTimeNow = $"{DateTime.Now:yyyyMMdd-HHmmss}";
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
                    fileName = $@"{localRoot}{outputLogPrefix}{dateTimeNow}.log";
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
                    fileName = $@"{localRoot}{outputLogPrefix}{dateTimeNow}-{sender}.log";
                    File.WriteAllText(fileName, OutputConsole.Text);
                }
            }
            else
            {
                fileName = $@"{localRoot}{outputLogPrefix}{dateTimeNow}.log";
                File.WriteAllText(fileName, OutputConsole.Text);
            }
        }
        private async void CheckUpdates(object sender, RoutedEventArgs e)
        // Checks for updates of the application
        {
            try
            {
                string release = await LatestReleaseTag(githubBinaryRepo, githubBinaryApi) ?? throw new InvalidOperationException("The latest release could not be determined.");
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
                else if (appVersion > releaseTag)
                {
                    MessageBox.Show($"Lucky you! You're ahead of the times!\nVersion {appVersion} is even newer than the current release!", "Where you're going, you don't need versions..", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
                else
                {
                    MessageBox.Show($"No new release of {displayName} found:\n{appVersion} is the most recent release.", $"No new release of {displayName} found", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] Unable to identify release:\n{ex}");
            }
        }

        private void ResultsButton_Click(object sender, RoutedEventArgs e)
        // Lists every SaltStack log (install, download / stage, WSL and offline) found on the system drive
        {
            try
            {
                if (resultsWindow is not null)
                {
                    resultsWindow.Activate();
                    return;
                }
                if (ResultsWindow.FindLogs().Count == 0)
                {
                    MessageBox.Show($"No {displayName} SaltStack log files ({localRoot}{saltLogPrefix}*.log) were found.\n\nRun an Install, Download, Stage for Offline, or Install Offline first, then try again.", "No results found", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                resultsWindow = new() { Owner = this };
                resultsWindow.Closed += (s, args) => resultsWindow = null;
                resultsWindow.Show();
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
            string versionFile = driveLetter + versionFileName;
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
                                  $"You can check the log(s) labelled {driveLetter}{saltLogPrefix}<version>.log";
                msgBoxVersion = "not installed.";
            }
            MessageBox.Show($"{displayName} is {currentVersion}", $"{displayName} {msgBoxVersion}", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ShowAbout(object sender, RoutedEventArgs e)
        // Shows the About box
        {
            MessageBoxResult result = MessageBox.Show(
                $"{displayName} v{appVersion}\n" +
                $"Author: {authorName}\n" +
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

        internal static void ConsoleOutput(string message)
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
                if (releaseData.Count == 0)
                {
                    return;
                }
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
            if (layoutItems.Count == 0)
            {
                layoutItems = await GetJsonLayout() ?? [];
            }
            (List<string> checkedList, _) = GetCheckStatus();
            HashSet<string> checkedItems = [.. checkedList];
            foreach (TreeItems section in layoutItems)
            {
                int row = 0;
                int column = 0;
                string sectionTitle = section.HeaderContent!;
                if (section.Tools!.Values.Any(tool => checkedItems.Contains(tool.CbName!)))
                {
                    groupName = $@"        <start:Group Name=""{sectionTitle}"">" + '\n';
                    xmlOutput.Append(groupName);
                    var tools = section.Tools;
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
            public string? SoftwareFile { get; set; }
        }
        private async Task<List<ConfigItems>> GetLocalJsonConfig()
        {
            try
            {
                await using FileStream stream = File.OpenRead(localConfigFile);
                ConsoleOutput($"[DEBUG] Using the local software configuration {localConfigFile}");
                return await JsonSerializer.DeserializeAsync<List<ConfigItems>>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Debug mode could not read the local software configuration {localConfigFile} ({ex.Message})");
                return [];
            }
        }
        private async Task<List<ConfigItems>> GetJsonConfig()
        {
            if (debugMode)
            {
                return await GetLocalJsonConfig();
            }
            try
            {
                using CancellationTokenSource cancellationToken = new(new TimeSpan(0, 0, 200));
                return await httpClient.GetFromJsonAsync<List<ConfigItems>>(configFile, cancellationToken.Token) ?? [];
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to get the software configuration - check your network connectivity and try again ({ex.Message})");
                return [];
            }
        }
        private async Task<List<TreeItems>> GetJsonLayout(bool hasLocalFallback = false)
        {
            try
            {
                using CancellationTokenSource cancellationToken = new(hasLocalFallback ? TimeSpan.FromSeconds(15) : TimeSpan.FromSeconds(200));
                return await httpClient.GetFromJsonAsync<List<TreeItems>>(layoutFile, cancellationToken.Token) ?? [];
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                if (!hasLocalFallback)
                {
                    OutputExpander.IsExpanded = true;
                }
                ConsoleOutput($"[{(hasLocalFallback ? "WARNING" : "ERROR")}] Unable to get JSON Layout - check your network connectivity and try again ({ex.Message})");
                return [];
            }
        }

        private async Task<List<TreeItems>> GetLocalJsonLayout()
        {
            List<TreeItems>? jsonData = [];
            try
            {
                await using FileStream stream = File.OpenRead(localLayoutFile);
                jsonData = await JsonSerializer.DeserializeAsync<List<TreeItems>>(stream) ?? [];
            }
            catch (JsonException ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Malformed local JSON layout - {ex.Message}");
            }
            catch (IOException ex)
            {
                OutputExpander.IsExpanded = true;
                ConsoleOutput($"[ERROR] Unable to read local JSON layout - {ex.Message}");
            }
            return jsonData!;
        }

        private readonly Dictionary<TreeViewItem, List<CheckBox>> originalChildOrder = [];
        private async Task<List<TreeItems>> LoadLocalLayout(string reason)
        {
            List<TreeItems> items = await GetLocalJsonLayout();
            if (items.Count > 0)
            {
                LayoutSourceText.Text = debugMode ? "Layout Source: Local (debug)" : "Layout Source: Local";
                ConsoleOutput($"[INFO] {reason} - loaded the layout from {localLayoutFile}");
            }
            return items;
        }
        private void ResetTree()
        // Clears the displayed tools and everything tied to them so a new layout can be built
        {
            XwaysReset();
            ClearSearch(this, new RoutedEventArgs());
            AllTools.Items.Clear();
            toolBoxes.Clear();
            originalChildOrder.Clear();
            sectionCounts.Clear();
            searchResults.Clear();
            CheckUncheckTextBox.Text = "Uncheck All";
        }
        private async Task GenerateTree()
        {
            List<TreeItems> jsonQuery = [];
            bool hasLocalLayout = File.Exists(localLayoutFile);
            string previousSource = LayoutSourceText.Text;
            if ((OfflineMode || debugMode) && hasLocalLayout)
            {
                jsonQuery = await LoadLocalLayout(debugMode ? "Debug mode" : "Offline mode");
            }
            if (jsonQuery.Count == 0 && !OfflineMode && !debugMode && CheckNetworkConnection.IsConnected())
            {
                jsonQuery = await GetJsonLayout(hasLocalLayout);
                LayoutSourceText.Text = jsonQuery.Count > 0 ? "Layout Source: GitHub" : "";
            }
            if (jsonQuery.Count == 0 && !OfflineMode && !debugMode && hasLocalLayout)
            {
                jsonQuery = await LoadLocalLayout("Unable to load the layout from GitHub");
            }
            if (jsonQuery.Count == 0)
            {
                OutputExpander.IsExpanded = true;
                if (layoutItems.Count == 0)
                {
                    LayoutSourceText.Text = "Layout Source: not loaded";
                    ConsoleOutput(debugMode
                        ? $"[ERROR] Debug mode needs a valid layout.json next to the application ({AppContext.BaseDirectory}) - it does not fall back to GitHub."
                        : $"[ERROR] No layout available - connect to the internet, or place a valid layout.json next to the application ({AppContext.BaseDirectory}) and launch it again.");
                }
                else
                {
                    LayoutSourceText.Text = previousSource;
                    ConsoleOutput("[ERROR] Unable to load the new layout, so the current layout was kept.");
                }
                return;
            }
            layoutItems = jsonQuery;
            layoutLoadedOffline = OfflineMode;
            ResetTree();
            int count = layoutItems.Count;
            for (int i = 0; i < count; i++)
            {
                string TVI = layoutItems[i].TVI!;
                string HEADER = layoutItems[i].HeaderContent!;
                string HEADERNAME = layoutItems[i].HeaderName!;
                TextBlock sectionCount = new()
                {
                    Foreground = countBrush,
                    FontSize = 11,
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                StackPanel sectionHeader = new() { Orientation = Orientation.Horizontal };
                sectionHeader.Children.Add(new TextBlock { Text = HEADER, VerticalAlignment = VerticalAlignment.Center });
                sectionHeader.Children.Add(sectionCount);
                CheckBox checkBox = new()
                {
                    Name = HEADERNAME,
                    Content = sectionHeader,
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
                sectionCounts[newChild] = sectionCount;
                var tools = layoutItems[i].Tools;
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
                    if (!toolCheckBox.IsEnabled)
                    {
                        continue;
                    }
                    toolCheckBox.Checked += ChildCheckChanged;
                    toolCheckBox.Unchecked += ChildCheckChanged;
                    XwaysWireTool(tool.Value.CbName!, toolCheckBox);
                    newChild.Items.Add(toolCheckBox);
                    toolBoxes[tool.Value.CbName!] = toolCheckBox;
                    if (!originalChildOrder.TryGetValue(newChild, out List<CheckBox>? children))
                    {
                        children = [];
                        originalChildOrder[newChild] = children;
                    }
                    children.Add(toolCheckBox);
                }
                UpdateSectionState(newChild);
            }
            await Dispatcher.InvokeAsync(FitTreeHeight, DispatcherPriority.Loaded);
            await Dispatcher.InvokeAsync(FitTreeWidth, DispatcherPriority.Loaded);
        }

        private void FitTreeWidth()
        // Sizes the window so the longest tool name fits in the tree without a horizontal scrollbar
        {
            if (WindowState != WindowState.Normal)
            {
                return;
            }
            List<TreeViewItem> sections = [.. AllTools.Items.OfType<TreeViewItem>()];
            List<bool> wasExpanded = [.. sections.Select(s => s.IsExpanded)];
            sections.ForEach(s => s.IsExpanded = true);
            AllTools.UpdateLayout();
            double widest = sections
                .SelectMany(s => s.Items.OfType<CheckBox>().Prepend((CheckBox)s.Header))
                .Max(cb => cb.TranslatePoint(new Point(cb.ActualWidth, 0), AllTools).X);
            for (int i = 0; i < sections.Count; i++)
            {
                sections[i].IsExpanded = wasExpanded[i];
            }
            AllTools.UpdateLayout();
            double target = Math.Clamp(Width + widest + SystemParameters.VerticalScrollBarWidth + treeSlack - AllTools.ActualWidth, MinWidth, SystemParameters.WorkArea.Width);
            Left -= (target - Width) / 2;
            MinWidth = target;
            Width = target;
        }

        private void ChildCheckChanged(object sender, RoutedEventArgs e)
        {
            if (bulkUpdating || sender is not CheckBox childCheckBox)
                return;

            if (FindLogicalParent<TreeViewItem>(childCheckBox) is TreeViewItem parentItem)
            {
                UpdateSectionState(parentItem);
            }
        }

        private async Task LocalLayout()
        {
            MessageBoxResult dlgResult = MessageBox.Show("On the following Dialog Box, please select where your Standalone Executables are stored from your previous installation.", "Important - Please Read!", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
            if (dlgResult != MessageBoxResult.OK)
            {
                return;
            }
            string folderPath = PickFolder("Select the directory where your standalone programs are stored...");
            if (folderPath != "")
            {
                string layout = await GenerateLayout(folderPath);
                await File.WriteAllTextAsync($@"{folderPath}\{displayName}-StartLayout.xml", layout);
                ConsoleOutput(@$"Customized Start Layout written to {folderPath}\{displayName}-StartLayout.xml");
                MessageBox.Show(@$"Customized Start Layout written to {folderPath}\{displayName}-StartLayout.xml", "Customized Start Layout Saved!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        internal static string PickFolder(string title)
        {
            OpenFolderDialog dialog = new()
            {
                Title = title,
                InitialDirectory = Environment.CurrentDirectory
            };
            return dialog.ShowDialog() == true ? dialog.FolderName : "";
        }
        private async void LocalLayoutClick(object sender, RoutedEventArgs e)
        { 
            await LocalLayout();
        }
        private void StandalonesPicker(object sender, RoutedEventArgs e)
        {
            string selectedPath = PickFolder("Select the directory where you would like to store your standalone files");
            if (selectedPath != "")
            {
                StandalonesPath.Text = selectedPath;
            }
        }
        private void DownloadsPicker(object sender, RoutedEventArgs e)
        {
            string selectedPath = PickFolder("Select the directory where you would like to store your downloaded files");
            if (selectedPath != "")
            {
                DownloadsPath.Text = selectedPath;
            }
        }
        private void ShowDebloatOptions(object sender, RoutedEventArgs e)
        {
            if (debloatWindow is not null)
            {
                debloatWindow.Activate();
                return;
            }
            debloatWindow = new()
            {
                Owner = this
            };
            debloatWindow.Closed += (s, args) => debloatWindow = null;
            debloatWindow.Show();
        }

        private readonly List<CheckBox> searchResults = [];
        private int searchIndex;

        public void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                bool pending = searchTimer.IsEnabled;
                searchTimer.Stop();
                if (pending)
                {
                    ApplySearch();
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    PreviousResult(sender, e);
                }
                else
                {
                    NextResult(sender, e);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && SearchBox.Text.Length > 0)
            {
                ClearSearch(sender, e);
                e.Handled = true;
            }
        }

        private void UpdateSearchCount()
        {
            SearchCountText.Text = SearchBox.Text.Trim().Length == 0 ? "" : searchResults.Count == 0 ? "No matches" : $"{searchIndex + 1} of {searchResults.Count}";
        }

        private void ApplySearch()
        {
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
                searchResults.Clear();
                searchIndex = -1;
                ClearSearch(this, new RoutedEventArgs());
            }
            else
            {
                ExpandCollapseTextBox.Text = "Collapse All";
                if (searchResults.Count > 0)
                {
                    searchIndex = 0;
                    searchResults[0].BringIntoView();
                    searchResults[0].FontWeight = FontWeights.Bold;
                }
            }
            UpdateSearchCount();
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
            UpdateSearchCount();
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
            UpdateSearchCount();
        }
        public void ClearSearch(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
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
            UpdateSearchCount();
        }


        private void ClearConsole(object sender, RoutedEventArgs e)
        {
            OutputConsole.Clear();
            OutputExpander.IsExpanded = false;
        }
        //private void TestButton_Click(object sender, RoutedEventArgs e)
        //Used simply for testing

        private void OutputExpander_Changed(object sender, RoutedEventArgs e)
        // The console takes whatever height the window has below the other boxes, so the window grows and shrinks with it and the tool list keeps its size
        {
            double margins = OutputConsole.Margin.Top + OutputConsole.Margin.Bottom;
            bool normal = WindowState == WindowState.Normal;
            if (OutputExpander.IsExpanded)
            {
                if (normal)
                {
                    Height = Math.Clamp(Height + consoleHeight + margins, MinHeight, SystemParameters.WorkArea.Height);
                }
                MinHeight = collapsedMinHeight + minConsoleHeight + margins;
            }
            else
            {
                if (normal)
                {
                    consoleHeight = Math.Max(minConsoleHeight, OutputConsole.ActualHeight);
                }
                MinHeight = collapsedMinHeight;
                if (normal)
                {
                    Height = Math.Clamp(Height - consoleHeight - margins, MinHeight, SystemParameters.WorkArea.Height);
                }
            }
        }
        private void AllTools_SizeChanged(object sender, SizeChangedEventArgs e)
        // Keeps the scrollbar and the highlights inside the rounded corners of the frame
        {
            AllTools.Clip = new RectangleGeometry(new Rect(0, 0, AllTools.ActualWidth, AllTools.ActualHeight), treeCornerRadius - 1, treeCornerRadius - 1);
        }
        private void FitTreeHeight()
        // Sizes the tool list to show every section collapsed without a scrollbar, level with the bottom of the X-Ways box, and moves the window to match
        {
            AllTools.UpdateLayout();
            double target = Math.Max(
                AllTools.Items.OfType<TreeViewItem>().Sum(item => ((FrameworkElement)item.Header).ActualHeight) + treeFrame,
                ActionColumn.DesiredSize.Height);
            double change = target - ToolListFrame.Height;
            ToolListFrame.Height = target;
            collapsedMinHeight += change;
            MinHeight += change;
            if (WindowState == WindowState.Normal)
            {
                Height += change;
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