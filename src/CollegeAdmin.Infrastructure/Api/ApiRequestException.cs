using CollegeAdmin.Contracts.Api;

namespace CollegeAdmin.Infrastructure.Api;

/// <summary>
/// Thrown by <see cref="ApiClient"/> when the server returns a structured error envelope
/// (success:false) or the HTTP call itself fails. Carries everything the error/retry/toast
/// system (docs/claude/09_ERROR_RETRY_TOAST_SYSTEM.md) needs to map to a user-facing message —
/// that mapping is built in Stage 5, this exception just carries the data for it.
/// </summary>
public sealed class ApiRequestException : Exception
{
    public string Code { get; }
    public bool Retryable { get; }
    public string CorrelationId { get; }
    public IReadOnlyDictionary<string, string>? FieldErrors { get; }

    /// <summary>Raw `error.details` from the envelope (a System.Text.Json.JsonElement once
    /// deserialized, since its shape varies by error code — e.g. CONFLICT's array of
    /// {type, message} from TimetableController). Callers that care about a specific code's
    /// details shape parse it themselves; most errors leave this null.</summary>
    public object? Details { get; }

    public ApiRequestException(ApiErrorPayload error)
        : base(error.Message)
    {
        Code = error.Code;
        Retryable = error.Retryable;
        CorrelationId = error.CorrelationId;
        FieldErrors = error.FieldErrors;
        Details = error.Details;
    }

    public ApiRequestException(string code, string message, bool retryable, Exception inner)
        : base(message, inner)
    {
        Code = code;
        Retryable = retryable;
        CorrelationId = "";
    }
}
