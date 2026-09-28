using System.IO;
using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using Microsoft.Win32;

namespace CollegeAdmin.Desktop.Views;

public partial class PyqsView : UserControl
{
    public PyqsView() => InitializeComponent();

    private void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: PyqDto paper } || DataContext is not PyqsViewModel viewModel)
        {
            return;
        }

        var reasonViewModel = new RejectionReasonViewModel("Reject Paper", $"Reason for rejecting \"{paper.Title}\":");
        var dialog = new RejectionReasonWindow(reasonViewModel);
        if (dialog.ShowDialog() == true)
        {
            _ = viewModel.RejectAsync(paper, reasonViewModel.Reason.Trim());
        }
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PyqsViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog { Filter = "Documents (*.pdf;*.doc;*.docx;*.jpg;*.jpeg;*.png;*.webp)|*.pdf;*.doc;*.docx;*.jpg;*.jpeg;*.png;*.webp|All files (*.*)|*.*" };
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

        await viewModel.UploadPaperFileAsync(Path.GetFileName(dialog.FileName), bytes);
    }
}
