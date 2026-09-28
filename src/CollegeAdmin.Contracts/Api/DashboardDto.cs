namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-14 — mirrors DashboardController's response shape. Every
/// field is nullable because the server omits a domain entirely when the caller has no `.view`
/// grant for it — null here means "not shown to this admin", never "zero of this exists".</summary>
public sealed record DashboardStatsDto
{
    public int? DepartmentCount { get; init; }
    public int? FacultyCount { get; init; }
    public int? CommitteeCount { get; init; }
    public int? NoticeCount { get; init; }
    public int? UpcomingEventCount { get; init; }
    public int? TimetableEntryCount { get; init; }
    public int? ProgrammeCount { get; init; }
    public int? StudentCount { get; init; }
}

public sealed record DashboardResult { public DashboardStatsDto Stats { get; init; } = new(); }
