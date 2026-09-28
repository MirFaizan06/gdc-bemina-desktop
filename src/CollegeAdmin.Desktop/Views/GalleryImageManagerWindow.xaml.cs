using System.IO;
using System.Windows;
using CollegeAdmin.Desktop.ViewModels;
using Microsoft.Win32;

namespace CollegeAdmin.Desktop.Views;

public partial class GalleryImageManagerWindow : Window
{
    private readonly GalleryImageManagerViewModel _viewModel;

    public GalleryImageManagerWindow(GalleryImageManagerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>Final Convergence Phase P0-7: replaces the raw free-text "type a path and hope it's
    /// correct" image field the original audit flagged — reads the picked file's bytes here (View-
    /// layer filesystem concern) and hands them to the ViewModel, which only uploads bytes.</summary>
    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Images (*.jpg;*.jpeg;*.png;*.webp;*.gif)|*.jpg;*.jpeg;*.png;*.webp;*.gif|All files (*.*)|*.*" };
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

        await _viewModel.UploadNewImageAsync(Path.GetFileName(dialog.FileName), bytes);
    }
}
