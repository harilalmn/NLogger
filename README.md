# NLogger

A lightweight WPF logging utility for .NET. Reference one DLL and you get two ways to log:

| Call | Effect |
|------|--------|
| `NLogger.LogW(message)` | Shows the message in a window. Text is **read-only but fully selectable/copyable**. The window accumulates messages. |
| `NLogger.Log(message)` | Appends the message to a **dated log file**. A *Log File Location* textbox + Browse button set the **root path**; NLogger creates a `YYYY-MMDD` folder underneath it. Defaults to `MyDocuments\NLogger`. |

Both methods are **thread-safe** and work from **any host** — WPF, WinForms, console apps, or services. If the host isn't a WPF app, NLogger spins up its own background UI thread automatically.

## Project layout

```
NLogger.slnx
src/NLogger/            # the class library (builds NLogger.dll)
samples/NLogger.Demo/   # a small WPF app that exercises the API
```

## Build

```powershell
dotnet build NLogger.slnx -c Release
```

The library is produced at `src/NLogger/bin/Release/net10.0-windows/NLogger.dll`.

## Use it

Add a reference to `NLogger.dll` (or the project), then:

```csharp
using NLoggerLib;

NLogger.LogW("Hello — select and copy me!");        // window
NLogger.Log("Written to the dated log file.");      // file
NLogger.LogBoth("Goes to both.", LogLevel.Warning); // both

// Levels: Info (default), Warning, Error, Success
NLogger.LogW("Something failed", LogLevel.Error);

// Inspect / change where Log() writes (persisted across runs):
string path = NLogger.LogFilePath;          // <root>\YYYY-MMDD\nlogger-YYYY-MMDD.log
string today = NLogger.CurrentLogFolder;    // <root>\YYYY-MMDD
NLogger.LogRoot = @"D:\Logs";               // root path; dated folder created underneath. Also editable live in the window.

NLogger.ShowWindow();                        // bring the window up without logging
```

> Namespace note: the methods live on the type `NLoggerLib.NLogger`. The assembly/DLL is named `NLogger`. Use `using NLoggerLib;` and call `NLogger.LogW(...)`.

## Window features

- Read-only, selectable, copyable message area (monospace, timestamped, level-tagged).
- **Log File Location** textbox (root path) with **Browse…** (folder picker) and **Open** (reveal today's dated folder in Explorer).
- **Copy All**, **Copy Selection**, **Save to File**, **Clear**, **Close** (Close just hides — logging keeps working).
- **Auto-scroll** toggle.
- Closing the window hides it; the next `LogW` reuses it.

## Configuration storage

The chosen root path is persisted to `%AppData%\NLogger\settings.json`, so it survives restarts. Editing the textbox (or setting `NLogger.LogRoot`) updates it immediately.

## Run the demo

```powershell
dotnet run --project samples/NLogger.Demo
```
