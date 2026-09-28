using System.Windows;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Closing the window via DialogResult (rather than Close() directly) is what makes
        // ShowDialog() in App.xaml.cs return true, so it can distinguish "logged in
        // successfully" from "the user closed the window".
        viewModel.LoginSucceeded += (_, _) => DialogResult = true;
    }

    // PasswordBox.Password is deliberately not data-bindable in WPF (it's not a DependencyProperty,
    // to avoid plaintext passwords sitting in the visual tree's binding infrastructure) — this
    // handler is the standard, minimal way to push it into the ViewModel instead.
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
}
