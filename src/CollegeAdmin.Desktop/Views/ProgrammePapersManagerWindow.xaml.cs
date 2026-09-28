using System.Windows;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class ProgrammePapersManagerWindow : Window
{
    public ProgrammePapersManagerWindow(ProgrammePapersManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
