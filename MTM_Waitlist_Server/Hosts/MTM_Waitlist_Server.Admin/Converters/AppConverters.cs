using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace MTM_Waitlist_Server.Admin.Converters;

/// <summary>Converts a <see cref="bool"/> to its logical inverse for IsEnabled bindings.</summary>
public sealed class BoolNegationConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is bool b && !b;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is bool b && !b;
}

/// <summary>
/// Converts a <see cref="bool"/> to <see cref="Visibility"/>.
/// <c>true</c> → <see cref="Visibility.Visible"/>, <c>false</c> → <see cref="Visibility.Collapsed"/>.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Visible;
}
/// Returns <see cref="Visibility.Visible"/> for non-empty strings,
/// <see cref="Visibility.Collapsed"/> for null or empty.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language) =>
        string.IsNullOrEmpty(value as string)
            ? Visibility.Collapsed
            : Visibility.Visible;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>
/// Converts a <see cref="bool"/> to an opacity value.
/// <c>true</c> (user exists — section disabled) → 0.4, <c>false</c> → 1.0.
/// </summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? 0.4 : 1.0;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
