using System;
using System.Globalization;
using System.Windows.Data;
using Drawing.Search.Common.Enums;

namespace Drawing.Search.Core.Utility;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.ToString() == parameter.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return null;

        return (bool)value ? parameter : Binding.DoNothing;
    }
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolean) return !boolean;

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
public class SearchTypeToAssemblyEnabledConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SearchType searchType)
        {
            return searchType == SearchType.Assembly;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
