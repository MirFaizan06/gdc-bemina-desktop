using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CollegeAdmin.Infrastructure.Auth;

/// <summary>
/// Encrypts the refresh token with Windows DPAPI (CurrentUser scope) before writing it to disk —
/// per docs/claude/10_SECURITY_MODEL.md's "protected Windows storage" requirement. DPAPI keys are
/// derived from the Windows user's own credentials, so the file is unreadable to any other
/// Windows account on the same machine and to anyone who just copies the file elsewhere.
/// </summary>
public sealed class DpapiTokenStore : ISecureTokenStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CollegeAdmin", "session.dat");

    public void SaveRefreshToken(string refreshToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        var plainBytes = Encoding.UTF8.GetBytes(refreshToken);
        var protectedBytes = ProtectedData.Protect(plainBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, protectedBytes);
    }

    public string? LoadRefreshToken()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(FilePath);
            var plainBytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException)
        {
            // Undecryptable — different Windows profile, corrupted, or tampered with. Treat as
            // "no stored session" rather than crashing startup, and remove the unusable file.
            Clear();
            return null;
        }
    }

    public void Clear()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}
