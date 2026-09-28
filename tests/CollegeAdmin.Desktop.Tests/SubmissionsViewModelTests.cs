using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the
/// desktop consumer for the new SubmissionsController (Contact/Grievance/Alumni). Committee-scoped
/// visibility (the IDOR fix) is entirely server-enforced — this ViewModel just surfaces whatever
/// the server returns, so these tests only need to prove the ViewModel wires actions through
/// correctly, not re-prove the server-side scoping (already covered by ScopeIsolationTest, P1-12).</summary>
public class SubmissionsViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsAllThreeLists()
    {
        var apiClient = new FakeApiClient();
        apiClient.ContactMessages.Add(new ContactMessageDto { Id = 1, Name = "A" });
        apiClient.Grievances.Add(new GrievanceDto { Id = 1, Name = "B" });
        apiClient.AlumniList.Add(new AlumniDto { Id = 1, Name = "C" });
        var viewModel = new SubmissionsViewModel(apiClient);
        await Task.Delay(20);

        Assert.Single(viewModel.Contact);
        Assert.Single(viewModel.Grievances);
        Assert.Single(viewModel.Alumni);
    }

    [Fact]
    public async Task ReplyContactAsync_SubmitsTheReplyBody_AndRefreshes()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new SubmissionsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.ReplyContactAsync(new ContactMessageDto { Id = 5, Name = "Test" }, "Thanks for reaching out.");

        Assert.Equal((5, "Thanks for reaching out."), apiClient.LastContactReply);
        Assert.Contains("Test", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ForwardContactAsync_SubmitsTheCommitteeIdAndNote()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new SubmissionsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.ForwardContactAsync(new ContactMessageDto { Id = 5, Name = "Test" }, 3, "Please review");

        Assert.Equal((5, 3, "Please review"), apiClient.LastContactForward);
    }

    [Fact]
    public async Task DeleteContactAsync_CallsTheApi()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new SubmissionsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.DeleteContactCommand.ExecuteAsync(new ContactMessageDto { Id = 9 });

        Assert.Equal(9, apiClient.LastDeletedContactId);
    }

    [Fact]
    public async Task ReplyGrievanceAsync_SubmitsTheReplyBody()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new SubmissionsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.ReplyGrievanceAsync(new GrievanceDto { Id = 6, Name = "Test" }, "We are looking into this.");

        Assert.Equal((6, "We are looking into this."), apiClient.LastGrievanceReply);
    }

    [Theory]
    [InlineData("pending", "in_review")]
    [InlineData("in_review", "resolved")]
    [InlineData("resolved", "pending")]
    public async Task AdvanceGrievanceStatusAsync_CyclesThroughTheThreeStatuses(string current, string expectedNext)
    {
        var apiClient = new FakeApiClient();
        var viewModel = new SubmissionsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.AdvanceGrievanceStatusCommand.ExecuteAsync(new GrievanceDto { Id = 4, Status = current });

        Assert.Equal((4, expectedNext), apiClient.LastGrievanceStatusUpdate);
    }

    [Fact]
    public async Task ToggleAlumnusShowcaseAsync_CallsTheApi()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new SubmissionsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.ToggleAlumnusShowcaseCommand.ExecuteAsync(new AlumniDto { Id = 2 });

        Assert.Equal(2, apiClient.LastAlumnusShowcaseToggled);
    }

    [Fact]
    public async Task GetCommitteeOptionsAsync_ReturnsWhatTheApiClientProvides()
    {
        var apiClient = new FakeApiClient();
        apiClient.CommitteesToReturn.Add(new CommitteeDto { Id = 1, Name = "IQAC" });
        var viewModel = new SubmissionsViewModel(apiClient);
        await Task.Delay(20);

        var committees = await viewModel.GetCommitteeOptionsAsync();

        Assert.Single(committees);
        Assert.Equal("IQAC", committees[0].Name);
    }
}
