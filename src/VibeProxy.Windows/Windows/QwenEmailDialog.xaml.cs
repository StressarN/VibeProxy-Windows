using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WinMessageBox = System.Windows.MessageBox;

namespace VibeProxy.Windows;

public partial class QwenEmailDialog : Window
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public QwenEmailDialog()
    {
        InitializeComponent();
    }

    public string Email => EmailBox.Text.Trim();

    private void OnDialogDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnContinue(object sender, RoutedEventArgs e)
    {
        if (!EmailRegex.IsMatch(Email))
        {
            WinMessageBox.Show(this, "Enter a valid email address.", "Invalid email", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
