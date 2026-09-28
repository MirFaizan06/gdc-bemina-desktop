using System.Linq;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the
/// desktop consumer for the new AcademicCalendarController.</summary>
public class AcademicCalendarViewModelTests
{
    /// <summary>Mirrors NavigationService.BuildAcademicCalendarViewModel's own deferred-closure
    /// wiring exactly — the loader reads `viewModel.AcademicYearFilter` at invocation time, not at
    /// construction time, since the ViewModel doesn't exist yet while this list is being built.</summary>
    private static AcademicCalendarViewModel Build(FakeApiClient apiClient)
    {
        AcademicCalendarViewModel? viewModel = null;
        var list = new GenericListViewModel(
            "Academic Calendar", "empty",
            async ct => (await apiClient.GetCalendarEventsAsync(viewModel?.AcademicYearFilter, ct)).Events.Cast<object>().ToList());
        viewModel = new AcademicCalendarViewModel(list, apiClient);
        return viewModel;
    }

    [Fact]
    public async Task Constructor_LoadsAcademicYearOptionsAndDefaultsTheFilter()
    {
        var apiClient = new FakeApiClient
        {
            CalendarEventsToReturn = new CalendarEventsResult
            {
                CurrentAcademicYear = "2026-27",
                AcademicYearOptions = ["2025-26", "2026-27", "2027-28"],
                GoogleSyncConfigured = true,
            },
        };
        var viewModel = Build(apiClient);
        await Task.Delay(20); // constructor's fire-and-forget LoadYearOptionsAsync

        Assert.Equal("2026-27", viewModel.AcademicYearFilter);
        Assert.Equal(3, viewModel.AcademicYearOptions.Count);
        Assert.True(viewModel.GoogleSyncConfigured);
    }

    [Fact]
    public async Task ChangingAcademicYearFilter_ReQueriesTheServerWithTheNewYear()
    {
        var apiClient = new FakeApiClient();
        var viewModel = Build(apiClient);
        await Task.Delay(20);

        viewModel.AcademicYearFilter = "2027-28";
        await Task.Delay(20);

        Assert.Equal("2027-28", apiClient.LastCalendarAcademicYearRequested);
    }

    [Fact]
    public async Task SyncHolidaysAsync_OnSuccess_SetsAResultMessage_AndRefreshesTheList()
    {
        var apiClient = new FakeApiClient
        {
            CalendarSyncResultToReturn = new CalendarSyncResult { Synced = true, Inserted = 5, Updated = 2 },
        };
        var viewModel = Build(apiClient);
        viewModel.AcademicYearFilter = "2026-27";
        await Task.Delay(20);

        await viewModel.SyncHolidaysCommand.ExecuteAsync(null);

        Assert.Equal(("2026-27", false), apiClient.LastCalendarSyncRequest);
        Assert.Contains("5 inserted", viewModel.SyncMessage);
        Assert.Contains("2 updated", viewModel.SyncMessage);
    }

    [Fact]
    public async Task SyncHolidaysAsync_WhenAlreadySynced_ShowsTheServersMessage()
    {
        var apiClient = new FakeApiClient
        {
            CalendarSyncResultToReturn = new CalendarSyncResult { Synced = false, Message = "Already synced." },
        };
        var viewModel = Build(apiClient);
        viewModel.AcademicYearFilter = "2026-27";
        await Task.Delay(20);

        await viewModel.SyncHolidaysCommand.ExecuteAsync(null);

        Assert.Equal("Already synced.", viewModel.SyncMessage);
    }

    [Fact]
    public async Task SyncHolidaysAsync_OnApiFailure_SetsErrorMessage_NotAnUnhandledException()
    {
        var apiClient = new FakeApiClient();
        var viewModel = Build(apiClient);
        viewModel.AcademicYearFilter = "2026-27";
        await Task.Delay(20);
        apiClient.ThrowOnCalendar = new ApiRequestException(new ApiErrorPayload { Code = "VALIDATION_ERROR", Message = "academicYear must be a valid academic year." });

        await viewModel.SyncHolidaysCommand.ExecuteAsync(null);

        Assert.Equal("academicYear must be a valid academic year.", viewModel.SyncMessage);
        Assert.False(viewModel.IsSyncing);
    }
}
