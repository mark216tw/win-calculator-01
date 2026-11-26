using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using Com.MiniCalculator.Helpers;
using Com.MiniCalculator.Services;
using Com.MiniCalculator.ViewModels;

namespace Com.MiniCalculator.Views;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;
    private readonly ISettingsService _settings;
    private readonly Func<HistoryWindow> _historyFactory;
    private readonly Func<SettingsWindow> _settingsFactory;
    private StandardView? _standardView;
    private ScientificView? _scientificView;

    public MainWindow(
        ShellViewModel shell,
        ISettingsService settings,
        Func<HistoryWindow> historyFactory,
        Func<SettingsWindow> settingsFactory)
    {
        _shell = shell;
        _settings = settings;
        _historyFactory = historyFactory;
        _settingsFactory = settingsFactory;

        DataContext = shell;
        InitializeComponent();
        RegisterShortcuts();
        WindowPlacementHelper.Restore(this, settings.Current.Window);
        _shell.PropertyChanged += OnShellPropertyChanged;
        ApplyMode(_shell.IsStandardSelected, focusInput: false);

        Closing += OnClosing;
        Loaded += OnLoaded;
    }

    private void RegisterShortcuts()
    {
        InputBindings.Add(new KeyBinding(new RelayCommand(OpenHistory), Key.H, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(OpenSettings), Key.OemComma, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(SelectStandardMode), Key.D1, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(SelectScientificMode), Key.D2, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(
            new RelayCommand(CopyFormattedResult),
            Key.C,
            ModifierKeys.Control | ModifierKeys.Shift));
    }

    private void OpenHistory() => ShowWindow(_historyFactory());

    private void OpenSettings() => ShowWindow(_settingsFactory());

    private void SelectStandardMode() => SelectMode(true);

    private void SelectScientificMode() => SelectMode(false);

    private void SelectMode(bool standard)
    {
        _shell.IsStandardSelected = standard;
        _shell.IsScientificSelected = !standard;
        FocusActiveInputDeferred();
    }

    private void CopyFormattedResult()
    {
        if (_shell.Calculator.CopyResultCommand.CanExecute(null))
        {
            _shell.Calculator.CopyResultCommand.Execute(null);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        Dispatcher.BeginInvoke(new Action(FocusActiveInput), DispatcherPriority.Input);
    }

    private void OnStandardChecked(object sender, RoutedEventArgs e) => FocusActiveInputDeferred();

    private void OnScientificChecked(object sender, RoutedEventArgs e) => FocusActiveInputDeferred();

    private void FocusActiveInputDeferred() =>
        Dispatcher.BeginInvoke(new Action(FocusActiveInput), DispatcherPriority.Input);

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ShellViewModel.IsStandardSelected)
            or nameof(ShellViewModel.IsScientificSelected)))
        {
            return;
        }

        if (_shell.IsStandardSelected)
        {
            ApplyMode(standard: true, focusInput: false);
        }
        else if (_shell.IsScientificSelected)
        {
            ApplyMode(standard: false, focusInput: false);
        }
    }

    private void ApplyMode(bool standard, bool focusInput)
    {
        if (standard)
        {
            _standardView ??= new StandardView { DataContext = _shell.Calculator };
            if (!ReferenceEquals(CalculatorHost.Content, _standardView))
            {
                CalculatorHost.Content = _standardView;
            }
        }
        else
        {
            _scientificView ??= new ScientificView { DataContext = _shell.Calculator };
            if (!ReferenceEquals(CalculatorHost.Content, _scientificView))
            {
                CalculatorHost.Content = _scientificView;
            }
        }

        if (focusInput)
        {
            FocusActiveInput();
        }
    }

    private void FocusActiveInput()
    {
        if (_shell.IsStandardSelected)
        {
            _standardView?.FocusInput();
        }
        else if (_shell.IsScientificSelected)
        {
            _scientificView?.FocusInput();
        }
    }

    public void ShowAndActivate()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(ShowAndActivate);
            return;
        }

        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        SavePlacement();
    }

    private void SavePlacement()
    {
        WindowPlacementHelper.Save(this, _settings.Current.Window);
        _settings.Save();
    }

    private void OnHistoryClick(object sender, RoutedEventArgs e) => ShowWindow(_historyFactory());

    private void OnSettingsClick(object sender, RoutedEventArgs e) => ShowWindow(_settingsFactory());

    private void ShowWindow(Window window)
    {
        window.Owner = this;
        window.ShowDialog();
    }
}
