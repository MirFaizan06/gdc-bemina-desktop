namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-10/§2b — mirrors PyqsController's row shape.</summary>
public sealed record PyqDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Subject { get; init; } = "";
    public string? Department { get; init; }
    public string? Programme { get; init; }
    public int Semester { get; init; }
    public string Type { get; init; } = "";
    public string? SubjectType { get; init; }
    public string? CourseType { get; init; }
    public string Year { get; init; } = "";
    public string FilePath { get; init; } = "";
    public string FileType { get; init; } = "";
    public string Status { get; init; } = "";
    public string? RejectionReason { get; init; }
    public string? RejectedAt { get; init; }
    public bool IsStaffUploaded { get; init; }
    public string? ContributorName { get; init; }
    public string? ContributorRoll { get; init; }
    public string? ContributorEmail { get; init; }
    public int? ContributorBatch { get; init; }
    public int Views { get; init; }
    public int Downloads { get; init; }
    public string CreatedAt { get; init; } = "";
}

public sealed record PyqStatusCountsDto
{
    public int All { get; init; }
    public int Pending { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Flagged { get; init; }
}

public sealed record PyqsListResult
{
    public IReadOnlyList<PyqDto> Papers { get; init; } = [];
    public PyqStatusCountsDto Counts { get; init; } = new();
}

public sealed record PyqResult { public PyqDto Paper { get; init; } = new(); }
