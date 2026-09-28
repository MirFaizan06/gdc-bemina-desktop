using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the desktop
/// consumer for the new PrincipalSectionController — the public Principal's-message website
/// content (name/designation/photo/message shown on the homepage and About page), edited by a
/// Super Admin or authorized editor. Not to be confused with PrincipalOverviewViewModel (P1-3's
/// read-only cross-module dashboard for a Principal-role admin).
/// </summary>
public sealed partial class PrincipalSectionViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public PrincipalSectionViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _designation = "";

    [ObservableProperty]
    private string _message = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayPhotoName))]
    private string? _photo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayPhotoName))]
    private string? _newPhotoFileName;

    public string DisplayPhotoName =>
        NewPhotoFileName
        ?? (string.IsNullOrWhiteSpace(Photo) ? "(no photo set)" : System.IO.Path.GetFileName(Photo));

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var current = await _apiClient.GetPrincipalSectionAsync();
            Name = current.Name ?? "";
            Designation = current.Designation ?? "";
            Message = current.Message ?? "";
            Photo = current.Photo;
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

    /// <summary>Called by the View's code-behind after it reads the picked file's bytes — uploads
    /// via the same /api/v1/uploads endpoint every other content module now uses.</summary>
    public async Task UploadPhotoAsync(string fileName, byte[] bytes)
    {
        ErrorMessage = null;
        try
        {
            Photo = await _apiClient.UploadFileAsync("principal", fileName, bytes);
            NewPhotoFileName = fileName;
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            var updated = await _apiClient.UpdatePrincipalSectionAsync(Name, Designation, Photo, Message);
            Name = updated.Name ?? "";
            Designation = updated.Designation ?? "";
            Message = updated.Message ?? "";
            Photo = updated.Photo;
            StatusMessage = "Saved.";
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
