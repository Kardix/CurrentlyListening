using System.Globalization;
using System.Windows;
using CurrentlyListening.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CurrentlyListening.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient(); // Registers IHttpClientFactory
                services.AddSingleton<MainWindow>();
                // Also register any services that depend on IHttpClientFactory here
            })
            .Build();

        // Resolve main window and show it
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        AppSettings.LoadSettings();
        var langCode = AppSettings.LangCode;
        if (!string.IsNullOrEmpty(langCode))
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(langCode);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(langCode);
        }
        if(!AppSettings.StartMinimized)
            mainWindow.Show();
    }
}
// This software is licensed under CC BY-NC-ND 4.0.
// Free for personal, non-commercial use only.
// Do not modify, distribute, or sell without written permission.
// (c) 2025 Tom "TKoNoR"