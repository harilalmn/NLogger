using System.IO;

namespace NLoggerLib;

/// <summary>
/// Static logging facade. Reference this assembly and call:
/// <list type="bullet">
/// <item><see cref="LogW(string)"/> — show a message in a selectable, copyable window.</item>
/// <item><see cref="Log(string)"/> — append a message to a dated log file.</item>
/// </list>
/// Both are safe to call from any thread and from any host (WPF, WinForms, console).
/// </summary>
public static class NLogger
{
    private static readonly object FileGate = new();
    private static readonly LoggerSettings Settings = LoggerSettings.Load();

    private static LogWindow? _window;

    /// <summary>
    /// Gets or sets the <b>root</b> location under which a dated <c>YYYY-MMDD</c> sub-folder is
    /// created for each day's log. Defaults to <c>MyDocuments\NLogger</c>. This is the value
    /// shown in (and set by) the window's <i>Log File Location</i> textbox/Browse button.
    /// Setting it persists the value for next run.
    /// </summary>
    public static string LogRoot
    {
        get => Settings.LogRoot;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || value == Settings.LogRoot)
                return;

            Settings.LogRoot = value;
            Settings.Save();
            UiThread.Post(() => _window?.SetLogRoot(value));
        }
    }

    /// <summary>The dated folder currently in use: <c>&lt;LogRoot&gt;\YYYY-MMDD</c>.</summary>
    public static string CurrentLogFolder =>
        Path.Combine(Settings.LogRoot, DateTime.Now.ToString("yyyy-MMdd"));

    /// <summary>Full path of the log file currently being written (dated folder + dated file name).</summary>
    public static string LogFilePath =>
        Path.Combine(CurrentLogFolder, $"nlogger-{DateTime.Now:yyyy-MMdd}.log");

    // ---- Window logging ------------------------------------------------------

    /// <summary>
    /// Shows <paramref name="message"/> in the NLogger window. The window accumulates messages
    /// and the text is read-only but fully selectable/copyable. Creates the window on first use.
    /// </summary>
    public static void LogW(string message) => LogW(message, LogLevel.Info);

    /// <summary>Window log with an explicit severity level.</summary>
    public static void LogW(string message, LogLevel level)
    {
        message ??= string.Empty;

        UiThread.Post(() =>
        {
            EnsureWindow();
            _window!.Append(message, level);

            if (!_window.IsVisible)
                _window.Show();

            _window.Activate();
        });
    }

    /// <summary>Brings the log window to the foreground, creating it if necessary.</summary>
    public static void ShowWindow() => UiThread.Post(() =>
    {
        EnsureWindow();
        if (!_window!.IsVisible)
            _window.Show();
        _window.Activate();
    });

    // ---- File logging --------------------------------------------------------

    /// <summary>
    /// Appends <paramref name="message"/> to the dated log file inside <see cref="CurrentLogFolder"/>,
    /// creating the folder if needed. Returns the path written to (empty string on failure).
    /// </summary>
    public static string Log(string message) => Log(message, LogLevel.Info);

    /// <summary>File log with an explicit severity level.</summary>
    public static string Log(string message, LogLevel level)
    {
        message ??= string.Empty;
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level.ToString().ToUpperInvariant()}] {message}{Environment.NewLine}";

        lock (FileGate)
        {
            try
            {
                Directory.CreateDirectory(CurrentLogFolder);
                string path = LogFilePath;
                File.AppendAllText(path, line);
                return path;
            }
            catch
            {
                // Logging must never throw into the host application.
                return string.Empty;
            }
        }
    }

    /// <summary>Convenience: write the message to both the window and the file.</summary>
    public static void LogBoth(string message, LogLevel level = LogLevel.Info)
    {
        LogW(message, level);
        Log(message, level);
    }

    private static void EnsureWindow()
    {
        if (_window is not null)
            return;

        _window = new LogWindow();
        _window.SetLogRoot(Settings.LogRoot);
        _window.LogRootChanged += root => LogRoot = root;
    }
}
