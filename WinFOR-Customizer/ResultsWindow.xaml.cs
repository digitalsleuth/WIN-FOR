using System.IO;
using System.Text;
using System.Windows;

namespace WinFORCustomizer
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window
    {
        public ResultsWindow(StringBuilder results, StringBuilder errors)
        {
            InitializeComponent();
            ShowErrors.Visibility = Visibility.Hidden;
            DisplayResults(results, errors);
        }
        public void DisplayResults(StringBuilder results, StringBuilder errors)
        {
            ResultsTextBox.Text = results.ToString();
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
                (StringBuilder _, StringBuilder errors) = (Application.Current.MainWindow as MainWindow)!.ProcessResults(releaseVersion);
                int errorsLines = errors.ToString().Split('\r').Length;
                if (errorsLines > 3)
                {
                    ErrorWindow errorWindow = new(errors)
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
