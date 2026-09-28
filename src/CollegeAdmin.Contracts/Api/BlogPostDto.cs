namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-10/§2b — mirrors BlogsController's row shape.</summary>
public sealed record BlogPostDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Slug { get; init; } = "";
    public string AuthorName { get; init; } = "";
    public string? AuthorEmail { get; init; }
    public string? Category { get; init; }
    public string? Excerpt { get; init; }
    public string Content { get; init; } = "";
    public string Status { get; init; } = "";
    public string? RejectionReason { get; init; }
    public bool IsStaffWritten { get; init; }
    public int Views { get; init; }
    public string? PublishedAt { get; init; }
    public string CreatedAt { get; init; } = "";
}

public sealed record BlogStatusCountsDto
{
    public int Pending { get; init; }
    public int Published { get; init; }
    public int Rejected { get; init; }
    public int Total { get; init; }
}

public sealed record BlogsListResult
{
    public IReadOnlyList<BlogPostDto> Posts { get; init; } = [];
    public BlogStatusCountsDto Counts { get; init; } = new();
}

public sealed record BlogPostResult { public BlogPostDto Post { get; init; } = new(); }
