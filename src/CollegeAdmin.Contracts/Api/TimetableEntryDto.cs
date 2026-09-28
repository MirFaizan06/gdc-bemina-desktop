namespace CollegeAdmin.Contracts.Api;

public sealed record TimetableEntryDto
{
    public int Id { get; init; }
    public string AcademicYear { get; init; } = "";
    public int ProgrammeId { get; init; }
    public int Semester { get; init; }
    public string? Section { get; init; }
    public string Subject { get; init; } = "";
    public int? FacultyId { get; init; }
    public string? Room { get; init; }
    public string DayOfWeek { get; init; } = "";
    public string StartTime { get; init; } = "";
    public string EndTime { get; init; } = "";
    public string Status { get; init; } = "";
    public int? DepartmentId { get; init; }
    public int? CommitteeId { get; init; }
}

public sealed record TimetableEntriesListResult { public IReadOnlyList<TimetableEntryDto> Entries { get; init; } = []; }
public sealed record TimetableEntryResult { public TimetableEntryDto Entry { get; init; } = new(); }
