namespace CollegeAdmin.Contracts.Api;

/// <summary>Mirrors BackupsController::generate()'s response — POST /api/v1/backups. Super-Admin-
/// only, download-only (see ADR/tracker for the deliberately-deferred, merge-only restore design).</summary>
public sealed record BackupGeneratedResult
{
    public string Token { get; init; } = "";
    public string Filename { get; init; } = "";
    public long SizeBytes { get; init; }
    public int ModuleCount { get; init; }
    public int FileCount { get; init; }
    public string GeneratedAt { get; init; } = "";
}

/// <summary>One row of GET /api/v1/backups — recent backup history, newest first. Token/Filename/
/// etc. are nullable because a failed generation (status "failed") never produced a real result —
/// BackupsController::index() reads them out of a possibly-null result_json.</summary>
public sealed record BackupSummaryDto
{
    public string? Token { get; init; }
    public string? Filename { get; init; }
    public long? SizeBytes { get; init; }
    public int? ModuleCount { get; init; }
    public int? FileCount { get; init; }
    public string? GeneratedAt { get; init; }
    public string Status { get; init; } = "";

    /// <summary>False once retention has deleted the underlying file (see DatabaseBackupService's
    /// "keep last 5" policy) — the row/history entry still exists, but Download would 404. UI should
    /// disable/hide the download action when this is false rather than let the user hit that 404.</summary>
    public bool FileAvailable { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed record BackupsListResult
{
    public IReadOnlyList<BackupSummaryDto> Backups { get; init; } = [];
}
