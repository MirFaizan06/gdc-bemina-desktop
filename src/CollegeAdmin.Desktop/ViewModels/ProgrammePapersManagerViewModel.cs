using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-20 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): `programme_papers`
/// was legacy-admin-panel-only despite already being a real, live-referenced table (the public
/// syllabus pages, the legacy Timetable form's paper picker, and the new
/// `timetable_entries.programme_paper_id` FK all point at it). A paper only ever makes sense in the
/// context of one specific programme, the same reasoning Gallery's images-under-an-album already
/// established — this mirrors GalleryImageManagerViewModel's shape rather than inventing a second
/// nested-resource pattern.
/// </summary>
public sealed partial class ProgrammePapersManagerViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly int _programmeId;

    public ProgrammePapersManagerViewModel(IApiClient apiClient, int programmeId, string programmeName)
    {
        _apiClient = apiClient;
        _programmeId = programmeId;
        ProgrammeName = programmeName;
        _ = LoadAsync();
    }

    public string ProgrammeName { get; }
    public ObservableCollection<ProgrammePaperDto> Papers { get; } = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsEmpty => !IsLoading && ErrorMessage is null && Papers.Count == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPaperCommand))]
    private string _newTitle = "";

    [ObservableProperty]
    private string _newSemester = "1";

    [ObservableProperty]
    private string _newPaperType = "Major";

    [ObservableProperty]
    private string _newPaperCode = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var papers = await _apiClient.GetProgrammePapersAsync(_programmeId);
            Papers.Clear();
            foreach (var paper in papers)
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
            OnPropertyChanged(nameof(IsEmpty));
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
                ["semester"] = NewSemester.Trim(),
                ["title"] = NewTitle.Trim(),
                ["paperType"] = NewPaperType.Trim(),
                ["paperCode"] = string.IsNullOrWhiteSpace(NewPaperCode) ? null : NewPaperCode.Trim(),
                ["status"] = "draft",
                ["sortOrder"] = Papers.Count,
            };

            var papers = await _apiClient.AddProgrammePaperAsync(_programmeId, fields);
            Papers.Clear();
            foreach (var paper in papers)
            {
                Papers.Add(paper);
            }
            NewTitle = "";
            NewPaperCode = "";
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 }
                ? string.Join(" ", ex.FieldErrors.Values)
                : ex.Message;
        }
    }

    private bool CanAddPaper() => NewTitle.Trim().Length > 0;

    [RelayCommand]
    private async Task DeletePaperAsync(ProgrammePaperDto paper)
    {
        ErrorMessage = null;
        try
        {
            var papers = await _apiClient.DeleteProgrammePaperAsync(_programmeId, paper.Id);
            Papers.Clear();
            foreach (var remaining in papers)
            {
                Papers.Add(remaining);
            }
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
