using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WinFORCustomizer
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window
    {
        private readonly ObservableCollection<LogEntry> logEntries;
        private ICollectionView logView;
        public ResultsWindow(string releaseVersion)
        {

            InitializeComponent();
            logEntries = new ObservableCollection<LogEntry>();
            logView = CollectionViewSource.GetDefaultView(logEntries);
            logView.Filter = FilterLogEntries;

            LogListView.ItemsSource = logView;
            string logFile = $@"C:\winfor-saltstack-{releaseVersion}.log";
            string wslLog = $@"C:\winfor-saltstack-{releaseVersion}-wsl.log";
            string downloadLog = $@"C:\winfor-saltstack-downloads-{releaseVersion}.log";
            var buttonMap = new Dictionary<string, Button>
            {
                { logFile, SaltStackLogButton },
                { wslLog, WSLLogButton },
                { downloadLog, DownloadLogButton },
            };
            int column = 0;
            foreach (var entry in buttonMap)
            {
                string filePath = entry.Key;
                Button button = entry.Value;

                if (File.Exists(filePath))
                {
                    button.Content = filePath;
                    button.Visibility = Visibility.Visible;
                    Grid.SetColumn(button, column);
                    column += 2;
                }
            }
        }
        GridViewColumnHeader? _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        void GridViewColumnHeader_Clicked(object sender, RoutedEventArgs e)
        {
            ListSortDirection direction;
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }
                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy!, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView =
              CollectionViewSource.GetDefaultView(LogListView.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void LoadLog_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            { 
                string? buttonContent = clickedButton.Content.ToString();
#pragma warning disable CS8604 // Possible null reference argument.
                List<LogEntry> parsedEntries = LogParser.ParseLog(buttonContent);
#pragma warning restore CS8604 // Possible null reference argument.
                logEntries.Clear();
                foreach (var entry in parsedEntries)
                {
                    logEntries.Add(entry);
                }
                if (logView == null)
                {
                    logView = CollectionViewSource.GetDefaultView(logEntries);
                    logView.Filter = FilterLogEntries;
                    LogListView.ItemsSource = logView;
                }
                else
                {
                    logView.Refresh();
                }
            }
 
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            DateFilterBox.Clear();
            LevelFilterBox.Clear();
            ResultFilterBox.Clear();
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            logEntries.Clear();
            LogLevelComboBox.Text = null;
            DateColumn.Width = 150;
            LevelColumn.Width = 80;
            ResultColumn.Width = 495;
        }

        private void ShowErrors_Click(object sender, RoutedEventArgs e)
        {
            LevelFilterBox.Text = "ERROR";
        }

        private bool FilterLogEntries(object item)
        {
            if (item is LogEntry entry)
            {
                bool dateMatch = string.IsNullOrEmpty(DateFilterBox.Text) ||
                                 entry.Date?.ToString().IndexOf(DateFilterBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;

                bool levelMatch = string.IsNullOrEmpty(LevelFilterBox.Text) ||
                                  entry.Level?.IndexOf(LevelFilterBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;

                bool resultMatch = string.IsNullOrEmpty(ResultFilterBox.Text) ||
                                   entry.Result?.IndexOf(ResultFilterBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;

                return dateMatch && levelMatch && resultMatch;
            }

            return false;
        }

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            logView.Refresh();
        }

        private void LogLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selectedLevel = (LogLevelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedLevel == "ALL")
            {
                LevelFilterBox.Clear();
            }
            else
            {
                LevelFilterBox.Text = selectedLevel;
            }
            
        }
        private void ContextMenuItem_CopyClick(object sender, RoutedEventArgs e)
        {
            CopySelectedLogEntries();
        }

        private void LogListView_KeyboardCopy(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                CopySelectedLogEntries();
                e.Handled = true;
            }
        }

        private void CopySelectedLogEntries()
        {
            if (LogListView.SelectedItems.Count == 0)
                return;

            var sb = new StringBuilder();

            foreach (var item in LogListView.SelectedItems)
            {
                if (item is LogEntry entry)
                {
                    // Format the row content how you'd like
                    sb.AppendLine($"{entry.Date}\t{entry.Level}\t{entry.Result}");
                }
            }

            Clipboard.SetText(sb.ToString());
        }
    }
    public class LogEntry
    {
        public required string Date { get; set; }
        public required string Module { get; set; }
        public required string Level { get; set; }
        public required string Pid { get; set; }
        public required string Result { get; set; }

        public override string ToString()
        {
            return $"{Date} [{Module}][{Level}][{Pid}] {Result}";
        }
    }

    public static partial class LogParser
    {
        private static readonly Regex logRegex = LogRegex();

        public static List<LogEntry> ParseLog(string filePath)
        {
            var entries = new List<LogEntry>();
            LogEntry? current = null;

            foreach (var line in File.ReadLines(filePath))
            {
                var match = logRegex.Match(line);
                if (match.Success)
                {
                    if (current != null)
                        entries.Add(current);

                    current = new LogEntry
                    {
                        Date = match.Groups["date"].Value,
                        Module = match.Groups["module"].Value.Trim(),
                        Level = match.Groups["level"].Value.Trim(),
                        Pid = match.Groups["pid"].Value,
                        Result = match.Groups["result"].Value
                    };
                }
                else if (current != null)
                {
                    current.Result += "\n" + line;
                }
            }

            if (current != null)
                entries.Add(current);

            return entries;
        }

        [GeneratedRegex(@"^(?<date>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3}) \[(?<module>.+?)\]\[(?<level>.{8})\]\[(?<pid>\d+)\] (?<result>.*)", RegexOptions.Compiled)]
        private static partial Regex LogRegex();
    }
}

