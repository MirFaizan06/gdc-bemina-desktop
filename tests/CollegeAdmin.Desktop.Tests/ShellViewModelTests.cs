using CollegeAdmin.Application.Navigation;
using CollegeAdmin.Application.Updates;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Tests;

internal sealed class FakeNavigationService : INavigationService
{
    public object? CurrentPage { get; private set; }
    public event EventHandler? CurrentPageChanged;
    public List<string> NavigatedKeys { get; } = [];

    public void NavigateTo(string pageKey)
    {
        NavigatedKeys.Add(pageKey);
        CurrentPage = pageKey;
        CurrentPageChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>No update available by default, so the existing navigation/logout tests don't need to
/// know or care about the updater — set CheckResult to opt a specific test into "update found."</summary>
internal sealed class FakeUpdateService : IUpdateService
{
    public UpdateCheckResult CheckResult { get; set; } = new(false, "1.0.0", "1.0.0", null, null, null);
    public UpdateDownloadResult DownloadResult { get; set; } = new(true, @"C:\temp\CollegeAdminSetup.msi", null);
    public string? LaunchedInstallerPath { get; private set; }

    public Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CheckResult);

    public Task<UpdateDownloadResult> DownloadAndVerifyAsync(UpdateCheckResult update, CancellationToken cancellationToken = default) =>
        Task.FromResult(DownloadResult);

    public void LaunchInstaller(string installerPath) => LaunchedInstallerPath = installerPath;
}

public class ShellViewModelTests
{
    [Fact]
    public void Constructor_NavigatesToFirstItem_AndSetsWelcomeMessage()
    {
        var nav = new FakeNavigationService();
        var auth = new FakeAuthSessionService();
        auth.LoginAsync("admin@test.local", "x").GetAwaiter().GetResult();

        var viewModel = new ShellViewModel(nav, auth, new FakeUpdateService(), new FakeConnectivityService(), new FakeToastService());

        Assert.Single(nav.NavigatedKeys);
        Assert.Equal("Dashboard", nav.NavigatedKeys[0]);
        Assert.Contains("Test", viewModel.WelcomeMessage); // time-based greeting + first name, e.g. "Good morning, Test"
        Assert.Equal(18, viewModel.NavItems.Count); // +1 for the new "PYQ" item (P1-10/§2b)
    }

    [Fact]
    public void SelectingNavItem_NavigatesToItsLabel()
    {
        var nav = new FakeNavigationService();
        var auth = new FakeAuthSessionService();
        auth.LoginAsync("admin@test.local", "x").GetAwaiter().GetResult(); // super_admin — sees every item, including the gated "timetable" one
        var viewModel = new ShellViewModel(nav, auth, new FakeUpdateService(), new FakeConnectivityService(), new FakeToastService());

        viewModel.SelectedNavItem = viewModel.NavItems.First(i => i.Key == "timetable");

        Assert.Equal("Timetable", nav.NavigatedKeys[^1]);
    }

    [Fact]
    public void Constructor_ScopedAdmin_OnlySeesNavItemsTheirCapabilitiesCover()
    {
        // Mirrors the cs-admin test fixture from the server-side live verification (Entry 20/22):
        // timetable + departments grants only, no students/admissions/admins/jobs capability at all.
        var auth = new FakeAuthSessionService();
        auth.SetAdmin(new CollegeAdmin.Contracts.Api.AdminProfile
        {
            Id = 2, Name = "CS Dept Admin", Role = "dept_admin",
            Capabilities =
            [
                new() { Capability = "departments.view", ScopeType = "department", ScopeId = 1 },
                new() { Capability = "timetable.view", ScopeType = "department", ScopeId = 1 },
            ],
        });

        var viewModel = new ShellViewModel(new FakeNavigationService(), auth, new FakeUpdateService(), new FakeConnectivityService(), new FakeToastService());

        var keys = viewModel.NavItems.Select(i => i.Key).ToList();
        Assert.Contains("dashboard", keys);
        Assert.Contains("system", keys);
        Assert.Contains("college", keys);   // departments.view
        Assert.Contains("timetable", keys); // timetable.view
        Assert.DoesNotContain("academics", keys);      // no students.*/admissions.*
        Assert.DoesNotContain("administration", keys);  // no admins.*
        Assert.DoesNotContain("reports", keys);          // no jobs.manage
        Assert.DoesNotContain("preferences", keys);      // no preferences.*
        Assert.DoesNotContain("auditlog", keys);         // no audit.view
    }

