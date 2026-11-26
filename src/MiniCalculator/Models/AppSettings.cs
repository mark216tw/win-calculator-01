using CommunityToolkit.Mvvm.ComponentModel;

namespace Com.MiniCalculator.Models;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    private DisplayMode _displayMode = DisplayMode.System;

    [ObservableProperty]
    private ThemeName _theme = ThemeName.ClassicBlue;

    [ObservableProperty]
    private bool _alwaysOnTop;

    [ObservableProperty]
    private AngleUnit _angleUnit = AngleUnit.Deg;

    [ObservableProperty]
    private double _memory;

    [ObservableProperty]
    private bool _isScientificMode;

    [ObservableProperty]
    private bool _scientificNotation;

    public WindowPlacement Window { get; set; } = new();

    public WindowPlacement CompactWindow { get; set; } = new();
}
