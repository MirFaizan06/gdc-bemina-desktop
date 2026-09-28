using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CollegeAdmin.Desktop.Views.Shared;

public partial class ErrorStateView : UserControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(ErrorStateView), new PropertyMetadata("Something went wrong."));

    public static readonly DependencyProperty RetryCommandProperty = DependencyProperty.Register(
        nameof(RetryCommand), typeof(ICommand), typeof(ErrorStateView), new PropertyMetadata(null));

    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }

    public ErrorStateView() => InitializeComponent();
}
