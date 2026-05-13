// Visibility and IValueConverter are resolved via GlobalUsings.cs

namespace MTM_Waitlist_Application.WinUI.Converters;

/// <summary>
/// Converts a <see cref="bool"/> to a <see cref="Visibility"/> value.
/// <c>true</c> → <see cref="Visibility.Visible"/>, <c>false</c> → <see cref="Visibility.Collapsed"/>.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility.Visible;
}

/// <summary>
/// Converts a <see cref="string"/> to <see cref="Visibility"/>.
/// Non-empty string → <see cref="Visibility.Visible"/>, null or empty → <see cref="Visibility.Collapsed"/>.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
