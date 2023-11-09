using System.IO;
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
        public ResultsWindow(StringBuilder results, StringBuilder errors, string logFile, string downloadLog, string wslPrepLog, string wslLog)
        {

            InitializeComponent();
            ShowErrors.Visibility = Visibility.Hidden;
            DisplayResults(results, errors, logFile);
        }
        public void DisplayResults(StringBuilder results, StringBuilder errors, string logFile)
        {
            string themeColour = "#FF1644B9";
            Color colour = (Color)ColorConverter.ConvertFromString(themeColour);
            SolidColorBrush brush = new(colour);
            MainGrid.Children.Add(new TextBox { Name = "LogNameTextBox", Text = logFile, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = true, AcceptsReturn = true, BorderThickness = new Thickness(0), FontSize = 14, });
            MainGrid.Children.Add(new Label { Height = 2, Width = 220, Background = brush, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 34, 0, 0) });
            MainGrid.Children.Add(new TextBox { Name = "ResultsTextBox", Text = results.ToString(), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 41, 0, 0), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, IsReadOnly = true, AcceptsReturn = true, BorderThickness = new Thickness(0), FontSize = 14, });
        
        //ResultsTextBox.Text = results.ToString();
            int errorsLines = errors.ToString().Split('\r').Length;
            if (errorsLines > 3)
            {
                ShowErrors.Visibility = Visibility.Visible;
            }
        }
        public void DisplayErrors(object sender, RoutedEventArgs e)
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
                (StringBuilder _, StringBuilder errors, string logFile, string downloadLog, string wslPrepLog, string wslLog) = (Application.Current.MainWindow as MainWindow)!.ProcessResults(releaseVersion);
                int errorsLines = errors.ToString().Split('\r').Length;
                if (errorsLines > 3)
                {
                    ErrorWindow errorWindow = new(errors, logFile)
                    {
                        Owner = this
                    };
                    errorWindow.Show();
                }
            }
            catch (FileNotFoundException)
            {

            }

        
        }
    }
}
