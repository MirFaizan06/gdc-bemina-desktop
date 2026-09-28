using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// A preference window's admin detail/monitor screen (docs/claude/phase2/06_PREFERENCE_SYSTEM.md's
/// admin flow: Open -> Monitor submissions -> Review anomalies -> Close). Window field edits and
/// plain list/create reuse the existing GenericListViewModel/GenericCreateViewModel pattern (see
/// NavigationService) — this ViewModel only covers what that generic shape can't: Open/Close state
/// transitions, the eligible/submitted/not-submitted summary, and per-submission Invalidate/Reopen.
/// Recording NEW choices on a student's behalf (submitChoices) has no desktop UI yet — a named,
/// deliberately deferred gap (see docs/claude/18_DEVLOG.md Entry 24): this screen is for monitoring
/// and correcting existing submissions, not for the primary submit flow, which belongs on the public
/// website once that integration work happens (explicitly out of scope this session).
/// </summary>
public sealed partial class PreferenceWindowDetailViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public PreferenceWindowDetailViewModel(IApiClient apiClient, PreferenceWindowDto window)
    {
        _apiClient = apiClient;
        Window = window;
        _ = RefreshAsync();
    }

    [ObservableProperty]
    private PreferenceWindowDto _window;

    [ObservableProperty]
    private PreferenceWindowSummaryDto? _summary;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<PreferenceSubmissionDto> Submissions { get; } = [];

    /// <summary>Bound to a TextBox next to each submission row's Invalidate button — a real reason
    /// capture, not a hardcoded string, since this action writes a real audit trail entry.</summary>
    [ObservableProperty]
    private string _invalidateReason = "";

    public bool CanOpen => Window.Status == "draft";
    public bool CanClose => Window.Status == "open";
    public bool CanRunAllotment => Window.Status == "closed";

    [ObservableProperty]
    private AllotmentRunDto? _latestRun;

    public bool HasProposedRun => LatestRun?.Status == "proposed";

    public ObservableCollection<AllotmentResultDto> AllotmentResults { get; } = [];

    /// <summary>Bound to the override panel — real reason capture, same rationale as InvalidateReason.</summary>
    [ObservableProperty]
    private string _overrideReason = "";

    /// <summary>The replacement subject's numeric ID, typed by the admin (the desktop has no subject
    /// picker UI yet for this panel — a named, smaller gap than the missing student-submission UI,
    /// since an admin correcting one result is a rare action, not the primary workflow).</summary>
    [ObservableProperty]
    private string _overrideSubjectId = "";

    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            Summary = await _apiClient.GetPreferenceWindowSummaryAsync(Window.Id);
            var submissions = await _apiClient.GetPreferenceWindowSubmissionsAsync(Window.Id);
            Submissions.Clear();
            foreach (var s in submissions)
            {
                Submissions.Add(s);
            }

            var runs = await _apiClient.GetAllotmentRunsAsync(Window.Id);
            LatestRun = runs.Count > 0 ? runs[0] : null; // server orders newest-first
            OnPropertyChanged(nameof(HasProposedRun));

            AllotmentResults.Clear();
            if (LatestRun is not null)
            {
                var results = await _apiClient.GetAllotmentResultsAsync(LatestRun.Id);
                foreach (var r in results)
                {
                    AllotmentResults.Add(r);
                }
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RunAllotmentAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _apiClient.RunAllotmentAsync(Window.Id);
            await RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FinalizeRunAsync()
    {
        if (LatestRun is null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _apiClient.FinalizeAllotmentRunAsync(LatestRun.Id);
            await RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OverrideResultAsync(AllotmentResultDto result)
    {
        if (string.IsNullOrWhiteSpace(OverrideReason))
        {
            ErrorMessage = "A reason is required to override an allotment.";
            return;
        }
        if (!int.TryParse(OverrideSubjectId, out var newSubjectId))
        {
            ErrorMessage = "Enter a valid replacement subject ID.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _apiClient.OverrideAllotmentResultAsync(result.Id, newSubjectId, OverrideReason);
            OverrideReason = "";
            OverrideSubjectId = "";
            await RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelResultAsync(AllotmentResultDto result)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _apiClient.CancelAllotmentResultAsync(result.Id);
            await RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenWindowAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            Window = await _apiClient.OpenPreferenceWindowAsync(Window.Id);
            OnPropertyChanged(nameof(CanOpen));
            OnPropertyChanged(nameof(CanClose));
            OnPropertyChanged(nameof(CanRunAllotment));
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CloseWindowAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            Window = await _apiClient.ClosePreferenceWindowAsync(Window.Id);
            OnPropertyChanged(nameof(CanOpen));
            OnPropertyChanged(nameof(CanClose));
            OnPropertyChanged(nameof(CanRunAllotment));
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task InvalidateSubmissionAsync(PreferenceSubmissionDto submission)
    {
        if (string.IsNullOrWhiteSpace(InvalidateReason))
        {
            ErrorMessage = "A reason is required to invalidate a submission.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _apiClient.InvalidateSubmissionAsync(submission.Id, InvalidateReason);
            InvalidateReason = "";
            await RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReopenSubmissionAsync(PreferenceSubmissionDto submission)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _apiClient.ReopenSubmissionAsync(submission.Id);
            await RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
