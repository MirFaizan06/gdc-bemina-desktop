namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-10/§2b — mirrors AcademicCalendarController's row shape.</summary>
public sealed record CalendarEventDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string EventType { get; init; } = "";
    public string StartDate { get; init; } = "";
    public string EndDate { get; init; } = "";
    public string AcademicYear { get; init; } = "";
    public int? LinkedEventId { get; init; }
    public string Status { get; init; } = "";
    public string Source { get; init; } = "";
    public string? GoogleEventId { get; init; }
}

public sealed record CalendarEventsResult
{
    public IReadOnlyList<CalendarEventDto> Events { get; init; } = [];
    public string CurrentAcademicYear { get; init; } = "";
    public IReadOnlyList<string> AcademicYearOptions { get; init; } = [];
    public bool GoogleSyncConfigured { get; init; }
}

public sealed record CalendarSyncResult
{
    public bool Synced { get; init; }
    public string? Message { get; init; }
    public int Inserted { get; init; }
    public int Updated { get; init; }
    public IReadOnlyList<CalendarEventDto> Events { get; init; } = [];
}
