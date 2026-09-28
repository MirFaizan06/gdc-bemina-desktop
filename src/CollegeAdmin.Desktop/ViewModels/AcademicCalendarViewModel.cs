using System.Collections.ObjectModel;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the desktop
/// consumer for the new AcademicCalendarController — Blogs/PYQ/Submissions/Certifications's sibling
/// slices in the same body of work. Wraps GenericListViewModel (composition, matching
/// TimetableViewModel's own established pattern for a module needing more than plain CRUD) so the
/// academic-year filter re-queries the server, and adds the one legacy admin action that has no
/// equivalent in the flat CRUD shape: triggering a Google Calendar holiday sync.
/// </summary>
public sealed partial class AcademicCalendarViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public AcademicCalendarViewModel(GenericListViewModel list, IApiClient apiClient)
    {
        List = list;
        _apiClient = apiClient;
        _ = LoadYearOptionsAsync();
    }

    public GenericListViewModel List { get; }

    public ObservableCollection<string> AcademicYearOptions { get; } = [];

    [ObservableProperty]
    private string _academicYearFilter = "";

    [ObservableProperty]
    private bool _googleSyncConfigured;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private string? _syncMessage;

    partial void OnAcademicYearFilterChanged(string value) => _ = List.RefreshAsync();

    private async Task LoadYearOptionsAsync()
    {
        try
        {
            var result = await _apiClient.GetCalendarEventsAsync();
            GoogleSyncConfigured = result.GoogleSyncConfigured;
            AcademicYearOptions.Clear();
            foreach (var year in result.AcademicYearOptions)
            {
                AcademicYearOptions.Add(year);
            }
            AcademicYearFilter = result.CurrentAcademicYear;
        }
        catch (ApiRequestException)
        {
            // Non-fatal — the year picker just stays empty; List's own loader still runs and
            // surfaces any real error through its own ErrorMessage.
        }
    }

    [RelayCommand]
    private async Task SyncHolidaysAsync()
    {
        if (string.IsNullOrWhiteSpace(AcademicYearFilter))
        {
            return;
        }

        IsSyncing = true;
        SyncMessage = null;
        try
        {
            var result = await _apiClient.SyncCalendarHolidaysAsync(AcademicYearFilter, clearFirst: false);
            SyncMessage = result.Synced
                ? $"Synced: {result.Inserted} inserted, {result.Updated} updated."
                : result.Message ?? "Already synced.";
            await List.RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            SyncMessage = ex.Message;
        }
        finally
        {
            IsSyncing = false;
        }
    }
}
