namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P0-7 — mirrors UploadsController::store()'s response shape.</summary>
public sealed record UploadResult
{
    public string Path { get; init; } = "";
}
