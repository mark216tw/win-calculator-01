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
    private const double NormalMinWidth = 440d;
    private const double NormalMinHeight = 620d;
    private const double CompactMinWidth = 190d;
    private const double CompactMinHeight = 250d;
    private const double CompactDefaultWidth = 230d;
    private const double CompactDefaultHeight = 300d;
    private const double LegacyCompactDefaultWidth = 300d;
    private const double LegacyCompactDefaultHeight = 420d;
    private const double LegacyCompactMinWidth = 260d;
    private const double LegacyCompactMinHeight = 380d;

    private readonly ShellViewModel _shell;
    private readonly ISettingsService _settings;
    private readonly Func<HistoryWindow> _historyFactory;
    private readonly Func<SettingsWindow> _settingsFactory;
    private StandardView? _standardView;
    private ScientificView? _scientificView;
    private CompactView? _compactView;
    private bool _isCompact;
    private bool _alwaysOnTopBeforeCompact;

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
        ApplyNormalLayout(focusInput: false);
        _shell.PropertyChanged += OnShellPropertyChanged;

        Closing += OnClosing;
        Loaded += OnLoaded;
    }

    private void RegisterShortcuts()
    {
        InputBindings.Add(new KeyBinding(new RelayCommand(OpenHistory), Key.H, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(OpenSettings), Key.OemComma, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(SelectStandardMode), Key.D1, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(SelectScientificMode), Key.D2, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(ToggleCompactMode), Key.M, ModifierKeys.Control));
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
        if (_isCompact)
        {
            return;
        }

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
        RegisterShortcuts();
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

        if (_isCompact)
        {
            if (_shell.IsScientificSelected)
            {
                _shell.IsStandardSelected = true;
            }

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
        if (_isCompact)
        {
            return;
        }

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

    private void ApplyNormalLayout(bool focusInput)
    {
        MinWidth = NormalMinWidth;
        MinHeight = NormalMinHeight;
        NormalToolbar.Visibility = Visibility.Visible;
        CompactToolbar.Visibility = Visibility.Collapsed;

        WindowPlacementHelper.Restore(this, _settings.Current.Window);
        ApplyMode(_shell.IsStandardSelected, focusInput: false);

        if (focusInput)
        {
            FocusActiveInputDeferred();
        }
    }

    private void ApplyCompactLayout()
    {
        MinWidth = CompactMinWidth;
        MinHeight = CompactMinHeight;
        NormalToolbar.Visibility = Visibility.Collapsed;
        CompactToolbar.Visibility = Visibility.Visible;

        _compactView ??= new CompactView { DataContext = _shell.Calculator };
        CalculatorHost.Content = _compactView;

        if (_settings.Current.CompactWindow.HasValue)
        {
            WindowPlacementHelper.Restore(this, _settings.Current.CompactWindow, allowMaximized: false);
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }

            if (UsesLegacyCompactSize())
            {
                WindowPlacementHelper.ApplyCompactDefaults(this, CompactDefaultWidth, CompactDefaultHeight);
            }
        }
        else
        {
            WindowPlacementHelper.ApplyCompactDefaults(this, CompactDefaultWidth, CompactDefaultHeight);
        }

        FocusActiveInputDeferred();
    }

    private bool UsesLegacyCompactSize() =>
        MatchesSize(LegacyCompactDefaultWidth, LegacyCompactDefaultHeight)
        || MatchesSize(LegacyCompactMinWidth, LegacyCompactMinHeight);

    private bool MatchesSize(double width, double height) =>
        Math.Abs(Width - width) < 2d && Math.Abs(Height - height) < 2d;

    private void FocusActiveInput()
    {
        if (_isCompact)
        {
            _compactView?.FocusInput();
        }
        else if (_shell.IsStandardSelected)
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

    private void ToggleCompactMode()
    {
        if (_isCompact)
        {
            ExitCompactMode();
            return;
        }

        if (!_shell.IsStandardSelected)
        {
            return;
        }

        EnterCompactMode();
    }

    private void OnCompactClick(object sender, RoutedEventArgs e)
    {
        if (_shell.IsStandardSelected)
        {
            EnterCompactMode();
        }
    }

    private void OnExpandClick(object sender, RoutedEventArgs e) => ExitCompactMode();

    private void EnterCompactMode()
    {
        if (_isCompact || !_shell.IsStandardSelected)
        {
            return;
        }

        WindowPlacementHelper.Save(this, _settings.Current.Window);
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }

        _alwaysOnTopBeforeCompact = _settings.Current.AlwaysOnTop;
        _settings.Current.AlwaysOnTop = true;
        _isCompact = true;
        _settings.Save();

        ApplyCompactLayout();
    }

    private void ExitCompactMode()
    {
        if (!_isCompact)
        {
            return;
        }

        WindowPlacementHelper.Save(this, _settings.Current.CompactWindow);
        _settings.Current.AlwaysOnTop = _alwaysOnTopBeforeCompact;
        _shell.IsStandardSelected = true;
        _settings.Current.IsScientificMode = false;
        _isCompact = false;

        ApplyNormalLayout(focusInput: true);
        _settings.Save();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_isCompact)
        {
            WindowPlacementHelper.Save(this, _settings.Current.CompactWindow);
            _settings.Current.AlwaysOnTop = _alwaysOnTopBeforeCompact;
        }
        else
        {
            WindowPlacementHelper.Save(this, _settings.Current.Window);
        }

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
