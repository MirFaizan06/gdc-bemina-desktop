using CollegeAdmin.Application.Notifications;

namespace CollegeAdmin.Desktop.Tests;

internal sealed class FakeToastService : IToastService
{
    public List<ToastMessage> Shown { get; } = [];

    public event EventHandler<ToastMessage>? ToastRequested;

    public void Show(string text, ToastSeverity severity = ToastSeverity.Info)
    {
        var toast = new ToastMessage(Guid.NewGuid().ToString("N"), text, severity);
        Shown.Add(toast);
        ToastRequested?.Invoke(this, toast);
    }
}
