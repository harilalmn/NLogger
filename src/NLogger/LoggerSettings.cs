using System.IO;
using System.Text.Json;

namespace NLoggerLib;

/// <summary>
/// Persisted configuration for <see cref="NLogger"/>. Stored as JSON in
/// <c>%AppData%\NLogger\settings.json</c> so the chosen log folder survives between runs.
/// </summary>
public sealed class LoggerSettings
{
    private static readonly object SyncRoot = new();

    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NLogger");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    /// <summary>
    /// Root location under which <see cref="NLogger.Log(string)"/> creates a dated
    /// <c>YYYY-MMDD</c> sub-folder for each day's log file.
    /// </summary>
    public string LogRoot { get; set; } = DefaultLogRoot();

    /// <summary>The default root location: <c>MyDocuments\NLogger</c>.</summary>
    public static string DefaultLogRoot()
    {
        string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(docs, "NLogger");
    }

    /// <summary>Loads settings from disk, falling back to defaults if the file is missing or invalid.</summary>
    public static LoggerSettings Load()
    {
        lock (SyncRoot)
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var loaded = JsonSerializer.Deserialize<LoggerSettings>(json);
                    if (loaded is not null && !string.IsNullOrWhiteSpace(loaded.LogRoot))
                        return loaded;
                }
            }
            catch
            {
                // Corrupt/unreadable settings should never break logging — fall back to defaults.
            }

            return new LoggerSettings();
        }
    }

    /// <summary>Persists the current settings to disk. Failures are swallowed (logging must not crash the host).</summary>
    public void Save()
    {
        lock (SyncRoot)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch
            {
                // Ignore — a non-writable settings file is not fatal.
            }
        }
    }
}
