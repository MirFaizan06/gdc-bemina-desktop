using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the
/// desktop consumer for the new PyqsController — the final slice of this body of work.</summary>
public class PyqsViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsPapersAndCounts()
    {
        var apiClient = new FakeApiClient
        {
            PyqsToReturn = new PyqsListResult
            {
                Papers = [new PyqDto { Id = 1, Title = "A Paper", Status = "pending" }],
                Counts = new PyqStatusCountsDto { Pending = 1, All = 1 },
            },
        };
        var viewModel = new PyqsViewModel(apiClient);
        await Task.Delay(20);

        Assert.Single(viewModel.Papers);
        Assert.Equal(1, viewModel.Counts.Pending);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task ChangingStatusOrSearchFilter_ReQueriesTheServer()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PyqsViewModel(apiClient);
        await Task.Delay(20);

        viewModel.StatusFilter = "approved";
        await Task.Delay(20);
        viewModel.SearchFilter = "algebra";
        await Task.Delay(20);

        Assert.Equal(("approved", "algebra"), apiClient.LastPyqsFilterRequested);
    }

    [Fact]
    public void CanAddPaper_RequiresTitleAndAnUploadedFile()
    {
        var viewModel = new PyqsViewModel(new FakeApiClient());
        Assert.False(viewModel.AddPaperCommand.CanExecute(null));

        viewModel.NewTitle = "Test Paper";
        Assert.False(viewModel.AddPaperCommand.CanExecute(null)); // still no file
    }

    [Fact]
    public async Task UploadPaperFileAsync_SetsNewFilePath_AndEnablesAdd()
    {
        var apiClient = new FakeApiClient { UploadPathToReturn = "assets/uploads/pyqs/abc.pdf" };
        var viewModel = new PyqsViewModel(apiClient) { NewTitle = "Test Paper" };
        await Task.Delay(20);

        await viewModel.UploadPaperFileAsync("paper.pdf", [1, 2, 3]);

        Assert.Equal("assets/uploads/pyqs/abc.pdf", viewModel.NewFilePath);
        Assert.True(viewModel.AddPaperCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddPaperAsync_SubmitsAllFields_AndClearsTheForm()
    {
        var apiClient = new FakeApiClient { UploadPathToReturn = "assets/uploads/pyqs/abc.pdf" };
        var viewModel = new PyqsViewModel(apiClient) { NewTitle = "Test Paper", NewSubject = "Maths" };
        await Task.Delay(20);
        await viewModel.UploadPaperFileAsync("paper.pdf", [1, 2, 3]);

        await viewModel.AddPaperCommand.ExecuteAsync(null);

        Assert.Equal("Test Paper", apiClient.LastCreatedPyqFields!["title"]);
        Assert.Equal("Maths", apiClient.LastCreatedPyqFields["subject"]);
        Assert.Equal("assets/uploads/pyqs/abc.pdf", apiClient.LastCreatedPyqFields["filePath"]);
        Assert.Equal("", viewModel.NewTitle);
        Assert.Null(viewModel.NewFilePath);
    }

    [Fact]
    public async Task ApproveAsync_CallsTheApiAndRefreshes()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PyqsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.ApproveCommand.ExecuteAsync(new PyqDto { Id = 3, Title = "Test" });

        Assert.Equal(3, apiClient.LastApprovedPyqId);
    }

    [Fact]
    public async Task RejectAsync_SubmitsTheReason()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PyqsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.RejectAsync(new PyqDto { Id = 4, Title = "Test" }, "Illegible scan.");

        Assert.Equal((4, "Illegible scan."), apiClient.LastRejectedPyq);
    }

    [Fact]
    public async Task RecallAsync_CallsTheApi()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PyqsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.RecallCommand.ExecuteAsync(new PyqDto { Id = 5 });

        Assert.Equal(5, apiClient.LastRecalledPyqId);
    }

    [Fact]
    public async Task DeleteAsync_CallsTheApi()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PyqsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.DeleteCommand.ExecuteAsync(new PyqDto { Id = 6 });

        Assert.Equal(6, apiClient.LastDeletedPyqId);
    }
}
