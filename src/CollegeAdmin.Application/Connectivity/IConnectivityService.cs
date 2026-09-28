namespace CollegeAdmin.Application.Connectivity;

/// <summary>
/// Final Convergence Phase P1-16 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): reflects real API
/// connectivity as observed by actual traffic (see ConnectivityTrackingHandler), not a separate
/// polling loop — SettingsViewModel already has its own live GET /api/v1/ping round-trip for its own
/// Connectivity section; this is the shell-wide signal derived from ordinary request/response flow.
/// </summary>
public interface IConnectivityService
{
    bool IsOnline { get; }

    /// <summary>Fires only on an actual state transition (online→offline or back) — never on every
    /// request — so a subscriber can toast/log a change without needing to de-duplicate itself.</summary>
    event EventHandler<bool>? ConnectivityChanged;

    void ReportSuccess();

    void ReportFailure();
}
