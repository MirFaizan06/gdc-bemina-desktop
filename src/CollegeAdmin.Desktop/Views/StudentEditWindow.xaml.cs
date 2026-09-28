using System.Windows;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class StudentEditWindow : Window
{
    public StudentEditWindow(StudentEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Saved += (_, _) => DialogResult = true;
        viewModel.Archived += (_, _) => DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ArchiveButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Archive this student? This is reversible by an admin, but hides them from the active list.",
            "Archive Student", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            ((StudentEditViewModel)DataContext).ArchiveCommand.Execute(null);
        }
    }
}
