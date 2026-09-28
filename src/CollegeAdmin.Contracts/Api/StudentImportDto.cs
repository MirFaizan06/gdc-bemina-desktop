using System.Text.Json;

namespace CollegeAdmin.Contracts.Api;

/// <summary>Mirrors student_import_batches — the Excel Import Pipeline's batch summary
/// (StudentImportsController). See docs/claude/phase2/04_EXCEL_IMPORT_PIPELINE.md.</summary>
public sealed record StudentImportBatchDto
{
    public int Id { get; init; }
    public string OriginalFilename { get; init; } = "";
    public string Status { get; init; } = "";
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int WarningRows { get; init; }
    public int ErrorRows { get; init; }
    public int ExcludedRows { get; init; }
    public int? CreatedCount { get; init; }
    public int? SkippedCount { get; init; }
    public string? ConfirmedAt { get; init; }
    public string CreatedAt { get; init; } = "";
}

public sealed record StudentImportValidationDto
{
    public string Status { get; init; } = "";
    public IReadOnlyList<string> Issues { get; init; } = [];
    public int? DuplicateOf { get; init; }
}

/// <summary>`Normalized`/`Raw` stay as raw JsonElement rather than a strongly-typed nested model —
/// the shape genuinely varies row to row (an unset related-table section serializes as `[]` from
/// PHP's json_encode(empty array), a populated one as a real object), so a fixed C# type would throw
/// on deserialization for perfectly valid rows. The ViewModel reads out only the handful of core
/// fields the Review grid/edit panel actually need (see StudentImportRowViewModel).</summary>
public sealed record StudentImportRowDto
{
    public int Id { get; init; }
    public int RowNo { get; init; }
    public JsonElement Normalized { get; init; }
    public StudentImportValidationDto Validation { get; init; } = new();
    public string Resolution { get; init; } = "pending";
    public int? MatchedStudentId { get; init; }
    public string Action { get; init; } = "";
}

public sealed record StudentImportBatchResult
{
    public StudentImportBatchDto Batch { get; init; } = new();
    public IReadOnlyList<string> HeaderIssues { get; init; } = [];
}

public sealed record StudentImportBatchOnlyResult { public StudentImportBatchDto Batch { get; init; } = new(); }
public sealed record StudentImportRowsResult { public IReadOnlyList<StudentImportRowDto> Rows { get; init; } = []; }
public sealed record StudentImportRowResult { public StudentImportRowDto Row { get; init; } = new(); }

public sealed record StudentImportConfirmResult
{
    public int BatchId { get; init; }
    public int Created { get; init; }
    public int Skipped { get; init; }
}
