using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CollegeAdmin.Desktop.Converters;

/// <summary>Shared visibility converters for the reusable Loading/Empty/Error state views (Stage 4).</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class NullOrEmptyToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Final Convergence Phase P1-14: Visible only when the bound value is non-null —
/// generic, unlike NullOrEmptyToCollapsedConverter which specifically casts to string (and so
/// always collapses a non-null nullable value type like int?, since `value as string` on a boxed
/// int is always null regardless of the int's own value). First needed for the Dashboard's
/// nullable stat fields (null = "not shown to this admin", not "zero").</summary>
public sealed class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Final Convergence Phase P1-10/§2b: Visible only when the bound value equals
/// ConverterParameter (case-insensitive string comparison) — first needed by Certifications'
/// Approve/Reject buttons (visible only while status is "pending"), reusable for Blogs/PYQ
/// moderation's own pending-status-gated actions.</summary>
public sealed class EqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value as string, parameter as string, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
