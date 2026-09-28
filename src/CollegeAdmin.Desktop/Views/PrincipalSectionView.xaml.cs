using System.IO;
using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Desktop.ViewModels;
using Microsoft.Win32;

namespace CollegeAdmin.Desktop.Views;

public partial class PrincipalSectionView : UserControl
{
    public PrincipalSectionView() => InitializeComponent();

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PrincipalSectionViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog { Filter = "Images (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true)
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

        await viewModel.UploadPhotoAsync(Path.GetFileName(dialog.FileName), bytes);
    }
}
