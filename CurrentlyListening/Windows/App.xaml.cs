using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CurrentlyListening.Properties;
using CurrentlyListening.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CurrentlyListening.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost _host;

    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpotifyRetranslator", "crash.log");

    private const long MaxCrashLogBytes = 5 * 1024 * 1024;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Catch anything that escapes so a startup fault is logged (and shown)
        // instead of the process vanishing before the window ever appears.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient(); // Registers IHttpClientFactory
                    services.AddSingleton<MainWindow>();
                    // Also register any services that depend on IHttpClientFactory here
                })
                .Build();

            AppSettings.LoadSettings();
            var culture = AppSettings.GetCulture();
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;

            // Resolve main window and show it
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            if (!AppSettings.StartMinimized)
                mainWindow.Show();
        }
        catch (Exception ex)
        {
            LogException("startup", ex);
            MessageBox.Show(
                $"{Translations.APP_FAILED_TO_START}\n\n{ex.Message}\n\n{Translations.APP_FAILED_TO_START_LOCATION}\n{CrashLogPath}",
            Translations.APP_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppSettings.TryWriteClosedText();
        base.OnExit(e);
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        base.OnSessionEnding(e);
        if (!e.Cancel)
            AppSettings.TryWriteClosedText();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("dispatcher", e.Exception);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogException("domain", e.ExceptionObject as Exception);
        if (e.IsTerminating)
            AppSettings.TryWriteClosedText();
    }

    // Faults from fire-and-forget tasks (e.g. LoadTokens / CheckForUpdatesAsync).
    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("task", e.Exception);
        e.SetObserved();
    }

    private static void LogException(string source, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath));
            var entry = new StringBuilder()
                .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ({source}) v{AppVersion.Display}")
                .AppendLine(ex?.ToString() ?? "Unknown error")
                .AppendLine();
            TrimCrashLog(Encoding.UTF8.GetByteCount(entry.ToString()));
            File.AppendAllText(CrashLogPath, entry.ToString());
        }
        catch
        {
            // Never let the crash logger itself throw.
        }
    }

    // Keeps crash.log under MaxCrashLogBytes by dropping the oldest half of it,
    // cut at an entry boundary, so the most recent history survives.
    private static void TrimCrashLog(int incomingBytes)
    {
        var log = new FileInfo(CrashLogPath);
        if (!log.Exists || log.Length + incomingBytes <= MaxCrashLogBytes)
            return;

        var text = File.ReadAllText(CrashLogPath);
        var keepFrom = text.IndexOf("\n[", text.Length / 2, StringComparison.Ordinal);
        File.WriteAllText(CrashLogPath, keepFrom < 0 ? "" : text.Substring(keepFrom + 1));
    }
}
// This software is licensed under CC BY-NC-ND 4.0.
// Free for personal, non-commercial use only.
// Do not modify, distribute, or sell without written permission.
// (c) 2026 Tom "TKoNoR"