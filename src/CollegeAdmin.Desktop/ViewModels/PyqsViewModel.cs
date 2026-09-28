using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the desktop
/// consumer for the new PyqsController — the final and largest slice of this body of work.
/// Moderation (Approve/Reject/Recall/Delete) mirrors CertificationsViewModel/BlogsViewModel's
/// shape; staff upload (Add Paper) reuses the same upload-then-create two-step flow every other
/// file-bearing module already uses. The reject-reason dialog is reused from Certifications/Blogs
/// even though the server itself accepts a blank PYQ rejection reason (matching legacy) — always
/// prompting for one here is a deliberate, minor UX tightening, not a functional restriction (a
/// future API consumer can still call reject with no reason, as the live-tested backend allows).
/// </summary>
public sealed partial class PyqsViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public PyqsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    public ObservableCollection<PyqDto> Papers { get; } = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private PyqStatusCountsDto _counts = new();

    [ObservableProperty]
    private string _statusFilter = "pending";

    [ObservableProperty]
    private string _searchFilter = "";

    partial void OnStatusFilterChanged(string value) => _ = LoadAsync();
    partial void OnSearchFilterChanged(string value) => _ = LoadAsync();

    // ── Add paper (staff upload) ────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPaperCommand))]
    private string _newTitle = "";

    [ObservableProperty]
    private string _newSubject = "";

    [ObservableProperty]
    private string _newSemester = "1";

    [ObservableProperty]
    private string _newType = "internal";

    [ObservableProperty]
    private string _newYear = DateTime.Now.Year.ToString();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPaperCommand))]
    [NotifyPropertyChangedFor(nameof(DisplayNewFileName))]
    private string? _newFilePath;

    [ObservableProperty]
    private string? _newFileName;

    public string DisplayNewFileName => NewFileName ?? "(no file selected)";

    /// <summary>Called by the View's code-behind after it reads the picked file's bytes — the
    /// same upload-then-create pattern every other file-bearing module already uses.</summary>
    public async Task UploadPaperFileAsync(string fileName, byte[] bytes)
    {
        ErrorMessage = null;
        try
        {
            NewFilePath = await _apiClient.UploadFileAsync("pyqs", fileName, bytes);
            NewFileName = fileName;
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddPaper))]
    private async Task AddPaperAsync()
    {
        ErrorMessage = null;
        try
        {
            var fields = new Dictionary<string, object?>
            {
                ["title"] = NewTitle.Trim(),
                ["subject"] = NewSubject.Trim(),
                ["semester"] = NewSemester.Trim(),
                ["type"] = NewType.Trim(),
                ["year"] = NewYear.Trim(),
                ["filePath"] = NewFilePath,
            };
            await _apiClient.CreatePyqAsync(fields);
            NewTitle = "";
            NewSubject = "";
            NewFilePath = null;
            NewFileName = null;
            StatusMessage = "Paper added and approved.";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
    }

    private bool CanAddPaper() => NewTitle.Trim().Length > 0 && !string.IsNullOrWhiteSpace(NewFilePath);

    // ── Moderation ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _apiClient.GetPyqsAsync(
                string.IsNullOrWhiteSpace(StatusFilter) ? null : StatusFilter,
                string.IsNullOrWhiteSpace(SearchFilter) ? null : SearchFilter);
            Counts = result.Counts;
            Papers.Clear();
            foreach (var paper in result.Papers)
            {
                Papers.Add(paper);
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
    private async Task ApproveAsync(PyqDto paper)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.ApprovePyqAsync(paper.Id);
            StatusMessage = $"Approved \"{paper.Title}\".";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>Called by the View's code-behind after the rejection-reason dialog is confirmed.</summary>
    public async Task RejectAsync(PyqDto paper, string? reason)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.RejectPyqAsync(paper.Id, reason);
            StatusMessage = $"Rejected \"{paper.Title}\".";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RecallAsync(PyqDto paper)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.RecallPyqAsync(paper.Id);
            StatusMessage = $"Recalled \"{paper.Title}\" to pending.";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(PyqDto paper)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.DeletePyqAsync(paper.Id);
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
