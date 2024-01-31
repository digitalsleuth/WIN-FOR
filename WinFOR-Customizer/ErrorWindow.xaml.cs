using System;
using System.Windows;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace WinFORCustomizer
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        private readonly List<string> logfiles;
        private readonly StringBuilder errors;
        private readonly StringBuilder errorIds;
        private readonly string releaseVersion;
        public ErrorWindow(StringBuilder errors, StringBuilder errorIds, List<string> logfiles, string releaseVersion, bool internalClick)
        {
            string themeColour = "#FF1644B9";
            Color colour = (Color)ColorConverter.ConvertFromString(themeColour);
            SolidColorBrush brush = new(colour);
            InitializeComponent();
            this.logfiles = logfiles;
            this.errors = errors;
            this.errorIds = errorIds;
            this.releaseVersion = releaseVersion;
            StringBuilder errorList;
            if (internalClick)
            {
                errorList = errors;
                ErrorDetails.Visibility = Visibility.Hidden;
                ShowLogFile.Margin = new Thickness(0, 0, 0, 0);
            }
            else
            {
                errorList = errorIds;
                ErrorDetails.Visibility = Visibility.Visible;
            }
            MainGrid.Children.Add(new TextBox { Name = "LogNameTextBox", Text = releaseVersion, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = true, AcceptsReturn = true, BorderThickness = new Thickness(0), FontSize = 14, });
            MainGrid.Children.Add(new Label { Height = 2, Width = 220, Background = brush, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 34, 0, 0) });
            MainGrid.Children.Add(new TextBox { Name = "ErrorsTextBox", Text = errorList.ToString(), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 41, 0, 0), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = true, AcceptsReturn = true, BorderThickness = new Thickness(0), FontSize = 14, });
        }
        public void OpenLogFiles(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (string logFile in logfiles)
                {
                    if (File.Exists(logFile))
                    {
                        ProcessStartInfo startInfo = new()
                        {
                            FileName = @$"{logFile}",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        };
                        Process process = new()
                        {
                            StartInfo = startInfo
                        };
                        process.Start();
                    }
                }
            }
            catch (FileNotFoundException fnf)
            {
                MessageBox.Show($"[ERROR] Unable to open log files: {fnf} ");
            }
        }
        public void DisplayLogErrors(object sender, RoutedEventArgs e)
        {
            try
            {
                ErrorWindow errorWindow = new(errors, errorIds, logfiles, releaseVersion, true)
                {
                    Owner = this
                };
                errorWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] Unable to display errors:\n{ex}");
            }

        }
    }
}
