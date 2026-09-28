namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-3 — mirrors PrincipalController::overview()'s response
/// shape. Deliberately minimal compared to legacy's PrincipalDashboardController (15+ breakdowns);
/// real per-role dashboards are P1-14's job, this is the honest read-only surface for today.</summary>
public sealed record PrincipalOverviewResult
{
    public PrincipalOverviewStatsDto Stats { get; init; } = new();
    public PrincipalStudentStatsDto StudentStats { get; init; } = new();
    public IReadOnlyList<FacultyByDeptDto> FacultyByDept { get; init; } = [];
    public IReadOnlyList<AdminsByRoleDto> AdminsByRole { get; init; } = [];
    public IReadOnlyList<ActivityLogEntryDto> RecentActivity { get; init; } = [];
}

public sealed record PrincipalOverviewStatsDto
{
    public int DepartmentCount { get; init; }
    public int FacultyActive { get; init; }
    public int CommitteeActive { get; init; }
    public int ProgrammeCount { get; init; }
    public int DocumentCount { get; init; }
    public int NoticeTotal { get; init; }
    public int NoticePublished { get; init; }
    public int NoticeDraft { get; init; }
    public int NewsTotal { get; init; }
    public int NewsPublished { get; init; }
    public int EventsUpcoming { get; init; }
    public int EventsPast { get; init; }
    public int GalleryAlbums { get; init; }
    public int GalleryImages { get; init; }
}

public sealed record PrincipalStudentStatsDto
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Courses { get; init; }
    public int AcademicYears { get; init; }
}

public sealed record FacultyByDeptDto
{
    public string DeptName { get; init; } = "";
    public int FacultyCount { get; init; }
}

public sealed record AdminsByRoleDto
{
    public string Role { get; init; } = "";
    public int Cnt { get; init; }
}
