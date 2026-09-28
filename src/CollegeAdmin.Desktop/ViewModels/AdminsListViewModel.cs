using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Stage 13's desktop UI over Stage 6's account-management API — list, ban/unban, force-logout.
/// Create/edit/capability-grant management is not built yet (server supports it, no screen).
/// </summary>
public sealed partial class AdminsListViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public AdminsListViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    public ObservableCollection<AdminSummary> Admins { get; } = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public bool IsEmpty => !IsLoading && ErrorMessage is null && Admins.Count == 0;

    /// <summary>Pre-fills directly from the row already in memory — AdminSummary already carries
    /// every field AdminsController::update() accepts, so no extra GET is needed (unlike the
    /// generic-module Edit flow, which fetches fresh because its list DTOs are deliberately
    /// non-exhaustive). The View opens the resulting window; this ViewModel stays WPF-independent.</summary>
    public AdminEditViewModel CreateEditViewModel(AdminSummary admin) => new(_apiClient, admin);

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var admins = await _apiClient.GetAdminsAsync();
            Admins.Clear();
            foreach (var admin in admins)
            {
                Admins.Add(admin);
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    private async Task BanAsync(AdminSummary admin)
    {
        try
        {
            var revoked = await _apiClient.BanAdminAsync(admin.Id, "Banned from desktop admin console.");
            StatusMessage = $"Banned {admin.Name} ({revoked} session(s) revoked).";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UnbanAsync(AdminSummary admin)
    {
        try
        {
            await _apiClient.UnbanAdminAsync(admin.Id);
            StatusMessage = $"Unbanned {admin.Name}.";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ForceLogoutAsync(AdminSummary admin)
    {
        try
        {
            var revoked = await _apiClient.ForceLogoutAdminAsync(admin.Id);
            StatusMessage = $"Revoked {revoked} session(s) for {admin.Name}.";
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
