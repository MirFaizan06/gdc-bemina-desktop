using System.Linq;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-1 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the desktop consumer
/// for ActivityLogController::index() — a fully-built, capability-gated, paginated/filterable
/// backend endpoint that had zero desktop screen at all until now (confirmed by the original audit:
/// no ViewModel, no View, no nav entry existed anywhere).
///
/// Wraps GenericListViewModel (composition, matching TimetableViewModel's own established pattern)
/// so filter changes just trigger a fresh server-side query via a new loader closure — the filters
/// genuinely change what the server returns, unlike Timetable's client-side-only grid filter.
/// Real page-by-page pagination controls are deliberately not built here (a generously large
/// perPage=200 is fetched instead) — that's P1-18's job (a shared GenericListView upgrade), not
/// duplicated per-screen here.
/// </summary>
public sealed partial class AuditLogViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public AuditLogViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        List = new GenericListViewModel(
            "Audit Log",
            "No matching activity found.",
            async ct => (await _apiClient.GetActivityLogAsync(
                string.IsNullOrWhiteSpace(ActionFilter) ? null : ActionFilter.Trim(),
                string.IsNullOrWhiteSpace(EntityFilter) ? null : EntityFilter.Trim(),
                int.TryParse(AdminIdFilter, out var adminId) ? adminId : null,
                ct)).Cast<object>().ToList());
    }

    public GenericListViewModel List { get; }

    [ObservableProperty]
    private string _actionFilter = "";

    [ObservableProperty]
    private string _entityFilter = "";

    [ObservableProperty]
    private string _adminIdFilter = "";

    partial void OnActionFilterChanged(string value) => _ = List.RefreshAsync();
    partial void OnEntityFilterChanged(string value) => _ = List.RefreshAsync();
    partial void OnAdminIdFilterChanged(string value) => _ = List.RefreshAsync();
}
