using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.Export;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class TimetableView : UserControl
{
    public TimetableView() => InitializeComponent();

    /// <summary>A grid card is read-only-click-to-edit (no drag-drop, no inline delete in this
    /// mode) — deliberately smaller scope than a full editable grid; deleting still works via the
    /// "View as List" toggle, which re-hosts the same GenericListView the flat-list mode already
    /// uses, confirmation and all.</summary>
    private async void EntryCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: TimetableEntryDto entry } || DataContext is not TimetableViewModel viewModel)
        {
            return;
        }

        await viewModel.List.EditCommand.ExecuteAsync(entry);
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TimetableViewModel viewModel)
        {
            return;
        }

        await CsvExporter.ExportInteractivelyAsync(viewModel.List.Items.ToList(), "Timetable-export.csv", viewModel.List.RecordExportAsync);
    }
}
