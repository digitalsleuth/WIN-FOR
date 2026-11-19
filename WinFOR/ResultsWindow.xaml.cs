using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WinFOR
{
    public partial class ResultsWindow : Window
    {
        private const double sourceColumnWidth = 110;
        private const double releaseColumnWidth = 110;
        private const double dateColumnWidth = 150;
        private const double levelColumnWidth = 80;
        private const double minResultColumnWidth = 240;
        private const double scrollbarAllowance = 30;
        private readonly DispatcherTimer filterTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
        private readonly DispatcherTimer selectionTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
        private readonly ObservableCollection<LogFileInfo> logFiles = [];
        private ObservableCollection<LogEntry> logEntries = [];
        private ICollectionView logView;
        private GridViewColumnHeader? lastHeaderClicked;
        private ListSortDirection lastDirection = ListSortDirection.Ascending;
        private string dateFilter = "";
        private string levelFilter = "";
        private string resultFilter = "";
        private int loadVersion;
        private int totalEntries;

        public ResultsWindow()
        {
            InitializeComponent();
            logView = CollectionViewSource.GetDefaultView(logEntries);
            logView.Filter = FilterLogEntries;
            LogListView.ItemsSource = logView;
            ICollectionView fileView = CollectionViewSource.GetDefaultView(logFiles);
            fileView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LogFileInfo.Release)));
            fileView.SortDescriptions.Add(new SortDescription(nameof(LogFileInfo.ReleaseOrder), ListSortDirection.Descending));
            fileView.SortDescriptions.Add(new SortDescription(nameof(LogFileInfo.Release), ListSortDirection.Descending));
            fileView.SortDescriptions.Add(new SortDescription(nameof(LogFileInfo.Modified), ListSortDirection.Descending));
            LogList.ItemsSource = fileView;
            filterTimer.Tick += (sender, e) => { filterTimer.Stop(); RefreshView(); };
            selectionTimer.Tick += async (sender, e) => { selectionTimer.Stop(); await LoadSelectedLogs(); };
            Closed += (sender, e) => { filterTimer.Stop(); selectionTimer.Stop(); };
            Loaded += (sender, e) => ScanLogs(null);
        }

        internal static List<LogFileInfo> FindLogs()
        {
            try
            {
                List<LogFileInfo> found = [.. Directory.EnumerateFiles(MainWindow.localRoot, $"{MainWindow.saltLogPrefix}*.log").Select(LogFileInfo.FromPath).OfType<LogFileInfo>()];
                foreach (IGrouping<string, LogFileInfo> release in found.GroupBy(log => log.Release))
                {
                    DateTime newest = release.Max(log => log.Modified);
                    release.ToList().ForEach(log => log.ReleaseOrder = newest);
                }
                return found;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return [];
            }
        }

        private void ScanLogs(HashSet<string>? keepSelected)
        {
            List<LogFileInfo> found = FindLogs();
            logFiles.Clear();
            found.ForEach(logFiles.Add);
            NoLogsText.Visibility = found.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            LogInfoDefaults(found.Count);
            LogFileInfo? newest = found.OrderByDescending(log => log.Modified).FirstOrDefault();
            foreach (LogFileInfo log in found.Where(log => keepSelected?.Contains(log.Path) ?? log == newest))
            {
                LogList.SelectedItems.Add(log);
            }
            found.ForEach(log => _ = log.LoadSummary());
        }

        private void LogInfoDefaults(int count)
        {
            SummaryText.Text = count == 0 ? $"No {MainWindow.saltLogPrefix.TrimEnd('-')} log files were found in {MainWindow.localRoot}" : "Select one or more log files to see their entries.";
            NoEntriesText.Visibility = Visibility.Visible;
            ShowSourceColumns(false);
        }

        private void ShowSourceColumns(bool show)
        {
            ReleaseColumn.Width = show ? releaseColumnWidth : 0;
            SourceColumn.Width = show ? sourceColumnWidth : 0;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ScanLogs([.. LogList.SelectedItems.OfType<LogFileInfo>().Select(log => log.Path)]);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                Refresh_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ResultFilterBox.Focus();
                e.Handled = true;
            }
        }

        private void LogItem_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && !item.IsSelected)
            {
                LogList.SelectedItems.Clear();
                item.IsSelected = true;
            }
        }

        private void LogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectionTimer.Stop();
            selectionTimer.Start();
        }

        private async Task LoadSelectedLogs()
        {
            List<LogFileInfo> selected = [.. LogList.SelectedItems.OfType<LogFileInfo>()];
            int version = ++loadVersion;
            if (selected.Count == 0)
            {
                SetEntries([], 0);
                LogInfoDefaults(logFiles.Count);
                return;
            }
            Cursor = Cursors.Wait;
            SummaryText.Text = selected.Count == 1 ? "Reading log..." : $"Reading {selected.Count} logs...";
            List<string> failed = [];
            try
            {
                List<LogEntry>?[] parsed = await Task.WhenAll(selected.Select(log => Task.Run(() =>
                {
                    try
                    {
                        return LogParser.ParseLog(log.Path, log.KindLabel, log.Release);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        lock (failed)
                        {
                            failed.Add(log.FileName);
                        }
                        return null;
                    }
                })));
                if (version != loadVersion)
                {
                    return;
                }
                IEnumerable<LogEntry> entries = parsed.OfType<List<LogEntry>>().SelectMany(list => list);
                SetEntries(selected.Count == 1 ? [.. entries] : [.. entries.OrderBy(entry => entry.Date, StringComparer.Ordinal)], selected.Count);
                if (failed.Count > 0)
                {
                    MessageBox.Show(this, $"These log files are locked or could not be read, and were skipped:\n{string.Join("\n", failed)}\nTry making a copy of the log file and reviewing its contents manually.", "Unable to open log file", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"The selected log files could not be processed:\n{ex}\nPlease consider reviewing them manually.", "Unable to open log file", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                if (version == loadVersion)
                {
                    Cursor = null;
                }
            }
        }

        private void SetEntries(List<LogEntry> entries, int logCount)
        {
            List<SortDescription> sorts = [.. logView.SortDescriptions];
            logEntries = new ObservableCollection<LogEntry>(entries);
            logView = CollectionViewSource.GetDefaultView(logEntries);
            logView.Filter = FilterLogEntries;
            foreach (SortDescription sort in sorts)
            {
                logView.SortDescriptions.Add(sort);
            }
            LogListView.ItemsSource = logView;
            totalEntries = entries.Count;
            ShowSourceColumns(logCount > 1);
            FitResultColumn();
            NoEntriesText.Visibility = logCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (logCount > 0)
            {
                ShowCounts(entries.Count);
            }
        }

        private void ShowCounts(int shown)
        {
            int errors = logEntries.Count(entry => entry.Level is "ERROR" or "CRITICAL");
            int warnings = logEntries.Count(entry => entry.Level == "WARNING");
            SummaryText.Text = $"{LogList.SelectedItems.Count} {(LogList.SelectedItems.Count == 1 ? "log" : "logs")} · {(shown == totalEntries ? $"{totalEntries:N0} entries" : $"{shown:N0} of {totalEntries:N0} entries")} · {errors:N0} errors · {warnings:N0} warnings";
        }

        private void RefreshView()
        {
            logView.Refresh();
            if (LogList.SelectedItems.Count > 0)
            {
                ShowCounts(logView.Cast<object>().Count());
            }
        }

        private void GridViewColumnHeader_Clicked(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader headerClicked || headerClicked.Role == GridViewColumnHeaderRole.Padding)
            {
                return;
            }
            ListSortDirection direction = headerClicked != lastHeaderClicked || lastDirection == ListSortDirection.Descending ? ListSortDirection.Ascending : ListSortDirection.Descending;
            string? sortBy = (headerClicked.Column.DisplayMemberBinding as Binding)?.Path.Path ?? headerClicked.Column.Header as string;
            using (logView.DeferRefresh())
            {
                logView.SortDescriptions.Clear();
                logView.SortDescriptions.Add(new SortDescription(sortBy!, direction));
            }
            headerClicked.Column.HeaderTemplate = Resources[direction == ListSortDirection.Ascending ? "HeaderTemplateArrowUp" : "HeaderTemplateArrowDown"] as DataTemplate;
            if (lastHeaderClicked != null && lastHeaderClicked != headerClicked)
            {
                lastHeaderClicked.Column.HeaderTemplate = null;
            }
            lastHeaderClicked = headerClicked;
            lastDirection = direction;
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            DateFilterBox.Clear();
            ResultFilterBox.Clear();
            LogLevelComboBox.SelectedIndex = 0;
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            ClearFilters_Click(sender, e);
            using (logView.DeferRefresh())
            {
                logView.SortDescriptions.Clear();
            }
            if (lastHeaderClicked != null)
            {
                lastHeaderClicked.Column.HeaderTemplate = null;
                lastHeaderClicked = null;
            }
            lastDirection = ListSortDirection.Ascending;
            ShowSourceColumns(LogList.SelectedItems.Count > 1);
            DateColumn.Width = dateColumnWidth;
            LevelColumn.Width = levelColumnWidth;
            FitResultColumn();
        }

        private void FitResultColumn()
        {
            ResultColumn.Width = Math.Max(minResultColumnWidth, LogListView.ActualWidth - ReleaseColumn.Width - SourceColumn.Width - DateColumn.Width - LevelColumn.Width - scrollbarAllowance);
        }

        private void LogListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FitResultColumn();
        }

        private bool FilterLogEntries(object item)
        {
            return item is LogEntry entry &&
                   (dateFilter.Length == 0 || entry.Date.Contains(dateFilter, StringComparison.OrdinalIgnoreCase)) &&
                   (levelFilter.Length == 0 || entry.Level.Contains(levelFilter, StringComparison.OrdinalIgnoreCase)) &&
                   (resultFilter.Length == 0 || entry.Result.Contains(resultFilter, StringComparison.OrdinalIgnoreCase));
        }

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            dateFilter = DateFilterBox.Text;
            resultFilter = ResultFilterBox.Text;
            filterTimer.Stop();
            filterTimer.Start();
        }

        private void LogLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogLevelComboBox.SelectedItem is not ComboBoxItem item)
            {
                return;
            }
            levelFilter = LogLevelComboBox.SelectedIndex == 0 ? "" : item.Content.ToString()!;
            filterTimer.Stop();
            filterTimer.Start();
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
            else if (e.Key is Key.Enter or Key.F2 && LogListView.SelectedItem is LogEntry entry)
            {
                ShowContent(entry);
                e.Handled = true;
            }
        }

        private void CopySelectedLogEntries()
        {
            if (LogListView.SelectedItems.Count == 0)
            {
                return;
            }
            StringBuilder text = new();
            foreach (LogEntry entry in LogListView.SelectedItems.OfType<LogEntry>())
            {
                text.AppendLine(logEntries.Count > 0 && SourceColumn.Width > 0 ? $"{entry.Release}\t{entry.Source}\t{entry.Date}\t{entry.Level}\t{entry.Result}" : $"{entry.Date}\t{entry.Level}\t{entry.Result}");
            }
            CopyText(text.ToString());
        }

        private void CopyPath_Click(object sender, RoutedEventArgs e)
        {
            CopyText(string.Join("\n", LogList.SelectedItems.OfType<LogFileInfo>().Select(log => log.Path)));
        }

        private static void CopyText(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                MessageBox.Show($"Unable to copy to the clipboard - another application may be using it.\n{ex.Message}", "Unable to copy", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            foreach (LogFileInfo log in LogList.SelectedItems.OfType<LogFileInfo>())
            {
                StartProcess(new ProcessStartInfo(log.Path) { UseShellExecute = true }, log.Path);
            }
        }

        private void ShowInFolder_Click(object sender, RoutedEventArgs e)
        {
            foreach (LogFileInfo log in LogList.SelectedItems.OfType<LogFileInfo>())
            {
                StartProcess(new ProcessStartInfo("explorer.exe", $"/select,\"{log.Path}\"") { UseShellExecute = true }, log.Path);
            }
        }

        private static void StartProcess(ProcessStartInfo startInfo, string logFile)
        {
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open the log file {logFile}:\n{ex}\nPlease consider reviewing this file manually.", "Unable to open log file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static T? VisualUpwardSearch<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && source is not T)
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as T;
        }

        private static string? GetCellValue(object row, GridViewColumn column)
        {
            if (column.DisplayMemberBinding is Binding binding)
            {
                return row.GetType().GetProperty(binding.Path.Path)?.GetValue(row)?.ToString();
            }
            return "";
        }

        private void ShowContent(LogEntry entry)
        {
            List<GridViewColumn> columns = [.. ((GridView)LogListView.View).Columns.Where(column => column.Width > 0)];
            int labelWidth = columns.Max(column => column.Header.ToString()!.Length);
            StringBuilder text = new();
            foreach (GridViewColumn column in columns)
            {
                string value = GetCellValue(entry, column) ?? "";
                text.AppendLine($"{column.Header.ToString()!.PadRight(labelWidth)} : {(value.Contains('\n') ? $"\n{value}" : value)}");
            }
            new ContentWindow(entry, text.ToString().TrimEnd())
            {
                Owner = this
            }.Show();
        }

        private void Header_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source || VisualUpwardSearch<Thumb>(source) == null || VisualUpwardSearch<GridViewColumnHeader>(source) is not GridViewColumnHeader header)
            {
                return;
            }
            e.Handled = true;
            PropertyInfo? property = typeof(LogEntry).GetProperty((header.Column.DisplayMemberBinding as Binding)?.Path.Path ?? "");
            if (property == null)
            {
                return;
            }
            Typeface typeface = new(LogListView.FontFamily, LogListView.FontStyle, LogListView.FontWeight, LogListView.FontStretch);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double MeasureText(string text) => new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, LogListView.FontSize, Brushes.Black, pixelsPerDip).WidthIncludingTrailingWhitespace;
            double widest = logView.Cast<LogEntry>()
                .Select(entry => property.GetValue(entry)?.ToString() ?? "")
                .Distinct()
                .OrderByDescending(value => value.Length)
                .Take(200)
                .Select(MeasureText)
                .DefaultIfEmpty(0)
                .Max();
            header.Column.Width = Math.Ceiling(Math.Max(widest + 14, MeasureText(header.Content?.ToString() ?? "") + 28));
        }

        private void Cell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && VisualUpwardSearch<ListViewItem>(source)?.Content is LogEntry entry)
            {
                ShowContent(entry);
            }
        }
    }

    public sealed partial class LogFileInfo(string path, string release, string kindLabel, DateTime modified, long size) : INotifyPropertyChanged
    {
        private const string unversionedRelease = "Unversioned";
        private static readonly Regex fileNameRegex = new($@"^{MainWindow.saltLogPrefix}(?<release>.+?)(?:-(?<kind>downloads|wsl|offline))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly SolidColorBrush mutedBrush = Frozen(0x77, 0x77, 0x77);
        private static readonly SolidColorBrush goodBrush = Frozen(0x0F, 0x5B, 0x1C);
        private static readonly SolidColorBrush badBrush = Frozen(0xCB, 0x39, 0x3B);
        private string summary = "Reading summary...";
        private Brush summaryBrush = mutedBrush;

        public event PropertyChangedEventHandler? PropertyChanged;
        public string Path { get; } = path;
        public string FileName { get; } = System.IO.Path.GetFileName(path);
        public string Release { get; } = release;
        public string KindLabel { get; } = kindLabel;
        public DateTime Modified { get; } = modified;
        public DateTime ReleaseOrder { get; set; } = modified;
        public string Detail { get; } = $"{System.IO.Path.GetFileName(path)} · {FormatSize(size)}";
        public string Summary
        {
            get => summary;
            private set
            {
                summary = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary)));
            }
        }
        public Brush SummaryBrush
        {
            get => summaryBrush;
            private set
            {
                summaryBrush = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SummaryBrush)));
            }
        }

        private static SolidColorBrush Frozen(byte r, byte g, byte b)
        {
            SolidColorBrush brush = new(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private static string FormatSize(long bytes)
        {
            return bytes switch
            {
                >= 1024 * 1024 => $"{bytes / 1048576.0:N1} MB",
                >= 1024 => $"{bytes / 1024.0:N0} KB",
                _ => $"{bytes} B"
            };
        }

        internal static LogFileInfo? FromPath(string path)
        {
            Match match = fileNameRegex.Match(System.IO.Path.GetFileNameWithoutExtension(path));
            if (!match.Success)
            {
                return null;
            }
            string release = match.Groups["release"].Value;
            string kind = match.Groups["kind"].Value;
            if (kind.Length == 0 && release == "offline")
            {
                kind = "offline";
                release = unversionedRelease;
            }
            else if (release.Equals(unversionedRelease, StringComparison.OrdinalIgnoreCase))
            {
                release = unversionedRelease;
            }
            FileInfo info = new(path);
            string kindLabel = kind switch
            {
                "downloads" => "Download / Stage",
                "wsl" => "WSL",
                "offline" => "Offline Install",
                _ => "Install"
            };
            return new LogFileInfo(path, release, kindLabel, info.LastWriteTime, info.Length);
        }

        internal async Task LoadSummary()
        {
            (string text, bool failed) = await Task.Run(() => ReadSummary(Path));
            Summary = text;
            SummaryBrush = text.StartsWith("No state summary", StringComparison.Ordinal) || text.StartsWith("Unable", StringComparison.Ordinal) ? mutedBrush : failed ? badBrush : goodBrush;
        }

        private static (string Text, bool Failed) ReadSummary(string path)
        {
            int? succeeded = null;
            int? failed = null;
            try
            {
                using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.SequentialScan);
                using StreamReader reader = new(stream, Encoding.UTF8, true, 65536);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length > 8 && line[0] is 'S' or 'F' && SummaryRegex().Match(line) is { Success: true } match)
                    {
                        int count = int.Parse(match.Groups["count"].Value);
                        if (match.Groups["name"].Value == "Succeeded")
                        {
                            succeeded = count;
                        }
                        else
                        {
                            failed = count;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or OverflowException)
            {
                return ("Unable to read this log", false);
            }
            return succeeded is null && failed is null ? ("No state summary (incomplete or still running)", false) : ($"{succeeded ?? 0:N0} succeeded · {failed ?? 0:N0} failed", failed > 0);
        }

        [GeneratedRegex(@"^(?<name>Succeeded|Failed):\s+(?<count>\d+)")]
        private static partial Regex SummaryRegex();
    }

    public class LogEntry
    {
        public required string Date { get; set; }
        public required string Module { get; set; }
        public required string Level { get; set; }
        public required string Pid { get; set; }
        public required string Result { get; set; }
        public string Source { get; set; } = "";
        public string Release { get; set; } = "";

        public override string ToString()
        {
            return $"{Date} [{Module}][{Level}][{Pid}] {Result}";
        }
    }

    public static partial class LogParser
    {
        private const int LogBufferSize = 65536;
        private static readonly Regex logRegex = LogRegex();

        public static List<LogEntry> ParseLog(string filePath, string source = "", string release = "")
        {
            var entries = new List<LogEntry>();
            LogEntry? current = null;
            StringBuilder continuation = new();
            void CompleteEntry()
            {
                if (current == null)
                    return;
                if (continuation.Length > 0)
                {
                    current.Result += continuation.ToString();
                    continuation.Clear();
                }
                entries.Add(current);
            }
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, LogBufferSize, FileOptions.SequentialScan))
            using (var reader = new StreamReader(fs, Encoding.UTF8, true, LogBufferSize))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var match = logRegex.Match(line);
                    if (match.Success)
                    {
                        CompleteEntry();

                        current = new LogEntry
                        {
                            Date = match.Groups["date"].Value,
                            Module = match.Groups["module"].Value.Trim(),
                            Level = match.Groups["level"].Value.Trim(),
                            Pid = match.Groups["pid"].Value,
                            Result = match.Groups["result"].Value,
                            Source = source,
                            Release = release
                        };
                    }
                    else if (current != null && line.Length != 0)
                    {
                        continuation.Append('\n').Append(line);
                    }
                }
            }

            CompleteEntry();
            return entries;
        }

        [GeneratedRegex(@"^(?<date>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3}) \[(?<module>.+?)\]\[(?<level>.{8})\]\[(?<pid>\d+)\] (?<result>.*)")]
        private static partial Regex LogRegex();

    }
}
