namespace CollegeAdmin.Contracts.Api;

public sealed record NoticeDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string? Body { get; init; }
    public string? Link { get; init; }
    public string? Category { get; init; }
    public bool IsMarquee { get; init; }
    public string PublishAt { get; init; } = "";
    public string? ExpireAt { get; init; }
    public int? DepartmentId { get; init; }
    public int? CommitteeId { get; init; }
    public string Status { get; init; } = "";
}
