using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Com.MiniCalculator.Helpers;
using Com.MiniCalculator.Models;
using Com.MiniCalculator.Services;
using Microsoft.Extensions.Logging;
using Localization = Com.MiniCalculator.Helpers.Localization;

namespace Com.MiniCalculator.ViewModels;

public partial class CalculatorViewModel : ObservableObject
{
    private readonly ICalculatorEngine _engine;
    private readonly IHistoryService _history;
    private readonly ISettingsService _settings;
    private readonly ILogger<CalculatorViewModel> _logger;

    private bool _suppressPreview;
    private bool _justEvaluated;
    private bool _hasRepeatOperation;
    private string _repeatOperator = string.Empty;
    private double _repeatOperand;
    private double _lastValue;
    private DispatcherTimer? _previewTimer;
    private DispatcherTimer? _statusTimer;

    public CalculatorViewModel(
        ICalculatorEngine engine,
        IHistoryService history,
        ISettingsService settings,
        ILogger<CalculatorViewModel> logger)
    {
        _engine = engine;
        _history = history;
        _settings = settings;
        _logger = logger;

        _angleUnit = settings.Current.AngleUnit;
        _isScientificNotation = settings.Current.ScientificNotation;
        History = history.Entries;
        settings.Current.PropertyChanged += OnSettingsPropertyChanged;
    }

    public ObservableCollection<HistoryEntry> History { get; }

    public int CaretIndex { get; set; }

    public int SelectionLength { get; set; }

    public double Memory => _settings.Current.Memory;

    public bool HasMemory => _settings.Current.Memory != 0d;

    public string AngleUnitLabel => AngleUnit switch
    {
        AngleUnit.Rad => "RAD",
        AngleUnit.Grad => "GRAD",
        _ => "DEG",
    };

    public string SinLabel => BuildTrigLabel("sin");

    public string CosLabel => BuildTrigLabel("cos");

    public string TanLabel => BuildTrigLabel("tan");

