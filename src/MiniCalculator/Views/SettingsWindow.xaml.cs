using System.Windows;
using Com.MiniCalculator.ViewModels;
using Localization = Com.MiniCalculator.Helpers.Localization;

namespace Com.MiniCalculator.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show(
            this,
            Localization.Get("Confirm_ResetSettings"),
            Localization.Get("Settings_Title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _viewModel.ResetToDefaults();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
