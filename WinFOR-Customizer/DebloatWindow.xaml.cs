using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinFORCustomizer
{
    /// <summary>
    /// Interaction logic for DebloatWindow.xaml
    /// </summary>
    public partial class DebloatWindow : Window
    {
        public DebloatWindow()
        {
            InitializeComponent();
            SetupDebloatOptions();
        }
        public static class DebloatSettings
        {
            public static List<string>? Selections;
            public static List<string>? DefaultOptions = new()
            {
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
            };
        }
        private List<string> GetRadioButtonsStatus()
        // Get the current state of all Radio Buttons and return a List of those which are not None
        {
            List<string> selectedRadioButtons = new();

            // Iterate through all TabItems in the TabControl
            foreach (TabItem tabItem in Tabs.Items)
            {
                // Find all RadioButton controls in each TabItem, which are in Grids, so we need to enumerate the Content
                List<RadioButton> radioButtons = MainWindow.GetLogicalChildCollection<RadioButton>((DependencyObject)tabItem.Content);
                foreach (RadioButton radioButton in radioButtons)
                {
                    if (radioButton.IsChecked == true && radioButton.Name != "None")
                    {
                        // Add the Name of the selected RadioButton to the list
                        selectedRadioButtons.Add(radioButton.Name);
                    }
                }
            }
            return selectedRadioButtons;
        }
        private void GetRadioButtonsStatus_Click(object sender, RoutedEventArgs e)
        // Get the current state of all Radio Buttons from GetRadioButtonStatus, and save it to the DebloatSettings.Selections variable
        {
            List<string> selectedRadioButtons = GetRadioButtonsStatus();
            DebloatSettings.Selections = selectedRadioButtons;
            MessageBox.Show("Selections saved!\nThese choices will be used for the debloat during installation.\n\nYou may now close this window.", "Selections saved!", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        // Set all Radio Buttons status of "IsChecked" to false
        {
            foreach (TabItem tabItem in Tabs.Items)
            {
                List<RadioButton> radioButtons = MainWindow.GetLogicalChildCollection<RadioButton>((DependencyObject)tabItem.Content);
                foreach (RadioButton radioButton in radioButtons)
                {
                    radioButton.IsChecked = false;
                }
            }
        }
        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        // Load all the default options from DebloatSettings.DefaultOptions
        {
            foreach (TabItem tabItem in Tabs.Items)
            {
                List<RadioButton> radioButtons = MainWindow.GetLogicalChildCollection<RadioButton>((DependencyObject)tabItem.Content);
                // Find all RadioButton controls in each TabItem, which are in Grids, so we need to enumerate the Content
                // Sets / shows the default options if debloat is not chosen.
                foreach (RadioButton radioButton in radioButtons) 
                {
                    foreach (string setting in DebloatSettings.DefaultOptions!)
                    {
                        if (radioButton.Name == setting)
                        {
                            radioButton.IsChecked = true;
                        }
                    }
                }
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
            List<TabItems>? jsonData = new();
            try
            {
                CancellationTokenSource cancellationToken = new(new TimeSpan(0, 0, 200));
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0");
                string uri = $@"https://raw.githubusercontent.com/digitalsleuth/winfor-salt/main/winfor/config/debloat.json";
                jsonData = await httpClient.GetFromJsonAsync<List<TabItems>>(uri, cancellationToken.Token);
            }
            catch (HttpRequestException)
            {
                MessageBox.Show($"[ERROR] Unable to get JSON Layout - check your network connectivity and try again");
            }
            return jsonData!;
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
                    Filter = "Preset File | *.preset"
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
                string themeColourBlue = "#FF1644B9";
                string themeColourRed = "#FFCB393B";
                Color colourBlue = (Color)ColorConverter.ConvertFromString(themeColourBlue);
                Color colourRed = (Color)ColorConverter.ConvertFromString(themeColourRed);
                SolidColorBrush themeBrushBlue = new(colourBlue);
                SolidColorBrush themeBrushRed = new(colourRed);
                var jsonData = await GetDebloatJson();

                foreach (var section in jsonData)
                {
                    string sectionHeader = section.Header!;
                    string sectionName = section.Name!;
                    Panel stackName0 = (Panel)FindName(section.StackName + "0");
                    Panel stackName1 = (Panel)FindName(section.StackName + "1");
                    int numGroupsPerPanel = (section.Settings!.Count + 1) / 2;
                    int groupNumber = 1;
                    foreach (var function in section.Settings!)
                    {
                        int numFunctions = function.Value.Options!.Length;
                        string functionContent = function.Value.RadioButtonContents!;

                        // Create a GroupBox to store the RadioButtons
                        GroupBox groupBox = new()
                        {
                            Header = function.Key,
                            Foreground = themeBrushRed,
                            Width = 360,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            BorderBrush = Brushes.CornflowerBlue

                        };

                        // Create a base StackPanel for the RadioButtons
                        StackPanel radioButtonPanel = new()
                        {
                            Orientation = Orientation.Horizontal
                        };
                        // Create a for loop for the functions and create a radio button for them

                        for (int i = 0; i <= numFunctions - 1; i++)
                        {
                            RadioButton optionRadioButton = new()
                            {
                                Content = functionContent!.Split("-")[i] + "   ",
                                GroupName = function.Key,
                                Name = function.Value.Options[i]
                            };

                            radioButtonPanel.Children.Add(optionRadioButton);
                        };
                        RadioButton doNothingRadioButton = new()
                        {
                            Content = "None",
                            Name = "None",
                            GroupName = function.Key
                        };

                        // Add RadioButtons to the StackPanel
                        radioButtonPanel.Children.Add(doNothingRadioButton);

                        // Add the StackPanel to the GroupBox
                        groupBox.Content = radioButtonPanel;

                        // Add the GroupBox to the main StackPanel
                        if (groupNumber <= numGroupsPerPanel)
                        {
                            stackName0.Children.Add(groupBox);
                        }
                        else
                        {
                            stackName1.Children.Add(groupBox);
                        }
                        groupNumber++;
                    }
                }
                if (DebloatSettings.Selections != null)
                {
                    foreach (TabItem tabItem in Tabs.Items)
                    {
                        List<RadioButton> radioButtons = MainWindow.GetLogicalChildCollection<RadioButton>((DependencyObject)tabItem.Content);
                        // Find all RadioButton controls in each TabItem, which are in Grids, so we need to enumerate the Content
                        foreach (string setting in DebloatSettings.Selections)
                        {
                            foreach (RadioButton radioButton in radioButtons)
                            {
                                if (radioButton.Name == setting)
                                {
                                    radioButton.IsChecked = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"[ERROR] Unable to display the debloat options window: {ex}");
            }
        }
    }
}
