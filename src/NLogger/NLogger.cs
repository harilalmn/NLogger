using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

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

    /// <summary>
    /// Shows <paramref name="json"/> in the NLogger window, pretty-printed with two-space
    /// indentation so nested structures are easy to read. Null is rendered as an empty object.
    /// </summary>
    public static void LogW(JsonObject json) => LogW(json, LogLevel.Info);

    /// <summary>Window log of a pretty-printed JSON object with an explicit severity level.</summary>
    public static void LogW(JsonObject json, LogLevel level) => LogW(FormatJson(json), level);

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

    /// <summary>
    /// Appends <paramref name="json"/> to the dated log file, pretty-printed with two-space
    /// indentation so nested structures are easy to read. Returns the path written to
    /// (empty string on failure). Null is rendered as an empty object.
    /// </summary>
    public static string Log(JsonObject json) => Log(json, LogLevel.Info);

    /// <summary>File log of a pretty-printed JSON object with an explicit severity level.</summary>
    public static string Log(JsonObject json, LogLevel level) => Log(FormatJson(json), level);

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

    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    /// <summary>
    /// Renders a <see cref="JsonObject"/> as indented JSON. A null object becomes <c>{}</c>;
    /// any serialization failure falls back to the object's default string representation so
    /// logging never throws into the host application.
    /// </summary>
    private static string FormatJson(JsonObject? json)
    {
        if (json is null)
            return "{}";

        try
        {
            return json.ToJsonString(IndentedJson);
        }
        catch
        {
            return json.ToString();
        }
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
