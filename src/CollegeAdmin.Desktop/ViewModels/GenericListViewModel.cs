using System.Collections.ObjectModel;
using System.Linq;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Read-only "just show me the rows" list screen shared by every simple content module (News,
/// Banners, FAQ, Documents, Events, Gallery, Faculty, Committees, Departments...) — built after
/// three dedicated ViewModel/View pairs (Notices, Admins, Departments) proved out the same shape
/// with nothing module-specific beyond the API call and column set, which DataGrid's
/// AutoGenerateColumns handles without per-module XAML. Modules needing custom row actions (like
/// Admins' ban/unban/force-logout) still get their own dedicated ViewModel/View — this is only for
/// the plain-list case.
/// </summary>
public sealed partial class GenericListViewModel : ObservableObject
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<object>>> _loader;
    private readonly Action? _onAdd;
    private readonly Func<object, Task>? _onEdit;
    private readonly Func<object, Task>? _onDelete;
    private readonly Func<IReadOnlyList<object>, Task>? _onBulkDelete;
    private readonly Func<object, Task>? _onManage;
    private readonly Action? _onImport;
    private readonly Func<int, Task>? _onExported;
    private readonly Func<object, Task>? _onRestore;
    private readonly int _pageSize;

    public GenericListViewModel(
        string title,
        string emptyDescription,
        Func<CancellationToken, Task<IReadOnlyList<object>>> loader,
        Action? onAdd = null,
        string addLabel = "Add",
        Func<object, Task>? onEdit = null,
        Func<object, Task>? onDelete = null,
        Func<IReadOnlyList<object>, Task>? onBulkDelete = null,
        Func<object, Task>? onManage = null,
        string manageLabel = "Manage",
        Func<int, Task>? onExported = null,
        Action? onImport = null,
        string importLabel = "Import",
        int pageSize = 50,
        bool isArchivable = false,
        Func<object, Task>? onRestore = null)
    {
        Title = title;
        EmptyDescription = emptyDescription;
        _loader = loader;
        _onAdd = onAdd;
        _addLabelWhenAddable = addLabel;
        _onEdit = onEdit;
        _onDelete = onDelete;
        _onBulkDelete = onBulkDelete;
        _onManage = onManage;
        ManageLabel = manageLabel;
        _onExported = onExported;
        _onImport = onImport;
        ImportLabel = importLabel;
        _pageSize = pageSize;
        IsArchivable = isArchivable;
        _onRestore = onRestore;
        _ = LoadAsync();
    }

    private readonly string _addLabelWhenAddable;

    public string Title { get; }
    public string EmptyDescription { get; }
    // Empty when there's no add handler, so EmptyStateView's action button doesn't render at all
    // (an empty Button.Content with a no-op command would be confusing, not just inert).
    public string AddLabel => _onAdd is not null ? _addLabelWhenAddable : "";
    public bool CanAdd => _onAdd is not null;
    public bool CanEdit => _onEdit is not null;
    // Final Convergence Phase P2 item 1 (docs/claude/FINAL_COMPLETION_TRACKER.md §4): while viewing
    // archived rows, the Delete/Archive button is hidden — an already-archived row can only be
    // Restored, not archived again, through this same button slot.
    public bool CanDelete => _onDelete is not null && !ShowArchived;
    public bool CanEditOrDelete => CanEdit || CanDelete;
    public string DeleteButtonLabel => IsArchivable ? "Archive" : "Delete";
    // A third, opt-in row action beyond Edit/Delete — currently only Gallery uses it ("Images"), to
    // open a nested-resource manager (album images) that doesn't fit a plain field-list Edit form.
    public bool CanManage => _onManage is not null;
    public string ManageLabel { get; }
    // A second, opt-in toolbar action beyond Add — currently only Students uses it (Excel Import,
    // docs/claude/phase2/04_EXCEL_IMPORT_PIPELINE.md), mirroring onAdd's own opt-in shape exactly.
    public bool CanImport => _onImport is not null;
    public string ImportLabel { get; }
    // Export is available whenever there's something loaded to export — not gated by a delegate
    // the way Add/Edit/Delete/Manage are, since every module's data is equally exportable.
    public bool CanExport => Items.Count > 0;
    // Final Convergence Phase P1-18: bulk delete reuses the single-row onDelete capability check —
    // a module that can delete one row can delete several, there is no separate "bulk" permission.
    public bool CanBulkDelete => _onDelete is not null && !ShowArchived;

    // Final Convergence Phase P2 item 1 (docs/claude/FINAL_COMPLETION_TRACKER.md §4): true only for
    // Departments/Faculty/Committees/Subjects/Programmes — every other module still hard-deletes.
    public bool IsArchivable { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete), nameof(CanBulkDelete), nameof(CanRestore))]
    private bool _showArchived;

    public bool CanRestore => IsArchivable && ShowArchived && _onRestore is not null;

    partial void OnShowArchivedChanged(bool value) => _ = LoadAsync();

    /// <summary>No confirmation needed — restoring is safe and non-destructive, unlike Delete/Archive,
    /// matching how Edit doesn't need one either.</summary>
    [RelayCommand]
    private async Task RestoreAsync(object row)
    {
        if (_onRestore is not null)
        {
            await _onRestore(row);
        }
    }

    public ObservableCollection<object> Items { get; } = [];

    // Final Convergence Phase P1-18 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): search + pagination
    // are an in-memory layer over the already-loaded Items — every one of this component's ~15
    // modules is lookup/config-sized (dozens to low hundreds of rows, not Students-scale data), and
    // P1-5 already guarantees Items holds the *entire* server-side dataset, not just one page, so
    // there is nothing to gain from real server-side search/pagination here. Column sort is NOT
    // reimplemented — WPF's DataGrid already provides default column-header sort over any bound
    // collection with no code required (confirmed in GenericListView.xaml: nothing disables it).
    [ObservableProperty]
    private string _searchText = "";

    public ObservableCollection<object> FilteredItems { get; } = [];
    public ObservableCollection<object> PagedItems { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    private int _currentPage = 1;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredItems.Count / (double)_pageSize));

    public string PageLabel => FilteredItems.Count == 0
        ? "No items"
        : $"Page {CurrentPage} of {TotalPages} ({FilteredItems.Count} item{(FilteredItems.Count == 1 ? "" : "s")})";

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        RebuildFilteredAndPagedItems();
    }

    [RelayCommand(CanExecute = nameof(CanGoNextPage))]
    private void NextPage()
    {
        CurrentPage++;
        RebuildPagedItems();
    }

    private bool CanGoNextPage() => CurrentPage < TotalPages;

    [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
    private void PreviousPage()
    {
        CurrentPage--;
        RebuildPagedItems();
    }

    private bool CanGoPreviousPage() => CurrentPage > 1;

    /// <summary>Case-insensitive substring match across every string property of the row — rows are
    /// opaque `object`s from many different modules with nothing else in common, the same reason
    /// GetRowId (NavigationService) already uses reflection for "the Id property" generically.</summary>
    private bool MatchesSearch(object row)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var needle = SearchText.Trim();
        return row.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => (string?)p.GetValue(row))
            .Any(v => v is not null && v.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private void RebuildFilteredAndPagedItems()
    {
        FilteredItems.Clear();
        foreach (var row in Items.Where(MatchesSearch))
        {
            FilteredItems.Add(row);
        }
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageLabel));
        RebuildPagedItems();
    }

    private void RebuildPagedItems()
    {
        PagedItems.Clear();
        foreach (var row in FilteredItems.Skip((CurrentPage - 1) * _pageSize).Take(_pageSize))
        {
            PagedItems.Add(row);
        }
        OnPropertyChanged(nameof(PageLabel));
        NextPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Add() => _onAdd?.Invoke();

    [RelayCommand]
    private void Import() => _onImport?.Invoke();

    [RelayCommand]
    private async Task EditAsync(object row)
    {
        if (_onEdit is not null)
        {
            await _onEdit(row);
        }
    }

    /// <summary>The confirmation prompt itself lives in GenericListView's code-behind (a WPF
    /// MessageBox), not here — this ViewModel stays UI-framework-agnostic so it can be exercised by
    /// plain unit tests (see GenericListViewModelTests). By the time this runs, the user has already
    /// confirmed.</summary>
    [RelayCommand]
    private async Task DeleteAsync(object row)
    {
        if (_onDelete is not null)
        {
            await _onDelete(row);
        }
    }

    /// <summary>Confirmation lives in GenericListView's code-behind, same as single-row delete.
    /// Prefers the module-specific onBulkDelete (deletes every row then refreshes once) when wired;
    /// falls back to looping the single-row delegate (one refresh per row) otherwise, so a
    /// construction site that hasn't been updated with onBulkDelete still works correctly, just
    /// less efficiently — never a hard requirement.</summary>
    [RelayCommand]
    private async Task BulkDeleteAsync(IReadOnlyList<object> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        if (_onBulkDelete is not null)
        {
            await _onBulkDelete(rows);
        }
        else if (_onDelete is not null)
        {
            foreach (var row in rows)
            {
                await _onDelete(row);
            }
        }
    }

    [RelayCommand]
    private async Task ManageAsync(object row)
    {
        if (_onManage is not null)
        {
            await _onManage(row);
        }
    }

    /// <summary>Called by the Create dialog's Saved handler (via the same onAdd closure's owner) to
    /// refresh the list after a successful create/edit — exposed so NavigationService's dialog
    /// wiring doesn't need to reach into private state.</summary>
    public Task RefreshAsync() => LoadAsync();

    /// <summary>Called by GenericListView's code-behind after it has already written the CSV file
    /// (a View-layer concern — SaveFileDialog and the actual file write live there, not here, so
    /// this ViewModel stays testable without touching disk). Best-effort: a failed audit call must
    /// never make the export itself look like it failed, since the file was already written.</summary>
    public async Task RecordExportAsync(int rowCount)
    {
        if (_onExported is null)
        {
            return;
        }

        try
        {
            await _onExported(rowCount);
        }
        catch (ApiRequestException)
        {
        }
    }

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsEmpty => !IsLoading && ErrorMessage is null && Items.Count == 0;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var rows = await _loader(CancellationToken.None);
            Items.Clear();
            foreach (var row in rows)
            {
                Items.Add(row);
            }
            CurrentPage = 1;
            RebuildFilteredAndPagedItems();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(CanExport));
        }
    }
}
