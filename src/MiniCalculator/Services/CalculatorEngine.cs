using System.Globalization;
using Com.MiniCalculator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Com.MiniCalculator.Services;

public sealed class CalculatorEngine : ICalculatorEngine
{
    private readonly ILogger<CalculatorEngine> _logger;

    public CalculatorEngine()
        : this(NullLogger<CalculatorEngine>.Instance)
    {
    }

    public CalculatorEngine(ILogger<CalculatorEngine> logger)
    {
        _logger = logger;
    }

    public EvaluationResult Evaluate(string expression, AngleUnit angleUnit)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return EvaluationResult.Ok(0d);
        }

        if (!TryBalanceParentheses(expression, out string balanced))
        {
            return EvaluationResult.Fail(CalculationError.Syntax);
        }

        try
        {
            var parser = new Parser(balanced, angleUnit);
            double value = parser.Parse();

            if (double.IsNaN(value))
            {
                return EvaluationResult.Fail(CalculationError.Domain);
            }

            if (double.IsInfinity(value))
            {
                return EvaluationResult.Fail(CalculationError.Overflow);
            }

            return EvaluationResult.Ok(value);
        }
        catch (CalculationException ex)
        {
            return EvaluationResult.Fail(ex.Error);
        }
        catch (OverflowException)
        {
            return EvaluationResult.Fail(CalculationError.Overflow);
        }
        catch (Exception ex)
        {
            LogMessages.EvaluationFailed(_logger, expression, ex);
            return EvaluationResult.Fail(CalculationError.Syntax);
        }
    }

    private static bool TryBalanceParentheses(string expression, out string balanced)
    {
        int depth = 0;
        foreach (char c in expression)
        {
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
                if (depth < 0)
                {
                    balanced = expression;
                    return false;
                }
            }
        }

        balanced = depth == 0 ? expression : expression + new string(')', depth);
        return true;
    }

    private sealed class CalculationException : Exception
    {
        public CalculationException(CalculationError error)
            : base(error.ToString())
        {
            Error = error;
        }

        public CalculationError Error { get; }
    }

    private sealed class Parser
    {
        private readonly string _text;
        private readonly AngleUnit _angleUnit;
        private int _position;

        public Parser(string text, AngleUnit angleUnit)
        {
            _text = text;
            _angleUnit = angleUnit;
        }

        public double Parse()
        {
            double value = ParseAdditive();
            SkipWhiteSpace();
            if (_position < _text.Length)
            {
                throw new CalculationException(CalculationError.Syntax);
            }

            return value;
        }

        private double ParseAdditive()
        {
            double value = ParseMultiplicative();
            while (true)
            {
                if (Match('+'))
                {
                    value += ParseMultiplicative();
                }
                else if (Match('-'))
                {
                    value -= ParseMultiplicative();
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseMultiplicative()
        {
            double value = ParseUnary();
            while (true)
            {
                if (Match('*'))
                {
                    value *= ParseUnary();
                }
                else if (Match('/'))
                {
                    double divisor = ParseUnary();
                    if (divisor == 0d)
                    {
                        throw new CalculationException(CalculationError.DivideByZero);
                    }

                    value /= divisor;
                }
                else if (MatchMod())
                {
                    double divisor = ParseUnary();
                    if (divisor == 0d)
                    {
                        throw new CalculationException(CalculationError.DivideByZero);
                    }

                    value %= divisor;
                }
                else if (StartsValue())
                {
                    value *= ParseUnary();
                }
                else
                {
                    return value;
                }
            }
        }

        private bool StartsValue()
        {
            SkipWhiteSpace();
            if (_position >= _text.Length)
            {
                return false;
            }

            char c = _text[_position];
            if (c == 'e' &&
                _position + 1 < _text.Length &&
                char.IsDigit(_text[_position + 1]))
            {
                return false;
            }

            return char.IsDigit(c) || c == '(' || c == 'π' || char.IsLetter(c);
        }

        private double ParseUnary()
        {
            if (Match('+'))
            {
                return ParseUnary();
            }

            if (Match('-'))
            {
                return -ParseUnary();
            }

            return ParsePower();
        }

        private double ParsePower()
        {
            double baseValue = ParsePostfix();
            if (Match('^'))
            {
                double exponent = ParseUnary();
                return Math.Pow(baseValue, exponent);
            }

            return baseValue;
        }

        private double ParsePostfix()
        {
            double value = ParsePrimary();
            while (true)
            {
                SkipWhiteSpace();
                if (_position >= _text.Length)
                {
                    return value;
                }

                char c = _text[_position];
                if (c == '%')
                {
                    _position++;
                    value /= 100d;
                }
                else if (c == '!')
                {
                    _position++;
                    value = Factorial(value);
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParsePrimary()
        {
            SkipWhiteSpace();
            if (_position >= _text.Length)
            {
                throw new CalculationException(CalculationError.Syntax);
            }

            char c = _text[_position];
            if (c == '(')
            {
                _position++;
                double value = ParseAdditive();
                if (!Match(')'))
                {
                    throw new CalculationException(CalculationError.Syntax);
                }

                return value;
            }

            if (char.IsDigit(c) || c == '.')
            {
                return ReadNumber();
            }

            if (char.IsLetter(c) || c == 'π')
            {
                return ReadIdentifier();
            }

            throw new CalculationException(CalculationError.Syntax);
        }

        private double ReadNumber()
        {
            int start = _position;
            bool seenDot = false;
            while (_position < _text.Length)
            {
                char c = _text[_position];
                if (char.IsDigit(c))
                {
                    _position++;
                    continue;
                }

                if (c == '.' && !seenDot)
                {
                    seenDot = true;
                    _position++;
                    continue;
                }

                break;
            }

            if (_position < _text.Length && _text[_position] == 'E')
            {
                int save = _position;
                _position++;
                if (_position < _text.Length && (_text[_position] == '+' || _text[_position] == '-'))
                {
                    _position++;
                }

                int digitStart = _position;
                while (_position < _text.Length && char.IsDigit(_text[_position]))
                {
                    _position++;
                }

                if (_position == digitStart)
                {
                    _position = save;
                }
            }

            ReadOnlySpan<char> token = _text.AsSpan(start, _position - start);
            if (token.Length == 0 || token.SequenceEqual("."))
            {
                throw new CalculationException(CalculationError.Syntax);
            }

            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                throw new CalculationException(CalculationError.Syntax);
            }

            return value;
        }

        private double ReadIdentifier()
        {
            SkipWhiteSpace();
            if (_text[_position] == 'π')
            {
                _position++;
                return Math.PI;
            }

            int start = _position;
            while (_position < _text.Length &&
                   (char.IsLetter(_text[_position]) || _text[_position] == '_'))
            {
                _position++;
            }

            string name = _text[start.._position].ToLowerInvariant();
            SkipWhiteSpace();

            if (_position < _text.Length && _text[_position] == '(')
            {
                _position++;
                var arguments = new List<double>();
                SkipWhiteSpace();
                if (_position < _text.Length && _text[_position] == ')')
                {
                    _position++;
                }
                else
                {
                    arguments.Add(ParseAdditive());
                    while (Match(','))
                    {
                        arguments.Add(ParseAdditive());
                    }

                    if (!Match(')'))
                    {
                        throw new CalculationException(CalculationError.Syntax);
                    }
                }

                return ApplyFunction(name, arguments);
            }

            return name switch
            {
                "pi" => Math.PI,
                "e" => Math.E,
                "rand" => Random.Shared.NextDouble(),
                _ => throw new CalculationException(CalculationError.Syntax),
            };
        }

        private double ApplyFunction(string name, IReadOnlyList<double> arguments)
        {
            switch (name)
            {
                case "sin":
                    return Math.Sin(ToRadians(Argument(arguments)));
                case "cos":
                    return Math.Cos(ToRadians(Argument(arguments)));
                case "tan":
                    return Math.Tan(ToRadians(Argument(arguments)));
                case "asin":
                    return FromRadians(Math.Asin(Argument(arguments)));
                case "acos":
                    return FromRadians(Math.Acos(Argument(arguments)));
                case "atan":
                    return FromRadians(Math.Atan(Argument(arguments)));
                case "sinh":
                    return Math.Sinh(Argument(arguments));
                case "cosh":
                    return Math.Cosh(Argument(arguments));
                case "tanh":
                    return Math.Tanh(Argument(arguments));
                case "asinh":
                    return Math.Asinh(Argument(arguments));
                case "acosh":
                    return Math.Acosh(Argument(arguments));
                case "atanh":
                    return Math.Atanh(Argument(arguments));
                case "ln":
                    return Math.Log(Argument(arguments));
                case "log":
                    return Math.Log10(Argument(arguments));
                case "log2":
                    return Math.Log2(Argument(arguments));
                case "exp":
                    return Math.Exp(Argument(arguments));
                case "sqrt":
                    return Math.Sqrt(Argument(arguments));
                case "cbrt":
                    return Math.Cbrt(Argument(arguments));
                case "abs":
                    return Math.Abs(Argument(arguments));
                case "floor":
                    return Math.Floor(Argument(arguments));
                case "ceil":
                    return Math.Ceiling(Argument(arguments));
                case "round":
                    return Math.Round(Argument(arguments), MidpointRounding.AwayFromZero);
                case "dms":
                    return ToDms(Argument(arguments));
                case "deg":
                    return FromDms(Argument(arguments));
                case "rand":
                    return Random.Shared.NextDouble();
                default:
                    throw new CalculationException(CalculationError.Syntax);
            }
        }

        private static double Argument(IReadOnlyList<double> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new CalculationException(CalculationError.Syntax);
            }

            return arguments[0];
        }

        private double ToRadians(double value) => _angleUnit switch
        {
            AngleUnit.Deg => value * Math.PI / 180d,
            AngleUnit.Grad => value * Math.PI / 200d,
            _ => value,
        };

        private double FromRadians(double value) => _angleUnit switch
        {
            AngleUnit.Deg => value * 180d / Math.PI,
            AngleUnit.Grad => value * 200d / Math.PI,
            _ => value,
        };

        private static double Factorial(double value)
        {
            if (value < 0d || value != Math.Floor(value))
            {
                throw new CalculationException(CalculationError.Domain);
            }

            if (value > 170d)
            {
                throw new CalculationException(CalculationError.Overflow);
            }

            double result = 1d;
            for (int i = 2; i <= (int)value; i++)
            {
                result *= i;
            }

            return result;
        }

        private static double ToDms(double value)
        {
            double sign = Math.Sign(value);
            value = Math.Abs(value);
            double degrees = Math.Floor(value);
            double totalMinutes = (value - degrees) * 60d;
            double minutes = Math.Floor(totalMinutes);
            double seconds = (totalMinutes - minutes) * 60d;
            return sign * (degrees + (minutes / 100d) + (seconds / 10000d));
        }

        private static double FromDms(double value)
        {
            double sign = Math.Sign(value);
            value = Math.Abs(value);
            double degrees = Math.Floor(value);
            double rest = (value - degrees) * 100d;
            return sign * (degrees + (rest / 60d) + ((rest - Math.Floor(rest)) * 0.006d));
        }

        private bool MatchMod()
        {
            SkipWhiteSpace();
            if (_position + 3 > _text.Length)
            {
                return false;
            }

            if (string.Compare(_text, _position, "mod", 0, 3, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            int after = _position + 3;
            if (after < _text.Length && (char.IsLetterOrDigit(_text[after]) || _text[after] == '_'))
            {
                return false;
            }

            _position = after;
            return true;
        }

        private bool Match(char expected)
        {
            SkipWhiteSpace();
            if (_position < _text.Length && Normalize(_text[_position]) == expected)
            {
                _position++;
                return true;
            }

            return false;
        }

        private static char Normalize(char c) => c switch
        {
            '×' => '*',
            '÷' => '/',
            '−' => '-',
            '–' => '-',
            _ => c,
        };

        private void SkipWhiteSpace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }
    }
}
