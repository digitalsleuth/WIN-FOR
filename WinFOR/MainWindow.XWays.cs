using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WinFOR
{
    // Optional X-Ways feature. To remove it, delete this file and the X-Ways card in MainWindow.xaml (its slot in ActionColumn stays reserved)
    public partial class MainWindow
    {
        private const string xwaysPlaceholder = "TOKENPLACEHOLDER";
        private const string xwaysToolName = "standalones_x_ways";
        private const string xwaysTemplatesName = "standalones_x_ways_templates";
        private static readonly Regex xwaysTokenRegex = new("(set auth_token = \")[^\"]*(\")");

        partial void XwaysCheckInputs(ref bool cancel, ref bool selected, ref string token)
        {
            if (XWays.IsChecked == true && XUser.Text != "" && XPass.Password != "")
            {
                token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{XUser.Text}:{XPass.Password}"));
                selected = true;
                ConsoleOutput($"X-Ways is selected and credentials have been provided");
            }
            else if (XWays.IsChecked == true)
            {
                ConsoleOutput("With X-Ways enabled, neither X-Ways Portal User nor X-Ways Portal Pass can be empty!");
                MessageBox.Show("With X-Ways enabled, neither X-Ways Portal User nor X-Ways Portal Pass can be empty!",
                                "X-Ways Portal Credentials Not Supplied",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                cancel = true;
            }
            else
            {
                token = xwaysPlaceholder;
                ConsoleOutput("X-Ways is not selected and will not be downloaded / installed");
            }
        }

        partial void XwaysInsertToken(bool selected, string token, bool install)
        // This function will take the provided authtoken and insert it in the required spot in the x-ways.sls State file once available.
        {
            if (!selected)
            {
                return;
            }
            ConsoleOutput("Adding authentication token to X-Ways state");
            string stateFile = XwaysStateFile(install);
            try
            {
                if (File.Exists(stateFile))
                {
                    string allText = File.ReadAllText(stateFile);
                    allText = allText.Replace($"{{% set auth_token = \"{xwaysPlaceholder}\" %}}", $"{{% set auth_token = \"{token}\" %}}");
                    File.WriteAllText(stateFile, allText);
                    ConsoleOutput($"Authentication token written to {stateFile}");
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to add X-Ways Token to {stateFile}:\n{ex}");
            }
        }

        partial void XwaysScrubToken(bool selected, bool install)
        // Puts the placeholder back once the install has finished so the credentials do not stay on disk
        {
            if (!selected)
            {
                return;
            }
            string stateFile = XwaysStateFile(install);
            try
            {
                if (File.Exists(stateFile))
                {
                    File.WriteAllText(stateFile, xwaysTokenRegex.Replace(File.ReadAllText(stateFile), $"${{1}}{xwaysPlaceholder}${{2}}"));
                    ConsoleOutput("Authentication token removed from the X-Ways state");
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput($"[ERROR] Unable to remove the X-Ways Token from {stateFile}:\n{ex}");
            }
        }

        partial void XwaysValidateField(Control field, string text, bool leaving, ref bool skipQuoteCheck, ref string? error)
        {
            if (field != XUser && field != XPass)
            {
                return;
            }
            skipQuoteCheck = true;
            if (XWays.IsChecked == true && text == "" && (leaving || !field.IsKeyboardFocusWithin))
            {
                error = "Required when X-Ways is selected.";
            }
        }

        partial void XwaysWireTool(string cbName, CheckBox toolCheckBox)
        {
            if (cbName == xwaysToolName)
            {
                toolCheckBox.Checked += XwaysChecked;
                toolCheckBox.Unchecked += XwaysUnchecked;
            }
        }

        partial void XwaysReset()
        {
            XWays.IsChecked = false;
        }

        private static string XwaysStateFile(bool install)
        {
            return install ? $@"{saltPath}\{src}\standalones\x-ways.sls" : $@"{saltPath}\{src}\downloads\standalones\x-ways.sls";
        }

        private void XwaysChecked(object sender, RoutedEventArgs e)
        // Determine if the X-Ways CheckBox is checked, and enable the user/pass boxes to enter credentials for the portal
        {
            if (XUser == null || XPass == null)
            {
                return;
            }
            XUser.IsEnabled = true;
            XPass.IsEnabled = true;
            XPassRevealBtn.IsEnabled = true;
            if (sender is CheckBox { Name: xwaysToolName })
            {
                XWays.IsChecked = true;
                SetTool(xwaysTemplatesName, true);
            }
            else if (sender is CheckBox { Name: "XWays" })
            {
                SetTool(xwaysToolName, true);
                SetTool(xwaysTemplatesName, true);
            }
        }

        private void XwaysUnchecked(object sender, RoutedEventArgs e)
        // Determine if the X-Ways CheckBox is unchecked, and disable the user/pass boxes
        {
            if (XUser == null || XPass == null)
            {
                return;
            }
            XUser.IsEnabled = false;
            XPass.IsEnabled = false;
            XPassRevealBtn.IsEnabled = false;
            SetFieldError(XUser, null);
            SetFieldError(XPass, null);
            if (sender is CheckBox { Name: xwaysToolName })
            {
                XWays.IsChecked = false;
                SetTool(xwaysTemplatesName, false);
            }
            else if (sender is CheckBox)
            {
                SetTool(xwaysToolName, false);
                SetTool(xwaysTemplatesName, false);
            }
        }

        private void XPass_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateField(XPass);
        }

        private void XUser_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateField(XUser, true);
        }

        private void XPass_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateField(XPass, true);
        }

        private void XPassReveal_Down(object sender, RoutedEventArgs e)
        {
            XPassVisible.Text = XPass.Password;
            XPassVisible.Visibility = Visibility.Visible;
            XPass.Visibility = Visibility.Hidden;
        }

        private void XPassReveal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Space or Key.Enter)
            {
                XPassReveal_Down(sender, e);
                e.Handled = true;
            }
        }

        private void XPassReveal_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Space or Key.Enter)
            {
                XPassReveal_Up(sender, e);
                e.Handled = true;
            }
        }

        private void XPassReveal_Up(object sender, RoutedEventArgs e)
        {
            XPassVisible.Clear();
            XPassVisible.Visibility = Visibility.Collapsed;
            XPass.Visibility = Visibility.Visible;
        }
    }
}
