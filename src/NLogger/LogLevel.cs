namespace NLoggerLib;

/// <summary>
/// Severity of a log entry. Controls colour in the window and the prefix written to file.
/// </summary>
public enum LogLevel
{
    /// <summary>Informational message (default).</summary>
    Info,
    /// <summary>Something unexpected but non-fatal.</summary>
    Warning,
    /// <summary>An error or failure.</summary>
    Error,
    /// <summary>A successful operation worth highlighting.</summary>
    Success
}
