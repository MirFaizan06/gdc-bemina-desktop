using CollegeAdmin.Application.Auth;
using CollegeAdmin.Contracts.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Records what it was called with — no real HTTP/DI — so ViewModel tests can verify
/// the exact wiring UI-input-simulation would otherwise be checking (see StartupFlowTests remarks
/// for why this replaces OS-level input simulation as the verification method for Stage 3).</summary>
internal sealed class FakeAuthSessionService : IAuthSessionService
{
    public bool IsAuthenticated { get; private set; }
    public AdminProfile? CurrentAdmin { get; private set; }
    public event EventHandler? SessionExpired;

    public string? LastLoginEmail { get; private set; }
    public string? LastLoginPassword { get; private set; }
    public Exception? ThrowOnLogin { get; set; }
    public bool LogoutCalled { get; private set; }

    public Task LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        LastLoginEmail = email;
        LastLoginPassword = password;

        if (ThrowOnLogin is not null)
        {
            throw ThrowOnLogin;
        }

        CurrentAdmin = new AdminProfile { Id = 1, Name = "Test Admin", Email = email, Role = "super_admin" };
        IsAuthenticated = true;
        return Task.CompletedTask;
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        LogoutCalled = true;
        IsAuthenticated = false;
        CurrentAdmin = null;
        return Task.CompletedTask;
    }

    public Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    /// <summary>Sets CurrentAdmin directly, bypassing LoginAsync's hardcoded super_admin profile —
    /// for tests exercising capability-based nav filtering (ShellViewModelTests) against a
    /// specific, non-super_admin capability set.</summary>
    public void SetAdmin(AdminProfile admin)
    {
        CurrentAdmin = admin;
        IsAuthenticated = true;
    }

    public void RaiseSessionExpired() => SessionExpired?.Invoke(this, EventArgs.Empty);
}
