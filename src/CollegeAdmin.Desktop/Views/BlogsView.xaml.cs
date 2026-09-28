using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class BlogsView : UserControl
{
    public BlogsView() => InitializeComponent();

    private void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: BlogPostDto post } || DataContext is not BlogsViewModel viewModel)
        {
            return;
        }

        var reasonViewModel = new RejectionReasonViewModel("Reject Blog Post", $"Reason for rejecting \"{post.Title}\" by {post.AuthorName}:");
        var dialog = new RejectionReasonWindow(reasonViewModel);
        if (dialog.ShowDialog() == true)
        {
            _ = viewModel.RejectAsync(post, reasonViewModel.Reason.Trim());
        }
    }
}
