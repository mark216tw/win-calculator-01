using System.ComponentModel;
using System.Windows;
using Com.MiniCalculator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Com.MiniCalculator.Services;

public sealed class ThemeService : IThemeService, IDisposable
{
    private const string PersonalizeKey =
        @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private readonly ISettingsService _settings;
    private readonly ILogger<ThemeService> _logger;
    private ResourceDictionary? _current;
    private bool _watchingSystemTheme;
    private bool _disposed;

    public ThemeService(ISettingsService settings, ILogger<ThemeService> logger)
    {
        _settings = settings;
        _logger = logger;
        _settings.Current.PropertyChanged += OnSettingsChanged;
    }

    public void StartWatchingSystemTheme()
    {
        if (_disposed || _watchingSystemTheme || _settings.Current.DisplayMode != DisplayMode.System)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _watchingSystemTheme = true;
    }

    public void Apply()
    {
        if (Application.Current is null)
        {
            return;
        }

        DisplayMode effectiveMode = ResolveEffectiveMode(_settings.Current.DisplayMode);
        ThemeName theme = _settings.Current.Theme;
        string relative = $"Styles/Themes/{effectiveMode}.{theme}.xaml";
        var uri = new Uri($"pack://application:,,,/MiniCalculator;component/{relative}", UriKind.Absolute);

        try
        {
            var dictionary = new ResourceDictionary { Source = uri };
            var merged = Application.Current.Resources.MergedDictionaries;
            if (_current is not null)
            {
                merged.Remove(_current);
            }

            merged.Add(dictionary);
            _current = dictionary;
        }
        catch (Exception ex)
        {
            LogMessages.ThemeApplyFailed(_logger, relative, ex);
        }
    }

    private static DisplayMode ResolveEffectiveMode(DisplayMode mode)
    {
        if (mode != DisplayMode.System)
        {
            return mode;
        }

        try
        {
            object? value = Registry.GetValue(PersonalizeKey, "AppsUseLightTheme", 1);
            if (value is int light)
            {
                return light == 0 ? DisplayMode.Dark : DisplayMode.Light;
            }
        }
        catch
        {
        }

        return DisplayMode.Light;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppSettings.DisplayMode) or nameof(AppSettings.Theme))
        {
            if (e.PropertyName == nameof(AppSettings.DisplayMode))
            {
                StartWatchingSystemTheme();
            }

            Apply();
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_settings.Current.DisplayMode != DisplayMode.System)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null)
        {
            dispatcher.BeginInvoke(Apply);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _settings.Current.PropertyChanged -= OnSettingsChanged;
        if (_watchingSystemTheme)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _watchingSystemTheme = false;
        }
    }
}
