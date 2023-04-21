using System.Text;
using System.Windows;

namespace WinFOR_Customizer
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
            Display_Results(results, errors);
        }
        public void Display_Results(StringBuilder results, StringBuilder errors)
        {
            ResultsTextBox.Text = results.ToString();
            int errors_lines = errors.ToString().Split('\n').Length;
            if (errors_lines > 3)
            {
                ShowErrors.Visibility = Visibility.Visible;
            }
        }
        public void Display_Errors(object sender, RoutedEventArgs e)
        {
            (StringBuilder _, StringBuilder errors) = (Application.Current.MainWindow as MainWindow)!.Process_Results();
            int errors_lines = errors.ToString().Split('\n').Length;
            if (errors_lines > 3)
            {
                ErrorWindow errorWindow = new(errors)
                {
                    Owner = this
                };
                errorWindow.Show();
            }
        }
    }
}
