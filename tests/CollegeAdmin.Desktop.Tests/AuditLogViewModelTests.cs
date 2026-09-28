using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-1 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the desktop
/// consumer for a fully-built, capability-gated backend audit endpoint that had zero screen at all
/// until now.</summary>
public class AuditLogViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsEntriesFromTheApi()
    {
        var apiClient = new FakeApiClient
        {
            ActivityLogEntriesToReturn = [new ActivityLogEntryDto { Id = 1, Action = "create", Entity = "notice" }],
        };

        var viewModel = new AuditLogViewModel(apiClient);
        await viewModel.List.LoadCommand.ExecuteAsync(null);

        Assert.Single(viewModel.List.Items);
    }

    [Fact]
    public async Task ChangingActionFilter_ReQueriesTheServerWithTheNewFilter()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new AuditLogViewModel(apiClient);
        await viewModel.List.LoadCommand.ExecuteAsync(null);

        viewModel.ActionFilter = "delete";
        await Task.Delay(20); // OnActionFilterChanged fires List.RefreshAsync() fire-and-forget

        Assert.Equal(("delete", null, null), apiClient.LastActivityLogFilter);
    }

    [Fact]
    public async Task ChangingEntityAndAdminIdFilters_ReQueriesWithBoth()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new AuditLogViewModel(apiClient) { EntityFilter = "student" };
        await Task.Delay(20);
        viewModel.AdminIdFilter = "7";
        await Task.Delay(20);

        Assert.Equal((null, "student", 7), apiClient.LastActivityLogFilter);
    }

    [Fact]
    public async Task BlankAdminIdFilter_IsTreatedAsNoFilter_NotZero()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new AuditLogViewModel(apiClient) { AdminIdFilter = "" };
        await Task.Delay(20);

        Assert.Null(apiClient.LastActivityLogFilter?.AdminId);
    }
}
