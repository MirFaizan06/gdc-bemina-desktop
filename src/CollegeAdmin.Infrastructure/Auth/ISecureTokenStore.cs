namespace CollegeAdmin.Infrastructure.Auth;

/// <summary>Persists the refresh token only — the access token is short-lived and kept in memory only.</summary>
public interface ISecureTokenStore
{
    void SaveRefreshToken(string refreshToken);

    /// <summary>Returns null if nothing is stored, or if what's stored can't be decrypted (see DpapiTokenStore).</summary>
    string? LoadRefreshToken();

    void Clear();
}
