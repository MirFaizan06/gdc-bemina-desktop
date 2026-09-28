namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-10/§2b — mirrors SubmissionsController's row shapes.</summary>
public sealed record ContactMessageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    public string? Subject { get; init; }
    public string Message { get; init; } = "";
    public bool IsRead { get; init; }
    public string? RepliedAt { get; init; }
    public string? ReplyBody { get; init; }
    public int? ForwardedToCommitteeId { get; init; }
    public int? ForwardedBy { get; init; }
    public string? ForwardedAt { get; init; }
    public string? ForwardNote { get; init; }
    public string CreatedAt { get; init; } = "";
}

public sealed record GrievanceDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    public string? Category { get; init; }
    public string Description { get; init; } = "";
    public string? FilePath { get; init; }
    public string Status { get; init; } = "";
    public bool IsRead { get; init; }
    public string? RepliedAt { get; init; }
    public string? ReplyBody { get; init; }
    public int? ForwardedToCommitteeId { get; init; }
    public int? ForwardedBy { get; init; }
    public string? ForwardedAt { get; init; }
    public string? ForwardNote { get; init; }
    public string CreatedAt { get; init; } = "";
}

public sealed record AlumniDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int? BatchYear { get; init; }
    public int? DepartmentId { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? CurrentOccupation { get; init; }
    public string? Message { get; init; }
    public bool IsRead { get; init; }
    public bool Showcase { get; init; }
    public string CreatedAt { get; init; } = "";
}

public sealed record ContactMessagesResult { public IReadOnlyList<ContactMessageDto> Messages { get; init; } = []; }
public sealed record ContactMessageResult { public ContactMessageDto Message { get; init; } = new(); }
public sealed record GrievancesResult { public IReadOnlyList<GrievanceDto> Grievances { get; init; } = []; }
public sealed record GrievanceResult { public GrievanceDto Grievance { get; init; } = new(); }
public sealed record AlumniListResult { public IReadOnlyList<AlumniDto> Alumni { get; init; } = []; }
public sealed record AlumnusResult { public AlumniDto Alumnus { get; init; } = new(); }
