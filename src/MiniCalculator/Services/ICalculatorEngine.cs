using Com.MiniCalculator.Models;

namespace Com.MiniCalculator.Services;

public interface ICalculatorEngine
{
    EvaluationResult Evaluate(string expression, AngleUnit angleUnit);
}
