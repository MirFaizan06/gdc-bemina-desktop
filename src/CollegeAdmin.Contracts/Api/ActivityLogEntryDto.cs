namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-1 — mirrors ActivityLogController::index()'s row shape.
/// Pagination metadata (page/perPage/total) currently lives only in the JSON envelope's `meta`
/// object, which ApiEnvelope&lt;TData&gt; doesn't expose today — this screen fetches a generously
/// large page instead of building real page-by-page controls, matching GetStudentsAsync's own
/// existing pattern; real pagination is P1-18's job (a shared GenericListView upgrade), not
/// duplicated here.</summary>
public sealed record ActivityLogEntryDto
{
    public int Id { get; init; }
    public int? AdminId { get; init; }
    public string? AdminName { get; init; }
    public string Action { get; init; } = "";
    public string Entity { get; init; } = "";
    public int? EntityId { get; init; }
    public string? Description { get; init; }
    public string CreatedAt { get; init; } = "";
}

public sealed record ActivityLogListResult
{
    public IReadOnlyList<ActivityLogEntryDto> Entries { get; init; } = [];
}
