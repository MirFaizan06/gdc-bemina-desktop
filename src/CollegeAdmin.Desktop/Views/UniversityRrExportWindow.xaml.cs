using System.IO;
using System.Windows;
using CollegeAdmin.Desktop.ViewModels;
using Microsoft.Win32;

namespace CollegeAdmin.Desktop.Views;

public partial class UniversityRrExportWindow : Window
{
    public UniversityRrExportWindow(UniversityRrExportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.ExportReady += OnExportReady;
    }

    private async void OnExportReady(object? sender, (string Filename, byte[] Bytes) export)
    {
        var dialog = new SaveFileDialog { FileName = export.Filename, Filter = "Excel Workbook (*.xlsx)|*.xlsx" };
        if (dialog.ShowDialog() == true)
        {
            await File.WriteAllBytesAsync(dialog.FileName, export.Bytes);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
