using System.Windows;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LoggedOut += (_, _) => Close();
    }
}
