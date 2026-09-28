using CollegeAdmin.Application.Connectivity;

namespace CollegeAdmin.Infrastructure.Connectivity;

public sealed class ConnectivityService : IConnectivityService
{
    // Optimistic by default — the app hasn't made a request yet at construction time, and assuming
    // online avoids flashing an "Offline" indicator on every fresh launch before the first call.
    public bool IsOnline { get; private set; } = true;

    public event EventHandler<bool>? ConnectivityChanged;

    public void ReportSuccess()
    {
        if (!IsOnline)
        {
            IsOnline = true;
            ConnectivityChanged?.Invoke(this, true);
        }
    }

    public void ReportFailure()
    {
        if (IsOnline)
        {
            IsOnline = false;
            ConnectivityChanged?.Invoke(this, false);
        }
    }
}
