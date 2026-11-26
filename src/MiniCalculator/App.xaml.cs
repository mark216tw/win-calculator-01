using System.IO;
using System.Runtime;
using System.Windows;
using System.Windows.Threading;
using Com.MiniCalculator.Services;
using Com.MiniCalculator.ViewModels;
using Com.MiniCalculator.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Localization = Com.MiniCalculator.Helpers.Localization;

namespace Com.MiniCalculator;

public sealed partial class App : Application, IDisposable
{
    private IHost? _host;
    private SingleInstanceService? _singleInstance;
    private Task? _hostStartTask;
    private bool _isExiting;
    private bool _disposed;

    public App()
    {
        EnableStartupProfile();
    }

    public static IServiceProvider? Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.TryAcquire())
        {
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _host = CreateHost(_singleInstance);
        Services = _host.Services;
        _singleInstance.AttachLogger(Services.GetRequiredService<ILogger<SingleInstanceService>>());

        IThemeService theme = Services.GetRequiredService<IThemeService>();
        theme.Apply();

        var main = Services.GetRequiredService<MainWindow>();
        MainWindow = main;

        main.Show();

        Dispatcher.BeginInvoke(
            () => CompletePostShowInitialization(main, theme),
            DispatcherPriority.ApplicationIdle);
    }

    private void CompletePostShowInitialization(MainWindow main, IThemeService theme)
    {
        if (_isExiting)
        {
            return;
        }

        _singleInstance?.StartListening(() => Dispatcher.Invoke(main.ShowAndActivate));
        theme.StartWatchingSystemTheme();

        if (_host is not null)
        {
            _hostStartTask = _host.StartAsync();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isExiting = true;
        Dispose();
        base.OnExit(e);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _singleInstance?.Dispose();
        _singleInstance = null;

        if (_host is not null)
        {
            if (_hostStartTask is not null)
            {
                try
                {
                    _hostStartTask.GetAwaiter().GetResult();
                    _host.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            _host.Dispose();
            _host = null;
        }
    }

    private static void EnableStartupProfile()
    {
        try
        {
            string profileDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MiniCalculator",
                "StartupProfile");
            Directory.CreateDirectory(profileDirectory);
            ProfileOptimization.SetProfileRoot(profileDirectory);
            ProfileOptimization.StartProfile("Startup.profile");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static IHost CreateHost(ISingleInstanceService singleInstance)
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = Array.Empty<string>(),
        });
        builder.Logging.ClearProviders();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MiniCalculator",
            "logs");
        builder.Logging.AddProvider(new FileLoggerProvider(logDirectory));

        builder.Services.AddSingleton<ICalculatorEngine, CalculatorEngine>();
        builder.Services.AddSingleton<IHistoryService, HistoryService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<ISingleInstanceService>(singleInstance);
        builder.Services.AddSingleton<CalculatorViewModel>();
        builder.Services.AddSingleton<ShellViewModel>();
        builder.Services.AddTransient<AboutViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<HistoryWindow>();
        builder.Services.AddTransient<SettingsWindow>();
        builder.Services.AddTransient<Func<HistoryWindow>>(provider => provider.GetRequiredService<HistoryWindow>);
        builder.Services.AddTransient<Func<SettingsWindow>>(provider => provider.GetRequiredService<SettingsWindow>);

        return builder.Build();
    }

    private static void LogException(Exception exception)
    {
        try
        {
            ILogger<App>? logger = Services?.GetService<ILogger<App>>();
            if (logger is not null)
            {
                LogMessages.UnhandledException(logger, exception);
            }
        }
        catch
        {
        }
    }

    private static void ShowUnhandledMessage()
    {
        MessageBox.Show(
            Localization.Get("Error_Unhandled"),
            Localization.Get("App_Title"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        ShowUnhandledMessage();
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogException(exception);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception);
        e.SetObserved();
    }
}
