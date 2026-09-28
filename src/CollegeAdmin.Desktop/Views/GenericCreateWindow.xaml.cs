using System.IO;
using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Desktop.ViewModels;
using Microsoft.Win32;

namespace CollegeAdmin.Desktop.Views;

public partial class GenericCreateWindow : Window
{
    private readonly GenericCreateViewModel _viewModel;

    public GenericCreateWindow(GenericCreateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.Saved += (_, _) => DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    /// <summary>Final Convergence Phase P0-7: reads the picked file's bytes here (a View-layer
    /// filesystem concern, matching StudentImportWindow's own established split) and hands them to
    /// the ViewModel, which only knows how to upload bytes, never a local path.</summary>
    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: FormField field })
        {
            return;
        }

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

        await _viewModel.UploadFieldAsync(field, Path.GetFileName(dialog.FileName), bytes);
    }
}
