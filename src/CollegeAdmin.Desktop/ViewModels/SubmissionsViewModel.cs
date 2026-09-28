using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the desktop
/// consumer for the new SubmissionsController — Contact/Grievance/Alumni admin handling. Public
/// submission forms stay on the legacy website (tracker's own "Preserve" note). One ViewModel
/// covers all three (Contact/Grievance/Alumni), each with its own list and its own real per-row
/// actions (Reply/Forward/Read/Delete, +Status for grievances, Showcase for alumni) rather than the
/// generic Edit/Delete shape — the server enforces committee-scoped visibility (the IDOR fix), so
/// these lists simply surface whatever the server already allows for the signed-in admin.
/// </summary>
public sealed partial class SubmissionsViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public SubmissionsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadContactAsync();
        _ = LoadGrievancesAsync();
        _ = LoadAlumniAsync();
    }

    public ObservableCollection<ContactMessageDto> Contact { get; } = [];
    public ObservableCollection<GrievanceDto> Grievances { get; } = [];
    public ObservableCollection<AlumniDto> Alumni { get; } = [];

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Lets the View's code-behind populate the Forward-to-committee dialog without
    /// reaching into DI itself — every other View in this app only ever talks to its own
    /// DataContext ViewModel, this keeps that consistent.</summary>
    public Task<IReadOnlyList<CommitteeDto>> GetCommitteeOptionsAsync() => _apiClient.GetCommitteesAsync();

    // ── Contact ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadContactAsync()
    {
        try
        {
            var rows = await _apiClient.GetContactMessagesAsync();
            Contact.Clear();
            foreach (var row in rows)
            {
                Contact.Add(row);
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task MarkContactReadAsync(ContactMessageDto message)
    {
        try
        {
            await _apiClient.MarkContactReadAsync(message.Id);
            await LoadContactAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>Called by the View's code-behind after the reply-text dialog is confirmed.</summary>
    public async Task ReplyContactAsync(ContactMessageDto message, string replyBody)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.ReplyContactAsync(message.Id, replyBody);
            StatusMessage = $"Reply sent to {message.Name}.";
            await LoadContactAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>Called by the View's code-behind after the forward-to-committee dialog is confirmed.</summary>
    public async Task ForwardContactAsync(ContactMessageDto message, int committeeId, string? note)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.ForwardContactAsync(message.Id, committeeId, note);
            StatusMessage = $"Forwarded {message.Name}'s message.";
            await LoadContactAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteContactAsync(ContactMessageDto message)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.DeleteContactAsync(message.Id);
            await LoadContactAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // ── Grievances ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadGrievancesAsync()
    {
        try
        {
            var rows = await _apiClient.GetGrievancesAsync();
            Grievances.Clear();
            foreach (var row in rows)
            {
                Grievances.Add(row);
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task MarkGrievanceReadAsync(GrievanceDto grievance)
    {
        try
        {
            await _apiClient.MarkGrievanceReadAsync(grievance.Id);
            await LoadGrievancesAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task ReplyGrievanceAsync(GrievanceDto grievance, string replyBody)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.ReplyGrievanceAsync(grievance.Id, replyBody);
            StatusMessage = $"Reply sent to {grievance.Name}.";
            await LoadGrievancesAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task ForwardGrievanceAsync(GrievanceDto grievance, int committeeId, string? note)
    {
        ErrorMessage = null;
        try
        {
            await _apiClient.ForwardGrievanceAsync(grievance.Id, committeeId, note);
            StatusMessage = $"Forwarded {grievance.Name}'s grievance.";
            await LoadGrievancesAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>Cycles pending -> in_review -> resolved -> pending, a simple one-click progression
    /// rather than a picker — matches this screen's overall "functional, not fancy" bar.</summary>
    [RelayCommand]
    private async Task AdvanceGrievanceStatusAsync(GrievanceDto grievance)
    {
        var next = grievance.Status switch
        {
            "pending" => "in_review",
            "in_review" => "resolved",
            _ => "pending",
        };
        ErrorMessage = null;
        try
        {
            await _apiClient.UpdateGrievanceStatusAsync(grievance.Id, next);
            await LoadGrievancesAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // ── Alumni ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadAlumniAsync()
    {
        try
        {
            var rows = await _apiClient.GetAlumniAsync();
            Alumni.Clear();
            foreach (var row in rows)
            {
                Alumni.Add(row);
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task MarkAlumnusReadAsync(AlumniDto alumnus)
    {
        try
        {
            await _apiClient.MarkAlumnusReadAsync(alumnus.Id);
            await LoadAlumniAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ToggleAlumnusShowcaseAsync(AlumniDto alumnus)
    {
        try
        {
            await _apiClient.ToggleAlumnusShowcaseAsync(alumnus.Id);
            await LoadAlumniAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAlumnusAsync(AlumniDto alumnus)
    {
        try
        {
            await _apiClient.DeleteAlumnusAsync(alumnus.Id);
            await LoadAlumniAsync();
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
