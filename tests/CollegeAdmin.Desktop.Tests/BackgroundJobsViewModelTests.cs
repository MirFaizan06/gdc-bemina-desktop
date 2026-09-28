using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

public class BackgroundJobsViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsJobs()
    {
        var apiClient = new FakeApiClient();
        apiClient.Jobs.Add(new BackgroundJobDto { Id = 1, Type = "cleanup_expired_sessions", Status = "finished" });
        var viewModel = new BackgroundJobsViewModel(apiClient);

        await Task.Delay(1);

        Assert.Single(viewModel.Jobs);
        Assert.False(viewModel.IsEmpty);
    }

    [Fact]
    public async Task RunCleanupAsync_CallsRunJobAsync_WithTheCleanupType()
    {
        var apiClient = new FakeApiClient
        {
            RunJobResultToReturn = new RunJobResult
            {
                Jobs = [new BackgroundJobDto { Id = 1, Type = "cleanup_expired_sessions", Status = "finished" }],
                Processed = 1,
                Failed = 0,
            },
        };
        var viewModel = new BackgroundJobsViewModel(apiClient);
        await Task.Delay(1);

        await viewModel.RunCleanupCommand.ExecuteAsync(null);

        Assert.Equal("cleanup_expired_sessions", apiClient.LastRunJobType);
        Assert.Single(viewModel.Jobs);
        Assert.Contains("Processed 1", viewModel.StatusMessage);
    }

    [Fact]
    public async Task RunCleanupAsync_OnApiRequestException_SetsErrorMessage()
    {
        var apiClient = new FakeApiClient { ThrowOnJobs = new ApiRequestException(new ApiErrorPayload
        {
            Code = "AUTHORIZATION_ERROR",
            Message = "You do not have permission to perform this action.",
        }) };
        var viewModel = new BackgroundJobsViewModel(apiClient);
        await Task.Delay(1);

        await viewModel.RunCleanupCommand.ExecuteAsync(null);

        Assert.Equal("You do not have permission to perform this action.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsRunning);
    }
}
