using System.Windows;

namespace WinFOR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += (sender, e) =>
            {
                MessageBox.Show($"An unexpected error occurred:\n{e.Exception}\n\nThe application will keep running so that the console output can still be saved.", "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
        }
    }
}
