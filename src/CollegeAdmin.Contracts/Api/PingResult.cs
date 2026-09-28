namespace CollegeAdmin.Contracts.Api;

/// <summary>Response shape of GET /api/v1/ping — the Stage 1 API exit-criterion endpoint.</summary>
public sealed record PingResult
{
    public string Status { get; init; } = "";
    public string Time { get; init; } = "";
    public string ApiVersion { get; init; } = "";
}
