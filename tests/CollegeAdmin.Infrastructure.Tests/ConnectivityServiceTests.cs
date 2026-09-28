using CollegeAdmin.Infrastructure.Connectivity;

namespace CollegeAdmin.Infrastructure.Tests;

public class ConnectivityServiceTests
{
    [Fact]
    public void IsOnline_DefaultsTrue_Optimistic()
    {
        var service = new ConnectivityService();

        Assert.True(service.IsOnline);
    }

    [Fact]
    public void ReportFailure_TransitionsToOffline_AndRaisesConnectivityChangedOnce()
    {
        var service = new ConnectivityService();
        var raised = new List<bool>();
        service.ConnectivityChanged += (_, isOnline) => raised.Add(isOnline);

        service.ReportFailure();
        service.ReportFailure(); // already offline — must not fire again

        Assert.False(service.IsOnline);
        Assert.Equal([false], raised);
    }

    [Fact]
    public void ReportSuccess_AfterFailure_TransitionsBackToOnline_AndRaisesConnectivityChanged()
    {
        var service = new ConnectivityService();
        service.ReportFailure();
        var raised = new List<bool>();
        service.ConnectivityChanged += (_, isOnline) => raised.Add(isOnline);

        service.ReportSuccess();
        service.ReportSuccess(); // already online — must not fire again

        Assert.True(service.IsOnline);
        Assert.Equal([true], raised);
    }

    [Fact]
    public void ReportSuccess_WhileAlreadyOnline_NeverRaisesConnectivityChanged()
    {
        var service = new ConnectivityService();
        var raised = false;
        service.ConnectivityChanged += (_, _) => raised = true;

        service.ReportSuccess();

        Assert.False(raised);
    }
}
