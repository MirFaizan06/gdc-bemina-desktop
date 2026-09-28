using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Tests;

public class GenericListViewModelTests
{
    private sealed record Row(int Id, string Name);

    [Fact]
    public async Task Constructor_NoEditOrDeleteCallbacks_CanEditAndCanDeleteAreFalse()
    {
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]));

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.CanEdit);
        Assert.False(viewModel.CanDelete);
        Assert.False(viewModel.CanEditOrDelete);
    }

    [Fact]
    public async Task EditCommand_InvokesOnEdit_WithTheSelectedRow()
    {
        object? editedRow = null;
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]),
            onEdit: row =>
            {
                editedRow = row;
                return Task.CompletedTask;
            });

        var row = new Row(7, "Test");
        await viewModel.EditCommand.ExecuteAsync(row);

        Assert.True(viewModel.CanEdit);
        Assert.Same(row, editedRow);
    }

    [Fact]
    public async Task DeleteCommand_InvokesOnDelete_WithTheSelectedRow()
    {
        object? deletedRow = null;
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]),
            onDelete: row =>
            {
                deletedRow = row;
                return Task.CompletedTask;
            });

        var row = new Row(3, "ToDelete");
        await viewModel.DeleteCommand.ExecuteAsync(row);

        Assert.True(viewModel.CanDelete);
        Assert.Same(row, deletedRow);
    }

    [Fact]
    public async Task RefreshAsync_ReloadsItemsFromLoader()
    {
        var callCount = 0;
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ =>
            {
                callCount++;
                return Task.FromResult<IReadOnlyList<object>>(callCount == 1 ? [] : [new Row(1, "First")]);
            });

        await Task.Delay(1); // let the constructor's fire-and-forget initial LoadAsync settle
        await viewModel.RefreshAsync();

        Assert.Equal(2, callCount);
        Assert.Single(viewModel.Items);
    }

    [Fact]
    public async Task CanExport_ReflectsWhetherAnyItemsAreLoaded()
    {
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([new Row(1, "First")]));

        await Task.Delay(1);

        Assert.True(viewModel.CanExport);
    }

    [Fact]
    public async Task CanExport_FalseWhenListIsEmpty()
    {
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]));

        await Task.Delay(1);

        Assert.False(viewModel.CanExport);
    }

    [Fact]
    public async Task RecordExportAsync_InvokesOnExported_WithTheRowCount()
    {
        int? recordedCount = null;
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]),
            onExported: count =>
            {
                recordedCount = count;
                return Task.CompletedTask;
            });

        await viewModel.RecordExportAsync(42);

        Assert.Equal(42, recordedCount);
    }

    [Fact]
    public async Task RecordExportAsync_NoOnExportedCallback_DoesNotThrow()
    {
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]));

        await viewModel.RecordExportAsync(3); // should simply no-op
    }

    // ---- Final Convergence Phase P1-18: search / pagination / bulk delete ----

    private static IReadOnlyList<object> FiveRows() =>
        [new Row(1, "Alpha"), new Row(2, "Beta"), new Row(3, "Gamma"), new Row(4, "Delta"), new Row(5, "Epsilon")];

    [Fact]
    public async Task SearchText_FiltersPagedItems_ByCaseInsensitiveSubstringOnAnyStringProperty()
    {
        var viewModel = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult(FiveRows()));
        await Task.Delay(1);

        viewModel.SearchText = "amm"; // matches "Gamma" only

        Assert.Single(viewModel.FilteredItems);
        Assert.Single(viewModel.PagedItems);
        Assert.Equal("Gamma", ((Row)viewModel.PagedItems[0]).Name);
    }

    [Fact]
    public async Task SearchText_BlankOrWhitespace_MatchesEveryRow()
    {
        var viewModel = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult(FiveRows()));
        await Task.Delay(1);

        viewModel.SearchText = "   ";

        Assert.Equal(5, viewModel.FilteredItems.Count);
    }

    [Fact]
    public async Task SearchText_Changing_ResetsCurrentPageToOne()
    {
        var viewModel = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult(FiveRows()), pageSize: 2);
        await Task.Delay(1);
        viewModel.NextPageCommand.Execute(null);
        Assert.Equal(2, viewModel.CurrentPage);

        viewModel.SearchText = "a"; // matches Alpha/Gamma/Delta

        Assert.Equal(1, viewModel.CurrentPage);
    }

    [Fact]
    public async Task PagedItems_SlicesFilteredItems_ByPageSize()
    {
        var viewModel = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult(FiveRows()), pageSize: 2);
        await Task.Delay(1);

        Assert.Equal(2, viewModel.PagedItems.Count);
        Assert.Equal(3, viewModel.TotalPages);
        Assert.Equal("Alpha", ((Row)viewModel.PagedItems[0]).Name);
        Assert.Equal("Beta", ((Row)viewModel.PagedItems[1]).Name);
    }

    [Fact]
    public async Task NextPageCommand_AdvancesPagedItems_AndStopsAtTheLastPage()
    {
        var viewModel = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult(FiveRows()), pageSize: 2);
        await Task.Delay(1);

        Assert.True(viewModel.NextPageCommand.CanExecute(null));
        viewModel.NextPageCommand.Execute(null);
        Assert.Equal(2, viewModel.CurrentPage);
        Assert.Equal("Gamma", ((Row)viewModel.PagedItems[0]).Name);

        viewModel.NextPageCommand.Execute(null); // page 3: just "Epsilon"
        Assert.Equal(3, viewModel.CurrentPage);
        Assert.Single(viewModel.PagedItems);
        Assert.False(viewModel.NextPageCommand.CanExecute(null));
    }

    [Fact]
    public async Task PreviousPageCommand_DisabledOnFirstPage_EnabledAfterAdvancing()
    {
        var viewModel = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult(FiveRows()), pageSize: 2);
        await Task.Delay(1);

        Assert.False(viewModel.PreviousPageCommand.CanExecute(null));

        viewModel.NextPageCommand.Execute(null);
        Assert.True(viewModel.PreviousPageCommand.CanExecute(null));

        viewModel.PreviousPageCommand.Execute(null);
        Assert.Equal(1, viewModel.CurrentPage);
    }

    [Fact]
    public async Task BulkDeleteAsync_PrefersOnBulkDelete_OverLoopingOnDelete()
    {
        IReadOnlyList<object>? bulkRows = null;
        var perRowDeleteCount = 0;
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult(FiveRows()),
            onDelete: _ => { perRowDeleteCount++; return Task.CompletedTask; },
            onBulkDelete: rows => { bulkRows = rows; return Task.CompletedTask; });
        await Task.Delay(1);

        var selected = new object[] { new Row(1, "Alpha"), new Row(2, "Beta") };
        await viewModel.BulkDeleteCommand.ExecuteAsync(selected);

        Assert.NotNull(bulkRows);
        Assert.Equal(2, bulkRows!.Count);
        Assert.Equal(0, perRowDeleteCount); // the per-row delegate must not have been looped
    }

    [Fact]
    public async Task BulkDeleteAsync_FallsBackToLoopingOnDelete_WhenOnBulkDeleteNotProvided()
    {
        var deletedRows = new List<object>();
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult(FiveRows()),
            onDelete: row => { deletedRows.Add(row); return Task.CompletedTask; });

        var selected = new object[] { new Row(1, "Alpha"), new Row(2, "Beta") };
        await viewModel.BulkDeleteCommand.ExecuteAsync(selected);

        Assert.Equal(2, deletedRows.Count);
    }

    [Fact]
    public async Task BulkDeleteAsync_EmptySelection_DoesNothing()
    {
        var called = false;
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]),
            onDelete: _ => { called = true; return Task.CompletedTask; });

        await viewModel.BulkDeleteCommand.ExecuteAsync(Array.Empty<object>());

        Assert.False(called);
    }

    [Fact]
    public void CanBulkDelete_ReflectsWhetherOnDeleteWasProvided()
    {
        var withDelete = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult<IReadOnlyList<object>>([]), onDelete: _ => Task.CompletedTask);
        var withoutDelete = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult<IReadOnlyList<object>>([]));

        Assert.True(withDelete.CanBulkDelete);
        Assert.False(withoutDelete.CanBulkDelete);
    }

    // ---- Final Convergence Phase P2 item 1: archive/restore ----

    [Fact]
    public void DeleteButtonLabel_IsArchiveWhenArchivable_OtherwiseDelete()
    {
        var archivable = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult<IReadOnlyList<object>>([]), isArchivable: true);
        var notArchivable = new GenericListViewModel("Rows", "None yet.", _ => Task.FromResult<IReadOnlyList<object>>([]));

        Assert.Equal("Archive", archivable.DeleteButtonLabel);
        Assert.Equal("Delete", notArchivable.DeleteButtonLabel);
    }

    /// <summary>The loader itself can't be re-wrapped after construction from a test, so this
    /// exercises CanDelete/CanRestore — the two properties GenericListView.xaml actually gates the
    /// Archive/Restore buttons on — rather than the loader's own fetch call. A separate
    /// live-HTTP-verified smoke test (see 18_DEVLOG.md) proves the real `archived=1` query param
    /// round-trips correctly through NavigationService's own archivableLoader wiring.</summary>
    [Fact]
    public async Task ShowArchived_Toggling_SwapsCanDeleteAndCanRestore()
    {
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]),
            onDelete: _ => Task.CompletedTask,
            isArchivable: true,
            onRestore: _ => Task.CompletedTask);
        await Task.Delay(1);

        Assert.False(viewModel.ShowArchived);
        Assert.True(viewModel.CanDelete); // archive action available while viewing active rows
        Assert.False(viewModel.CanRestore);

        viewModel.ShowArchived = true;
        await Task.Delay(1); // OnShowArchivedChanged fires LoadAsync fire-and-forget

        Assert.False(viewModel.CanDelete); // can't archive an already-archived row
        Assert.True(viewModel.CanRestore);
    }

    [Fact]
    public void CanRestore_FalseWhenNotArchivable_EvenIfOnRestoreProvided()
    {
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]),
            onRestore: _ => Task.CompletedTask)
        {
            ShowArchived = true,
        };

        Assert.False(viewModel.CanRestore);
    }

    [Fact]
    public void CanRestore_FalseWhenOnRestoreNotProvided_EvenIfArchivableAndShowingArchived()
    {
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]),
            isArchivable: true)
        {
            ShowArchived = true,
        };

        Assert.False(viewModel.CanRestore);
    }

    [Fact]
    public async Task RestoreCommand_InvokesOnRestore_WithTheSelectedRow()
    {
        object? restoredRow = null;
        var viewModel = new GenericListViewModel(
            "Rows", "None yet.",
            _ => Task.FromResult<IReadOnlyList<object>>([]),
            isArchivable: true,
            onRestore: row =>
            {
                restoredRow = row;
                return Task.CompletedTask;
            });

        var row = new Row(9, "Restored");
        await viewModel.RestoreCommand.ExecuteAsync(row);

        Assert.Same(row, restoredRow);
    }
}
