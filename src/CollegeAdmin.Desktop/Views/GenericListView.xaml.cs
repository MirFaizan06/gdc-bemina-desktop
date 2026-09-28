using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Desktop.Export;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class GenericListView : UserControl
{
    public GenericListView() => InitializeComponent();

    /// <summary>SaveFileDialog/the write/the audit call all live in CsvExporter.ExportInteractivelyAsync
    /// (shared with TimetableView) — GenericListViewModel only ever sees the row count afterward,
    /// via RecordExportAsync, so it stays unit-testable without touching the filesystem. Exports
    /// FilteredItems (Final Convergence Phase P1-18: "current view", matching 08_UI_UX_SYSTEM.md's
    /// "export current view/selected") — every row that matches the active search, not just the
    /// one page currently visible in the grid.</summary>
    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GenericListViewModel viewModel)
        {
            return;
        }

        await CsvExporter.ExportInteractivelyAsync(viewModel.FilteredItems.ToList(), $"{viewModel.Title.Replace(' ', '-')}-export.csv", viewModel.RecordExportAsync);
    }

    /// <summary>Confirmation lives here, not in the ViewModel, so GenericListViewModel stays
    /// WPF-independent and unit-testable — DeleteCommand only ever runs once the user has already
    /// said yes. Wording branches on IsArchivable (Final Convergence Phase P2 item 1): an archived
    /// row isn't destroyed, so the destructive "cannot be undone" wording would be actively wrong.</summary>
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: { } row } || DataContext is not GenericListViewModel viewModel)
        {
            return;
        }

        var result = MessageBox.Show(
            viewModel.IsArchivable
                ? "Archive this item? It will be hidden from lists and the public site, and can be restored later."
                : "Delete this item? This cannot be undone.",
            viewModel.IsArchivable ? "Confirm Archive" : "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            viewModel.DeleteCommand.Execute(row);
        }
    }

    /// <summary>Final Convergence Phase P1-18: same confirmation-in-code-behind shape as the
    /// single-row delete above. DataGrid.SelectedItems is a plain CLR property (not a bindable
    /// DependencyProperty), so it's read directly here rather than bound — the same reason Export
    /// already reads viewModel state directly from code-behind instead of via a binding.</summary>
    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GenericListViewModel viewModel)
        {
            return;
        }

        var selected = Grid.SelectedItems.Cast<object>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Select one or more rows first.", "Delete Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var itemWord = selected.Count == 1 ? "item" : "items";
        var result = MessageBox.Show(
            viewModel.IsArchivable
                ? $"Archive {selected.Count} selected {itemWord}? They will be hidden from lists and the public site, and can be restored later."
                : $"Delete {selected.Count} selected {itemWord}? This cannot be undone.",
            viewModel.IsArchivable ? "Confirm Archive" : "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            viewModel.BulkDeleteCommand.Execute(selected);
        }
    }
}
