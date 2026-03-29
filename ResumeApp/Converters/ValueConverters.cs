using System.Globalization;

namespace ResumeApp.Converters;

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            bool boolValue => !boolValue,
            null => true,
            _ => false
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolValue ? !boolValue : false;
}

public class InverseIntToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int intValue ? intValue == 0 : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
