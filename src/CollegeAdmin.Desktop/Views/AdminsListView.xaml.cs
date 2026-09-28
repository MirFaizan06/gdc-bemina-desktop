using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class AdminsListView : UserControl
{
    public AdminsListView() => InitializeComponent();

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AdminSummary admin } || DataContext is not AdminsListViewModel viewModel)
        {
            return;
        }

        var editViewModel = viewModel.CreateEditViewModel(admin);
        var window = new AdminEditWindow(editViewModel);
        if (window.ShowDialog() == true)
        {
            _ = viewModel.LoadCommand.ExecuteAsync(null);
        }
    }

    /// <summary>Final Convergence Phase P1-4 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): Ban had no
    /// confirmation at all before this — a single misclick revoked every session for the wrong admin.
    /// Mirrors GenericListView.xaml.cs's own DeleteButton_Click pattern: confirmation lives here in the
    /// code-behind, not the ViewModel, so BanCommand stays a plain testable RelayCommand that always
    /// runs once invoked — no "did the user actually confirm" state to fake in a unit test.</summary>
    private void BanButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AdminSummary admin } || DataContext is not AdminsListViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Ban {admin.Name}? This will immediately revoke all of their active sessions and block them from logging in until unbanned.",
            "Confirm Ban",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            viewModel.BanCommand.Execute(admin);
        }
    }
}
