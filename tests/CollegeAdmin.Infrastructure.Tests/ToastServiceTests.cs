using CollegeAdmin.Application.Notifications;
using CollegeAdmin.Infrastructure.Notifications;

namespace CollegeAdmin.Infrastructure.Tests;

public class ToastServiceTests
{
    [Fact]
    public void Show_RaisesToastRequested_WithTheGivenTextAndSeverity()
    {
        var service = new ToastService();
        ToastMessage? received = null;
        service.ToastRequested += (_, toast) => received = toast;

        service.Show("Exported 5 rows.", ToastSeverity.Success);

        Assert.NotNull(received);
        Assert.Equal("Exported 5 rows.", received!.Text);
        Assert.Equal(ToastSeverity.Success, received.Severity);
        Assert.False(string.IsNullOrEmpty(received.Id));
    }

    [Fact]
    public void Show_DefaultsToInfoSeverity_WhenNotSpecified()
    {
        var service = new ToastService();
        ToastMessage? received = null;
        service.ToastRequested += (_, toast) => received = toast;

        service.Show("Just so you know.");

        Assert.Equal(ToastSeverity.Info, received!.Severity);
    }

    [Fact]
    public void Show_EachCall_GetsADistinctId()
    {
        var service = new ToastService();
        var ids = new List<string>();
        service.ToastRequested += (_, toast) => ids.Add(toast.Id);

        service.Show("First");
        service.Show("Second");

        Assert.Equal(2, ids.Distinct().Count());
    }
}
