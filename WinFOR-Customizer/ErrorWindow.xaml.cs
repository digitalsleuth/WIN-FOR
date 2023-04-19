using System.Windows;

namespace WinFOR_Customizer
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public ErrorWindow(string errors)
        {
            InitializeComponent();
            ErrorsTextBox.Text = errors;
        }
    }
}
