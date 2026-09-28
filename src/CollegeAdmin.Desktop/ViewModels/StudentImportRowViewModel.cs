using System.Text.Json;
using CollegeAdmin.Contracts.Api;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Wraps one staged row (StudentImportRowDto) for the Review grid — pulls the handful of core
/// fields an admin can quick-fix (Name/Application No/Course/Semester) out of the row's freeform
/// `Normalized` JsonElement, and exposes them as editable bound properties. Only these four are
/// editable here, not the full 68-field record — a deliberately smaller surface than
/// StudentEditWindow's, matching what actually needs correcting during import review (missing/typo'd
/// identity or academic fields), not full data entry.
/// </summary>
public sealed partial class StudentImportRowViewModel : ObservableObject
{
    public StudentImportRowViewModel(StudentImportRowDto dto)
    {
        Id = dto.Id;
        RowNo = dto.RowNo;
        ApplyServerResult(dto);

        _name = ReadString(dto.Normalized, "name") ?? "";
        _boardRegNo = ReadString(dto.Normalized, "board_reg_no") ?? "";
        _typeOfCourse = ReadString(dto.Normalized, "type_of_course") ?? "";
        _semester = ReadNumberAsString(dto.Normalized, "semester") ?? "";
    }

    public int Id { get; }
    public int RowNo { get; }

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private string _issues = "";

    [ObservableProperty]
    private string _resolution = "pending";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _boardRegNo = "";

    [ObservableProperty]
    private string _typeOfCourse = "";

    [ObservableProperty]
    private string _semester = "";

    /// <summary>Re-syncs Status/Issues/Resolution (and the four editable fields, in case the edit
    /// itself changed one of them) from a fresh server response — called after upload and after
    /// every save/revalidate, so the grid never shows stale classification.</summary>
    public void ApplyServerResult(StudentImportRowDto dto)
    {
        Status = dto.Validation.Status;
        Issues = string.Join(" ", dto.Validation.Issues);
        Resolution = dto.Resolution;
    }

    private static string? ReadString(JsonElement root, string property)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(property, out var value))
        {
            return null;
        }
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static string? ReadNumberAsString(JsonElement root, string property)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(property, out var value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.String => value.GetString(),
            _ => null,
        };
    }
}
