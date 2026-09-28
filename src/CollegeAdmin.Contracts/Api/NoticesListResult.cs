namespace CollegeAdmin.Contracts.Api;

public sealed record NoticesListResult
{
    public IReadOnlyList<NoticeDto> Notices { get; init; } = [];
}

public sealed record NoticeResult
{
    public NoticeDto Notice { get; init; } = new();
}
