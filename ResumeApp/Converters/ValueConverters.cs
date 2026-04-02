using System.Globalization;
using ResumeApp.Views.Controls;

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

/// <summary>Returns true when SelectedTab matches ConverterParameter (shows active pill).</summary>
public class NavTabMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NavTab current && parameter is string param &&
            Enum.TryParse<NavTab>(param, out var target))
            return current == target;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns true when SelectedTab does NOT match ConverterParameter (shows inactive icon).</summary>
public class NavTabNotMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NavTab current && parameter is string param &&
            Enum.TryParse<NavTab>(param, out var target))
            return current != target;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
