using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CollegeAdmin.Application.Caching;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Wraps the Timetable nav item's GenericListViewModel (composition, not inheritance —
/// GenericListViewModel is sealed and its Add/Edit/Delete/Export commands are already fully built
/// and tested) to add a weekly grid visualization alongside the existing flat list. List mode
/// literally embeds a GenericListView bound to List, so every already-working piece (Create/Edit/
/// Delete/Export CSV) keeps working unchanged; only Grid mode's cells are new. Deliberately does
/// NOT add drag-drop, bulk edit, version history, or XLSX import — those remain open (see
/// docs/claude/18_DEVLOG.md's Timetable entries), this is specifically the grid visualization.
/// </summary>
public sealed partial class TimetableViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly ILookupCache _lookupCache;

    public TimetableViewModel(GenericListViewModel list, IApiClient apiClient, ILookupCache lookupCache)
    {
        List = list;
        _apiClient = apiClient;
        _lookupCache = lookupCache;
        List.Items.CollectionChanged += OnItemsChanged;
        RebuildGrid();
        _ = LoadProgrammeOptionsAsync();
    }

    public GenericListViewModel List { get; }

    public ObservableCollection<TimetableGridRow> GridRows { get; } = [];

    /// <summary>Final Convergence Phase P0-4: replaces the raw numeric filter textbox with a real
    /// picker. This is a client-side filter over already-loaded rows (see RebuildGrid), so a
    /// fresh, non-blocking fetch on construction is enough — no dialog is waiting on it.</summary>
    public ObservableCollection<FormFieldOption> ProgrammeOptions { get; } = [new("", "(All programmes)")];

    private async Task LoadProgrammeOptionsAsync()
    {
        var programmes = await _lookupCache.GetOrFetchAsync("programmes", ct => _apiClient.GetProgrammesAsync(ct));
        foreach (var p in programmes)
        {
            ProgrammeOptions.Add(new FormFieldOption(p.Id.ToString(), $"{p.Name} ({p.DepartmentName})"));
        }
    }

    [ObservableProperty]
    private bool _isGridView = true;

    [ObservableProperty]
    private string _filterProgrammeId = "";

    [ObservableProperty]
    private string _filterSemester = "";

    [RelayCommand]
    private void ToggleView() => IsGridView = !IsGridView;

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildGrid();

    partial void OnFilterProgrammeIdChanged(string value) => RebuildGrid();
    partial void OnFilterSemesterChanged(string value) => RebuildGrid();

    private void RebuildGrid()
    {
        GridRows.Clear();

        var entries = List.Items.OfType<TimetableEntryDto>().AsEnumerable();
        if (int.TryParse(FilterProgrammeId, out var programmeId))
        {
            entries = entries.Where(e => e.ProgrammeId == programmeId);
        }
        if (int.TryParse(FilterSemester, out var semester))
        {
            entries = entries.Where(e => e.Semester == semester);
        }

        var byStartTime = entries
            .GroupBy(e => e.StartTime)
            .OrderBy(g => g.Key);

        foreach (var group in byStartTime)
        {
            var forDay = (string day) => (IReadOnlyList<TimetableEntryDto>)group.Where(e => e.DayOfWeek == day).ToList();
            GridRows.Add(new TimetableGridRow(
                TimeLabel: group.Key.Length >= 5 ? group.Key[..5] : group.Key,
                Mon: forDay("mon"),
                Tue: forDay("tue"),
                Wed: forDay("wed"),
                Thu: forDay("thu"),
                Fri: forDay("fri"),
                Sat: forDay("sat")));
        }
    }
}
