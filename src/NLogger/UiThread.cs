using System.Windows;
using System.Windows.Threading;

namespace NLoggerLib;

/// <summary>
/// Provides a WPF <see cref="Dispatcher"/> NLogger can marshal UI work onto.
/// <para>
/// If the host process is already a WPF application we reuse its dispatcher. Otherwise
/// (console apps, services, unit tests, WinForms hosts) we spin up a dedicated background
/// STA thread that owns a private <see cref="Application"/> and runs its own message pump,
/// so the logger window works regardless of the host's UI model.
/// </para>
/// </summary>
internal static class UiThread
{
    private static readonly object Gate = new();
    private static Dispatcher? _dispatcher;

    public static Dispatcher Dispatcher
    {
        get
        {
            if (_dispatcher is { } existing)
                return existing;

            lock (Gate)
            {
                if (_dispatcher is { } d)
                    return d;

                // Reuse the host application's dispatcher when one exists.
                if (Application.Current is { Dispatcher: { } appDispatcher })
                {
                    _dispatcher = appDispatcher;
                    return _dispatcher;
                }

                // No WPF Application yet: create our own UI thread.
                using var ready = new ManualResetEventSlim(false);
                var thread = new Thread(() =>
                {
                    // An Application instance is required so packaged (BAML) resources resolve.
                    _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    _dispatcher = Dispatcher.CurrentDispatcher;
                    ready.Set();
                    Dispatcher.Run();
                })
                {
                    Name = "NLogger UI",
                    IsBackground = true
                };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                ready.Wait();

                return _dispatcher!;
            }
        }
    }

    /// <summary>Runs <paramref name="action"/> on the UI thread and waits for it to complete.</summary>
    public static void Invoke(Action action) => Dispatcher.Invoke(action);

    /// <summary>Posts <paramref name="action"/> to the UI thread without waiting.</summary>
    public static void Post(Action action) => Dispatcher.BeginInvoke(action);
}
