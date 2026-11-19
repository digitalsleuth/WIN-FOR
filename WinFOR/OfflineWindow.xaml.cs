using System;
using System.IO;
using System.Windows;

namespace WinFOR
{
    public partial class OfflineWindow : Window
    {
        public string SelectedStandalones { get; private set; } = "";
        public string SelectedDownloads { get; private set; } = "";
        public string SelectedUser { get; private set; } = "";

        public OfflineWindow(string standalones, string downloads, string user)
        {
            InitializeComponent();
            StandalonesPath.Text = standalones;
            DownloadsPath.Text = downloads;
            UserName.Text = user;
            Placeholder.SetText(StandalonesPath, MainWindow.defaultStandalonesPath);
            Placeholder.SetText(DownloadsPath, MainWindow.defaultDownloadsPath);
            StandalonesLabel.ToolTip = StandalonesPath.ToolTip = MainWindow.standalonesToolTip;
            DownloadsLabel.ToolTip = DownloadsPath.ToolTip = $"Choose the folder that holds the files downloaded by the 'Offline' option.\nIt must contain the offline folder.\nIf left blank, the default is {MainWindow.defaultDownloadsPath}";
            Placeholder.SetText(UserName, Environment.UserName);
        }

        private static string ResolvePath(string value, string fallback)
        {
            return value.Trim() == "" ? fallback : Path.TrimEndingDirectorySeparator(value.Trim());
        }

        private bool DownloadsAreValid(string path)
        {
            string missing = !File.Exists($@"{path}\offline\{MainWindow.src}\offline\init.sls") ? $@"\offline\{MainWindow.src}\offline\init.sls"
                : !File.Exists($@"{path}\offline\Salt\salt-call.exe") ? @"\offline\Salt\salt-call.exe"
                : "";
            if (missing == "")
            {
                return true;
            }
            MainWindow.ConsoleOutput($"[ERROR] {path} does not contain the file: {missing}.\nPlease check your path and try again.");
            MessageBox.Show(this, $"{path} does not contain the file: {missing}.\nPlease check your path and try again.", "Offline files not found!", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        private void StandalonesPicker(object sender, RoutedEventArgs e)
        {
            string selectedPath = MainWindow.PickFolder("Select the directory where you would like to store your standalone files");
            if (selectedPath != "")
            {
                StandalonesPath.Text = selectedPath;
            }
        }

        private void DownloadsPicker(object sender, RoutedEventArgs e)
        {
            string selectedPath = MainWindow.PickFolder("Select the directory where your downloaded files are located");
            if (selectedPath != "" && DownloadsAreValid(selectedPath))
            {
                DownloadsPath.Text = selectedPath;
            }
        }

        private void InstallClick(object sender, RoutedEventArgs e)
        {
            string downloads = ResolvePath(DownloadsPath.Text, MainWindow.defaultDownloadsPath);
            string standalones = ResolvePath(StandalonesPath.Text, MainWindow.defaultStandalonesPath);
            string user = UserName.Text.Trim() == "" ? Environment.UserName : UserName.Text.Trim();
            if (!DownloadsAreValid(downloads) || !MainWindow.InputsAreValid(user, standalones, downloads))
            {
                return;
            }
            SelectedDownloads = downloads;
            SelectedStandalones = standalones;
            SelectedUser = user;
            DialogResult = true;
        }
    }
}
