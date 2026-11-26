using System.Globalization;
using System.Text;

namespace Com.MiniCalculator.Helpers;

public static class NumberFormatter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Format(double value, bool scientific)
    {
        if (double.IsNaN(value))
        {
            return "NaN";
        }

        if (double.IsPositiveInfinity(value))
        {
            return "∞";
        }

        if (double.IsNegativeInfinity(value))
        {
            return "-∞";
        }

        if (value == 0d)
        {
            return "0";
        }

        double abs = Math.Abs(value);
        bool useScientific = scientific || abs >= 1e16 || abs < 1e-4;
        if (useScientific)
        {
            return value.ToString("0.###############E+0", Invariant);
        }

        string general = value.ToString("G16", Invariant);
        if (general.Contains('E', StringComparison.OrdinalIgnoreCase))
        {
            return value.ToString("0.###############E+0", Invariant);
        }

        bool negative = general.StartsWith('-');
        if (negative)
        {
            general = general[1..];
        }

        string[] parts = general.Split('.');
        string integerPart = Group(parts[0]);
        string result = parts.Length > 1 ? integerPart + "." + parts[1] : integerPart;
        return negative ? "-" + result : result;
    }

    public static string ToExpression(double value)
    {
        if (value == 0d)
        {
            return "0";
        }

        return Format(value, false).Replace(",", string.Empty, StringComparison.Ordinal);
    }

    private static string Group(string digits)
    {
        if (digits.Length <= 3)
        {
            return digits;
        }

        var builder = new StringBuilder(digits.Length + (digits.Length / 3));
        int first = digits.Length % 3;
        if (first == 0)
        {
            first = 3;
        }

        builder.Append(digits, 0, first);
        for (int i = first; i < digits.Length; i += 3)
        {
            builder.Append(',');
            builder.Append(digits, i, 3);
        }

        return builder.ToString();
    }
}
