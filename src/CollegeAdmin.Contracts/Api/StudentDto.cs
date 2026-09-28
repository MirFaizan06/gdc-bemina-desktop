namespace CollegeAdmin.Contracts.Api;

/// <summary>
/// Phase 2 — mirrors StudentsController::SAFE_COLUMNS exactly. Deliberately does NOT carry
/// passwordHash/mustChangePassword/failedAttempts/lockedUntil — the server never sends them (see
/// the controller's own remarks on the legacy CSV export's PII leak this fixes), so there is
/// nothing for this DTO to (fail to) protect either.
///
/// Slice 1 (docs/claude/phase2/PHASE2_IMPLEMENTATION_PLAN.md): BoardRegNo/TypeOfCourse/Semester
/// became nullable server-side (student_draft_support_migration.sql) so a Draft record can exist
/// with almost nothing filled in — Semester in particular MUST be int? here, not int: System.Text.Json
/// throws on a JSON null into a non-nullable value-type property, which a real Draft record's
/// response would trigger immediately were this still `int`.
/// </summary>
public sealed record StudentDto
{
    public int Id { get; init; }
    public string? BoardRegNo { get; init; }
    public string? UnivRegNo { get; init; }
    public string? RollNo { get; init; }
    public string? Name { get; init; }
    public string? Parentage { get; init; }
    public string? FatherName { get; init; }
    public string? MotherName { get; init; }
    public string Gender { get; init; } = "";
    public string? Dob { get; init; }
    public string? Address { get; init; }
    public string? District { get; init; }
    public string? Tehsil { get; init; }
    public string? Category { get; init; }
    public string? AdmissionCategory { get; init; }
    public string? IncomeClass { get; init; }
    public string? Community { get; init; }
    public bool IsOrphan { get; init; }
    public string? Mobile { get; init; }
    public string? Email { get; init; }
    public string? TypeOfCourse { get; init; }
    public int? ProgrammeId { get; init; }
    public int? Semester { get; init; }
    public int? AcademicYear { get; init; }
    public string? Major1Name { get; init; }
    public string? Major1Dept { get; init; }
    public string? MinorName { get; init; }
    public string? Status { get; init; }
    public string? IntakeStatus { get; init; }
    public string Lifecycle { get; init; } = "active";
    public string? Notes { get; init; }
    public string? CreatedAt { get; init; }
    public string? UpdatedAt { get; init; }
}

public sealed record StudentsListResult { public IReadOnlyList<StudentDto> Students { get; init; } = []; }

public sealed record PossibleDuplicateDto
{
    public string Field { get; init; } = "";
    public StudentDto Student { get; init; } = new();
}

public sealed record StudentResult
{
    public StudentDto Student { get; init; } = new();
    public IReadOnlyList<PossibleDuplicateDto> PossibleDuplicates { get; init; } = [];
}

public sealed record StudentAddressDto
{
    public string? HouseNo { get; init; }
    public string? Locality { get; init; }
    public string? Tehsil { get; init; }
    public string? District { get; init; }
    public string? StateUt { get; init; }
    public string? PinCode { get; init; }
}

public sealed record StudentPriorEducationDto
{
    public string? Board { get; init; }
    public string? RegistrationNo { get; init; }
    public string? RollNo { get; init; }
    public decimal? Percentage { get; init; }
    public string? Stream { get; init; }
    public string? SchoolName { get; init; }
    public string? SchoolDistrict { get; init; }
    public string? SchoolStateUt { get; init; }
    public int? YearOfPassing { get; init; }
}

public sealed record StudentPriorSubjectDto
{
    public string SubjectName { get; init; } = "";
    public decimal? Marks { get; init; }
    public int SortOrder { get; init; }
}

public sealed record StudentCategoryDocumentDto
{
    public string? CertificateNumber { get; init; }
    public string? CertificateFileUrl { get; init; }
}

public sealed record StudentBankAccountDto
{
    public string? AccountHolder { get; init; }
    public string? Relationship { get; init; }
    public string? AccountNumber { get; init; }
    public string? BankName { get; init; }
    public string? Ifsc { get; init; }
    public string? BranchName { get; init; }
}

public sealed record StudentAdmissionPaymentDto
{
    public decimal? AmountPaid { get; init; }
    public string? TransactionId { get; init; }
    public string? BankReferenceNo { get; init; }
    public string? PaymentGateway { get; init; }
    public string? PaymentDate { get; init; }
}

public sealed record StudentFilesDto
{
    public string? SignatureUrl { get; init; }
    public string? PassbookUrl { get; init; }
}

/// <summary> Mirrors StudentsController::showFull()'s response — the whole canonical record in one call. </summary>
public sealed record StudentFullDto
{
    public StudentDto Student { get; init; } = new();
    public StudentAddressDto? Address { get; init; }
    public Dictionary<string, StudentPriorEducationDto> PriorEducation { get; init; } = [];
    public IReadOnlyList<StudentPriorSubjectDto> PriorSubjects { get; init; } = [];
    public StudentCategoryDocumentDto? CategoryDocument { get; init; }
    public StudentBankAccountDto? BankAccount { get; init; }
    public StudentAdmissionPaymentDto? Payment { get; init; }
    public StudentFilesDto? Files { get; init; }
}

public sealed record StudentFullResult { public StudentFullDto Student { get; init; } = new(); }

public sealed record StudentHistoryEntryDto
{
    public int Id { get; init; }
    public string Action { get; init; } = "";
    public string? Description { get; init; }
    public string? AdminName { get; init; }
    public string CreatedAt { get; init; } = "";
}

public sealed record StudentAcademicHistoryEntryDto
{
    public string? TypeOfCourse { get; init; }
    public int? Semester { get; init; }
    public int? AcademicYear { get; init; }
    public string? ArchivedAt { get; init; }
}

public sealed record StudentHistoryResult
{
    public IReadOnlyList<StudentAcademicHistoryEntryDto> AcademicHistory { get; init; } = [];
    public IReadOnlyList<StudentHistoryEntryDto> AuditLog { get; init; } = [];
}
