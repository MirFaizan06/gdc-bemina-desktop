namespace CollegeAdmin.Contracts.Api;

/// <summary>
/// Mirrors the JSON envelope every /api/v1 endpoint responds with — see
/// gdc-bemina-website-main/app/Api/ApiResponse.php and docs/claude/06_API_AND_NETWORKING.md.
/// Success responses carry <see cref="CorrelationId"/> at the top level; error responses carry
/// it nested inside <see cref="Error"/> — deliberately asymmetric to match the server exactly.
/// </summary>
public sealed record ApiEnvelope<TData>
{
    public bool Success { get; init; }
    public TData? Data { get; init; }
    public PaginationMeta? Meta { get; init; }
    public string? CorrelationId { get; init; }
    public ApiErrorPayload? Error { get; init; }
}

/// <summary>
/// Final Convergence Phase P1-5 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the server's `meta`
/// object carries page/perPage/total on list endpoints (e.g. StudentsController::index()) but an
/// empty `{}` on everything else — every field here is nullable for exactly that reason, and every
/// reader must treat a null <see cref="Total"/> as "this endpoint isn't paginated", not as zero.
/// </summary>
public sealed record PaginationMeta
{
    public int? Page { get; init; }
    public int? PerPage { get; init; }
    public int? Total { get; init; }
}

public sealed record ApiErrorPayload
{
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
    public object? Details { get; init; }
    public IReadOnlyDictionary<string, string>? FieldErrors { get; init; }
    public bool Retryable { get; init; }
    public string CorrelationId { get; init; } = "";
}
