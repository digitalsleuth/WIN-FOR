using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;


namespace WinFOR
{
    /// <summary>
    /// Interaction logic for DebloatWindow.xaml
    /// </summary>
    public partial class DebloatWindow : Window
    {
        private const string saltConfigFolder = $@"{MainWindow.saltPath}\{MainWindow.src}\config";
        private const string tempDir = MainWindow.debloatTempDir;
        private static readonly string[] debloatScripts = ["Win10.ps1", "Win10.psm1"];
        private const double SegmentRadius = 3;
        private static SolidColorBrush accentBrush => (SolidColorBrush)Application.Current.FindResource("AccentBrush");
        private static readonly SolidColorBrush segmentBorderBrush = new(Color.FromRgb(0xD0, 0xD0, 0xD0));
        private static List<TabItems>? cachedOptions;
        private readonly List<RadioButton> radioButtons = [];
        private readonly List<SettingRow> settingRows = [];
        private readonly Dictionary<string, StackPanel> categoryPanels = [];
        private readonly Dictionary<string, TextBlock> categoryHeaders = [];
        private readonly Dictionary<string, TextBlock> categoryCounts = [];
        private bool bulkUpdating;
        //private static readonly Version windowsOS = Environment.OSVersion.Version;

        public DebloatWindow()
        {
            InitializeComponent();
            MaxHeight = SystemParameters.WorkArea.Height;
            Title = "Debloat Options - loading...";
            SetupDebloatOptions();
        }
        public static class DebloatSettings
        {
            public static List<string> Selections = [];
            public static readonly HashSet<string> DefaultOptions =
            [
                "RequireAdmin",
                "DisableTelemetry",
                "DisableCortana",
                "DisableWiFiSense",
                "DisableSmartScreen",
                "DisableWebSearch",
                "DisableAppSuggestions",
                "DisableLocation",
                "DisableMapUpdates",
                "DisableFeedback",
                "DisableTailoredExperiences",
                "DisableAdvertisingID",
                "DisableWebLangList",
                "DisableErrorReporting",
                "DisableDiagTrack",
                "DisableWAPPush",
                "DisableUWPBackgroundApps",
                "DisableUWPVoiceActivation",
                "DisableUWPNotifications",
                "DisableUWPAccountInfo",
                "DisableUWPContacts",
                "DisableUWPCalendar",
                "DisableUWPPhoneCalls",
                "DisableUWPCallHistory",
                "DisableUWPEmail",
                "DisableUWPTasks",
                "DisableUWPMessaging",
                "DisableUWPRadios",
                "DisableUWPOtherDevices",
                "DisableUWPDiagInfo",
                "SetUACLow",
                "DisableDefenderCloud",
                "HideAccountProtectionWarn",
                "DisableDownloadBlocking",
                "EnableDotNetStrongCrypto",
                "EnableF8BootMenu",
                "SetDEPOptOut",
                "SetCurrentNetworkPrivate",
                "SetUnknownNetworksPrivate",
                "DisableNetDevicesAutoInst",
                "DisableLLMNR",
                "DisableRemoteAssistance",
                "DisableUpdateMSRT",
                "EnableUpdateMSProducts",
                "DisableUpdateAutoDownload",
                "DisableUpdateRestart",
                "DisableMaintenanceWakeUp",
                "DisableAutoRestartSignOn",
                "DisableSharedExperiences",
                "DisableAutoplay",
                "DisableAutorun",
                "EnableNTFSLongPaths",
                "DisableSleepTimeout",
                "EnableFastStartup",
                "DisableActionCenter",
                "DisableLockScreen",
                "HideNetworkFromLockScreen",
                "DisableLockScreenBlur",
                "DisableAeroShake",
                "DisableAccessibilityKeys",
                "ShowTaskManagerDetails",
                "ShowFileOperationsDetails",
                "HideTaskbarSearch",
                "SetTaskbarCombineWhenFull",
                "HideTaskbarPeopleIcon",
                "ShowTrayIcons",
                "DisableSearchAppInStore",
                "DisableNewAppPrompt",
                "SetControlPanelSmallIcons",
                "DisableShortcutInName",
                "SetVisualFXPerformance",
                "SetAppsDarkMode",
                "EnableNumlock",
                "DisableF1HelpKey",
                "DisableNewsFeeds",
                "DisableMeetNow",
                "DisableWidgets",
                "HideTaskbarChatIcon",
                "DisableNewsAndInterests",
                "ShowExplorerTitleFullPath",
                "ShowKnownExtensions",
                "ShowHiddenFiles",
                "ShowSuperHiddenFiles",
                "ShowEmptyDrives",
                "ShowFolderMergeConflicts",
                "EnableNavPaneExpand",
                "ShowNavPaneAllFolders",
                "HideSyncNotifications",
                "HideRecentShortcuts",
                "SetExplorerThisPC",
                "ShowQuickAccess",
                "ShowThisPCOnDesktop",
                "HideDesktopFromThisPC",
                "HideDocumentsFromThisPC",
                "HideDownloadsFromThisPC",
                "HideMusicFromThisPC",
                "HidePicturesFromThisPC",
                "HideVideosFromThisPC",
                "Hide3DObjectsFromThisPC",
                "Hide3DObjectsFromExplorer",
                "HideIncludeInLibraryMenu",
                "HideGiveAccessToMenu",
                "HideShareMenu",
                "DisableThumbnailCache",
                "DisableThumbsDBOnNetwork",
                "DisableOneDrive",
                "UninstallOneDrive",
                "UninstallMsftBloat",
                "UninstallThirdPartyBloat",
                "DisableXboxFeatures",
                "DisableAdobeFlash",
                "DisableEdgePreload",
                "DisableEdgeShortcutCreation",
                "DisableIEFirstRun",
                "DisableFirstLogonAnimation",
                "DisableMediaSharing",
                "EnableDeveloperMode",
                "SetPhotoViewerAssociation",
                "AddPhotoViewerOpenWith",
                "UninstallXPSPrinter",
                "RemoveFaxPrinter",
                "UnpinStartMenuTiles",
                "UnpinTaskbarIcons"
            ];
        }

        
        private List<string> GetRadioButtonsStatus()
        // Names of the checked Radio Buttons which are not None
        {
            return [.. radioButtons.Where(radioButton => radioButton.IsChecked == true && radioButton.Name != "None").Select(radioButton => radioButton.Name)];
        }
        private void GetRadioButtonsStatus_Click(object sender, RoutedEventArgs e)
        // Get the current state of all Radio Buttons from GetRadioButtonStatus, and save it to the DebloatSettings.Selections variable
        {
            DebloatSettings.Selections = GetRadioButtonsStatus();
            RefreshState();
            MessageBox.Show("Selections saved!\nThese choices will be used for the debloat during installation.\n\nYou may now close this window, or click the Debloat Now button to begin.", "Selections saved!", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        // Set all Radio Buttons status of "IsChecked" to false
        {
            bulkUpdating = true;
            foreach (RadioButton radioButton in radioButtons)
            {
                radioButton.IsChecked = false;
            }
            bulkUpdating = false;
            RefreshState();
        }
        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        // Load all the default options from DebloatSettings.DefaultOptions
        {
            //List<string> laterOptions = ["DisableNewsAndInterests"];
            //if (windowsOS.Major == 11 && windowsOS.Build >= 22000)
            //{
            //foreach (string option in laterOptions)
            //{
            //DebloatSettings.DefaultOptions!.Remove(option);
            //defaultOptions.Remove(option);
            //}
            //}
            bulkUpdating = true;
            foreach (RadioButton radioButton in radioButtons)
            {
                if (DebloatSettings.DefaultOptions.Contains(radioButton.Name))
                {
                    radioButton.IsChecked = true;
                }
            }
            bulkUpdating = false;
            RefreshState();
        }

        private async void DebloatButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> selectedRadioButtons = GetRadioButtonsStatus();
            if (selectedRadioButtons.Count == 0)
            {
                MessageBox.Show("No options selected.\nChoose at least one option, or click Defaults, before using Debloat Now.", "No options selected!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            DebloatButton.IsEnabled = false;
            try
            {
                Directory.CreateDirectory(tempDir);
                await File.WriteAllTextAsync($@"{tempDir}\debloat.preset", string.Join("\n", selectedRadioButtons));
                string? releaseTag = null;
                foreach (string file in debloatScripts)
                {
                    string localScript = $@"{saltConfigFolder}\{file}";
                    if (File.Exists(localScript))
                    {
                        File.Copy(localScript, $@"{tempDir}\{file}", true);
                        continue;
                    }
                    releaseTag ??= await MainWindow.LatestSaltRelease();
                    string uri = $@"{MainWindow.githubRaw}/{releaseTag}/{MainWindow.src}/config/{file}";
                    if (releaseTag is null || !await MainWindow.FileDownload(uri, $@"{tempDir}\{file}"))
                    {
                        MessageBox.Show(releaseTag is null ? "Unable to determine the latest release - check your network connectivity and try again." : $"Unable to download {uri}.", $"Unable to download required debloat file {file}.", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                ProcessStartInfo startInfo = new()
                {
                    FileName = "powershell.exe",
                    Arguments = @"-nop -ep Bypass -File ""Win10.ps1"" -include ""Win10.psm1"" -preset ""debloat.preset""",
                    UseShellExecute = true,
                    WorkingDirectory = tempDir
                };
                using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("powershell.exe did not start.");
                await process.WaitForExitAsync();
                if (process.ExitCode == 0)
                {
                    MessageBox.Show("Debloat complete!", "Debloat complete!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"The debloat script finished with exit code {process.ExitCode}.\nSome options may not have been applied.","Debloat finished with errors", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to complete debloat:\n{ex}.", "Unable to complete debloat!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DebloatButton.IsEnabled = true;
            }
        }

        public class TabItems
        {
            public string? Header { get; set; }
            public string? Name { get; set; }
            public string? StackName { get; set; }
            public Dictionary<string, SettingsList>? Settings { get; set; }
        }
        public class SettingsList
        {

            public string[]? Options { get; set; }
            public string? RadioButtonContents { get; set; }
        }

        private static async Task<List<TabItems>> GetDebloatJson()
        {
            if (cachedOptions is not null)
            {
                return cachedOptions;
            }
            try
            {
                string localJson = $@"{saltConfigFolder}\debloat.json";
                if (File.Exists(localJson))
                {
                    await using FileStream stream = File.OpenRead(localJson);
                    cachedOptions = await JsonSerializer.DeserializeAsync<List<TabItems>>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                else
                {
                    string releaseTag = await MainWindow.LatestSaltRelease() ?? "main";
                    using CancellationTokenSource cancellationToken = new(TimeSpan.FromSeconds(30));
                    cachedOptions = await MainWindow.httpClient.GetFromJsonAsync<List<TabItems>>($"{MainWindow.githubRaw}/{releaseTag}/{MainWindow.src}/config/debloat.json", cancellationToken.Token);
                }
                return cachedOptions ?? [];
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException)
            {
                MessageBox.Show($"[ERROR] Unable to get the debloat options - check your network connectivity and try again ({ex.Message})");
                return [];
            }
        }
        private void SaveAsFileButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> selectedRadioButtons = GetRadioButtonsStatus();
            if (selectedRadioButtons.Count > 0)
            {
                SaveSelectionsAsFile(selectedRadioButtons);
            }
            else
            {
                MessageBox.Show($"No options selected.\nNo preset file will be generated.", "No options selected!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        private static void SaveSelectionsAsFile(List<string> debloatSettings)
        // Save the selections as a preset file
        {
            try
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "Preset File (*.preset)|*.preset",
                    DefaultExt = ".preset",
                    FileName = "debloat.preset"
                };
                if (saveFileDialog.ShowDialog() == true)
                    File.WriteAllText(saveFileDialog.FileName, string.Join("\n", debloatSettings));

            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] Unable to save selected file: {ex}", "Unable to save file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public async void SetupDebloatOptions()
        {
            try
            {
                var jsonData = await GetDebloatJson();
                bulkUpdating = true;
                foreach (var section in jsonData)
                {
                    string category = section.Header ?? section.Name ?? "Other";
                    StackPanel panel = new() { Visibility = Visibility.Collapsed };
                    TextBlock header = new()
                    {
                        Text = category,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = accentBrush,
                        Margin = new Thickness(0, 12, 0, 2),
                        Visibility = Visibility.Collapsed
                    };
                    panel.Children.Add(header);
                    categoryPanels[category] = panel;
                    categoryHeaders[category] = header;
                    foreach (var function in section.Settings!)
                    {
                        string[] options = function.Value.Options!;
                        string[] labels = function.Value.RadioButtonContents!.Split('-');
                        List<RadioButton> optionButtons = [];
                        StackPanel segments = new() { Orientation = Orientation.Horizontal };
                        for (int i = 0; i < options.Length; i++)
                        {
                            double left = i == 0 ? SegmentRadius : 0;
                            double right = i == options.Length - 1 ? SegmentRadius : 0;
                            RadioButton optionRadioButton = new()
                            {
                                Style = (Style)FindResource("SegmentRadio"),
                                Content = i < labels.Length ? labels[i] : options[i],
                                GroupName = $"{category}|{function.Key}",
                                Name = options[i],
                                Tag = new CornerRadius(left, right, right, left),
                                BorderThickness = new Thickness(i == 0 ? 0 : 1, 0, 0, 0)
                            };
                            optionRadioButton.Checked += OptionChanged;
                            optionRadioButton.Unchecked += OptionChanged;
                            optionRadioButton.PreviewMouseLeftButtonDown += Option_PreviewMouseLeftButtonDown;
                            optionRadioButton.PreviewKeyDown += Option_PreviewKeyDown;
                            optionButtons.Add(optionRadioButton);
                            segments.Children.Add(optionRadioButton);
                        }
                        Border bar = new()
                        {
                            Width = 3,
                            Margin = new Thickness(0, 2, 10, 2),
                            Background = accentBrush,
                            CornerRadius = new CornerRadius(1.5),
                            Visibility = Visibility.Hidden
                        };
                        TextBlock name = new()
                        {
                            Text = function.Key,
                            FontSize = 12.5,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        };
                        Border segmentFrame = new()
                        {
                            BorderBrush = segmentBorderBrush,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(SegmentRadius + 1),
                            Margin = new Thickness(12, 0, 4, 0),
                            Child = segments
                        };
                        Grid rowGrid = new();
                        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        Grid.SetColumn(name, 1);
                        Grid.SetColumn(segmentFrame, 2);
                        rowGrid.Children.Add(bar);
                        rowGrid.Children.Add(name);
                        rowGrid.Children.Add(segmentFrame);
                        Border container = new()
                        {
                            Style = (Style)FindResource("SettingRow"),
                            ToolTip = string.Join(", ", options),
                            Child = rowGrid
                        };
                        panel.Children.Add(container);
                        settingRows.Add(new SettingRow { Category = category, Name = function.Key, Container = container, Bar = bar, Options = optionButtons });
                        radioButtons.AddRange(optionButtons);
                    }
                    OptionsHost.Children.Add(panel);
                    TextBlock count = new() { FontSize = 11, Opacity = 0.75, VerticalAlignment = VerticalAlignment.Center };
                    Grid itemGrid = new();
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    Grid.SetColumn(count, 1);
                    itemGrid.Children.Add(new TextBlock { Text = category, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis });
                    itemGrid.Children.Add(count);
                    categoryCounts[category] = count;
                    CategoryList.Items.Add(new ListBoxItem { Content = itemGrid, Tag = category });
                }
                foreach (RadioButton radioButton in radioButtons)
                {
                    if (DebloatSettings.Selections.Contains(radioButton.Name))
                    {
                        radioButton.IsChecked = true;
                    }
                }
                if (jsonData.Count == 0)
                {
                    NoResultsText.Text = "The debloat options could not be loaded.";
                }
                bulkUpdating = false;
                RefreshState();
                CategoryList.SelectedIndex = 0;
                ApplyView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"[ERROR] Unable to display the debloat options window: {ex}");
            }
            finally
            {
                bulkUpdating = false;
                Title = "Debloat Options";
            }
        }

        private sealed class SettingRow
        {
            public required string Category { get; init; }
            public required string Name { get; init; }
            public required Border Container { get; init; }
            public required Border Bar { get; init; }
            public required List<RadioButton> Options { get; init; }
        }

        private void RefreshState()
        // Updates the row markers, the per-category counts and the summary line from the current selections
        {
            foreach (SettingRow row in settingRows)
            {
                row.Bar.Visibility = row.Options.Any(option => option.IsChecked == true) ? Visibility.Visible : Visibility.Hidden;
            }
            foreach ((string category, TextBlock count) in categoryCounts)
            {
                int selected = settingRows.Count(row => row.Category == category && row.Options.Any(option => option.IsChecked == true));
                count.Text = $"{selected} / {settingRows.Count(row => row.Category == category)}";
                count.FontWeight = selected > 0 ? FontWeights.SemiBold : FontWeights.Normal;
            }
            List<string> selections = GetRadioButtonsStatus();
            int categories = settingRows.Where(row => row.Options.Any(option => option.IsChecked == true)).Select(row => row.Category).Distinct().Count();
            string summary = selections.Count == 0 ? "No options selected" : $"{selections.Count} {(selections.Count == 1 ? "option" : "options")} selected in {categories} {(categories == 1 ? "category" : "categories")}";
            SummaryText.Text = selections.ToHashSet().SetEquals(DebloatSettings.Selections)
                ? summary + (selections.Count == 0 ? " - installation will skip the debloat" : " - saved for installation")
                : summary + " - not saved yet, use Save Settings";
        }

        private void ApplyView()
        // Shows the selected category, or every matching row from all categories while a search is active
        {
            string query = SearchBox.Text.Trim();
            bool searching = query.Length > 0;
            string? current = (CategoryList.SelectedItem as ListBoxItem)?.Tag as string;
            bool anyShown = false;
            foreach ((string category, StackPanel panel) in categoryPanels)
            {
                bool anyMatch = false;
                foreach (SettingRow row in settingRows.Where(row => row.Category == category))
                {
                    bool match = !searching || row.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || row.Options.Any(option => option.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
                    row.Container.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                    anyMatch |= match;
                }
                categoryHeaders[category].Visibility = searching ? Visibility.Visible : Visibility.Collapsed;
                bool show = searching ? anyMatch : category == current;
                panel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                anyShown |= show;
            }
            NoResultsText.Visibility = anyShown ? Visibility.Collapsed : Visibility.Visible;
            OptionsScroll.ScrollToTop();
        }

        private void OptionChanged(object sender, RoutedEventArgs e)
        {
            if (!bulkUpdating)
            {
                RefreshState();
            }
        }

        private void Option_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        // Clicking the selected segment clears the setting (the old "None" choice)
        {
            if (sender is RadioButton { IsChecked: true } radioButton)
            {
                radioButton.IsChecked = false;
                radioButton.Focus();
                e.Handled = true;
            }
        }

        private void Option_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && sender is RadioButton { IsChecked: true } radioButton)
            {
                radioButton.IsChecked = false;
                e.Handled = true;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyView();
        }

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchBox.Text.Length > 0)
            {
                SearchBox.Clear();
            }
            else
            {
                ApplyView();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && SearchBox.Text.Length > 0)
            {
                SearchBox.Clear();
                e.Handled = true;
            }
        }
    }
}