    [Fact]
    public void Constructor_AdminWithOnlyAuditView_SeesAuditLogNavItem_ButNotAdministration()
    {
        // Final Convergence Phase P1-1: the original audit's own named finding — audit.view and
        // admins.manage/.ban/.manage_sessions are genuinely different grants, so an admin with only
        // the former must be able to reach the audit log without also being able to reach admin
        // account management.
        var auth = new FakeAuthSessionService();
        auth.SetAdmin(new CollegeAdmin.Contracts.Api.AdminProfile
        {
            Id = 3, Name = "Audit Only Admin", Role = "editor",
            Capabilities = [new() { Capability = "audit.view", ScopeType = "system", ScopeId = null }],
        });

        var viewModel = new ShellViewModel(new FakeNavigationService(), auth, new FakeUpdateService(), new FakeConnectivityService(), new FakeToastService());

        var keys = viewModel.NavItems.Select(i => i.Key).ToList();
        Assert.Contains("auditlog", keys);
        Assert.DoesNotContain("administration", keys);
    }

    [Fact]
    public void Constructor_PrincipalWithAutoGrantedCapabilities_SeesInstitutionOverview_ButNotAdministrationOrSettings()
    {
        // Final Convergence Phase P1-3: mirrors the live capability set AdminsController::store()
        // auto-grants a new role=principal admin (Capabilities::PRINCIPAL_AUTO_GRANT) — proves the
        // nav surfaces exactly what that grant set implies, no more, no less.
        var auth = new FakeAuthSessionService();
        auth.SetAdmin(new CollegeAdmin.Contracts.Api.AdminProfile
        {
            Id = 4, Name = "Principal", Role = "principal",
            Capabilities =
            [
                new() { Capability = "departments.view", ScopeType = "system", ScopeId = null },
                new() { Capability = "notices.view", ScopeType = "system", ScopeId = null },
                new() { Capability = "principal.overview", ScopeType = "system", ScopeId = null },
            ],
        });

        var viewModel = new ShellViewModel(new FakeNavigationService(), auth, new FakeUpdateService(), new FakeConnectivityService(), new FakeToastService());

        var keys = viewModel.NavItems.Select(i => i.Key).ToList();
        Assert.Contains("principaloverview", keys);
        Assert.Contains("website", keys);       // notices.view
        Assert.Contains("college", keys);       // departments.view
        Assert.DoesNotContain("administration", keys);
        Assert.DoesNotContain("auditlog", keys);
    }

    [Fact]
    public void Constructor_AdmissionsOnlyAdmin_DoesNotSeeCollegeStructure()
    {
        var auth = new FakeAuthSessionService();
        auth.SetAdmin(new CollegeAdmin.Contracts.Api.AdminProfile
        {
            Id = 3, Name = "Admissions Admin", Role = "committee_admin",
            Capabilities = [new() { Capability = "admissions.view", ScopeType = "system" }],
        });

        var viewModel = new ShellViewModel(new FakeNavigationService(), auth, new FakeUpdateService(), new FakeConnectivityService(), new FakeToastService());

        var keys = viewModel.NavItems.Select(i => i.Key).ToList();
        Assert.Contains("academics", keys);          // admissions.manage
        Assert.DoesNotContain("college", keys);       // no departments/faculty/committees.*
        Assert.DoesNotContain("timetable", keys);     // no timetable.*
    }

    [Fact]
    public async Task LogoutCommand_CallsAuthServiceLogout_AndRaisesLoggedOut()
    {
        var auth = new FakeAuthSessionService();
        var viewModel = new ShellViewModel(new FakeNavigationService(), auth, new FakeUpdateService(), new FakeConnectivityService(), new FakeToastService());
        var loggedOutRaised = false;
        viewModel.LoggedOut += (_, _) => loggedOutRaised = true;

        await viewModel.LogoutCommand.ExecuteAsync(null);

        Assert.True(auth.LogoutCalled);
        Assert.True(loggedOutRaised);
    }

    [Fact]
    public void Constructor_NoUpdateAvailable_BannerStaysHidden()
    {
        var updateService = new FakeUpdateService(); // default: no update
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), updateService, new FakeConnectivityService(), new FakeToastService());

