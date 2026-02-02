using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace InsightMediaGenerator.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var key = boolValue ? "Success600Brush" : "Error600Brush";
            if (Application.Current.TryFindResource(key) is SolidColorBrush brush)
                return brush;
        }
        if (Application.Current.TryFindResource("TextTertiaryBrush") is SolidColorBrush fallback)
            return fallback;
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a plan name (FREE, TRIAL, STD, PRO, ENT) to its tier-specific color brush.
/// Uses PlanXxxBrush resources defined in Styles.xaml per Insight-Common standard.
/// </summary>
public class PlanToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var plan = value as string ?? "FREE";
        var resourceKey = plan switch
        {
            "TRIAL" => "PlanTrialBrush",
            "STD" => "PlanStdBrush",
            "PRO" => "PlanProBrush",
            "ENT" => "PlanEntBrush",
            _ => "PlanFreeBrush"
        };

        if (Application.Current.TryFindResource(resourceKey) is SolidColorBrush brush)
            return brush;
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a plan name to a semi-transparent background brush for badges.
/// </summary>
public class PlanToBgConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var plan = value as string ?? "FREE";
        var resourceKey = plan switch
        {
            "TRIAL" => "PlanTrialBrush",
            "STD" => "PlanStdBrush",
            "PRO" => "PlanProBrush",
            "ENT" => "PlanEntBrush",
            _ => "PlanFreeBrush"
        };

        if (Application.Current.TryFindResource(resourceKey) is SolidColorBrush brush)
        {
            var color = brush.Color;
            color.A = 20; // ~8% opacity for subtle background
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(Color.FromArgb(20, 168, 162, 158));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a plan name to a semi-transparent border brush for badges.
/// </summary>
public class PlanToBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var plan = value as string ?? "FREE";
        var resourceKey = plan switch
        {
            "TRIAL" => "PlanTrialBrush",
            "STD" => "PlanStdBrush",
            "PRO" => "PlanProBrush",
            "ENT" => "PlanEntBrush",
            _ => "PlanFreeBrush"
        };

        if (Application.Current.TryFindResource(resourceKey) is SolidColorBrush brush)
        {
            var color = brush.Color;
            color.A = 64; // ~25% opacity for border
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(Color.FromArgb(64, 168, 162, 158));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
