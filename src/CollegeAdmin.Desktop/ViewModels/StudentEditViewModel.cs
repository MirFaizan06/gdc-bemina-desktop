using System.Collections.ObjectModel;
using System.Linq;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// The Student Manager's dedicated Create/Edit form (docs/claude/phase2/03_STUDENT_MANAGER_UI.md) —
/// deliberately NOT built on GenericCreateViewModel/FormField's flat single-list shape, since that
/// pattern is explicitly wrong for this module: real Draft/Finalize states, sectioned fields, and a
/// soft duplicate-candidate warning that must never block a save the way the flat modules' hard
/// required-field check does.
///
/// Scope note (named, not silently skipped): this covers the core `students` row's fields only —
/// address/prior-education/bank/payment/documents (the related-table sections
/// StudentsController::saveRelated() already supports server-side) have no editing UI yet. That is a
/// deliberately separate, larger desktop slice, not started here.
/// </summary>
public sealed partial class StudentEditViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly int? _studentId;

    public StudentEditViewModel(IApiClient apiClient, StudentDto? existing = null, IReadOnlyList<FormFieldOption>? programmeOptions = null)
    {
        _apiClient = apiClient;
        _studentId = existing?.Id;
        IsEditMode = existing is not null;
        Title = existing is null ? "New Student" : $"Edit Student — {existing.Name}";
        Lifecycle = existing?.Lifecycle ?? "draft";

        Identity = new FormSection("Identity", [
            new FormField("name", "Full Name") { Value = existing?.Name ?? "" },
            new FormField("fatherName", "Father's Name") { Value = existing?.FatherName ?? "" },
            new FormField("motherName", "Mother's Name") { Value = existing?.MotherName ?? "" },
            new FormField("gender", "Gender (Male/Female/Other)") { Value = existing?.Gender ?? "" },
            new FormField("dob", "Date of Birth (YYYY-MM-DD)") { Value = existing?.Dob ?? "" },
            new FormField("boardRegNo", "Board Registration Number") { Value = existing?.BoardRegNo ?? "" },
            new FormField("univRegNo", "University Registration Number") { Value = existing?.UnivRegNo ?? "" },
            new FormField("rollNo", "Roll Number") { Value = existing?.RollNo ?? "" },
        ]);

        Contact = new FormSection("Contact", [
            new FormField("mobile", "Mobile") { Value = existing?.Mobile ?? "" },
            new FormField("email", "Email") { Value = existing?.Email ?? "" },
        ]);

        Academic = new FormSection("Academic", [
            new FormField("typeOfCourse", "Course (BA/BSC/BCOM/BCA/BBA/MA/MSC/MCOM/MBA/Other)") { Value = existing?.TypeOfCourse ?? "" },
            // Final Convergence Phase P1-19 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): StudentDto
            // has carried ProgrammeId since P0-4, but this form had zero UI for it — named and
            // deliberately deferred at the time, not forgotten. Reuses the same FormField.Options
            // ComboBox capability P0-4 built for Timetable/Preference Windows/RR export, rather than
            // inventing a second picker mechanism.
            new FormField("programmeId", "Programme", options: programmeOptions) { Value = existing?.ProgrammeId?.ToString() ?? "" },
            new FormField("semester", "Semester") { Value = existing?.Semester?.ToString() ?? "" },
            new FormField("academicYear", "Academic Year (e.g. 2027)") { Value = existing?.AcademicYear?.ToString() ?? "" },
        ]);

        Category = new FormSection("Category", [
            new FormField("category", "Social / Claimed Category") { Value = existing?.Category ?? "" },
            new FormField("admissionCategory", "Admission Category") { Value = existing?.AdmissionCategory ?? "" },
            new FormField("community", "Religion / Community") { Value = existing?.Community ?? "" },
            new FormField("incomeClass", "Income Class") { Value = existing?.IncomeClass ?? "" },
        ]);

        Notes = new FormSection("Notes", [
            new FormField("notes", "Notes", isMultiline: true) { Value = existing?.Notes ?? "" },
        ]);

        Sections = [Identity, Contact, Academic, Category, Notes];
    }

    public string Title { get; }
    public bool IsEditMode { get; }
    public IReadOnlyList<FormSection> Sections { get; }
    private FormSection Identity { get; }
    private FormSection Contact { get; }
    private FormSection Academic { get; }
    private FormSection Category { get; }
    private FormSection Notes { get; }

    private string _lifecycle = "draft";

    /// <summary>Manually raises PropertyChanged for the two derived properties below — matches
    /// GenericListViewModel's established pattern (manual OnPropertyChanged after a plain field
    /// write) rather than relying on [ObservableProperty]'s own change notification, which only
    /// covers the field itself, not IsDraft/CanArchive.</summary>
    public string Lifecycle
    {
        get => _lifecycle;
        private set
        {
            _lifecycle = value;
            OnPropertyChanged(nameof(Lifecycle));
            OnPropertyChanged(nameof(IsDraft));
            OnPropertyChanged(nameof(CanArchive));
        }
    }

    public bool IsDraft => Lifecycle == "draft";
    // Archive only makes sense once the record actually exists — never offered mid-create.
    public bool CanArchive => IsEditMode && Lifecycle != "archived";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ObservableCollection<string> _duplicateWarnings = [];

    public event EventHandler? Saved;
    public event EventHandler? Archived;

    private Dictionary<string, object?> CollectFields() =>
        Sections.SelectMany(s => s.Fields)
            .ToDictionary(f => f.Key, f => string.IsNullOrWhiteSpace(f.Value) ? (object?)null : f.Value);

    [RelayCommand]
    private Task SaveDraftAsync() => SubmitAsync(finalize: false);

    [RelayCommand]
    private Task FinalizeAsync() => SubmitAsync(finalize: true);

    private async Task SubmitAsync(bool finalize)
    {
        IsBusy = true;
        ErrorMessage = null;
        DuplicateWarnings.Clear();
        try
        {
            var fields = CollectFields();
            StudentResult result = _studentId is int id
                ? await _apiClient.UpdateStudentAsync(id, fields)
                : await _apiClient.CreateStudentAsync(fields, finalize);

            // An update doesn't take a `finalize` flag the way create does (see IApiClient) — an
            // edit that should also finalize needs the separate explicit FinalizeStudentAsync call,
            // matching StudentsController's own two-step design (finalize is its own audited action,
            // not folded silently into every save).
            if (finalize && _studentId is int existingId)
            {
                result = new StudentResult { Student = await _apiClient.FinalizeStudentAsync(existingId) };
            }

            Lifecycle = result.Student.Lifecycle;
            foreach (var dup in result.PossibleDuplicates)
            {
                DuplicateWarnings.Add($"Possible duplicate ({dup.Field}): {dup.Student.Name} (#{dup.Student.Id})");
            }

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 }
                ? string.Join(" ", ex.FieldErrors.Values)
                : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ArchiveAsync()
    {
        if (_studentId is not int id)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var updated = await _apiClient.ArchiveStudentAsync(id);
            Lifecycle = updated.Lifecycle;
            Archived?.Invoke(this, EventArgs.Empty);
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
