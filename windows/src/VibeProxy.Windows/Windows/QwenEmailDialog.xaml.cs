using System.Text.RegularExpressions;
using System.Windows;

namespace VibeProxy.Windows;

public partial class QwenEmailDialog : Window
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public QwenEmailDialog()
    {
        InitializeComponent();
    }

    public string Email => EmailBox.Text.Trim();

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnContinue(object sender, RoutedEventArgs e)
    {
        if (!EmailRegex.IsMatch(Email))
        {
            MessageBox.Show(this, "Enter a valid email address.", "Invalid email", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
