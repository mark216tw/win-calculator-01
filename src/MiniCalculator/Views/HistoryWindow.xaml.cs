using System.Windows;
using System.Windows.Input;
using Com.MiniCalculator.Models;
using Com.MiniCalculator.Services;
using Com.MiniCalculator.ViewModels;
using Localization = Com.MiniCalculator.Helpers.Localization;

namespace Com.MiniCalculator.Views;

public partial class HistoryWindow : Window
{
    private readonly CalculatorViewModel _viewModel;

    public HistoryWindow(CalculatorViewModel viewModel, IHistoryService history)
    {
        _viewModel = viewModel;
        history.EnsureLoaded();
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.History.Count == 0)
        {
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            this,
            Localization.Get("Confirm_ClearHistory"),
            Localization.Get("History_Title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _viewModel.ClearHistoryCommand.Execute(null);
        }
    }

    private void OnHistoryDoubleClick(object sender, MouseButtonEventArgs e) => LoadSelectedEntry();

    private void OnHistoryPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && HistoryList.IsKeyboardFocusWithin)
        {
            LoadSelectedEntry();
            e.Handled = true;
        }
    }

    private void LoadSelectedEntry()
    {
        if (HistoryList.SelectedItem is not HistoryEntry entry)
        {
            return;
        }

        _viewModel.LoadHistoryCommand.Execute(entry);
        HistoryList.SelectedIndex = -1;
        Close();
    }
}
