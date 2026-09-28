using CollegeAdmin.Application.Auth;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-14 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the desktop
/// consumer for the new DashboardController — every admin's landing page, previously an
/// unresolved nav key that fell straight through to a bare placeholder (confirmed by the original
/// audit, and confirmed still true live before this was built). The View renders only the stat
/// cards whose value is non-null, so an admin with no grant for a domain simply never sees a card
/// for it — never a misleading "0".
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public DashboardViewModel(IApiClient apiClient, IAuthSessionService authSessionService)
    {
        _apiClient = apiClient;
        var admin = authSessionService.CurrentAdmin;
        Greeting = admin is not null ? $"{TimeGreeting.Now()}, {TimeGreeting.FirstName(admin.Name)}!" : "Welcome";
        _ = LoadAsync();
    }

    public string Greeting { get; }

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private DashboardStatsDto _stats = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Stats = await _apiClient.GetDashboardAsync();
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
