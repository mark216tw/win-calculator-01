using Com.MiniCalculator.Helpers;

namespace Com.MiniCalculator.Tests;

public class NumberFormatterTests
{
    [Theory]
    [InlineData(0, false, "0")]
    [InlineData(0.1 + 0.2, false, "0.3")]
    [InlineData(1234567, false, "1,234,567")]
    [InlineData(-1234.5, false, "-1,234.5")]
    [InlineData(100000000, false, "100,000,000")]
    [InlineData(1234, true, "1.234E+3")]
    [InlineData(0.00001, false, "1E-5")]
    [InlineData(1e16, false, "1E+16")]
    public void FormatsValues(double value, bool scientific, string expected)
    {
        Assert.Equal(expected, NumberFormatter.Format(value, scientific));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(-5, "-5")]
    [InlineData(2.5, "2.5")]
    [InlineData(0.1 + 0.2, "0.3")]
    [InlineData(1234567, "1234567")]
    [InlineData(0.00001, "1E-5")]
    public void ToExpressionProducesParsableText(double value, string expected)
    {
        Assert.Equal(expected, NumberFormatter.ToExpression(value));
    }
}
