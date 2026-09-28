using System.Globalization;
using System.Net.Http.Headers;
using CollegeAdmin.Application.Auth;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Infrastructure.Auth;

/// <summary>
/// Implements both IAuthSessionService (what ViewModels see) and IAccessTokenProvider (what
/// AuthHeaderHandler needs to attach tokens to IApiClient's business calls).
///
/// Deliberately talks to /auth/login, /auth/refresh, /auth/me, /auth/logout and /auth/logout-all
/// through its OWN HttpClient (the "AuthApi" named client, registered with no AuthHeaderHandler
/// attached) rather than through IApiClient — constructor-injecting IApiClient here would create
/// a circular dependency at DI-container-build time: IApiClient's pipeline needs
/// AuthHeaderHandler, which needs IAccessTokenProvider, which resolves to this same class. Using
/// a separate client for auth's own calls breaks that cycle entirely, and also sidesteps a
/// second, subtler bug it would otherwise cause: GetAccessTokenAsync (below) calls the refresh
/// endpoint while holding _refreshLock — if that HTTP call were routed back through
/// AuthHeaderHandler, it would call GetAccessTokenAsync again and deadlock on its own lock.
/// </summary>
public sealed class AuthSessionService : IAuthSessionService, IAccessTokenProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);

    private readonly HttpClient _authClient;
    private readonly ISecureTokenStore _tokenStore;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;
    private string? _refreshToken;

    public AuthSessionService(IHttpClientFactory httpClientFactory, ISecureTokenStore tokenStore)
    {
        _authClient = httpClientFactory.CreateClient("AuthApi");
        _tokenStore = tokenStore;
    }

    public bool IsAuthenticated { get; private set; }
    public AdminProfile? CurrentAdmin { get; private set; }
    public event EventHandler? SessionExpired;

    public async Task LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            email,
            password,
            deviceLabel = Environment.MachineName,
            clientVersion = AppVersion(),
        };

        var result = await ApiEnvelopeHttp.PostAsync<LoginResult>(_authClient, "auth/login", body, cancellationToken);

        ApplyTokens(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken, result.RefreshTokenExpiresAt);
        _tokenStore.SaveRefreshToken(result.RefreshToken);
        CurrentAdmin = result.Admin;
        IsAuthenticated = true;
    }

    public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var storedRefreshToken = _tokenStore.LoadRefreshToken();
        if (storedRefreshToken is null)
        {
            return false;
        }

        try
        {
            var refreshed = await ApiEnvelopeHttp.PostAsync<RefreshResult>(
                _authClient, "auth/refresh", new { refreshToken = storedRefreshToken }, cancellationToken);
            ApplyTokens(refreshed.AccessToken, refreshed.AccessTokenExpiresAt, refreshed.RefreshToken, refreshed.RefreshTokenExpiresAt);
            _tokenStore.SaveRefreshToken(refreshed.RefreshToken);

            var me = await ApiEnvelopeHttp.GetAsync<MeResult>(_authClient, "auth/me", cancellationToken, AttachAuthHeader);
            CurrentAdmin = me.Admin;
            IsAuthenticated = true;
            return true;
        }
        catch (ApiRequestException)
        {
            ClearLocalSession();
            return false;
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ApiEnvelopeHttp.PostAsync<LogoutResult>(_authClient, "auth/logout", null, cancellationToken, AttachAuthHeader);
        }
        catch (ApiRequestException)
        {
            // Best-effort: the user is logging out either way. If the server call failed (e.g.
            // offline, or the session was already invalid), the local state is cleared below
            // regardless — this must never leave the app appearing "logged in" with no way out.
        }

        ClearLocalSession();
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_accessToken is null)
        {
            return null;
        }

        if (DateTimeOffset.UtcNow < _accessTokenExpiresAt - RefreshSkew)
        {
            return _accessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Re-check: another call may have already refreshed while this one waited for the lock.
            if (DateTimeOffset.UtcNow < _accessTokenExpiresAt - RefreshSkew)
            {
                return _accessToken;
            }

            if (_refreshToken is null)
            {
                return null;
            }

            var refreshed = await ApiEnvelopeHttp.PostAsync<RefreshResult>(
                _authClient, "auth/refresh", new { refreshToken = _refreshToken }, cancellationToken);
            ApplyTokens(refreshed.AccessToken, refreshed.AccessTokenExpiresAt, refreshed.RefreshToken, refreshed.RefreshTokenExpiresAt);
            _tokenStore.SaveRefreshToken(refreshed.RefreshToken);

            return _accessToken;
        }
        catch (ApiRequestException)
        {
            ClearLocalSession();
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void AttachAuthHeader(HttpRequestMessage request)
    {
        if (_accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    private void ApplyTokens(string accessToken, string accessTokenExpiresAt, string refreshToken, string refreshTokenExpiresAt)
    {
        _accessToken = accessToken;
        _accessTokenExpiresAt = DateTimeOffset.Parse(accessTokenExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        _refreshToken = refreshToken;
        _ = refreshTokenExpiresAt; // not tracked client-side; the server is the source of truth for refresh expiry
    }

    private void ClearLocalSession()
    {
        _accessToken = null;
        _refreshToken = null;
        CurrentAdmin = null;
        IsAuthenticated = false;
        _tokenStore.Clear();
    }

    // ToString(3): matches the 3-part version the server's config/desktop_releases.json manifest
    // and AuthController's minimum-version check use — the assembly's own 4-part Version would
    // still compare correctly via PHP's version_compare, but 3-part keeps this consistent with
    // UpdateService's own version string everywhere else in the app.
    private static string AppVersion() =>
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
}