        Assert.False(viewModel.IsUpdateBannerVisible);
    }

    [Fact]
    public async Task Constructor_UpdateAvailable_ShowsBanner_WithVersionInMessage()
    {
        var updateService = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(true, "1.0.0", "1.2.0", "https://example.test/setup.msi", "abc123", "Notes"),
        };
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), updateService, new FakeConnectivityService(), new FakeToastService());

        // The check runs fire-and-forget from the constructor; give it a tick to complete.
        await Task.Delay(20);

        Assert.True(viewModel.IsUpdateBannerVisible);
        Assert.Contains("1.2.0", viewModel.UpdateBannerMessage);
        Assert.Contains("1.0.0", viewModel.UpdateBannerMessage);
    }

    // ---- Final Convergence Phase P2 item 10: release-notes rendering ----

    [Fact]
    public async Task Constructor_UpdateAvailable_WithReleaseNotes_SetsUpdateReleaseNotes()
    {
        var updateService = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(true, "1.0.0", "1.2.0", "https://example.test/setup.msi", "abc123", "Fixed the thing."),
        };
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), updateService, new FakeConnectivityService(), new FakeToastService());
        await Task.Delay(20);

        Assert.Equal("Fixed the thing.", viewModel.UpdateReleaseNotes);
        Assert.False(viewModel.IsReleaseNotesExpanded);
    }

    [Fact]
    public async Task Constructor_UpdateAvailable_WithBlankReleaseNotes_LeavesUpdateReleaseNotesNull()
    {
        var updateService = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(true, "1.0.0", "1.2.0", "https://example.test/setup.msi", "abc123", "   "),
        };
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), updateService, new FakeConnectivityService(), new FakeToastService());
        await Task.Delay(20);

        Assert.Null(viewModel.UpdateReleaseNotes);
    }

    [Fact]
    public void ToggleReleaseNotesCommand_TogglesIsReleaseNotesExpanded()
    {
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), new FakeUpdateService(), new FakeConnectivityService(), new FakeToastService());

        viewModel.ToggleReleaseNotesCommand.Execute(null);
        Assert.True(viewModel.IsReleaseNotesExpanded);

        viewModel.ToggleReleaseNotesCommand.Execute(null);
        Assert.False(viewModel.IsReleaseNotesExpanded);
    }

    [Fact]
    public void DismissUpdateBannerCommand_HidesTheBanner()
    {
        var updateService = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(true, "1.0.0", "1.2.0", "https://example.test/setup.msi", "abc123", null),
        };
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), updateService, new FakeConnectivityService(), new FakeToastService())
        {
            IsUpdateBannerVisible = true,
        };

        viewModel.DismissUpdateBannerCommand.Execute(null);

        Assert.False(viewModel.IsUpdateBannerVisible);
    }

    [Fact]
    public async Task DownloadUpdateCommand_OnVerificationFailure_SetsErrorMessage_AndDoesNotLaunch()
    {
        var updateService = new FakeUpdateService
        {
            CheckResult = new UpdateCheckResult(true, "1.0.0", "1.2.0", "https://example.test/setup.msi", "abc123", null),
            DownloadResult = new UpdateDownloadResult(false, null, "The downloaded installer failed integrity verification and was discarded. Please try again."),
        };
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), updateService, new FakeConnectivityService(), new FakeToastService());
        await Task.Delay(20);

        await viewModel.DownloadUpdateCommand.ExecuteAsync(null);

        Assert.Equal("The downloaded installer failed integrity verification and was discarded. Please try again.", viewModel.UpdateErrorMessage);
        Assert.Null(updateService.LaunchedInstallerPath);
    }

    // ---- Final Convergence Phase P1-16: connectivity indicator + toasts ----

    [Fact]
    public void Constructor_ReflectsConnectivityService_InitialIsOnlineState()
    {
        var connectivity = new FakeConnectivityService();
        connectivity.ReportFailure(); // offline before the shell is even constructed

        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), new FakeUpdateService(), connectivity, new FakeToastService(), autoDismissToasts: false);

        Assert.False(viewModel.IsOnline);
        Assert.Equal("Offline", viewModel.ConnectivityLabel);
    }

    [Fact]
    public void ConnectivityChanged_ToOffline_UpdatesIsOnline_AndShowsAToast()
    {
        var connectivity = new FakeConnectivityService();
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), new FakeUpdateService(), connectivity, new FakeToastService(), autoDismissToasts: false);

        connectivity.ReportFailure();

        Assert.False(viewModel.IsOnline);
        Assert.Equal("Offline", viewModel.ConnectivityLabel);
        Assert.Single(viewModel.Toasts);
        Assert.Contains("offline", viewModel.Toasts[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConnectivityChanged_BackToOnline_UpdatesIsOnline_AndShowsAToast()
    {
        var connectivity = new FakeConnectivityService();
        connectivity.ReportFailure();
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), new FakeUpdateService(), connectivity, new FakeToastService(), autoDismissToasts: false);

        connectivity.ReportSuccess();

        Assert.True(viewModel.IsOnline);
        Assert.Equal("Online", viewModel.ConnectivityLabel);
        Assert.Contains("restored", viewModel.Toasts[^1].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToastService_Show_AddsToToasts()
    {
        var toastService = new FakeToastService();
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), new FakeUpdateService(), new FakeConnectivityService(), toastService, autoDismissToasts: false);

        toastService.Show("Exported 3 rows.");

        Assert.Single(viewModel.Toasts);
        Assert.Equal("Exported 3 rows.", viewModel.Toasts[0].Text);
    }

    [Fact]
    public void DismissToastCommand_RemovesTheGivenToast()
    {
        var toastService = new FakeToastService();
        var viewModel = new ShellViewModel(new FakeNavigationService(), new FakeAuthSessionService(), new FakeUpdateService(), new FakeConnectivityService(), toastService, autoDismissToasts: false);
        toastService.Show("Exported 3 rows.");
        var toast = viewModel.Toasts[0];

        viewModel.DismissToastCommand.Execute(toast);

        Assert.Empty(viewModel.Toasts);
    }
}
