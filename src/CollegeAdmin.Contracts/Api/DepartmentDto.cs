namespace CollegeAdmin.Contracts.Api;

public sealed record DepartmentDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? Description { get; init; }
    public string Status { get; init; } = "";

    /// <summary>Final Convergence Phase P1-2 — previously unsettable through the new API/desktop at
    /// all; changing a department's HOD required the legacy admin panel.</summary>
    public int? HodFacultyId { get; init; }

    /// <summary>Final Convergence Phase P2 item 1 — non-null means archived (soft-deleted).</summary>
    public string? ArchivedAt { get; init; }
}

public sealed record DepartmentsListResult
{
    public IReadOnlyList<DepartmentDto> Departments { get; init; } = [];
}
