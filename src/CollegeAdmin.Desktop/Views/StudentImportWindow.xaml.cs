using System.Windows;
using CollegeAdmin.Desktop.ViewModels;
using Microsoft.Win32;

namespace CollegeAdmin.Desktop.Views;

public partial class StudentImportWindow : Window
{
    public StudentImportWindow(StudentImportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void ChooseFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx" };
        if (dialog.ShowDialog() == true)
        {
            await ((StudentImportViewModel) DataContext).UploadCommand.ExecuteAsync(dialog.FileName);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
