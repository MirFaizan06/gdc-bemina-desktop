namespace CollegeAdmin.Contracts.Api;

public sealed record BanResult
{
    public bool Banned { get; init; }
    public int RevokedSessions { get; init; }
}

public sealed record ForceLogoutResult
{
    public int RevokedSessions { get; init; }
}
