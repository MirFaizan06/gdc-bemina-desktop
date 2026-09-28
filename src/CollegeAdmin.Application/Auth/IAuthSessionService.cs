using CollegeAdmin.Contracts.Api;

namespace CollegeAdmin.Application.Auth;

/// <summary>
/// The desktop app's session lifecycle, as ViewModels see it — login/logout/silent-restore and
/// "who is currently signed in". Implemented in CollegeAdmin.Infrastructure (AuthSessionService),
/// which also owns token storage/refresh; ViewModels depend on this interface only, never on the
/// HTTP/token-storage details behind it (docs/claude/16_CODING_STANDARDS.md: "ViewModels do not
/// directly use HTTP/SQLite").
/// </summary>
public interface IAuthSessionService
{
    bool IsAuthenticated { get; }
    AdminProfile? CurrentAdmin { get; }

    /// <summary>
    /// Raised when a silent token refresh fails (refresh token expired, revoked, or reuse was
    /// detected server-side) — the caller is no longer authenticated and must show the login
    /// screen again. Not raised by an explicit LogoutAsync() call, which is a normal, expected
    /// transition the caller already knows about.
    /// </summary>
    event EventHandler? SessionExpired;

    /// <summary>Throws CollegeAdmin.Infrastructure.Api.ApiRequestException on invalid credentials/lockout.</summary>
    Task LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Best-effort: clears local session state even if the server call fails (e.g. offline).</summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Called once at startup. Returns true if a stored refresh token was valid and the session was restored.</summary>
    Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);
}
