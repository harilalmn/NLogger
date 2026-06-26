using System.Windows;
using NLoggerLib;

namespace NLogger.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Status.Text = $"Log file: {NLoggerLib.NLogger.LogFilePath}";
    }

    private string Msg => string.IsNullOrWhiteSpace(Input.Text) ? "(empty)" : Input.Text;

    private void LogW_Click(object sender, RoutedEventArgs e) => NLoggerLib.NLogger.LogW(Msg);

    private void Log_Click(object sender, RoutedEventArgs e)
    {
        string path = NLoggerLib.NLogger.Log(Msg);
        Status.Text = $"Wrote to: {path}";
    }

    private void LogBoth_Click(object sender, RoutedEventArgs e) => NLoggerLib.NLogger.LogBoth(Msg);

    private void Warn_Click(object sender, RoutedEventArgs e) => NLoggerLib.NLogger.LogW(Msg, LogLevel.Warning);

    private void Error_Click(object sender, RoutedEventArgs e) => NLoggerLib.NLogger.LogBoth(Msg, LogLevel.Error);
}
