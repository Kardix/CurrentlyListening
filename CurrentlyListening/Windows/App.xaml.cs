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

    // UI-thread exceptions after startup (e.g. a poll-timer tick). Log, tell the
    // user, and keep running rather than letting a transient fault kill the app.
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("dispatcher", e.Exception);
        e.Handled = true;
    }

    // Last-resort net for exceptions on any thread. Cannot prevent termination,
    // but ensures the fault is recorded before the process dies.
    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogException("domain", e.ExceptionObject as Exception);
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
                .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ({source})")
                .AppendLine(ex?.ToString() ?? "Unknown error")
                .AppendLine();
            File.AppendAllText(CrashLogPath, entry.ToString());
        }
        catch
        {
            // Never let the crash logger itself throw.
        }
    }
}
// This software is licensed under CC BY-NC-ND 4.0.
// Free for personal, non-commercial use only.
// Do not modify, distribute, or sell without written permission.
// (c) 2026 Tom "TKoNoR"