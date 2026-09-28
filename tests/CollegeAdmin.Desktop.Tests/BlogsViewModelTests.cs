using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the
/// desktop consumer for the new BlogsController.</summary>
public class BlogsViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsPostsAndCounts()
    {
        var apiClient = new FakeApiClient
        {
            BlogsToReturn = new BlogsListResult
            {
                Posts = [new BlogPostDto { Id = 1, Title = "A Post", Status = "pending" }],
                Counts = new BlogStatusCountsDto { Pending = 1, Total = 1 },
            },
        };
        var viewModel = new BlogsViewModel(apiClient);
        await Task.Delay(20);

        Assert.Single(viewModel.Posts);
        Assert.Equal(1, viewModel.Counts.Pending);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task ChangingStatusFilter_ReQueriesTheServer()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new BlogsViewModel(apiClient);
        await Task.Delay(20);

        viewModel.StatusFilter = "rejected";
        await Task.Delay(20);

        Assert.Equal("rejected", apiClient.LastBlogsStatusRequested);
    }

    [Fact]
    public async Task ApproveAsync_CallsTheApiAndRefreshes()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new BlogsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.ApproveCommand.ExecuteAsync(new BlogPostDto { Id = 3, Title = "Test" });

        Assert.Equal(3, apiClient.LastApprovedBlogId);
        Assert.Contains("Test", viewModel.StatusMessage);
    }

    [Fact]
    public async Task RejectAsync_SubmitsTheReason()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new BlogsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.RejectAsync(new BlogPostDto { Id = 4, Title = "Test" }, "Not relevant.");

        Assert.Equal((4, "Not relevant."), apiClient.LastRejectedBlog);
    }

    [Fact]
    public async Task DeleteAsync_CallsTheApi()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new BlogsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.DeleteCommand.ExecuteAsync(new BlogPostDto { Id = 8 });

        Assert.Equal(8, apiClient.LastDeletedBlogId);
    }

    [Fact]
    public async Task LoadAsync_OnApiFailure_SetsErrorMessage()
    {
        var apiClient = new FakeApiClient { ThrowOnBlogs = new ApiRequestException(new ApiErrorPayload { Code = "SERVER_ERROR", Message = "Failed." }) };
        var viewModel = new BlogsViewModel(apiClient);
        await Task.Delay(20);

        Assert.Equal("Failed.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsLoading);
    }
}
