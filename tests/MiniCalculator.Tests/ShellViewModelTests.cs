using System.Collections.ObjectModel;
using Com.MiniCalculator.Models;
using Com.MiniCalculator.Services;
using Com.MiniCalculator.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.MiniCalculator.Tests;

public class ShellViewModelTests
{
    private sealed class FakeSettingsService : ISettingsService
    {
        public AppSettings Current { get; } = new();

        public string SettingsPath => "test-settings.json";

        public void Save()
        {
        }
    }

    private sealed class FakeHistoryService : IHistoryService
    {
        public ObservableCollection<HistoryEntry> Entries { get; } = new();

        public void EnsureLoaded()
        {
        }

        public void Add(string expression, string result)
        {
            Entries.Insert(0, new HistoryEntry
            {
                Expression = expression,
                Result = result,
                CreatedAt = DateTimeOffset.Now,
            });
        }

        public void Clear() => Entries.Clear();
    }

    private static (ShellViewModel Shell, FakeSettingsService Settings) Create()
    {
        var settings = new FakeSettingsService();
        var calculator = new CalculatorViewModel(
            new CalculatorEngine(),
            new FakeHistoryService(),
            settings,
            NullLogger<CalculatorViewModel>.Instance);
        return (new ShellViewModel(calculator, settings), settings);
    }

    [Fact]
    public void ModeSelectionPersistsToSettings()
    {
        (ShellViewModel shell, FakeSettingsService settings) = Create();

        shell.IsScientificSelected = true;

        Assert.True(settings.Current.IsScientificMode);
        Assert.False(shell.IsStandardSelected);
    }

    [Fact]
    public void SettingsChangeUpdatesSelection()
    {
        (ShellViewModel shell, FakeSettingsService settings) = Create();

        settings.Current.IsScientificMode = true;

        Assert.True(shell.IsScientificSelected);
        Assert.False(shell.IsStandardSelected);
    }

    [Fact]
    public void ResetToDefaultsRestoresValues()
    {
        (ShellViewModel shell, FakeSettingsService settings) = Create();
        settings.Current.DisplayMode = DisplayMode.Dark;
        settings.Current.Theme = ThemeName.Teal;
        settings.Current.AlwaysOnTop = true;
        settings.Current.AngleUnit = AngleUnit.Rad;
        settings.Current.Memory = 5d;
        settings.Current.IsScientificMode = true;
        settings.Current.ScientificNotation = true;

        var viewModel = new SettingsViewModel(settings, new AboutViewModel());
        viewModel.ResetToDefaults();

        Assert.Equal(DisplayMode.System, settings.Current.DisplayMode);
        Assert.Equal(ThemeName.ClassicBlue, settings.Current.Theme);
        Assert.False(settings.Current.AlwaysOnTop);
        Assert.Equal(AngleUnit.Deg, settings.Current.AngleUnit);
        Assert.Equal(0d, settings.Current.Memory);
        Assert.False(settings.Current.IsScientificMode);
        Assert.False(settings.Current.ScientificNotation);
        Assert.True(shell.IsStandardSelected);
        Assert.False(shell.IsScientificSelected);
    }
}
