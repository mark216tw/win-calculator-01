using Com.MiniCalculator.Models;
using Com.MiniCalculator.Services;

namespace Com.MiniCalculator.Tests;

public class CalculatorEngineTests
{
    private readonly CalculatorEngine _engine = new();

    private double Evaluate(string expression, AngleUnit angleUnit = AngleUnit.Deg)
    {
        EvaluationResult result = _engine.Evaluate(expression, angleUnit);
        Assert.True(result.Success, $"Expected success but got {result.Error} for '{expression}'");
        return result.Value;
    }

    [Theory]
    [InlineData("1+2", 3)]
    [InlineData("2+3*4", 14)]
    [InlineData("(2+3)*4", 20)]
    [InlineData("10/4", 2.5)]
    [InlineData("2^3", 8)]
    [InlineData("2^3^2", 512)]
    [InlineData("-2^2", -4)]
    [InlineData("2^0.5", 1.4142135623730951)]
    [InlineData("10 mod 3", 1)]
    [InlineData("10%", 0.1)]
    [InlineData("200*10%", 20)]
    [InlineData("5!", 120)]
    [InlineData("3!^2", 36)]
    [InlineData("sqrt(9)", 3)]
    [InlineData("cbrt(27)", 3)]
    [InlineData("abs(-5)", 5)]
    [InlineData("floor(2.9)", 2)]
    [InlineData("ceil(2.1)", 3)]
    [InlineData("round(2.5)", 3)]
    [InlineData("1.5+2.25", 3.75)]
    public void EvaluatesBasicExpressions(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression), 10);
    }

    [Theory]
    [InlineData(AngleUnit.Deg, 30, 0.5)]
    [InlineData(AngleUnit.Rad, 0, 0)]
    public void EvaluatesTrigonometry(AngleUnit angleUnit, double input, double expected)
    {
        Assert.Equal(expected, Evaluate($"sin({input})", angleUnit), 10);
    }

    [Fact]
    public void InverseTrigonometryRespectsAngleUnit()
    {
        Assert.Equal(30, Evaluate("asin(0.5)", AngleUnit.Deg), 10);
        Assert.Equal(100, Evaluate("asin(1)", AngleUnit.Grad), 10);
    }

    [Fact]
    public void EvaluatesConstants()
    {
        Assert.Equal(Math.PI, Evaluate("π"), 10);
        Assert.Equal(Math.E, Evaluate("e"), 10);
        Assert.Equal(1, Evaluate("ln(e)"), 10);
        Assert.Equal(1, Evaluate("log(10)"), 10);
    }

    [Theory]
    [InlineData("2(3+4)", 14)]
    [InlineData("(2)(3)", 6)]
    [InlineData("3sin(30)", 1.5)]
    public void EvaluatesImplicitMultiplication(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression), 10);
    }

    [Fact]
    public void EvaluatesImplicitMultiplicationWithConstants()
    {
        Assert.Equal(2 * Math.PI, Evaluate("2π"), 10);
        Assert.Equal(2 * Math.E, Evaluate("2e"), 10);
    }

    [Fact]
    public void AutoBalancesMissingClosingParentheses()
    {
        Assert.Equal(3, Evaluate("(1+2"), 10);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyExpressionReturnsZero(string expression)
    {
        EvaluationResult result = _engine.Evaluate(expression, AngleUnit.Deg);
        Assert.True(result.Success);
        Assert.Equal(0, result.Value);
    }

    [Theory]
    [InlineData("1/0", CalculationError.DivideByZero)]
    [InlineData("5 mod 0", CalculationError.DivideByZero)]
    [InlineData("2+", CalculationError.Syntax)]
    [InlineData("1)", CalculationError.Syntax)]
    [InlineData("abc(1)", CalculationError.Syntax)]
    [InlineData("(-1)!", CalculationError.Domain)]
    [InlineData("1.5!", CalculationError.Domain)]
    public void ReportsErrors(string expression, CalculationError expected)
    {
        EvaluationResult result = _engine.Evaluate(expression, AngleUnit.Deg);
        Assert.False(result.Success);
        Assert.Equal(expected, result.Error);
    }

    [Fact]
    public void AcceptsUnicodeOperators()
    {
        Assert.Equal(14, Evaluate("2 ＋ 3 × 4".Replace("＋", "+", StringComparison.Ordinal)), 10);
        Assert.Equal(5, Evaluate("10 ÷ 2"), 10);
        Assert.Equal(1, Evaluate("3 − 2"), 10);
    }
}
