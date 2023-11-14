using System.Windows;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using Microsoft.VisualBasic.Logging;

namespace WinFORCustomizer
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        private readonly List<string> logfiles;
        public ErrorWindow(StringBuilder errors, List<string> logfiles, string releaseVersion)
        {
            string themeColour = "#FF1644B9";
            Color colour = (Color)ColorConverter.ConvertFromString(themeColour);
            SolidColorBrush brush = new(colour); 
            InitializeComponent();
            this.logfiles = logfiles;
            MainGrid.Children.Add(new TextBox { Name = "LogNameTextBox", Text = releaseVersion, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = true, AcceptsReturn = true, BorderThickness = new Thickness(0), FontSize = 14, });
            MainGrid.Children.Add(new Label { Height = 2, Width = 220, Background = brush, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 34, 0, 0) });
            MainGrid.Children.Add(new TextBox { Name = "ErrorsTextBox", Text = errors.ToString(), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 41, 0, 0), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = true, AcceptsReturn = true, BorderThickness = new Thickness(0), FontSize = 14, });
        }
        public void OpenLogFiles(object sender, RoutedEventArgs e)
        {
            string versionFileSalt = @"C:\ProgramData\Salt Project\Salt\srv\salt\winfor\VERSION";
            string versionFileLocal = @"C:\winfor-version";

            try
            {
                string releaseVersion = "";
                if (File.Exists(versionFileSalt))
                {
                    releaseVersion = File.ReadAllText($"{versionFileSalt}").TrimEnd();
                }
                else if (File.Exists(versionFileLocal))
                {
                    releaseVersion = File.ReadAllText($"{versionFileLocal}").TrimEnd();
                }
                else
                {
                    throw new FileNotFoundException("VERSION files not found");
                }
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
            catch (FileNotFoundException)
            {

            }


        }
    }
}
