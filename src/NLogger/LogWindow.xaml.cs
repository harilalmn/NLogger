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

    /// <summary>A single logged entry, kept so the view can be re-rendered when display options change.</summary>
    private readonly record struct Entry(DateTime Time, LogLevel Level, string Message);

    /// <summary>Backing store of entries used to rebuild the message area when toggles change.</summary>
    private readonly List<Entry> _entries = new();

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
        var entry = new Entry(DateTime.Now, level, message);
        _entries.Add(entry);

        MessageBox.AppendText(Format(entry));

        if (AutoScrollBox.IsChecked == true)
        {
            MessageBox.CaretIndex = MessageBox.Text.Length;
            MessageBox.ScrollToEnd();
        }
    }

    /// <summary>Renders a single entry honoring the current Hide Timestamp / Hide Log Level toggles.</summary>
    private string Format(Entry entry)
    {
        var sb = new System.Text.StringBuilder();
        if (HideTimestampBox?.IsChecked != true)
            sb.Append($"[{entry.Time:HH:mm:ss}] ");
        if (HideLogLevelBox?.IsChecked != true)
            sb.Append($"[{entry.Level.ToString().ToUpperInvariant()}] ");
        sb.Append(entry.Message);
        sb.Append(Environment.NewLine);
        return sb.ToString();
    }

    /// <summary>Rebuilds the whole message area from the stored entries; used when display options change.</summary>
    private void Rerender()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var entry in _entries)
            sb.Append(Format(entry));

        MessageBox.Text = sb.ToString();

        if (AutoScrollBox.IsChecked == true)
        {
            MessageBox.CaretIndex = MessageBox.Text.Length;
            MessageBox.ScrollToEnd();
        }
    }

    private void DisplayOption_Changed(object sender, RoutedEventArgs e) => Rerender();

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

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _entries.Clear();
        MessageBox.Clear();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
}
