using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-15 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the "System" nav
/// item's replacement — renamed "Settings" and now hosts connectivity status, app version, and
/// basic diagnostics (API base URL, last successful request time) alongside the existing Credits
/// content (CLAUDE.md's developer-credit requirement), which previously had this screen to itself.
/// Supersedes the standalone CreditsViewModel/CreditsView rather than wrapping them, since both
/// screens share the same load-on-construct/LoadingView/ErrorStateView shape and folding avoids
/// maintaining two near-identical ViewModels for what is now one screen.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public SettingsViewModel(IApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        ApiBaseUrl = configuration["Api:BaseUrl"] ?? "(not configured)";
        AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        _ = LoadAsync();
    }

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _company = "";

    [ObservableProperty]
    private string _developer = "";

    [ObservableProperty]
    private string _businessEmail = "";

    [ObservableProperty]
    private string _appVersion = "";

    [ObservableProperty]
    private string _apiBaseUrl = "";

    [ObservableProperty]
    private bool _isCheckingConnectivity;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private DateTime? _lastSuccessfulCheckAt;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var credits = await _apiClient.GetCreditsAsync();
            Company = credits.Company;
            Developer = credits.Developer;
            BusinessEmail = credits.BusinessEmail;
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }

        await CheckConnectivityAsync();
    }

    /// <summary>A real round-trip to GET /api/v1/ping, not a cached/assumed value — the
    /// "connectivity status" the tracker asks for is meaningless if it isn't live. Never throws:
    /// a failed ping is a legitimate, expected outcome on this screen (that's the whole point of
    /// showing it), not an error state.</summary>
    [RelayCommand]
    private async Task CheckConnectivityAsync()
    {
        IsCheckingConnectivity = true;
        try
        {
            await _apiClient.PingAsync();
            IsConnected = true;
            LastSuccessfulCheckAt = DateTime.Now;
        }
        catch (ApiRequestException)
        {
            IsConnected = false;
        }
        finally
        {
            IsCheckingConnectivity = false;
        }
    }
}
