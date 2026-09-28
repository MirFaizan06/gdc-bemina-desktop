using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Stage 16's desktop screen — currently the "Reports & Exports" nav group's only real content.
/// Per-module CSV export already lives inline on every list screen (GenericListView's Export CSV
/// button), so this page is specifically the job history + "run cleanup now" trigger, not a
/// generic reports landing page (that remains future work if/when more report types exist).
/// </summary>
public sealed partial class BackgroundJobsViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public BackgroundJobsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    public ObservableCollection<BackgroundJobDto> Jobs { get; } = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCleanupCommand))]
    private bool _isRunning;

    public bool IsEmpty => !IsLoading && ErrorMessage is null && Jobs.Count == 0;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var jobs = await _apiClient.GetJobsAsync();
            Jobs.Clear();
            foreach (var job in jobs)
            {
                Jobs.Add(job);
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

    [RelayCommand(CanExecute = nameof(CanRunCleanup))]
    private async Task RunCleanupAsync()
    {
        IsRunning = true;
        StatusMessage = null;
        ErrorMessage = null;
        try
        {
            var result = await _apiClient.RunJobAsync("cleanup_expired_sessions");
            Jobs.Clear();
            foreach (var job in result.Jobs)
            {
                Jobs.Add(job);
            }
            StatusMessage = $"Processed {result.Processed} job(s), {result.Failed} failed.";
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    private bool CanRunCleanup() => !IsRunning;
}
