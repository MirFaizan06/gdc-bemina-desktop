using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the desktop
/// consumer for the new CertificationsController — admin review only (application/upload/
/// download/HMAC-signed public QR verification pages stay web-based per the tracker's own
/// "Preserve" note). Not built on GenericListViewModel: Approve/Reject are the two real actions
/// here, not Edit/Delete, and Reject needs a reason prompt — the same "several distinct per-row
/// actions" shape AdminsListViewModel already uses for Ban/Unban/ForceLogout.
/// </summary>
public sealed partial class CertificationsViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public CertificationsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    public ObservableCollection<CertificateApplicationDto> Applications { get; } = [];
    public ObservableCollection<CertificateTypeDto> Types { get; } = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private CertificationStatsDto _stats = new();

    [ObservableProperty]
    private string _statusFilter = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddTypeCommand))]
    private string _newTypeCode = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddTypeCommand))]
    private string _newTypeName = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddTypeCommand))]
    private string _newTypePrefix = "";

    partial void OnStatusFilterChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _apiClient.GetCertificationsAsync(string.IsNullOrWhiteSpace(StatusFilter) ? null : StatusFilter);
            Stats = result.Stats;
            Applications.Clear();
            foreach (var app in result.Applications)
            {
                Applications.Add(app);
            }
            Types.Clear();
            foreach (var type in result.Types)
            {
                Types.Add(type);
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

    [RelayCommand]
    private async Task ApproveAsync(CertificateApplicationDto application)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.ApproveCertificationAsync(application.Id);
            StatusMessage = $"Approved application #{application.Id}.";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>Called by the View's code-behind after the rejection-reason dialog is confirmed —
    /// mirrors GalleryImageManagerViewModel's UploadNewImageAsync split (dialog in the View, API
    /// call in the ViewModel).</summary>
    public async Task RejectAsync(CertificateApplicationDto application, string reason)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.RejectCertificationAsync(application.Id, reason);
            StatusMessage = $"Rejected application #{application.Id}.";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddType))]
    private async Task AddTypeAsync()
    {
        ErrorMessage = null;
        try
        {
            var types = await _apiClient.CreateCertificateTypeAsync(NewTypeCode.Trim(), NewTypeName.Trim(), NewTypePrefix.Trim(), null);
            Types.Clear();
            foreach (var type in types)
            {
                Types.Add(type);
            }
            NewTypeCode = "";
            NewTypeName = "";
            NewTypePrefix = "";
            StatusMessage = "Certificate type added — off by default until a certificate template exists for it.";
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
    }

    private bool CanAddType() => NewTypeCode.Trim().Length > 0 && NewTypeName.Trim().Length > 0 && NewTypePrefix.Trim().Length > 0;

    [RelayCommand]
    private async Task ToggleTypeAsync(CertificateTypeDto type)
    {
        ErrorMessage = null;
        try
        {
            var types = await _apiClient.ToggleCertificateTypeAsync(type.Id);
            Types.Clear();
            foreach (var t in types)
            {
                Types.Add(t);
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
