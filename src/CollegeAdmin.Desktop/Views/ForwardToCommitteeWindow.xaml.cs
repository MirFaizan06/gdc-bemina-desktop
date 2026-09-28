using System.Windows;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class ForwardToCommitteeWindow : Window
{
    private readonly ForwardToCommitteeViewModel _viewModel;

    public ForwardToCommitteeWindow(ForwardToCommitteeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCommittee is null)
        {
            MessageBox.Show("Choose a committee.", "Forward", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
