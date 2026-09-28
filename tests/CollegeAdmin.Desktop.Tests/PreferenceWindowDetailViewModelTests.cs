using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

public class PreferenceWindowDetailViewModelTests
{
    [Fact]
    public void Constructor_LoadsSummaryAndSubmissions()
    {
        var apiClient = new FakeApiClient
        {
            PreferenceWindowSummaryToReturn = new PreferenceWindowSummaryDto { Eligible = 5, Submitted = 3, NotSubmitted = 2 },
            PreferenceSubmissionsToReturn = [new PreferenceSubmissionDto { Id = 1, StudentName = "A" }],
        };
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 9, Status = "open" });

        // Constructor kicks off a fire-and-forget refresh — give it a tick.
        System.Threading.Thread.Sleep(20);

        Assert.Equal(5, viewModel.Summary!.Eligible);
        Assert.Single(viewModel.Submissions);
    }

    [Fact]
    public void CanOpenAndCanClose_ReflectWindowStatus()
    {
        var draftViewModel = new PreferenceWindowDetailViewModel(new FakeApiClient(), new PreferenceWindowDto { Id = 1, Status = "draft" });
        Assert.True(draftViewModel.CanOpen);
        Assert.False(draftViewModel.CanClose);

        var openViewModel = new PreferenceWindowDetailViewModel(new FakeApiClient(), new PreferenceWindowDto { Id = 1, Status = "open" });
        Assert.False(openViewModel.CanOpen);
        Assert.True(openViewModel.CanClose);
    }

    [Fact]
    public async Task OpenWindowAsync_UpdatesWindowAndFlags()
    {
        var apiClient = new FakeApiClient { PreferenceWindowActionResultToReturn = new PreferenceWindowDto { Id = 1, Status = "open" } };
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 1, Status = "draft" });

        await viewModel.OpenWindowCommand.ExecuteAsync(null);

        Assert.Equal("open", viewModel.Window.Status);
        Assert.False(viewModel.CanOpen);
        Assert.True(viewModel.CanClose);
    }

    [Fact]
    public async Task InvalidateSubmissionAsync_WithNoReason_SetsErrorMessage_AndDoesNotCallTheServer()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 1, Status = "open" }) { InvalidateReason = "" };

        await viewModel.InvalidateSubmissionCommand.ExecuteAsync(new PreferenceSubmissionDto { Id = 5 });

        Assert.Equal("A reason is required to invalidate a submission.", viewModel.ErrorMessage);
        Assert.Null(apiClient.LastInvalidatedSubmission);
    }

    [Fact]
    public async Task InvalidateSubmissionAsync_WithAReason_CallsTheServer_AndClearsTheReasonField()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 1, Status = "open" })
        {
            InvalidateReason = "Student requested a correction",
        };

        await viewModel.InvalidateSubmissionCommand.ExecuteAsync(new PreferenceSubmissionDto { Id = 5 });

        Assert.Equal((5, "Student requested a correction"), apiClient.LastInvalidatedSubmission);
        Assert.Equal("", viewModel.InvalidateReason);
    }

    [Fact]
    public async Task ReopenSubmissionAsync_CallsTheServerWithTheSubmissionId()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 1, Status = "open" });

        await viewModel.ReopenSubmissionCommand.ExecuteAsync(new PreferenceSubmissionDto { Id = 7 });

        Assert.Equal(7, apiClient.LastReopenedSubmissionId);
    }

    [Fact]
    public void CanRunAllotment_IsTrueOnlyWhenWindowIsClosed()
    {
        var closed = new PreferenceWindowDetailViewModel(new FakeApiClient(), new PreferenceWindowDto { Id = 1, Status = "closed" });
        Assert.True(closed.CanRunAllotment);

        var open = new PreferenceWindowDetailViewModel(new FakeApiClient(), new PreferenceWindowDto { Id = 1, Status = "open" });
        Assert.False(open.CanRunAllotment);
    }

    [Fact]
    public async Task RunAllotmentAsync_RefreshesLatestRunAndResults()
    {
        var apiClient = new FakeApiClient
        {
            AllotmentRunsToReturn = [new AllotmentRunDto { Id = 5, Status = "proposed" }],
            AllotmentResultsToReturn = [new AllotmentResultDto { Id = 1, StudentName = "A" }],
        };
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 1, Status = "closed" });

        await viewModel.RunAllotmentCommand.ExecuteAsync(null);

        Assert.Equal(5, viewModel.LatestRun!.Id);
        Assert.True(viewModel.HasProposedRun);
        Assert.Single(viewModel.AllotmentResults);
    }

    [Fact]
    public async Task OverrideResultAsync_WithNoReason_SetsErrorMessage_AndDoesNotCallTheServer()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 1, Status = "closed" })
        {
            OverrideReason = "", OverrideSubjectId = "3",
        };

        await viewModel.OverrideResultCommand.ExecuteAsync(new AllotmentResultDto { Id = 9 });

        Assert.Equal("A reason is required to override an allotment.", viewModel.ErrorMessage);
        Assert.Null(apiClient.LastOverriddenResult);
    }

    [Fact]
    public async Task OverrideResultAsync_WithAnInvalidSubjectId_SetsErrorMessage()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 1, Status = "closed" })
        {
            OverrideReason = "Correction", OverrideSubjectId = "not-a-number",
        };

        await viewModel.OverrideResultCommand.ExecuteAsync(new AllotmentResultDto { Id = 9 });

        Assert.Equal("Enter a valid replacement subject ID.", viewModel.ErrorMessage);
        Assert.Null(apiClient.LastOverriddenResult);
    }

    [Fact]
    public async Task OverrideResultAsync_WithValidInput_CallsTheServer_AndClearsTheFields()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 1, Status = "closed" })
        {
            OverrideReason = "Wrong subject", OverrideSubjectId = "3",
        };

        await viewModel.OverrideResultCommand.ExecuteAsync(new AllotmentResultDto { Id = 9 });

        Assert.Equal((9, 3, "Wrong subject"), apiClient.LastOverriddenResult);
        Assert.Equal("", viewModel.OverrideReason);
        Assert.Equal("", viewModel.OverrideSubjectId);
    }

    [Fact]
    public async Task CancelResultAsync_CallsTheServerWithTheResultId()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, new PreferenceWindowDto { Id = 1, Status = "closed" });

        await viewModel.CancelResultCommand.ExecuteAsync(new AllotmentResultDto { Id = 12 });

        Assert.Equal(12, apiClient.LastCancelledResultId);
    }
}
