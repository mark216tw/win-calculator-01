using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Com.MiniCalculator.Models;
using Com.MiniCalculator.Services;

namespace Com.MiniCalculator.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    public ShellViewModel(CalculatorViewModel calculator, ISettingsService settings)
    {
        Calculator = calculator;
        Settings = settings.Current;

        IsStandardSelected = !Settings.IsScientificMode;
        IsScientificSelected = Settings.IsScientificMode;

        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    public CalculatorViewModel Calculator { get; }

    public AppSettings Settings { get; }

    [ObservableProperty]
    private bool _isStandardSelected = true;

    [ObservableProperty]
    private bool _isScientificSelected;

    partial void OnIsStandardSelectedChanged(bool value)
    {
        if (value)
        {
            IsScientificSelected = false;
            Settings.IsScientificMode = false;
        }
    }

    partial void OnIsScientificSelectedChanged(bool value)
    {
        if (value)
        {
            IsStandardSelected = false;
            Settings.IsScientificMode = true;
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppSettings.IsScientificMode))
        {
            return;
        }

        bool scientific = Settings.IsScientificMode;
        if (IsScientificSelected != scientific)
        {
            IsScientificSelected = scientific;
        }

        if (IsStandardSelected != !scientific)
        {
            IsStandardSelected = !scientific;
        }
    }
}
