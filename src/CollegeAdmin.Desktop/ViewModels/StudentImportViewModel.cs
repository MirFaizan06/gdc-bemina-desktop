using System.Collections.ObjectModel;
using System.IO;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// The Excel Import Pipeline's desktop workflow (docs/claude/phase2/04_EXCEL_IMPORT_PIPELINE.md,
/// PHASE2_IMPLEMENTATION_PLAN.md Slice 2): Select File -> upload+stage -> Review grid (edit/
/// exclude/revalidate) -> Confirm -> Result — one window, not nine separate screens, since the
/// underlying steps are either fully automatic (Parse/Header-Mapping/Normalize/Validate all happen
/// server-side in one upload call) or naturally belong together in one continuous review session
/// (Review/Summary/Confirm/Result).
/// </summary>
public sealed partial class StudentImportViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public StudentImportViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _resultMessage;

    [ObservableProperty]
    private StudentImportBatchDto? _batch;

    [ObservableProperty]
    private StudentImportRowViewModel? _selectedRow;

    public ObservableCollection<StudentImportRowViewModel> Rows { get; } = [];
    public ObservableCollection<string> HeaderIssues { get; } = [];

    public bool HasBatch => Batch is not null;

    [RelayCommand]
    private async Task UploadAsync(string filePath)
    {
        IsBusy = true;
        ErrorMessage = null;
        ResultMessage = null;
        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath);
            var filename = Path.GetFileName(filePath);
            var result = await _apiClient.UploadStudentImportAsync(filename, bytes);

            Batch = result.Batch;
            OnPropertyChanged(nameof(HasBatch));
            HeaderIssues.Clear();
            foreach (var issue in result.HeaderIssues)
            {
                HeaderIssues.Add(issue);
            }

            await LoadRowsAsync();
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

    private async Task LoadRowsAsync()
    {
        if (Batch is null)
        {
            return;
        }

        var rows = await _apiClient.GetStudentImportRowsAsync(Batch.Id);
        Rows.Clear();
        foreach (var row in rows)
        {
            Rows.Add(new StudentImportRowViewModel(row));
        }
    }

    /// <summary>Saves an admin's inline edit to one staged row (Name/Application No/Course/Semester
    /// and/or its include/exclude resolution) and re-syncs the row's classification from the
    /// server's fresh revalidation — never touches `students`, still staging only.</summary>
    [RelayCommand]
    private async Task SaveRowEditAsync(StudentImportRowViewModel row)
    {
        if (Batch is null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var overrides = new Dictionary<string, object?>
            {
                ["name"] = string.IsNullOrWhiteSpace(row.Name) ? null : row.Name,
                ["board_reg_no"] = string.IsNullOrWhiteSpace(row.BoardRegNo) ? null : row.BoardRegNo,
                ["type_of_course"] = string.IsNullOrWhiteSpace(row.TypeOfCourse) ? null : row.TypeOfCourse,
                ["semester"] = int.TryParse(row.Semester, out var semester) ? semester : null,
            };

            var updated = await _apiClient.UpdateStudentImportRowAsync(Batch.Id, row.Id, overrides, row.Resolution);
            row.ApplyServerResult(updated);
            await RefreshBatchSummaryAsync();
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

    private async Task RefreshBatchSummaryAsync()
    {
        if (Batch is null)
        {
            return;
        }
        Batch = await _apiClient.GetStudentImportBatchAsync(Batch.Id);
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (Batch is null)
        {
            return;
        }
        if (Batch.Status == "committed")
        {
            ErrorMessage = "This import has already been confirmed.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _apiClient.ConfirmStudentImportAsync(Batch.Id);
            ResultMessage = $"{result.Created} student(s) created, {result.Skipped} row(s) skipped.";
            await RefreshBatchSummaryAsync();
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
