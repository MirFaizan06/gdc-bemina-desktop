using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-14 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the desktop
/// consumer for the new read-only `/dashboard` endpoint. Every admin's landing page; the ViewModel
/// only exposes what the server sent, so an omitted (null) stat means "not shown to this admin",
/// never "zero".</summary>
public class DashboardViewModelTests
{
    [Fact]
    public async Task LoadAsync_PopulatesOnlyTheStatsTheApiReturned()
    {
        var apiClient = new FakeApiClient
        {
            DashboardToReturn = new DashboardStatsDto
            {
                DepartmentCount = 2,
                FacultyCount = 2,
                NoticeCount = 1,
            },
        };

        var viewModel = new DashboardViewModel(apiClient, new FakeAuthSessionService());
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Stats.DepartmentCount);
        Assert.Equal(2, viewModel.Stats.FacultyCount);
        Assert.Equal(1, viewModel.Stats.NoticeCount);
        Assert.Null(viewModel.Stats.CommitteeCount);
        Assert.Null(viewModel.Stats.UpcomingEventCount);
        Assert.Null(viewModel.Stats.TimetableEntryCount);
        Assert.Null(viewModel.Stats.ProgrammeCount);
        Assert.Null(viewModel.Stats.StudentCount);
        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_OnApiFailure_SetsErrorMessage_NotAnUnhandledException()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnDashboard = new ApiRequestException(new ApiErrorPayload { Code = "SERVER_ERROR", Message = "Failed.", Retryable = true }),
        };

        var viewModel = new DashboardViewModel(apiClient, new FakeAuthSessionService());
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Failed.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsLoading);
    }
}
