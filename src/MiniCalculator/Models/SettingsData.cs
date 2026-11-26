namespace Com.MiniCalculator.Models;

internal sealed class SettingsData
{
    public DisplayMode DisplayMode { get; set; } = DisplayMode.System;

    public ThemeName Theme { get; set; } = ThemeName.ClassicBlue;

    public bool AlwaysOnTop { get; set; }

    public AngleUnit AngleUnit { get; set; } = AngleUnit.Deg;

    public double Memory { get; set; }

    public bool IsScientificMode { get; set; }

    public bool ScientificNotation { get; set; }

    public WindowPlacement Window { get; set; } = new();

    public WindowPlacement CompactWindow { get; set; } = new();
}
