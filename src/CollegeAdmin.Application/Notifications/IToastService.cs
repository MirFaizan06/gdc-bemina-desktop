namespace CollegeAdmin.Application.Notifications;

public enum ToastSeverity
{
    Info,
    Success,
    Warning,
    Error,
}

public sealed record ToastMessage(string Id, string Text, ToastSeverity Severity);

/// <summary>
/// Final Convergence Phase P1-16 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): a plain in-memory
/// pub/sub, no WPF dependency — ShellViewModel (the one shell-chrome ViewModel MainWindow binds to
/// directly) is the actual subscriber that renders these as an overlay and owns auto-dismiss timing.
/// </summary>
public interface IToastService
{
    event EventHandler<ToastMessage>? ToastRequested;

    void Show(string text, ToastSeverity severity = ToastSeverity.Info);
}
