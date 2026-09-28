namespace CollegeAdmin.Contracts.Api;

/// <summary>Mirrors UniversityRrController::preview()/export() — docs/claude/phase2/
/// University_RR_Export_Claude_Addendum.md, Slice 5.</summary>
public sealed record UniversityRrTotalsDto
{
    public int Total { get; init; }
    public int Ready { get; init; }
    public int Warning { get; init; }
    public int Blocked { get; init; }
}

public sealed record UniversityRrPreviewRowDto
{
    public int StudentId { get; init; }
    public string StudentName { get; init; } = "";
    public IReadOnlyList<string> Row { get; init; } = [];
    public string Status { get; init; } = "";
    public IReadOnlyList<string> Issues { get; init; } = [];
}

public sealed record UniversityRrPreviewResult
{
    public IReadOnlyList<string> Headers { get; init; } = [];
    public IReadOnlyList<UniversityRrPreviewRowDto> Rows { get; init; } = [];
    public UniversityRrTotalsDto Totals { get; init; } = new();

    /// <summary>Final Convergence Phase P0-2 — false until a human verifies the 4 ADR-019 field
    /// mappings (UniversityRrMapper::MAPPING_VERIFIED) against real university RR data.</summary>
    public bool MappingVerified { get; init; }
}

public sealed record UniversityRrExportResult
{
    public string Filename { get; init; } = "";
    public string FileBase64 { get; init; } = "";
    public UniversityRrTotalsDto Totals { get; init; } = new();
    public bool MappingVerified { get; init; }
}
