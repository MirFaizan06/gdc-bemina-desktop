using CollegeAdmin.Application.Notifications;

namespace CollegeAdmin.Infrastructure.Notifications;

public sealed class ToastService : IToastService
{
    public event EventHandler<ToastMessage>? ToastRequested;

    public void Show(string text, ToastSeverity severity = ToastSeverity.Info) =>
        ToastRequested?.Invoke(this, new ToastMessage(Guid.NewGuid().ToString("N"), text, severity));
}
