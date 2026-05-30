using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using MTM_Waitlist_Server.Core.Models.Splash;
using System;
using Windows.UI;

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

/// <summary>
/// Converts a <see cref="bool"/> to the inverse <see cref="Visibility"/>.
/// <c>false</c> → <see cref="Visibility.Visible"/>, <c>true</c> → <see cref="Visibility.Collapsed"/>.
/// </summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Collapsed;
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

/// <summary>
/// Converts a <see cref="StartupStepState"/> to a Segoe MDL2 Assets glyph string
/// used to display the step's current state in the splash screen step list.
/// </summary>
public sealed class StepStateToGlyphConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is StartupStepState state
            ? state switch
            {
                StartupStepState.Succeeded => "\uE73E",  // Accept checkmark
                StartupStepState.Failed => "\uE711",  // Error / cancel
                StartupStepState.InProgress => "\uE9F5",  // Sync / spinner glyph
                StartupStepState.Skipped => "\uE89B",  // Forward skip
                _ => "\uE7BA",  // Bullet / pending
            }
            : "\uE7BA";

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>
/// Converts a <see cref="StartupStepState"/> to a <see cref="SolidColorBrush"/>
/// used to colour the state glyph in the splash screen step list.
/// </summary>
public sealed class StepStateToColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is StartupStepState state)
        {
            var color = state switch
            {
                StartupStepState.Succeeded => Windows.UI.Color.FromArgb(0xFF, 0x10, 0xB9, 0x81),
                StartupStepState.Failed => Windows.UI.Color.FromArgb(0xFF, 0xEF, 0x44, 0x44),
                StartupStepState.InProgress => Windows.UI.Color.FromArgb(0xFF, 0x60, 0xA5, 0xFA),
                StartupStepState.Skipped => Windows.UI.Color.FromArgb(0xFF, 0x9C, 0xA3, 0xAF),
                _ => Windows.UI.Color.FromArgb(0xFF, 0x6B, 0x72, 0x80),
            };
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
        }
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(0xFF, 0x6B, 0x72, 0x80));
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
