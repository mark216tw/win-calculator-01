using System.IO;
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

public partial class App : Application
{
    private IHost? _host;
    private ISingleInstanceService? _singleInstance;

    public static IServiceProvider? Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _host = CreateHost();
        _host.Start();
        Services = _host.Services;

        _singleInstance = Services.GetRequiredService<ISingleInstanceService>();
        if (!_singleInstance.TryAcquire())
        {
            Shutdown();
            return;
        }

        IThemeService theme = Services.GetRequiredService<IThemeService>();
        theme.Apply();

        var main = Services.GetRequiredService<MainWindow>();
        MainWindow = main;
        _singleInstance.StartListening(() => Dispatcher.Invoke(main.ShowAndActivate));

        main.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();

        if (_host is not null)
        {
            try
            {
                _host.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            _host.Dispose();
            _host = null;
        }

        base.OnExit(e);
    }

    private static IHost CreateHost()
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
        builder.Services.AddSingleton<ISingleInstanceService, SingleInstanceService>();
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
