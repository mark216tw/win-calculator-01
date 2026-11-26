using Com.MiniCalculator.Models;
using Com.MiniCalculator.Services;

namespace Com.MiniCalculator.ViewModels;

public sealed class SettingsViewModel
{
    public SettingsViewModel(ISettingsService settings, AboutViewModel about)
    {
        Settings = settings.Current;
        About = about;
    }

    public AppSettings Settings { get; }

    public AboutViewModel About { get; }

    public void ResetToDefaults()
    {
        Settings.DisplayMode = DisplayMode.System;
        Settings.Theme = ThemeName.ClassicBlue;
        Settings.AlwaysOnTop = false;
        Settings.AngleUnit = AngleUnit.Deg;
        Settings.Memory = 0d;
        Settings.IsScientificMode = false;
        Settings.ScientificNotation = false;
    }
}
