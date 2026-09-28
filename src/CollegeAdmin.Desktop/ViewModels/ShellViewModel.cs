using System.Collections.ObjectModel;
using System.Linq;
using CollegeAdmin.Application.Auth;
using CollegeAdmin.Application.Connectivity;
using CollegeAdmin.Application.Navigation;
using CollegeAdmin.Application.Notifications;
using CollegeAdmin.Application.Updates;
using CollegeAdmin.Domain.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Nav groups per docs/claude/08_UI_UX_SYSTEM.md, now filtered by the signed-in admin's actual
/// capability grants (AdminProfile.Capabilities, added alongside this — see
/// docs/claude/18_DEVLOG.md Entry 23) rather than shown unconditionally to every role. This is a
/// UX improvement, not the authorization boundary itself — the server re-checks every real
/// read/write regardless of what the sidebar shows (Authorization::require() on every endpoint,
/// confirmed by a full audit this session); hiding an item here only stops an admin from wasting a
/// click on something they'd be 403'd on anyway, and keeps unrelated modules (e.g. Departments, for
/// an admissions-only admin) out of view entirely rather than visible-but-forbidden.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAuthSessionService _authSessionService;
    private readonly IUpdateService _updateService;
    private readonly IConnectivityService _connectivityService;
    private readonly IToastService _toastService;
    private UpdateCheckResult? _pendingUpdate;

    /// <summary>Each nav item's required capabilities (any ONE is enough to show it) — mirrors
    /// which capability each tab's underlying controller actually requires (see NavigationService's
    /// BuildXxxTabs() methods for the exact API calls each nav item makes). "dashboard" and "system"
    /// (Settings, which hosts Credits as a section per P1-15) have no entry here and are always
    /// shown — neither exposes scoped data; Credits in particular must stay reachable by everyone
    /// per CLAUDE.md's developer-credit requirement.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> NavCapabilities = new Dictionary<string, string[]>
    {
        ["website"] = ["notices.view", "news.view", "banners.view", "faq.view", "documents.view", "events.view", "gallery.view"],
        ["college"] = ["departments.view", "faculty.view", "committees.view"],
        ["academics"] = ["students.view", "admissions.view", "internships.view"],
        ["preferences"] = ["preferences.view"],
        ["timetable"] = ["timetable.view"],
        ["committees"] = ["committees.view"],
        ["reports"] = ["jobs.manage"],
        ["administration"] = ["admins.manage", "admins.ban", "admins.manage_sessions"],
        // Final Convergence Phase P1-1: the original audit's own finding — an admin granted ONLY
        // audit.view (no admins.* capability) would have server-side rights to view the audit log
        // but no nav item to reach it, since "administration" never included audit.view. This is
        // its own, separate nav entry rather than folded into "administration" for exactly that
        // reason — the two capabilities are genuinely different grants.
        ["auditlog"] = ["audit.view"],
        // Final Convergence Phase P1-3: its own nav entry, gated on the new principal.overview
        // capability specifically — not folded into any other group, matching auditlog's own
        // reasoning above (a distinct grant deserves its own reachable nav item).
        ["principaloverview"] = ["principal.overview"],
        ["academiccalendar"] = ["calendar.view", "calendar.manage"],
        ["principalsection"] = ["principalsection.view", "principalsection.manage"],
        ["certifications"] = ["certifications.view", "certifications.manage"],
        ["submissions"] = ["submissions.view", "submissions.manage"],
        ["blogs"] = ["blogs.view", "blogs.manage"],
        ["pyqs"] = ["pyqs.view", "pyqs.manage"],
    };

    private readonly TimeSpan _toastDismissDelay;
    private readonly bool _autoDismissToasts;

    public ShellViewModel(
        INavigationService navigationService,
        IAuthSessionService authSessionService,
        IUpdateService updateService,
        IConnectivityService connectivityService,
        IToastService toastService,
        TimeSpan? toastDismissDelay = null,
        bool autoDismissToasts = true)
    {
        _navigationService = navigationService;
        _authSessionService = authSessionService;
        _updateService = updateService;
        _connectivityService = connectivityService;
        _toastService = toastService;
        _toastDismissDelay = toastDismissDelay ?? TimeSpan.FromSeconds(4);
        // Tests pass false: they only assert that Show()/a connectivity transition adds a toast (and
        // that DismissToastCommand removes one manually) — the real timer-based auto-dismiss is a
        // production-only behavior this codebase doesn't unit-test elsewhere either (matching
        // XamlLoadsTests' own precedent of not exercising real WPF timer/animation paths). Scheduling
        // a live Task.Delay per test was tried and measurably made the suite far slower to tear down
        // (a real 30s delay added ~2 minutes across 5 tests) without testing anything meaningful.
        _autoDismissToasts = autoDismissToasts;
        _navigationService.CurrentPageChanged += (_, _) => CurrentPage = _navigationService.CurrentPage;

        IsOnline = _connectivityService.IsOnline;
        _connectivityService.ConnectivityChanged += OnConnectivityChanged;
        _toastService.ToastRequested += OnToastRequested;

        var admin = authSessionService.CurrentAdmin;
        WelcomeMessage = admin is not null ? $"{TimeGreeting.Now()}, {TimeGreeting.FirstName(admin.Name)}" : "";

        var allItems = new[]
        {
            new NavItem("dashboard", "Dashboard", "Dashboard"),
            new NavItem("website", "Website Content", "Website"),
            new NavItem("college", "College Structure", "College"),
            new NavItem("academics", "Academics", "Academics"),
            new NavItem("preferences", "Preferences", "Preferences"),
            new NavItem("timetable", "Timetable", "Timetable"),
            new NavItem("committees", "Committees", "Committees"),
            new NavItem("reports", "Reports & Exports", "Reports"),
            new NavItem("administration", "Administration", "Administration"),
            new NavItem("auditlog", "Audit Log", "Audit Log"),
            new NavItem("principaloverview", "Institution Overview", "Institution Overview"),
            new NavItem("academiccalendar", "Academic Calendar", "Academic Calendar"),
            new NavItem("principalsection", "Principal's Message", "Principal's Message"),
            new NavItem("certifications", "Certifications", "Certifications"),
            new NavItem("submissions", "Submissions", "Submissions"),
            new NavItem("blogs", "Blogs", "Blogs"),
            new NavItem("pyqs", "PYQ", "PYQ"),
            new NavItem("system", "Settings", "Settings"),
        };

        NavItems = new ObservableCollection<NavItem>(
            allItems.Where(item => IsVisible(item.Key, admin)));

        NavigateTo(NavItems[0]);

        // Fire-and-forget by design: a failed/slow version check must never delay the shell from
        // appearing, and CheckForUpdateAsync itself never throws (see UpdateService's remarks).
        _ = CheckForUpdateAsync();
    }

    [ObservableProperty]
    private bool _isUpdateBannerVisible;

    [ObservableProperty]
    private string _updateBannerMessage = "";

    // Final Convergence Phase P2 item 10 (docs/claude/FINAL_COMPLETION_TRACKER.md §4):
    // UpdateCheckResult.ReleaseNotes has existed since Stage 17 but was never surfaced anywhere in
    // the UI — the banner only ever showed the version numbers. Collapsed by default so the banner
    // stays compact for the common case (nothing noteworthy to read before updating).
    [ObservableProperty]
    private string? _updateReleaseNotes;

    [ObservableProperty]
    private bool _isReleaseNotesExpanded;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadUpdateCommand))]
    private bool _isUpdateBusy;

    [ObservableProperty]
    private string? _updateErrorMessage;

    private async Task CheckForUpdateAsync()
    {
        var result = await _updateService.CheckForUpdateAsync();
        if (result.IsUpdateAvailable)
        {
            _pendingUpdate = result;
            UpdateBannerMessage = $"A new version ({result.LatestVersion}) is available. You're running {result.CurrentVersion}.";
            UpdateReleaseNotes = string.IsNullOrWhiteSpace(result.ReleaseNotes) ? null : result.ReleaseNotes;
            IsUpdateBannerVisible = true;
        }
    }

    [RelayCommand]
    private void DismissUpdateBanner() => IsUpdateBannerVisible = false;

    [RelayCommand]
    private void ToggleReleaseNotes() => IsReleaseNotesExpanded = !IsReleaseNotesExpanded;

    [RelayCommand(CanExecute = nameof(CanDownloadUpdate))]
    private async Task DownloadUpdateAsync()
    {
        if (_pendingUpdate is null)
        {
            return;
        }

        IsUpdateBusy = true;
        UpdateErrorMessage = null;
        try
        {
            var download = await _updateService.DownloadAndVerifyAsync(_pendingUpdate);
            if (!download.Success)
            {
                UpdateErrorMessage = download.ErrorMessage;
                return;
            }

            _updateService.LaunchInstaller(download.LocalFilePath!);
            System.Windows.Application.Current.Shutdown();
        }
        finally
        {
            IsUpdateBusy = false;
        }
    }

    private bool CanDownloadUpdate() => !IsUpdateBusy;

    /// <summary>"dashboard"/"system" have no entry in NavCapabilities and are always visible; every
    /// other key needs at least one of its listed capabilities. A null admin (should not happen post-
    /// login, but the type is nullable) fails closed — nothing gated shows.</summary>
    private static bool IsVisible(string navKey, Contracts.Api.AdminProfile? admin)
    {
        if (!NavCapabilities.TryGetValue(navKey, out var required))
        {
            return true;
        }
        return admin is not null && required.Any(admin.HasCapability);
    }

    public ObservableCollection<NavItem> NavItems { get; }

    [ObservableProperty]
    private NavItem? _selectedNavItem;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _welcomeMessage;

    public event EventHandler? LoggedOut;

    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        if (value is not null)
        {
            NavigateTo(value);
        }
    }

    private void NavigateTo(NavItem item) => _navigationService.NavigateTo(item.Label);

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authSessionService.LogoutAsync();
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }

    // Final Convergence Phase P1-16: reflects IConnectivityService's real traffic-derived state
    // (see ConnectivityTrackingHandler) — not a separate polling loop of its own. A toast marks the
    // transition; the ConnectivityLabel/IsOnline pair is the persistent indicator in the shell chrome.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectivityLabel))]
    private bool _isOnline;

    public string ConnectivityLabel => IsOnline ? "Online" : "Offline";

    private void OnConnectivityChanged(object? sender, bool isOnline)
    {
        IsOnline = isOnline;
        _toastService.Show(
            isOnline ? "Connection restored." : "Connection lost — working offline.",
            isOnline ? ToastSeverity.Success : ToastSeverity.Warning);
    }

    public ObservableCollection<ToastMessage> Toasts { get; } = [];

    private void OnToastRequested(object? sender, ToastMessage toast)
    {
        Toasts.Add(toast);
        if (_autoDismissToasts)
        {
            _ = DismissAfterDelayAsync(toast);
        }
    }

    private async Task DismissAfterDelayAsync(ToastMessage toast)
    {
        await Task.Delay(_toastDismissDelay);
        Toasts.Remove(toast);
    }

    [RelayCommand]
    private void DismissToast(ToastMessage toast) => Toasts.Remove(toast);
}
