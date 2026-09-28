using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-3 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the desktop
/// consumer for the new read-only `/principal/overview` endpoint.</summary>
public class PrincipalOverviewViewModelTests
{
    [Fact]
    public async Task LoadAsync_PopulatesStatsAndCollectionsFromTheApi()
    {
        var apiClient = new FakeApiClient
        {
            PrincipalOverviewToReturn = new PrincipalOverviewResult
            {
                Stats = new PrincipalOverviewStatsDto { DepartmentCount = 5, NoticePublished = 12 },
                StudentStats = new PrincipalStudentStatsDto { Total = 300, Active = 280 },
                FacultyByDept = [new FacultyByDeptDto { DeptName = "Computer Science", FacultyCount = 8 }],
                AdminsByRole = [new AdminsByRoleDto { Role = "principal", Cnt = 1 }],
                RecentActivity = [new ActivityLogEntryDto { Id = 1, Action = "create", Entity = "notice" }],
            },
        };

        var viewModel = new PrincipalOverviewViewModel(apiClient);
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(5, viewModel.Stats.DepartmentCount);
        Assert.Equal(12, viewModel.Stats.NoticePublished);
        Assert.Equal(300, viewModel.StudentStats.Total);
        Assert.Single(viewModel.FacultyByDept);
        Assert.Equal("Computer Science", viewModel.FacultyByDept[0].DeptName);
        Assert.Single(viewModel.AdminsByRole);
        Assert.Single(viewModel.RecentActivity);
        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_OnApiFailure_SetsErrorMessage_NotAnUnhandledException()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnPrincipalOverview = new ApiRequestException(new ApiErrorPayload { Code = "SERVER_ERROR", Message = "Failed.", Retryable = true }),
        };

        var viewModel = new PrincipalOverviewViewModel(apiClient);
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Failed.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsLoading);
    }
}
