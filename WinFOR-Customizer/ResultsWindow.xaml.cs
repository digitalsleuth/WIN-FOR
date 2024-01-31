using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinFORCustomizer
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window
    {
        private readonly StringBuilder errors;
        private readonly StringBuilder errorIds;
        private readonly List<string> logFiles;
        private readonly string releaseVersion;
        public ResultsWindow(StringBuilder results, StringBuilder errors, StringBuilder errorIds, List<string> logFiles, string releaseVersion)
        {

            InitializeComponent();
            this.errors = errors;
            this.errorIds = errorIds;
            this.logFiles = logFiles;
            this.releaseVersion = releaseVersion;
            ShowErrors.Visibility = Visibility.Hidden;
            string themeColour = "#FF1644B9";
            Color colour = (Color)ColorConverter.ConvertFromString(themeColour);
            SolidColorBrush brush = new(colour);
            MainGrid.Children.Add(new TextBox { Name = "VersionTextBox", Text = releaseVersion, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(2, 10, 0, 0), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = true, AcceptsReturn = true, BorderThickness = new Thickness(0), FontSize = 14, });
            MainGrid.Children.Add(new Label { Height = 2, Width = 300, Background = brush, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 34, 0, 0) });
            MainGrid.Children.Add(new TextBox { Name = "ResultsTextBox", Text = results.ToString(), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 38, 0, 0), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = true, AcceptsReturn = true, BorderThickness = new Thickness(0), FontSize = 14, });
            int errorIdLength = errorIds.Length;
            if (errorIdLength > 0)
            {
                ShowErrors.Visibility = Visibility.Visible;
            }
        }
        public void DisplayErrors(object sender, RoutedEventArgs e)
        {
            try
            {
                ErrorWindow errorWindow = new(errors, errorIds, logFiles, releaseVersion, false)
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
