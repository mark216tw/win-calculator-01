using System.Globalization;
using System.Windows.Data;

namespace Com.MiniCalculator.Helpers;

public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null && value.Equals(parameter);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? parameter! : Binding.DoNothing;
    }
}
