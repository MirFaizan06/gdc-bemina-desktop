namespace CollegeAdmin.Contracts.Api;

/// <summary>Mirrors preference_windows (docs/claude/phase2/06_PREFERENCE_SYSTEM.md, Slice 3).</summary>
public sealed record PreferenceWindowDto
{
    public int Id { get; init; }
    public string AcademicSession { get; init; } = "";
    public int Semester { get; init; }
    public int? ProgrammeId { get; init; }
    public string SubjectType { get; init; } = "";
    public string? OpensAt { get; init; }
    public string? ClosesAt { get; init; }
    public string Status { get; init; } = "";
    public int MinChoices { get; init; }
    public int MaxChoices { get; init; }
    public int CreatedBy { get; init; }
    public int? PublishedBy { get; init; }
    public string CreatedAt { get; init; } = "";
}

public sealed record PreferenceWindowsListResult { public IReadOnlyList<PreferenceWindowDto> Windows { get; init; } = []; }
public sealed record PreferenceWindowResult { public PreferenceWindowDto Window { get; init; } = new(); }

public sealed record PreferenceWindowSummaryDto
{
    public int Eligible { get; init; }
    public int Submitted { get; init; }
    public int NotSubmitted { get; init; }
}

public sealed record PreferenceWindowSummaryResult { public PreferenceWindowSummaryDto Summary { get; init; } = new(); }

public sealed record PreferenceChoiceDto
{
    public int Id { get; init; }
    public int SubjectId { get; init; }
    public int RankOrder { get; init; }
    public string SubjectName { get; init; } = "";
    public string SubjectCode { get; init; } = "";
}

public sealed record PreferenceSubmissionDto
{
    public int Id { get; init; }
    public int StudentId { get; init; }
    public int WindowId { get; init; }
    public string Status { get; init; } = "";
    public string? SubmittedAt { get; init; }
    public string? InvalidatedReason { get; init; }
    public int Version { get; init; }
    public string StudentName { get; init; } = "";
    public string? StudentBoardRegNo { get; init; }
    public IReadOnlyList<PreferenceChoiceDto> Choices { get; init; } = [];
}

public sealed record PreferenceSubmissionsResult { public IReadOnlyList<PreferenceSubmissionDto> Submissions { get; init; } = []; }

/// <summary>Mirrors `subjects` — the shared catalog feeding both Preferences (this slice) and
/// Subject Allotments (Slice 4).</summary>
public sealed record SubjectDto
{
    public int Id { get; init; }
    public string SubjectType { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int? DepartmentId { get; init; }
    public int? Quota { get; init; }
    public string Status { get; init; } = "";

    /// <summary>Final Convergence Phase P2 item 1 — non-null means archived (soft-deleted).</summary>
    public string? ArchivedAt { get; init; }
}

public sealed record SubjectsListResult { public IReadOnlyList<SubjectDto> Subjects { get; init; } = []; }

/// <summary>Mirrors allotment_runs (docs/claude/phase2/07_ALLOTMENT_ENGINE.md, Slice 4).</summary>
public sealed record AllotmentRunDto
{
    public int Id { get; init; }
    public int PreferenceWindowId { get; init; }
    public string Status { get; init; } = "";
    public string StartedAt { get; init; } = "";
    public string? CompletedAt { get; init; }
    public string? FinalizedAt { get; init; }
}

public sealed record AllotmentRunSummaryDto
{
    public int Processed { get; init; }
    public int Allotted { get; init; }
    public int Unallotted { get; init; }
}

public sealed record RunAllotmentResult
{
    public int RunId { get; init; }
    public AllotmentRunSummaryDto Summary { get; init; } = new();
}

/// <summary>One row from student_subject_allotments, joined with student/subject display names —
/// mirrors AllotmentRunsController::results().</summary>
public sealed record AllotmentResultDto
{
    public int Id { get; init; }
    public int StudentId { get; init; }
    public string StudentName { get; init; } = "";
    public string? StudentBoardRegNo { get; init; }
    public int? SubjectId { get; init; }
    public string? SubjectCode { get; init; }
    public string? SubjectName { get; init; }
    public string AllotmentStatus { get; init; } = "";
    public bool IsManualOverride { get; init; }
    public string? Remarks { get; init; }
}

public sealed record AllotmentRunsListResult { public IReadOnlyList<AllotmentRunDto> Runs { get; init; } = []; }
public sealed record AllotmentResultsListResult { public IReadOnlyList<AllotmentResultDto> Results { get; init; } = []; }
