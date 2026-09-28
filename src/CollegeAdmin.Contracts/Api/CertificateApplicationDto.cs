namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-10/§2b — mirrors CertificationsController's row shapes.</summary>
public sealed record CertificateApplicationDto
{
    public int Id { get; init; }
    public int CertificateTypeId { get; init; }
    public int StudentId { get; init; }
    public string Purpose { get; init; } = "";
    public string? PhotoPath { get; init; }
    public string Status { get; init; } = "";
    public string? CertificateNumber { get; init; }
    public string? RejectionReason { get; init; }
    public int? ReviewedBy { get; init; }
    public string? ReviewedAt { get; init; }
    public string CreatedAt { get; init; } = "";
    public string TypeCode { get; init; } = "";
    public string TypeName { get; init; } = "";
    public string? StudentName { get; init; }
    public string? BoardRegNo { get; init; }
    public string? RollNo { get; init; }
    public int? Semester { get; init; }
    public string? TypeOfCourse { get; init; }
    public string? ReviewerName { get; init; }
}

public sealed record CertificateTypeDto
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Prefix { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int NumberOffset { get; init; }
}

public sealed record CertificationStatsDto
{
    public int Total { get; init; }
    public int Pending { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
}

public sealed record CertificationsListResult
{
    public IReadOnlyList<CertificateApplicationDto> Applications { get; init; } = [];
    public CertificationStatsDto Stats { get; init; } = new();
    public IReadOnlyList<CertificateTypeDto> Types { get; init; } = [];
}

public sealed record CertificationApplicationResult { public CertificateApplicationDto Application { get; init; } = new(); }

public sealed record CertificateTypesResult { public IReadOnlyList<CertificateTypeDto> Types { get; init; } = []; }
