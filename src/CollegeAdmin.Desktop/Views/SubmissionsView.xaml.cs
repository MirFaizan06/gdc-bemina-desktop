using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Views;

public partial class SubmissionsView : UserControl
{
    public SubmissionsView() => InitializeComponent();

    private void ReplyContactButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ContactMessageDto message } || DataContext is not SubmissionsViewModel viewModel)
        {
            return;
        }

        var reasonViewModel = new RejectionReasonViewModel("Reply to Contact Message", $"Reply to {message.Name} ({message.Email}):");
        var dialog = new RejectionReasonWindow(reasonViewModel);
        if (dialog.ShowDialog() == true)
        {
            _ = viewModel.ReplyContactAsync(message, reasonViewModel.Reason.Trim());
        }
    }

    private async void ForwardContactButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ContactMessageDto message } || DataContext is not SubmissionsViewModel viewModel)
        {
            return;
        }

        var committees = await LoadCommitteesAsync(viewModel);
        if (committees is null)
        {
            return;
        }

        var forwardViewModel = new ForwardToCommitteeViewModel(committees);
        var dialog = new ForwardToCommitteeWindow(forwardViewModel);
        if (dialog.ShowDialog() == true && forwardViewModel.SelectedCommittee is not null)
        {
            _ = viewModel.ForwardContactAsync(message, forwardViewModel.SelectedCommittee.Id, string.IsNullOrWhiteSpace(forwardViewModel.Note) ? null : forwardViewModel.Note.Trim());
        }
    }

    private void ReplyGrievanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: GrievanceDto grievance } || DataContext is not SubmissionsViewModel viewModel)
        {
            return;
        }

        var reasonViewModel = new RejectionReasonViewModel("Reply to Grievance", $"Reply to {grievance.Name} ({grievance.Email}):");
        var dialog = new RejectionReasonWindow(reasonViewModel);
        if (dialog.ShowDialog() == true)
        {
            _ = viewModel.ReplyGrievanceAsync(grievance, reasonViewModel.Reason.Trim());
        }
    }

    private async void ForwardGrievanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: GrievanceDto grievance } || DataContext is not SubmissionsViewModel viewModel)
        {
            return;
        }

        var committees = await LoadCommitteesAsync(viewModel);
        if (committees is null)
        {
            return;
        }

        var forwardViewModel = new ForwardToCommitteeViewModel(committees);
        var dialog = new ForwardToCommitteeWindow(forwardViewModel);
        if (dialog.ShowDialog() == true && forwardViewModel.SelectedCommittee is not null)
        {
            _ = viewModel.ForwardGrievanceAsync(grievance, forwardViewModel.SelectedCommittee.Id, string.IsNullOrWhiteSpace(forwardViewModel.Note) ? null : forwardViewModel.Note.Trim());
        }
    }

    private static async Task<IReadOnlyList<CommitteeDto>?> LoadCommitteesAsync(SubmissionsViewModel viewModel)
    {
        try
        {
            return await viewModel.GetCommitteeOptionsAsync();
        }
        catch (ApiRequestException ex)
        {
            MessageBox.Show($"Could not load committees: {ex.Message}", "Forward", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }
}
