namespace CollegeAdmin.Contracts.Api;

/// <summary>Mirrors AdminsController::index()'s row shape (a snake_case DB row, not presentAdmin()'s camelCase DTO).</summary>
public sealed record AdminSummary
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public bool IsActive { get; init; }
    public bool IsBanned { get; init; }
    public string? BannedReason { get; init; }
    public string? TimeoutUntil { get; init; }
    public string? LastLoginAt { get; init; }
}

public sealed record AdminsListResult
{
    public IReadOnlyList<AdminSummary> Admins { get; init; } = [];
}
