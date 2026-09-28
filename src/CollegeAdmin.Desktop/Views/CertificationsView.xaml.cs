using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class CertificationsView : UserControl
{
    public CertificationsView() => InitializeComponent();

    /// <summary>Final Convergence Phase P1-10/§2b: confirmation + reason prompt live here in the
    /// code-behind, not the ViewModel — mirrors GenericListView.xaml.cs's own DeleteButton_Click
    /// pattern (confirmation stays out of the ViewModel so RejectCommand-equivalent logic stays
    /// plainly testable).</summary>
    private void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CertificateApplicationDto application } || DataContext is not CertificationsViewModel viewModel)
        {
            return;
        }

        var reasonViewModel = new RejectionReasonViewModel(
            "Reject Application",
            $"Reason for rejecting {application.StudentName}'s {application.TypeName} application:");
        var dialog = new RejectionReasonWindow(reasonViewModel);
        if (dialog.ShowDialog() == true)
        {
            _ = viewModel.RejectAsync(application, reasonViewModel.Reason.Trim());
        }
    }
}
