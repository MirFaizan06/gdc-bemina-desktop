using System.IO;
using System.Windows;
using CollegeAdmin.Desktop.ViewModels;
using Microsoft.Win32;

namespace CollegeAdmin.Desktop.Views;

public partial class NoticeCreateWindow : Window
{
    private readonly NoticeCreateViewModel _viewModel;

    public NoticeCreateWindow(NoticeCreateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.Saved += (_, _) => DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    /// <summary>Final Convergence Phase P0-7: reads the picked file's bytes here (a View-layer
    /// filesystem concern) and hands them to the ViewModel, which only uploads bytes.</summary>
    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        byte[] bytes;
        try
        {
            bytes = await File.ReadAllBytesAsync(dialog.FileName);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Could not read the selected file: {ex.Message}", "Upload", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await _viewModel.UploadFileAsync(Path.GetFileName(dialog.FileName), bytes);
    }
}
