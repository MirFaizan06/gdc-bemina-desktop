using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using CollegeAdmin.Application.Caching;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Stage 14's Create form. The server does real, blocking faculty/room/class-section conflict
/// detection (TimetableEntry::conflictsFor) — this form's job is to surface a CONFLICT response
/// clearly (which existing entry it clashes with), not to re-implement the conflict rule
/// client-side; the server is the single source of truth for "is this slot free" per the Source of
/// Truth rule in CLAUDE.md.
/// </summary>
public sealed partial class TimetableEntryCreateViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly ILookupCache _lookupCache;
    private int? _editingId;

    public TimetableEntryCreateViewModel(IApiClient apiClient, ILookupCache lookupCache)
    {
        _apiClient = apiClient;
        _lookupCache = lookupCache;
    }

    [ObservableProperty]
    private string _windowTitle = "New Timetable Entry";

    /// <summary>Final Convergence Phase P0-4: replaces the raw numeric "Programme ID" textbox with
    /// a real picker. NavigationService awaits LoadProgrammeOptionsAsync() before ever showing this
    /// dialog, so the ComboBox is already populated by the time the window appears.</summary>
    public ObservableCollection<FormFieldOption> ProgrammeOptions { get; } = [];

    public async Task LoadProgrammeOptionsAsync()
    {
        var programmes = await _lookupCache.GetOrFetchAsync("programmes", ct => _apiClient.GetProgrammesAsync(ct));
        ProgrammeOptions.Clear();
        foreach (var p in programmes)
        {
            ProgrammeOptions.Add(new FormFieldOption(p.Id.ToString(), $"{p.Name} ({p.DepartmentName})"));
        }
    }

    public string[] Days { get; } = ["mon", "tue", "wed", "thu", "fri", "sat"];

    public string[] ScopeOptions { get; } = ["General", "Department", "Committee"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDepartmentScope), nameof(IsCommitteeScope))]
    private string _selectedScope = "General";

    public bool IsDepartmentScope => SelectedScope == "Department";
    public bool IsCommitteeScope => SelectedScope == "Committee";

    [ObservableProperty]
    private string _departmentId = "";

    [ObservableProperty]
    private string _committeeId = "";

    /// <summary>Switches this form into edit mode, pre-filled from a fresh GET of the record. Save
    /// then calls UpdateGenericAsync against TimetableController::update() — which re-runs the same
    /// blocking conflict check as create (excluding this entry itself), so DescribeConflict below
    /// already handles a CONFLICT response from an edit exactly like it does from a create.</summary>
    public void InitializeForEdit(int id, IReadOnlyDictionary<string, string?> fields)
    {
        _editingId = id;
        WindowTitle = "Edit Timetable Entry";
        AcademicYear = fields.GetValueOrDefault("academicYear") ?? "";
        ProgrammeId = fields.GetValueOrDefault("programmeId") ?? "";
        Semester = fields.GetValueOrDefault("semester") ?? "";
        Section = fields.GetValueOrDefault("section") ?? "";
        Subject = fields.GetValueOrDefault("subject") ?? "";
        FacultyId = fields.GetValueOrDefault("facultyId") ?? "";
        Room = fields.GetValueOrDefault("room") ?? "";
        SelectedDay = fields.GetValueOrDefault("dayOfWeek") ?? "mon";
        StartTime = TrimToHoursMinutes(fields.GetValueOrDefault("startTime"));
        EndTime = TrimToHoursMinutes(fields.GetValueOrDefault("endTime"));
        DepartmentId = fields.GetValueOrDefault("departmentId") ?? "";
        CommitteeId = fields.GetValueOrDefault("committeeId") ?? "";
        SelectedScope = DepartmentId.Length > 0 ? "Department" : CommitteeId.Length > 0 ? "Committee" : "General";
        SaveCommand.NotifyCanExecuteChanged();
    }

    // The server stores TIME columns as HH:MM:SS; the form's validation regex is HH:MM only.
    private static string TrimToHoursMinutes(string? value) =>
        value is { Length: >= 5 } ? value[..5] : value ?? "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _academicYear = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _programmeId = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _semester = "";

    [ObservableProperty]
    private string _section = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _subject = "";

    [ObservableProperty]
    private string _facultyId = "";

    [ObservableProperty]
    private string _room = "";

    [ObservableProperty]
    private string _selectedDay = "mon";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _startTime = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _endTime = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public event EventHandler? Saved;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            int? deptId = IsDepartmentScope && int.TryParse(DepartmentId, out var d) ? d : null;
            int? commId = IsCommitteeScope && int.TryParse(CommitteeId, out var c) ? c : null;

            if (_editingId is int id)
            {
                var fields = new Dictionary<string, string?>
                {
                    ["academicYear"] = AcademicYear.Trim(),
                    ["programmeId"] = ProgrammeId,
                    ["semester"] = Semester,
                    ["section"] = string.IsNullOrWhiteSpace(Section) ? null : Section.Trim(),
                    ["subject"] = Subject.Trim(),
                    ["facultyId"] = string.IsNullOrWhiteSpace(FacultyId) ? null : FacultyId,
                    ["room"] = string.IsNullOrWhiteSpace(Room) ? null : Room.Trim(),
                    ["dayOfWeek"] = SelectedDay,
                    ["startTime"] = StartTime.Trim(),
                    ["endTime"] = EndTime.Trim(),
                    ["departmentId"] = deptId?.ToString(),
                    ["committeeId"] = commId?.ToString(),
                };
                await _apiClient.UpdateGenericAsync("timetable", id, fields);
            }
            else
            {
                await _apiClient.CreateTimetableEntryAsync(
                    AcademicYear.Trim(),
                    int.Parse(ProgrammeId),
                    int.Parse(Semester),
                    string.IsNullOrWhiteSpace(Section) ? null : Section.Trim(),
                    Subject.Trim(),
                    int.TryParse(FacultyId, out var f) ? f : null,
                    string.IsNullOrWhiteSpace(Room) ? null : Room.Trim(),
                    SelectedDay,
                    StartTime.Trim(),
                    EndTime.Trim(),
                    deptId,
                    commId);
            }

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Code == "CONFLICT" ? DescribeConflict(ex) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>CONFLICT's `details` is an array of {type, message} from
    /// TimetableController::describeConflicts() — join every reason so the admin sees exactly
    /// which existing entry (faculty/room/section) blocked the save, not just "conflict".</summary>
    private static string DescribeConflict(ApiRequestException ex)
    {
        if (ex.Details is not JsonElement details || details.ValueKind != JsonValueKind.Array)
        {
            return ex.Message;
        }

        var messages = new List<string>();
        foreach (var item in details.EnumerateArray())
        {
            if (item.TryGetProperty("message", out var messageProp))
            {
                messages.Add(messageProp.GetString() ?? "");
            }
        }

        return messages.Count > 0 ? string.Join(" ", messages) : ex.Message;
    }

    private bool CanSave() =>
        !IsBusy
        && int.TryParse(ProgrammeId, out _)
        && int.TryParse(Semester, out _)
        && AcademicYear.Trim().Length > 0
        && Subject.Trim().Length > 0
        && System.Text.RegularExpressions.Regex.IsMatch(StartTime.Trim(), @"^\d{2}:\d{2}$")
        && System.Text.RegularExpressions.Regex.IsMatch(EndTime.Trim(), @"^\d{2}:\d{2}$");
}
