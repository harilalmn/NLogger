using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NLoggerLib;

/// <summary>
/// The window shown by <see cref="NLogger.LogW(string)"/>. Displays accumulated, read-only but
/// selectable log text and lets the user configure the root location where
/// <see cref="NLogger.Log(string)"/> creates its dated folders.
/// </summary>
public partial class LogWindow : Window
{
    /// <summary>Raised when the user edits the log-root textbox so the facade can persist it.</summary>
    public event Action<string>? LogRootChanged;

    /// <summary>Creates the log window. Use <see cref="NLogger.ShowWindow"/> rather than constructing directly.</summary>
    public LogWindow()
    {
        InitializeComponent();
        LocationBox.LostFocus += (_, _) => RaiseRootChanged();

        // Closing the window should just hide it — the logger may be used again later.
        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }

    /// <summary>Sets the root path shown in the textbox without re-raising the change event.</summary>
    public void SetLogRoot(string root)
    {
        if (LocationBox.Text != root)
            LocationBox.Text = root;
    }

    /// <summary>Appends a timestamped, severity-tagged entry to the message area.</summary>
    public void Append(string message, LogLevel level)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpperInvariant()}] {message}{Environment.NewLine}";

        MessageBox.AppendText(line);

        if (AutoScrollBox.IsChecked == true)
        {
            MessageBox.CaretIndex = MessageBox.Text.Length;
            MessageBox.ScrollToEnd();
        }
    }

    private void RaiseRootChanged()
    {
        string root = LocationBox.Text.Trim();
        if (!string.IsNullOrEmpty(root))
            LogRootChanged?.Invoke(root);
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select the root location for dated log folders",
            Multiselect = false
        };

        string current = LocationBox.Text.Trim();
        if (Directory.Exists(current))
            dialog.InitialDirectory = current;

        if (dialog.ShowDialog(this) == true)
        {
            LocationBox.Text = dialog.FolderName;
            RaiseRootChanged();
        }
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        string root = LocationBox.Text.Trim();
        string folder = Path.Combine(root, DateTime.Now.ToString("yyyy-MMdd"));
        try
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.AppendText($"[NLogger] Could not open folder: {ex.Message}{Environment.NewLine}");
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MessageBox.Text))
            Clipboard.SetText(MessageBox.Text);
    }

    private void CopySelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MessageBox.SelectedText))
            Clipboard.SetText(MessageBox.SelectedText);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save log contents",
            FileName = $"nlogger-{DateTime.Now:yyyy-MMdd-HHmmss}.log",
            Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, MessageBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.AppendText($"[NLogger] Could not save: {ex.Message}{Environment.NewLine}");
            }
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e) => MessageBox.Clear();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
}
