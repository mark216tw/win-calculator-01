namespace Com.MiniCalculator.Models;

public readonly record struct EvaluationResult(bool Success, double Value, CalculationError Error)
{
    public static EvaluationResult Ok(double value) => new(true, value, CalculationError.None);

    public static EvaluationResult Fail(CalculationError error) => new(false, 0d, error);
}
