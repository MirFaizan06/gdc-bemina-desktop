using System.Windows;
using System.Windows.Controls;

namespace CollegeAdmin.Desktop.Views.Shared;

public partial class LoadingView : UserControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(LoadingView), new PropertyMetadata("Loading..."));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public LoadingView() => InitializeComponent();
}
