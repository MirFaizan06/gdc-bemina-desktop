namespace CollegeAdmin.Contracts.Api;

/// <summary>Response shapes for the /api/v1/auth/* endpoints (App\Api\Controllers\AuthController.php).</summary>
public sealed record LoginResult
{
    public AdminProfile Admin { get; init; } = new();
    public string AccessToken { get; init; } = "";
    public string AccessTokenExpiresAt { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public string RefreshTokenExpiresAt { get; init; } = "";
}

public sealed record RefreshResult
{
    public string AccessToken { get; init; } = "";
    public string AccessTokenExpiresAt { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public string RefreshTokenExpiresAt { get; init; } = "";
}

public sealed record MeResult
{
    public AdminProfile Admin { get; init; } = new();
}

public sealed record LogoutResult
{
    public bool LoggedOut { get; init; }
}

public sealed record LogoutAllResult
{
    public bool LoggedOut { get; init; }
    public int RevokedSessions { get; init; }
}
