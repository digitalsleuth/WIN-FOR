using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WinFOR
{
    public partial class ContentWindow : Window
    {
        private const double WidthPx = 700;
        private const double MinHeightPx = 200;
        private const double MaxHeightPx = 520;
        private const double ChromePx = 120;
        private const double LineHeightPx = 16;
        private const double CharsPerLine = 88;
        private static readonly SolidColorBrush errorBrush = new(Color.FromRgb(0xCB, 0x39, 0x3B));
        private static readonly SolidColorBrush warningBrush = new(Color.FromRgb(0xE0, 0xA0, 0x20));

        public ContentWindow(LogEntry entry, string text)
        {
            InitializeComponent();
            Title = $"{entry.Date} · {entry.Level}";
            ContentBox.Text = text;
            LevelStrip.Background = entry.Level.ToUpperInvariant() switch
            {
                "ERROR" or "CRITICAL" => errorBrush,
                "WARNING" => warningBrush,
                _ => Brushes.Transparent
            };
            int lineCount = text.Split('\n').Length;
            InfoText.Text = $"{lineCount:N0} {(lineCount == 1 ? "line" : "lines")} · {text.Length:N0} characters";
            int displayedLines = text.Split('\n').Sum(line => Math.Max(1, (int)Math.Ceiling(line.Length / CharsPerLine)));
            Rect workArea = SystemParameters.WorkArea;
            Width = Math.Min(WidthPx, workArea.Width * 0.9);
            Height = Math.Clamp(displayedLines * LineHeightPx + ChromePx, MinHeightPx, Math.Min(MaxHeightPx, workArea.Height * 0.9));
            Loaded += (sender, e) => ContentBox.Focus();
        }

        private void WrapChanged(object sender, RoutedEventArgs e)
        {
            if (ContentBox == null)
            {
                return;
            }
            ContentBox.TextWrapping = WrapCheck.IsChecked == true ? TextWrapping.Wrap : TextWrapping.NoWrap;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyAll();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control && ContentBox.SelectionLength == 0)
            {
                CopyAll();
                e.Handled = true;
            }
        }

        private void CopyAll()
        {
            try
            {
                Clipboard.SetText(ContentBox.Text);
            }
            catch (COMException ex)
            {
                MessageBox.Show($"Unable to copy to the clipboard - another application may be using it.\n{ex.Message}", "Unable to copy", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
