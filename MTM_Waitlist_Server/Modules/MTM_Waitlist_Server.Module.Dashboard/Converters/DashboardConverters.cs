using Microsoft.UI.Xaml.Data;
using System;

namespace MTM_Waitlist_Server.Module.Dashboard.Converters;

/// <summary>Converts a <see cref="bool"/> to its logical inverse.</summary>
public sealed class BoolNegationConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is bool b && !b;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is bool b && !b;
}
