using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-3 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the desktop consumer
/// of the new read-only `/principal/overview` endpoint, gated on the new `principal.overview`
/// capability (auto-granted to a new role=principal admin — see Capabilities::PRINCIPAL_AUTO_GRANT
/// server-side). Deliberately no write action anywhere on this screen or its ViewModel.
/// </summary>
public sealed partial class PrincipalOverviewViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public PrincipalOverviewViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private PrincipalOverviewStatsDto _stats = new();

    [ObservableProperty]
    private PrincipalStudentStatsDto _studentStats = new();

    public ObservableCollection<FacultyByDeptDto> FacultyByDept { get; } = [];
    public ObservableCollection<AdminsByRoleDto> AdminsByRole { get; } = [];
    public ObservableCollection<ActivityLogEntryDto> RecentActivity { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _apiClient.GetPrincipalOverviewAsync();
            Stats = result.Stats;
            StudentStats = result.StudentStats;

            FacultyByDept.Clear();
            foreach (var row in result.FacultyByDept)
            {
                FacultyByDept.Add(row);
            }

            AdminsByRole.Clear();
            foreach (var row in result.AdminsByRole)
            {
                AdminsByRole.Add(row);
            }

            RecentActivity.Clear();
            foreach (var row in result.RecentActivity)
            {
                RecentActivity.Add(row);
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
