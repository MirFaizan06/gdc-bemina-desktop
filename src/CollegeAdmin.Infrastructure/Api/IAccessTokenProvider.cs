namespace CollegeAdmin.Infrastructure.Api;

/// <summary>
/// What <see cref="AuthHeaderHandler"/> needs to attach an Authorization header — nothing more.
/// Implemented by AuthSessionService, but kept as this narrow interface (rather than having the
/// handler depend on AuthSessionService/IAuthSessionService directly) so IApiClient's DI
/// registration never needs to know AuthSessionService exists, and vice versa — see
/// AuthSessionService's class remarks for the circular-dependency this specifically avoids.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Returns a currently-valid access token, transparently refreshing it first if it's expired
    /// or about to expire. Returns null if there is no session (not logged in, or the session
    /// could not be silently refreshed) — callers then send the request unauthenticated, which
    /// the server will reject with 401 for any route that requires 'auth'.
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
