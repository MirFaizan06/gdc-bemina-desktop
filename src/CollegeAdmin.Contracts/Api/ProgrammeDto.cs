namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P0-4 — the canonical Programme/Course record, reusing the
/// legacy `programmes` table's exact field set (no second data model was introduced server-side).</summary>
public sealed record ProgrammeDto
{
    public int Id { get; init; }
    public int DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string Name { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Level { get; init; } = "";
    public int DurationYears { get; init; }
    public int DurationSemesters { get; init; }
    public int? Seats { get; init; }
    public string? AnnualFee { get; init; }
    public string? Eligibility { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public string Status { get; init; } = "";

    /// <summary>Final Convergence Phase P2 item 1 — non-null means archived (soft-deleted).</summary>
    public string? ArchivedAt { get; init; }
}

public sealed record ProgrammesListResult
{
    public IReadOnlyList<ProgrammeDto> Programmes { get; init; } = [];
}
