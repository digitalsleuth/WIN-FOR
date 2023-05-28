using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinFOR_Customizer
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {

        public TreeView? AllTools { get; set; }
        public SearchWindow()
        {
            InitializeComponent();
            search_box.Focus();
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = search_box.Text.ToLower();          
            foreach (TreeViewItem ti in MainWindow.GetLogicalChildCollection<TreeViewItem>(AllTools!))
            {
                ti.IsExpanded = true;
            }
            foreach (CheckBox checkBox in MainWindow.GetLogicalChildCollection<CheckBox>(AllTools!))

            {
                string content = checkBox.Content.ToString()!.ToLower();
                if (content.Contains(searchText))
                {
                    if (checkBox.Name.StartsWith("header"))
                    {
                        checkBox.IsEnabled = false;
                        continue;
                    }
                    else
                    {
                        checkBox.Foreground = Brushes.Red;
                        checkBox.BringIntoView();
                    }
                }
                else
                {
                    AllTools!.Items.Remove(checkBox);
                    if (checkBox.Name.StartsWith("header"))
                    {
                        checkBox.IsEnabled = false;
                        continue;
                    }
                    else
                    {
                        checkBox.Foreground = Brushes.Black;
                        checkBox.Visibility = Visibility.Hidden;
                    }
                }
                if (searchText == "")
                {
                    checkBox.IsEnabled = true;
                    checkBox.Foreground = Brushes.Black;
                    checkBox.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
