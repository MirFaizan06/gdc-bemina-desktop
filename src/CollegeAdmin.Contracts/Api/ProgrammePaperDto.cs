namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-20 — mirrors ProgrammePapersController's row shape.</summary>
public sealed record ProgrammePaperDto
{
    public int Id { get; init; }
    public int ProgrammeId { get; init; }
    public int Semester { get; init; }
    public string? PaperCode { get; init; }
    public string Title { get; init; } = "";
    public string? ShortTitle { get; init; }
    public string PaperType { get; init; } = "";
    public int? Credits { get; init; }
    public int? TheoryHours { get; init; }
    public int? PracticalHours { get; init; }
    public int SortOrder { get; init; }
    public string Status { get; init; } = "";
}

public sealed record ProgrammePapersResult { public IReadOnlyList<ProgrammePaperDto> Papers { get; init; } = []; }
