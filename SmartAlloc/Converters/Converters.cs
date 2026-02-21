using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SmartAlloc.Models;
using SmartAlloc.Services;

namespace SmartAlloc.Converters;

public class PinDotsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var pin = value as string ?? "";
        var dots = new List<bool>();
        for (int i = 0; i < pin.Length; i++)
            dots.Add(true);
        return dots;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value
            ? new SolidColorBrush(Color.FromRgb(0x6C, 0x63, 0xFF))
            : new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x4A));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try { return (Color)System.Windows.Media.ColorConverter.ConvertFromString(value?.ToString() ?? "#6C63FF"); }
        catch { return Color.FromRgb(0x6C, 0x63, 0xFF); }
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TxTypeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is TransactionType t && t == TransactionType.Income
            ? Color.FromRgb(0x27, 0xAE, 0x60)
            : Color.FromRgb(0xE7, 0x4C, 0x3C);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hasValue = !string.IsNullOrEmpty(value as string);
        if (parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase))
            hasValue = !hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class MoneyDisplayConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] == DependencyProperty.UnsetValue) return "–";

        bool isPrivate = values.Length > 1 && values[1] is bool b && b;
        if (isPrivate) return "• • •";

        decimal amount = values[0] switch
        {
            decimal d  => d,
            double  db => (decimal)db,
            float   f  => (decimal)f,
            _          => 0m
        };

        return CurrencyDisplayService.Current?.Format(amount) ?? $"{amount:N2} PLN";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ReminderDaysLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            0 => "No reminder",
            1 => "1 day before",
            2 => "2 days before",
            3 => "3 days before",
            7 => "1 week before",
            _ => $"{value} days before"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CategoryNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string name)
            return LocalizationService.Current.TranslateCategory(name);
        return value ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
