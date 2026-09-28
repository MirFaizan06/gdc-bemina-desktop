using System.Linq;
using System.Windows;
using CollegeAdmin.Application;
using CollegeAdmin.Application.Auth;
using CollegeAdmin.Application.Navigation;
using CollegeAdmin.Application.Updates;
using CollegeAdmin.Desktop.Navigation;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Desktop.Views;
using CollegeAdmin.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CollegeAdmin.Desktop;

/// <summary>
/// Composition root. Builds a Generic Host purely for its configuration+DI container (no web
/// server, no background service) — appsettings.json is the base config source; Api:BaseUrl must
/// be overridden for a real deployment (appsettings.Production.json or an environment variable),
/// the checked-in default is local-dev only.
///
/// Startup flow (Stage 3): try to silently restore a session from the stored refresh token; if
/// that fails, show the login window; only once authenticated does MainWindow appear. This is a
/// deliberately minimal flow — Stage 4 replaces it with a real navigation shell, and closing
/// MainWindow simply exits the app for now rather than minimizing/reopening.
/// </summary>
// Base class qualified explicitly: `using CollegeAdmin.Application;` (the layer) and
// `using System.Windows;` (which exposes the `Application` class) both bring an "Application"
// identifier into scope, and the unqualified name resolves to the namespace, not the class.
public partial class App : System.Windows.Application
{
    private IHost? _host;
    private bool _isTransitioningToLogin;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Prevent WPF from auto-shutting down when the LoginWindow closes (it's shown before
        // MainWindow exists, so "last window closed" would otherwise fire prematurely). Shutdown()
        // is called explicitly at the two points that should actually end the app.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _host = Host.CreateDefaultBuilder()
            // Host.CreateDefaultBuilder() resolves appsettings.json relative to
            // Directory.GetCurrentDirectory() by default — fine for a web app always launched
            // from its own project folder, but wrong for a desktop app, which can be started from
            // a shortcut, Start Menu entry, or any other working directory. Pinning the content
            // root to the executable's own folder makes config loading independent of how the app
            // was launched. (Found live: the app crashed at startup with "Configuration key
            // 'Api:BaseUrl' is required but was not set" the moment it was launched from a
            // terminal whose working directory wasn't this folder — exactly the scenario a real
            // Start Menu launch would also hit.)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                var currentAppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
                services
                    .AddApplication()
                    .AddInfrastructure(context.Configuration, currentAppVersion);

                services.AddTransient<LoginViewModel>();
                services.AddTransient<LoginWindow>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddTransient<PlaceholderPageViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AdminsListViewModel>();
                services.AddTransient<BackgroundJobsViewModel>();
                services.AddTransient<NoticeCreateViewModel>();
                services.AddTransient<TimetableEntryCreateViewModel>();
                // GenericListViewModel/TabbedListViewModel are constructed directly by
                // NavigationService (they need per-call title/loader parameters that don't fit
                // typical DI-container transient resolution) — not registered here.
                services.AddTransient<ShellViewModel>();
                services.AddTransient<MainWindow>();
            })
            .Build();

        _host.Start();

        _ = RunStartupFlowAsync();
    }

    /// <summary>Boot sequence: animated splash ("Starting..." -> "Checking for updates...") ->
    /// silent session restore -> Login (skipped if restore succeeded) -> a brief "Preparing your
    /// dashboard..." splash beat -> the real shell. The update check here is real (not cosmetic) —
    /// it's the same IUpdateService.CheckForUpdateAsync() ShellViewModel would otherwise call itself
    /// on first load, just moved earlier so the splash's status text means something.</summary>
    private async Task RunStartupFlowAsync()
    {
        var splash = new SplashWindow();
        splash.Show();

        var updateService = _host!.Services.GetRequiredService<IUpdateService>();
        splash.SetStatus("Checking for updates...");
        _ = updateService.CheckForUpdateAsync(); // fire-and-forget: ShellViewModel does the real, user-facing check itself; this just keeps the splash message honest without blocking boot on a slow/offline network.

        var authSessionService = _host.Services.GetRequiredService<IAuthSessionService>();
        authSessionService.SessionExpired += OnSessionExpired;

        splash.SetStatus("Signing you in...");
        var restored = await authSessionService.TryRestoreSessionAsync();

        if (restored)
        {
            await ShowLoadingBeatAsync(splash);
            splash.Close();
            ShowMainWindow();
            return;
        }

        splash.Close();
        ShowLoginThenMain();
    }

    /// <summary>A short, deliberate minimum-display delay (not artificial slowness for its own
    /// sake) so the "Preparing your dashboard..." moment is perceptible rather than an instant,
    /// jarring cut — ShellViewModel's own construction is fast enough that without this the splash
    /// would flash by unreadably.</summary>
    private static async Task ShowLoadingBeatAsync(SplashWindow splash)
    {
        splash.SetStatus("Preparing your dashboard...");
        await Task.Delay(500);
    }

    private async void ShowLoginThenMain()
    {
        var loginWindow = _host!.Services.GetRequiredService<LoginWindow>();
        var loggedIn = loginWindow.ShowDialog();

        if (loggedIn == true)
        {
            var splash = new SplashWindow();
            splash.Show();
            await ShowLoadingBeatAsync(splash);
            splash.Close();
            ShowMainWindow();
        }
        else
        {
            Shutdown();
        }
    }

    private void ShowMainWindow()
    {
        var mainWindow = _host!.Services.GetRequiredService<MainWindow>();
        // Closing the main window normally exits the app (no tray/reopen support yet — Stage 4
        // territory). Suppressed during the SessionExpired transition below, where the window is
        // closed programmatically only to immediately reopen the login screen, not to exit.
        mainWindow.Closed += (_, _) =>
        {
            if (!_isTransitioningToLogin)
            {
                Shutdown();
            }
        };
        mainWindow.Show();
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _isTransitioningToLogin = true;
            foreach (var window in Windows.OfType<MainWindow>().ToList())
            {
                window.Close();
            }
            _isTransitioningToLogin = false;

            ShowLoginThenMain();
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
        base.OnExit(e);
    }
}
