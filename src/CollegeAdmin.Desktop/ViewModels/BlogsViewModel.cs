using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the desktop
/// consumer for the new BlogsController — moderation only (public submission/self-service-edit
/// stay on the legacy website). Mirrors CertificationsViewModel's shape (Approve/Reject as the
/// real per-row actions, reusing the same RejectionReasonWindow prompt Certifications/Submissions
/// already established) rather than the generic Edit/Delete pattern.
/// </summary>
public sealed partial class BlogsViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public BlogsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    public ObservableCollection<BlogPostDto> Posts { get; } = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private BlogStatusCountsDto _counts = new();

    [ObservableProperty]
    private string _statusFilter = "pending";

    partial void OnStatusFilterChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _apiClient.GetBlogsAsync(string.IsNullOrWhiteSpace(StatusFilter) ? null : StatusFilter);
            Counts = result.Counts;
            Posts.Clear();
            foreach (var post in result.Posts)
            {
                Posts.Add(post);
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
    private async Task ApproveAsync(BlogPostDto post)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.ApproveBlogAsync(post.Id);
            StatusMessage = $"Published \"{post.Title}\".";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>Called by the View's code-behind after the rejection-reason dialog is confirmed.</summary>
    public async Task RejectAsync(BlogPostDto post, string reason)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.RejectBlogAsync(post.Id, reason);
            StatusMessage = $"Rejected \"{post.Title}\".";
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(BlogPostDto post)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.DeleteBlogAsync(post.Id);
            await LoadAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
