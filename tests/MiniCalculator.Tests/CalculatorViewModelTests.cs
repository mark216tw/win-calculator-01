using System.Collections.ObjectModel;
using Com.MiniCalculator.Models;
using Com.MiniCalculator.Services;
using Com.MiniCalculator.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.MiniCalculator.Tests;

public class CalculatorViewModelTests
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

    private static (CalculatorViewModel ViewModel, FakeSettingsService Settings, FakeHistoryService History) Create()
    {
        var settings = new FakeSettingsService();
        var history = new FakeHistoryService();
        var viewModel = new CalculatorViewModel(
            new CalculatorEngine(),
            history,
            settings,
            NullLogger<CalculatorViewModel>.Instance);
        return (viewModel, settings, history);
    }

    [Fact]
    public void InsertsAndEvaluatesExpression()
    {
        (CalculatorViewModel viewModel, _, FakeHistoryService history) = Create();

        foreach (string token in new[] { "2", "+", "3", "×", "4" })
        {
            viewModel.InsertCommand.Execute(token);
        }

        Assert.Equal("2+3×4", viewModel.Expression);

        viewModel.EvaluateCommand.Execute(null);

        Assert.Equal("14", viewModel.ResultDisplay);
        Assert.False(viewModel.IsError);
        Assert.Single(history.Entries);
        Assert.Equal("2+3×4", history.Entries[0].Expression);
    }

    [Fact]
    public void DigitAfterEvaluateStartsNewExpression()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "2+2";
        viewModel.EvaluateCommand.Execute(null);

        viewModel.InsertCommand.Execute("7");

        Assert.Equal("7", viewModel.Expression);
    }

    [Fact]
    public void OperatorAfterEvaluateContinuesWithResult()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "2+2";
        viewModel.EvaluateCommand.Execute(null);

        viewModel.InsertCommand.Execute("+");

        Assert.Equal("4+", viewModel.Expression);
    }

    [Fact]
    public void ToggleSignAddsAndRemovesLeadingMinus()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "5";

        viewModel.ToggleSignCommand.Execute(null);
        Assert.Equal("-5", viewModel.Expression);

        viewModel.ToggleSignCommand.Execute(null);
        Assert.Equal("5", viewModel.Expression);
    }

    [Fact]
    public void MemoryStoresAndRecallsValue()
    {
        (CalculatorViewModel viewModel, FakeSettingsService settings, _) = Create();
        viewModel.Expression = "10";

        viewModel.MemoryOperationCommand.Execute("MS");
        Assert.Equal(10, settings.Current.Memory);
        Assert.True(viewModel.HasMemory);

        viewModel.ClearAllCommand.Execute(null);
        viewModel.MemoryOperationCommand.Execute("MR");

        Assert.Equal("10", viewModel.Expression);
    }

    [Fact]
    public void ScientificNotationReformatsPreview()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "1234";

        viewModel.IsScientificNotation = true;

        Assert.Equal("1.234E+3", viewModel.ResultDisplay);
    }

    [Fact]
    public void SquareWrapsExpression()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "9";

        viewModel.SquareCommand.Execute(null);
        Assert.Equal("(9)^2", viewModel.Expression);

        viewModel.EvaluateCommand.Execute(null);
        Assert.Equal("81", viewModel.ResultDisplay);
    }

    [Fact]
    public void CycleAnglePersistsToSettings()
    {
        (CalculatorViewModel viewModel, FakeSettingsService settings, _) = Create();

        viewModel.CycleAngleCommand.Execute(null);

        Assert.Equal(AngleUnit.Rad, viewModel.AngleUnit);
        Assert.Equal(AngleUnit.Rad, settings.Current.AngleUnit);
        Assert.Equal("RAD", viewModel.AngleUnitLabel);
    }

    [Fact]
    public void LoadHistoryRestoresExpression()
    {
        (CalculatorViewModel viewModel, _, FakeHistoryService history) = Create();
        var entry = new HistoryEntry { Expression = "3×3", Result = "9" };
        history.Entries.Add(entry);

        viewModel.LoadHistoryCommand.Execute(entry);

        Assert.Equal("3×3", viewModel.Expression);
        Assert.Equal("9", viewModel.ResultDisplay);
    }

    [Fact]
    public void InvalidExpressionShowsLocalizedError()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "1/0";

        viewModel.EvaluateCommand.Execute(null);

        Assert.True(viewModel.IsError);
        Assert.Equal("無法除以零", viewModel.ResultDisplay);
    }

    [Fact]
    public void ClearHistoryEmptiesCollection()
    {
        (CalculatorViewModel viewModel, _, FakeHistoryService history) = Create();
        history.Entries.Add(new HistoryEntry { Expression = "1+1", Result = "2" });

        viewModel.ClearHistoryCommand.Execute(null);

        Assert.Empty(history.Entries);
    }

    [Fact]
    public void ManualEditAfterEvaluateKeepsEditedExpression()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "2+2";
        viewModel.EvaluateCommand.Execute(null);

        viewModel.CaretIndex = viewModel.Expression.Length;
        viewModel.BackspaceCommand.Execute(null);
        viewModel.InsertCommand.Execute("3");

        Assert.Equal("2+3", viewModel.Expression);
    }

    [Fact]
    public void ToggleSignAfterEvaluateNegatesResult()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "2+2";
        viewModel.EvaluateCommand.Execute(null);

        viewModel.ToggleSignCommand.Execute(null);

        Assert.Equal("-4", viewModel.ResultDisplay);
        Assert.Equal("-4", viewModel.Expression);
        Assert.False(viewModel.IsError);
    }

    [Fact]
    public void ModAfterEvaluateContinuesFromResult()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "10";
        viewModel.EvaluateCommand.Execute(null);

        viewModel.InsertCommand.Execute(" mod ");
        viewModel.InsertCommand.Execute("3");
        viewModel.EvaluateCommand.Execute(null);

        Assert.Equal("1", viewModel.ResultDisplay);
    }

    [Fact]
    public void MemoryRecallWhenEmptyDoesNotSetError()
    {
        (CalculatorViewModel viewModel, _, _) = Create();

        viewModel.MemoryOperationCommand.Execute("MR");

        Assert.False(viewModel.IsError);
        Assert.Equal("0", viewModel.ResultDisplay);
        Assert.Equal("記憶體為空", viewModel.StatusMessage);
    }

    [Fact]
    public void ClearEntryRemovesTrailingNumber()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "2+3×4";

        viewModel.ClearEntryCommand.Execute(null);

        Assert.Equal("2+3×", viewModel.Expression);
    }

    [Fact]
    public void ClearEntryAfterEvaluateClearsAll()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "2+2";
        viewModel.EvaluateCommand.Execute(null);

        viewModel.ClearEntryCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.Expression);
        Assert.Equal("0", viewModel.ResultDisplay);
    }

    [Fact]
    public void RepeatedEqualsRepeatsLastOperation()
    {
        (CalculatorViewModel viewModel, _, FakeHistoryService history) = Create();
        viewModel.Expression = "2+3";

        viewModel.EvaluateCommand.Execute(null);
        viewModel.EvaluateCommand.Execute(null);
        viewModel.EvaluateCommand.Execute(null);

        Assert.Equal("11", viewModel.ResultDisplay);
        Assert.Equal(3, history.Entries.Count);
    }

    [Fact]
    public void CopyResultIsDisabledOnError()
    {
        (CalculatorViewModel viewModel, _, _) = Create();
        viewModel.Expression = "1/0";
        viewModel.EvaluateCommand.Execute(null);

        Assert.False(viewModel.CopyResultCommand.CanExecute(null));
        Assert.False(viewModel.CopyRawResultCommand.CanExecute(null));
    }

    [Fact]
    public void ScientificNotationPersistsToSettings()
    {
        (CalculatorViewModel viewModel, FakeSettingsService settings, _) = Create();

        viewModel.IsScientificNotation = true;

        Assert.True(settings.Current.ScientificNotation);
    }

    [Fact]
    public void RepeatedEqualsDoesNotRepeatExponent()
    {
        (CalculatorViewModel viewModel, _, FakeHistoryService history) = Create();
        viewModel.Expression = "1E+5";

        viewModel.EvaluateCommand.Execute(null);
        viewModel.EvaluateCommand.Execute(null);

        Assert.Equal("100,000", viewModel.ResultDisplay);
        Assert.Single(history.Entries);
    }
}
