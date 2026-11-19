using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinFOR
{
    public static class Placeholder
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached("Text", typeof(string), typeof(Placeholder), new PropertyMetadata("", OnTextChanged));

        public static string GetText(DependencyObject element)
        {
            return (string)element.GetValue(TextProperty);
        }

        public static void SetText(DependencyObject element, string value)
        {
            element.SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            if (element is not Control control)
            {
                return;
            }
            control.GotKeyboardFocus -= Refresh;
            control.LostKeyboardFocus -= Refresh;
            control.GotKeyboardFocus += Refresh;
            control.LostKeyboardFocus += Refresh;
            if (control is TextBox textBox)
            {
                textBox.TextChanged -= Refresh;
                textBox.TextChanged += Refresh;
            }
            else if (control is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= Refresh;
                passwordBox.PasswordChanged += Refresh;
            }
            Update(control);
        }

        private static void Refresh(object sender, RoutedEventArgs e)
        {
            if (sender is Control control)
            {
                Update(control);
            }
        }

        private static void Update(Control control)
        {
            bool empty = control is TextBox textBox ? textBox.Text.Length == 0 : control is PasswordBox passwordBox && passwordBox.Password.Length == 0;
            if (!empty || control.IsKeyboardFocused || GetText(control).Length == 0)
            {
                control.ClearValue(Control.BackgroundProperty);
                return;
            }
            control.Background = new VisualBrush(new TextBlock
            {
                Text = GetText(control),
                FontSize = control.FontSize,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                Margin = new Thickness(4, 0, 0, 0)
            })
            {
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Center,
                Stretch = Stretch.None
            };
        }
    }
}
