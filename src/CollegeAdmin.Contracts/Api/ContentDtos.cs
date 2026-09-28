namespace CollegeAdmin.Contracts.Api;

// Row DTOs for the Stage 10 content modules' generic list screens. Deliberately not exhaustive of
// every server column — just enough for a useful list view; add fields if a Create/Edit form later
// needs them.

public sealed record NewsDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? Excerpt { get; init; }
    public string PublishedAt { get; init; } = "";
    public string Status { get; init; } = "";
}
public sealed record NewsListResult { public IReadOnlyList<NewsDto> NewsItems { get; init; } = []; }

public sealed record BannerDto
{
    public int Id { get; init; }
    public string? Title { get; init; }
    public string MediaPath { get; init; } = "";
    public string MediaType { get; init; } = "";
    public int SortOrder { get; init; }
    public string Status { get; init; } = "";
}
public sealed record BannersListResult { public IReadOnlyList<BannerDto> Banners { get; init; } = []; }

public sealed record FaqDto
{
    public int Id { get; init; }
    public string Question { get; init; } = "";
    public string Answer { get; init; } = "";
    public string Status { get; init; } = "";
}
public sealed record FaqsListResult { public IReadOnlyList<FaqDto> Faqs { get; init; } = []; }

public sealed record DocumentDto
{
    public int Id { get; init; }
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string? AcademicYear { get; init; }
    public string Status { get; init; } = "";
}
public sealed record DocumentsListResult { public IReadOnlyList<DocumentDto> Documents { get; init; } = []; }

public sealed record EventDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string EventDate { get; init; } = "";
    public string? Location { get; init; }
    public int? DepartmentId { get; init; }
    public string Status { get; init; } = "";
}
public sealed record EventsListResult { public IReadOnlyList<EventDto> Events { get; init; } = []; }

public sealed record GalleryAlbumDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Slug { get; init; } = "";
    public int? DepartmentId { get; init; }
    public string Status { get; init; } = "";
}
public sealed record GalleryAlbumsListResult { public IReadOnlyList<GalleryAlbumDto> Albums { get; init; } = []; }

public sealed record GalleryImageDto
{
    public int Id { get; init; }
    public int AlbumId { get; init; }
    public string ImagePath { get; init; } = "";
    public string? Caption { get; init; }
    public int SortOrder { get; init; }
}
public sealed record GalleryImagesResult { public IReadOnlyList<GalleryImageDto> Images { get; init; } = []; }

public sealed record BackgroundJobDto
{
    public int Id { get; init; }
    public string Type { get; init; } = "";
    public string Status { get; init; } = "";
    public int Progress { get; init; }
    public int? CreatedBy { get; init; }
    public string CreatedAt { get; init; } = "";
    public string? StartedAt { get; init; }
    public string? FinishedAt { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }

    /// <summary>Final Convergence Phase P2 item 2 — a success-summary the server now records for
    /// tracked-but-synchronous jobs (Excel Import stage/confirm, RR Export); null for jobs that
    /// never recorded one (every job type that existed before this, and any failed job). Typed
    /// `object?` (matches ApiErrorPayload.Details' own precedent) since its shape varies by job
    /// type — deserializes to a boxed JsonElement, which renders as readable JSON text in the
    /// Jobs screen's AutoGenerateColumns DataGrid with no extra code needed.</summary>
    public object? Result { get; init; }
}
public sealed record JobsListResult { public IReadOnlyList<BackgroundJobDto> Jobs { get; init; } = []; }
public sealed record RunJobResult
{
    public IReadOnlyList<BackgroundJobDto> Jobs { get; init; } = [];
    public int Processed { get; init; }
    public int Failed { get; init; }
}

public sealed record FacultyMemberDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Designation { get; init; } = "";
    public int? DepartmentId { get; init; }
    public string Status { get; init; } = "";
    public int? LinkedFacultyId { get; init; }

    /// <summary>Final Convergence Phase P2 item 1 — non-null means archived (soft-deleted).</summary>
    public string? ArchivedAt { get; init; }
}
public sealed record FacultyListResult { public IReadOnlyList<FacultyMemberDto> Faculty { get; init; } = []; }

public sealed record CommitteeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string AcademicYear { get; init; } = "";
    public string Status { get; init; } = "";

    /// <summary>Final Convergence Phase P2 item 1 — non-null means archived (soft-deleted).</summary>
    public string? ArchivedAt { get; init; }
}
public sealed record CommitteesListResult { public IReadOnlyList<CommitteeDto> Committees { get; init; } = []; }