    [ObservableProperty]
    private string _expression = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyResultCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyRawResultCommand))]
    private string _resultDisplay = "0";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyResultCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyRawResultCommand))]
    private bool _isError;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private AngleUnit _angleUnit;

    [ObservableProperty]
    private bool _isSecondFunction;

    [ObservableProperty]
    private bool _isHyperbolic;

    [ObservableProperty]
    private bool _isScientificNotation;

    private static bool IsValueStartToken(string token)
    {
        string trimmed = token.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed.Equals("mod", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trimmed.Length == 1)
        {
            char c = trimmed[0];
            return char.IsDigit(c) || c == '.' || c == '(' || c == 'π' || c == 'e';
        }

        return true;
    }

    private string BuildTrigLabel(string baseName)
    {
        if (IsHyperbolic)
        {
            return IsSecondFunction ? baseName + "h⁻¹" : baseName + "h";
        }

        return IsSecondFunction ? baseName + "⁻¹" : baseName;
    }

    private void RaiseTrigLabels()
    {
        OnPropertyChanged(nameof(SinLabel));
        OnPropertyChanged(nameof(CosLabel));
        OnPropertyChanged(nameof(TanLabel));
    }

    partial void OnExpressionChanged(string value)
    {
        _justEvaluated = false;
        _hasRepeatOperation = false;

        if (_suppressPreview)
        {
            return;
        }

        SchedulePreview();
    }

    partial void OnAngleUnitChanged(AngleUnit value)
    {
        if (_settings.Current.AngleUnit != value)
        {
            _settings.Current.AngleUnit = value;
        }

        OnPropertyChanged(nameof(AngleUnitLabel));
        SchedulePreview();
    }

    partial void OnIsSecondFunctionChanged(bool value) => RaiseTrigLabels();

    partial void OnIsHyperbolicChanged(bool value) => RaiseTrigLabels();

    partial void OnIsScientificNotationChanged(bool value)
    {
        if (_settings.Current.ScientificNotation != value)
        {
            _settings.Current.ScientificNotation = value;
        }

        SchedulePreview();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.AngleUnit))
        {
            AngleUnit = _settings.Current.AngleUnit;
        }
        else if (e.PropertyName == nameof(AppSettings.Memory))
        {
            OnPropertyChanged(nameof(Memory));
            OnPropertyChanged(nameof(HasMemory));
        }
        else if (e.PropertyName == nameof(AppSettings.ScientificNotation))
        {
            IsScientificNotation = _settings.Current.ScientificNotation;
        }
    }

    private void SchedulePreview()
    {
        if (Application.Current is null)
        {
            RefreshPreview();
            return;
        }

        if (_previewTimer is null)
        {
            _previewTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            _previewTimer.Tick += OnPreviewTimerTick;
        }

        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private void OnPreviewTimerTick(object? sender, EventArgs e)
    {
        _previewTimer?.Stop();
        RefreshPreview();
    }

    private void CancelPendingPreview() => _previewTimer?.Stop();

    private void ShowStatus(string message)
    {
        StatusMessage = message;
        if (Application.Current is null)
        {
            return;
        }

        if (_statusTimer is null)
        {
            _statusTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(2),
            };
            _statusTimer.Tick += OnStatusTimerTick;
        }

        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private void OnStatusTimerTick(object? sender, EventArgs e)
    {
        _statusTimer?.Stop();
        StatusMessage = null;
    }

    private void ClearStatus()
    {
        _statusTimer?.Stop();
        StatusMessage = null;
    }

    private void RefreshPreview()
    {
        if (string.IsNullOrWhiteSpace(Expression))
        {
            _lastValue = 0d;
            IsError = false;
            ResultDisplay = "0";
            return;
        }

        EvaluationResult result = _engine.Evaluate(Expression, AngleUnit);
        if (result.Success)
        {
            _lastValue = result.Value;
            IsError = false;
            ResultDisplay = NumberFormatter.Format(result.Value, IsScientificNotation);
        }
        else
        {
            ResultDisplay = string.Empty;
            IsError = false;
        }
    }

    private static string GetErrorMessage(CalculationError error) => error switch
    {
        CalculationError.DivideByZero => Localization.Get("Calc_Error_DivideByZero"),
        CalculationError.Domain => Localization.Get("Calc_Error_Domain"),
        CalculationError.Overflow => Localization.Get("Calc_Error_Overflow"),
        _ => Localization.Get("Calc_Error_Syntax"),
    };

    [RelayCommand]
    private void Insert(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        ClearStatus();

        if (_justEvaluated)
        {
            if (IsValueStartToken(token))
            {
                _suppressPreview = true;
                Expression = string.Empty;
                _suppressPreview = false;
            }
            else
            {
                _suppressPreview = true;
                Expression = NumberFormatter.ToExpression(_lastValue);
                _suppressPreview = false;
            }

            _justEvaluated = false;
            CaretIndex = Expression.Length;
        }

        int index = Math.Clamp(CaretIndex, 0, Expression.Length);
        Expression = Expression.Insert(index, token);
        CaretIndex = index + token.Length;
        SelectionLength = 0;
    }

    [RelayCommand]
    private void InsertTrig(string? function)
    {
        if (string.IsNullOrEmpty(function))
        {
            return;
        }

        string name = IsHyperbolic ? function + "h" : function;
        if (IsSecondFunction)
        {
            name = "a" + name;
        }

        Insert(name + "(");
    }

    [RelayCommand]
    private void Evaluate()
    {
        CancelPendingPreview();
        ClearStatus();

        if (_justEvaluated)
        {
            if (_hasRepeatOperation)
            {
                EvaluateRepeat();
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(Expression))
        {
            ResultDisplay = "0";
            IsError = false;
            return;
        }

        EvaluationResult result = _engine.Evaluate(Expression, AngleUnit);
        if (result.Success)
        {
            _lastValue = result.Value;
            string formatted = NumberFormatter.Format(result.Value, IsScientificNotation);
            ResultDisplay = formatted;
            IsError = false;
            _justEvaluated = true;
            _hasRepeatOperation = TryGetRepeatOperation(Expression, out _repeatOperator, out _repeatOperand);
            _history.Add(Expression, formatted);
        }
        else
        {
            IsError = true;
            ResultDisplay = GetErrorMessage(result.Error);
            _justEvaluated = false;
            _hasRepeatOperation = false;
        }
    }

    private void EvaluateRepeat()
    {
        double left = _lastValue;
        string expression = $"({NumberFormatter.ToExpression(left)}) {_repeatOperator} {NumberFormatter.ToExpression(_repeatOperand)}";
        EvaluationResult result = _engine.Evaluate(expression, AngleUnit);
        if (!result.Success)
        {
            IsError = true;
            ResultDisplay = GetErrorMessage(result.Error);
            _justEvaluated = false;
            _hasRepeatOperation = false;
            return;
        }

        _lastValue = result.Value;
        string formatted = NumberFormatter.Format(result.Value, IsScientificNotation);
        ResultDisplay = formatted;
        IsError = false;
        _justEvaluated = true;
        string historyExpression =
            $"{NumberFormatter.Format(left, IsScientificNotation)} {DisplayOperator(_repeatOperator)} {NumberFormatter.Format(_repeatOperand, IsScientificNotation)}";
        _history.Add(historyExpression, formatted);
    }

    [RelayCommand]
    private void Backspace()
    {
        ClearStatus();

        if (SelectionLength > 0)
        {
            int start = Math.Clamp(CaretIndex, 0, Expression.Length);
            int length = Math.Clamp(SelectionLength, 0, Expression.Length - start);
            Expression = Expression.Remove(start, length);
            CaretIndex = start;
            SelectionLength = 0;
            return;
        }

        if (CaretIndex <= 0 || Expression.Length == 0)
        {
            return;
        }

        int position = Math.Min(CaretIndex, Expression.Length);
        Expression = Expression.Remove(position - 1, 1);
        CaretIndex = position - 1;
        SelectionLength = 0;
    }

    [RelayCommand]
    private void ClearAll()
    {
        CancelPendingPreview();
        ClearStatus();
        _justEvaluated = false;
        _hasRepeatOperation = false;
        _suppressPreview = true;
        Expression = string.Empty;
        _suppressPreview = false;
        CaretIndex = 0;
        SelectionLength = 0;
        _lastValue = 0d;
        IsError = false;
        ResultDisplay = "0";
    }

    [RelayCommand]
    private void ClearEntry()
    {
        CancelPendingPreview();
        ClearStatus();

        if (_justEvaluated || string.IsNullOrEmpty(Expression))
        {
            ClearAll();
            return;
        }

        string expression = Expression;
        int end = expression.Length;
        while (end > 0 && char.IsWhiteSpace(expression[end - 1]))
        {
            end--;
        }

        int start = end;
        while (start > 0 && char.IsDigit(expression[start - 1]))
        {
            start--;
        }

        if (start > 0 && expression[start - 1] == '.')
        {
            start--;
            while (start > 0 && char.IsDigit(expression[start - 1]))
            {
                start--;
            }
        }

        if (start == end)
        {
            ClearAll();
            return;
        }

        _justEvaluated = false;
        _hasRepeatOperation = false;
        _suppressPreview = true;
        Expression = expression.Remove(start, end - start);
        _suppressPreview = false;
        CaretIndex = start;
        SelectionLength = 0;
        IsError = false;
        RefreshPreview();
    }

    [RelayCommand]
    private void ToggleSign()
    {
        ClearStatus();

        if (_justEvaluated)
        {
            _suppressPreview = true;
            Expression = NumberFormatter.ToExpression(-_lastValue);
            _suppressPreview = false;
            _lastValue = -_lastValue;
            CaretIndex = Expression.Length;
            SelectionLength = 0;
            IsError = false;
            ResultDisplay = NumberFormatter.Format(_lastValue, IsScientificNotation);
            _justEvaluated = true;
            return;
        }

        if (string.IsNullOrEmpty(Expression))
        {
            Insert("-");
            return;
        }

        if (Expression.StartsWith('-'))
        {
            Expression = Expression[1..];
            CaretIndex = Math.Max(0, CaretIndex - 1);
        }
        else
        {
            Expression = "-" + Expression;
            CaretIndex = Math.Min(Expression.Length, CaretIndex + 1);
        }

        SelectionLength = 0;
    }

    [RelayCommand]
    private void Reciprocal() => WrapExpression("1/(", ")");

    [RelayCommand]
    private void Square() => WrapExpression("(", ")^2");

    [RelayCommand]
    private void Cube() => WrapExpression("(", ")^3");

    [RelayCommand]
    private void Sqrt() => WrapExpression("sqrt(", ")");

    private void WrapExpression(string prefix, string suffix)
    {
        CancelPendingPreview();
        ClearStatus();
        string inner = _justEvaluated || string.IsNullOrWhiteSpace(Expression)
            ? NumberFormatter.ToExpression(_lastValue)
            : Expression;

        _justEvaluated = false;
        _hasRepeatOperation = false;
        _suppressPreview = true;
        Expression = prefix + inner + suffix;
        _suppressPreview = false;
        CaretIndex = Expression.Length;
        SelectionLength = 0;
        RefreshPreview();
    }

    [RelayCommand]
    private void CycleAngle()
    {
        AngleUnit = AngleUnit switch
        {
            AngleUnit.Deg => AngleUnit.Rad,
            AngleUnit.Rad => AngleUnit.Grad,
            _ => AngleUnit.Deg,
        };
    }

    private double CurrentValue()
    {
        if (!string.IsNullOrWhiteSpace(Expression))
        {
            EvaluationResult result = _engine.Evaluate(Expression, AngleUnit);
            if (result.Success)
            {
                return result.Value;
            }
        }

        return _lastValue;
    }

    private void SetMemory(double value)
    {
        _settings.Current.Memory = value;
        OnPropertyChanged(nameof(Memory));
        OnPropertyChanged(nameof(HasMemory));
    }

    [RelayCommand]
    private void MemoryOperation(string? operation)
    {
        CancelPendingPreview();
        ClearStatus();

        switch (operation)
        {
            case "MC":
                SetMemory(0d);
                break;
            case "MR":
                if (_settings.Current.Memory == 0d)
                {
                    ShowStatus(Localization.Get("Calc_MemoryEmpty"));
                }
                else
                {
                    Insert(NumberFormatter.ToExpression(_settings.Current.Memory));
                }

                break;
            case "MS":
                SetMemory(CurrentValue());
                break;
            case "M+":
                SetMemory(Memory + CurrentValue());
                break;
            case "M-":
                SetMemory(Memory - CurrentValue());
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopyResult))]
    private void CopyResult() => CopyToClipboard(ResultDisplay);

    [RelayCommand(CanExecute = nameof(CanCopyResult))]
    private void CopyRawResult()
    {
        string raw = ResultDisplay.Replace(",", string.Empty, StringComparison.Ordinal);
        CopyToClipboard(raw);
    }

    private bool CanCopyResult() => !IsError && !string.IsNullOrWhiteSpace(ResultDisplay);

    private void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            Clipboard.SetText(text);
            ShowStatus(Localization.Get("Calc_Copied"));
        }
        catch (Exception ex)
        {
            LogMessages.ClipboardCopyFailed(_logger, ex);
        }
    }

    private bool TryGetRepeatOperation(string expression, out string operation, out double operand)
    {
        operation = string.Empty;
        operand = 0d;

        string text = expression.TrimEnd();
        if (text.Length == 0)
        {
            return false;
        }

        int operatorIndex = -1;
        char operatorChar = '\0';
        int depth = 0;
        for (int i = text.Length - 1; i >= 0; i--)
        {
            char c = text[i];
            if (c == ')')
            {
                depth++;
                continue;
            }

            if (c == '(')
            {
                depth--;
                continue;
            }

            if (depth != 0)
            {
                continue;
            }

            if (c is '+' or '-' or '*' or '/' or '×' or '÷' or '−' or '–' or '^')
            {
                if ((c is '+' or '-' or '−' or '–') && IsUnaryPosition(text, i))
                {
                    continue;
                }

                operatorIndex = i;
                operatorChar = c;
                break;
            }
        }

        int modIndex = FindLastTopLevelMod(text);
        if (modIndex > operatorIndex)
        {
            operatorIndex = modIndex;
            operatorChar = 'm';
        }

        if (operatorIndex < 0)
        {
            return false;
        }

        int rightStart = operatorChar == 'm' ? operatorIndex + 3 : operatorIndex + 1;
        string right = text[rightStart..].Trim();
        if (right.Length == 0)
        {
            return false;
        }

        EvaluationResult result = _engine.Evaluate(right, AngleUnit);
        if (!result.Success)
        {
            return false;
        }

        operation = operatorChar switch
        {
            'm' => "mod",
            '×' or '*' => "*",
            '÷' or '/' => "/",
            '−' or '–' or '-' => "-",
            _ => operatorChar.ToString(),
        };
        operand = result.Value;
        return true;
    }

    private static bool IsUnaryPosition(string text, int index)
    {
        int previous = index - 1;
        while (previous >= 0 && char.IsWhiteSpace(text[previous]))
        {
            previous--;
        }

        if (previous < 0)
        {
            return true;
        }

        char c = text[previous];
        if (c == 'E')
        {
            return true;
        }

        return c is '+' or '-' or '*' or '/' or '×' or '÷' or '−' or '–' or '^' or '(' or ',';
    }

    private static int FindLastTopLevelMod(string text)
    {
        int depth = 0;
        for (int i = text.Length - 3; i >= 0; i--)
        {
            char c = text[i];
            if (c == ')')
            {
                depth++;
                continue;
            }

            if (c == '(')
            {
                depth--;
                continue;
            }

            if (depth == 0 && IsModAt(text, i))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsModAt(string text, int index)
    {
        if (index + 3 > text.Length)
        {
            return false;
        }

        if (string.Compare(text, index, "mod", 0, 3, StringComparison.OrdinalIgnoreCase) != 0)
        {
            return false;
        }

        bool leftBoundary = index == 0 || !(char.IsLetter(text[index - 1]) || text[index - 1] == '_');
        int after = index + 3;
        bool rightBoundary = after >= text.Length || !(char.IsLetterOrDigit(text[after]) || text[after] == '_');
        return leftBoundary && rightBoundary;
    }

    private static string DisplayOperator(string operation) => operation switch
    {
        "*" => "×",
        "/" => "÷",
        "-" => "−",
        _ => operation,
    };

    [RelayCommand]
    private void LoadHistory(HistoryEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        CancelPendingPreview();
        ClearStatus();
        _justEvaluated = false;
        _hasRepeatOperation = false;
        _suppressPreview = true;
        Expression = entry.Expression;
        _suppressPreview = false;
        CaretIndex = Expression.Length;
        SelectionLength = 0;
        RefreshPreview();
    }

    [RelayCommand]
    private void ClearHistory() => _history.Clear();
}
