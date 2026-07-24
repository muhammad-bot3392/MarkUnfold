using Avalonia.Media;
using Avalonia.Data.Converters;
using MarkItDownGUI.Models;
using System;
using System.Globalization;

namespace MarkItDownGUI.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FileStatus status)
        {
            return status switch
            {
                FileStatus.Queued => new SolidColorBrush(Color.Parse("#6B7280")),
                FileStatus.Converting => new SolidColorBrush(Color.Parse("#3B82F6")),
                FileStatus.Done => new SolidColorBrush(Color.Parse("#10B981")),
                FileStatus.Failed => new SolidColorBrush(Color.Parse("#EF4444")),
                _ => new SolidColorBrush(Color.Parse("#6B7280")),
            };
        }
        return new SolidColorBrush(Color.Parse("#6B7280"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FileStatus status)
        {
            return status switch
            {
                FileStatus.Queued => "\uE78F",
                FileStatus.Converting => "\uE8C8",
                FileStatus.Done => "\uE73E",
                FileStatus.Failed => "\uE711",
                _ => "\uE78F",
            };
        }
        return "\uE78F";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string s)
        {
            var parts = s.Split('|');
            return b ? parts[0] : (parts.Length > 1 ? parts[1] : "");
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
