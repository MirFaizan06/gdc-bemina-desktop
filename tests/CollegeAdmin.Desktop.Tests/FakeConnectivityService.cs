using CollegeAdmin.Application.Connectivity;

namespace CollegeAdmin.Desktop.Tests;

internal sealed class FakeConnectivityService : IConnectivityService
{
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
