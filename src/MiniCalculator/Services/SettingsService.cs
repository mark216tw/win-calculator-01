using System.IO;
using Com.MiniCalculator.Models;
using Microsoft.Extensions.Logging;

namespace Com.MiniCalculator.Services;

public sealed class SettingsService : ISettingsService, IDisposable
{
    private readonly JsonFileStore<SettingsData> _store;

    public SettingsService(ILogger<SettingsService> logger)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MiniCalculator");
        SettingsPath = Path.Combine(folder, "settings.json");

        _store = new JsonFileStore<SettingsData>(SettingsPath, AppJsonContext.Default.SettingsData, logger);
        Current = ToSettings(_store.Load());
        Current.PropertyChanged += OnSettingsChanged;
    }

    public AppSettings Current { get; }

    public string SettingsPath { get; }

    public void Save() => _store.RequestSave(ToSettingsData(Current));

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Save();

    private static AppSettings ToSettings(SettingsData? data)
    {
        if (data is null)
        {
            return new AppSettings();
        }

        return new AppSettings
        {
            DisplayMode = data.DisplayMode,
            Theme = data.Theme,
            AlwaysOnTop = data.AlwaysOnTop,
            AngleUnit = data.AngleUnit,
            Memory = data.Memory,
            IsScientificMode = data.IsScientificMode,
            ScientificNotation = data.ScientificNotation,
            Window = Copy(data.Window),
            CompactWindow = Copy(data.CompactWindow),
        };
    }

    private static SettingsData ToSettingsData(AppSettings settings) => new()
    {
        DisplayMode = settings.DisplayMode,
        Theme = settings.Theme,
        AlwaysOnTop = settings.AlwaysOnTop,
        AngleUnit = settings.AngleUnit,
        Memory = settings.Memory,
        IsScientificMode = settings.IsScientificMode,
        ScientificNotation = settings.ScientificNotation,
        Window = Copy(settings.Window),
        CompactWindow = Copy(settings.CompactWindow),
    };

    private static WindowPlacement Copy(WindowPlacement? source)
    {
        if (source is null)
        {
            return new WindowPlacement();
        }

        return new WindowPlacement
        {
            HasValue = source.HasValue,
            Left = source.Left,
            Top = source.Top,
            Width = source.Width,
            Height = source.Height,
            Maximized = source.Maximized,
        };
    }

    public void Dispose()
    {
        Current.PropertyChanged -= OnSettingsChanged;
        _store.Dispose();
    }
}
