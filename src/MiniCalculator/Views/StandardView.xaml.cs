using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Com.MiniCalculator.ViewModels;

namespace Com.MiniCalculator.Views;

public partial class StandardView : UserControl
{
    public StandardView()
    {
        InitializeComponent();
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnAnyButtonClick));
    }

    public void FocusInput() => FocusExpression();

    private void OnExpressionSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is CalculatorViewModel viewModel)
        {
            viewModel.CaretIndex = ExpressionBox.CaretIndex;
            viewModel.SelectionLength = ExpressionBox.SelectionLength;
        }
    }

    private void OnAnyButtonClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(FocusExpression), DispatcherPriority.Input);
    }

    private void FocusExpression()
    {
        if (DataContext is not CalculatorViewModel viewModel || !IsVisible)
        {
            return;
        }

        ExpressionBox.Focus();
        string text = ExpressionBox.Text ?? string.Empty;
        ExpressionBox.CaretIndex = Math.Clamp(viewModel.CaretIndex, 0, text.Length);
        ExpressionBox.SelectionLength = 0;
    }
}
