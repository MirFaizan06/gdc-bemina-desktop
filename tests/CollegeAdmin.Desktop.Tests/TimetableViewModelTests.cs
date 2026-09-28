using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Tests;

public class TimetableViewModelTests
{
    private static TimetableEntryDto Entry(int id, string day, string startTime, int programmeId = 1, int semester = 1) => new()
    {
        Id = id,
        AcademicYear = "2026-27",
        ProgrammeId = programmeId,
        Semester = semester,
        Subject = $"Subject {id}",
        DayOfWeek = day,
        StartTime = startTime,
        EndTime = "10:00:00",
        Status = "draft",
    };

    private static GenericListViewModel BuildList(IReadOnlyList<object> entries) =>
        new("Timetable", "None yet.", _ => Task.FromResult(entries));

    [Fact]
    public async Task Constructor_PopulatesProgrammeOptions_WithAnAllProgrammesEntryFirst()
    {
        var list = BuildList([]);
        await Task.Delay(5);
        var apiClient = new FakeApiClient
        {
            ProgrammesToReturn = [new ProgrammeDto { Id = 7, Name = "BSc", DepartmentName = "Science" }],
        };

        var viewModel = new TimetableViewModel(list, apiClient, new FakeLookupCache());
        await Task.Delay(5); // let the constructor's fire-and-forget load settle

        Assert.Equal(2, viewModel.ProgrammeOptions.Count);
        Assert.Equal("(All programmes)", viewModel.ProgrammeOptions[0].Label);
        Assert.Equal("7", viewModel.ProgrammeOptions[1].Value);
    }

    [Fact]
    public async Task Constructor_BuildsOneGridRowPerDistinctStartTime_SortedAscending()
    {
        var list = BuildList([
            Entry(1, "mon", "10:00:00"),
            Entry(2, "tue", "09:00:00"),
        ]);
        await Task.Delay(5); // let the wrapped list's constructor LoadAsync settle

        var viewModel = new TimetableViewModel(list, new FakeApiClient(), new FakeLookupCache());

        Assert.Equal(2, viewModel.GridRows.Count);
        Assert.Equal("09:00", viewModel.GridRows[0].TimeLabel);
        Assert.Equal("10:00", viewModel.GridRows[1].TimeLabel);
    }

    [Fact]
    public async Task Constructor_PlacesEntriesInTheCorrectDayColumn()
    {
        var list = BuildList([Entry(1, "wed", "09:00:00")]);
        await Task.Delay(5);

        var viewModel = new TimetableViewModel(list, new FakeApiClient(), new FakeLookupCache());

        var row = Assert.Single(viewModel.GridRows);
        Assert.Single(row.Wed);
        Assert.Empty(row.Mon);
        Assert.Empty(row.Tue);
    }

    [Fact]
    public async Task Constructor_TwoEntriesSameDayAndTime_BothKeptInThatCell()
    {
        // Different sections/programmes can legitimately share a day+time slot in different rooms
        // — the grid must not silently drop one.
        var list = BuildList([
            Entry(1, "mon", "09:00:00", programmeId: 1),
            Entry(2, "mon", "09:00:00", programmeId: 2),
        ]);
        await Task.Delay(5);

        var viewModel = new TimetableViewModel(list, new FakeApiClient(), new FakeLookupCache());

        var row = Assert.Single(viewModel.GridRows);
        Assert.Equal(2, row.Mon.Count);
    }

    [Fact]
    public async Task FilterProgrammeId_NarrowsTheGridToMatchingEntriesOnly()
    {
        var list = BuildList([
            Entry(1, "mon", "09:00:00", programmeId: 1),
            Entry(2, "mon", "09:00:00", programmeId: 2),
        ]);
        await Task.Delay(5);
        var viewModel = new TimetableViewModel(list, new FakeApiClient(), new FakeLookupCache());

        viewModel.FilterProgrammeId = "2";

        var row = Assert.Single(viewModel.GridRows);
        Assert.Single(row.Mon);
        Assert.Equal(2, row.Mon[0].Id);
    }

    [Fact]
    public async Task ToggleViewCommand_FlipsIsGridView()
    {
        var list = BuildList([]);
        await Task.Delay(5);
        var viewModel = new TimetableViewModel(list, new FakeApiClient(), new FakeLookupCache());
        Assert.True(viewModel.IsGridView);

        viewModel.ToggleViewCommand.Execute(null);

        Assert.False(viewModel.IsGridView);
    }
}
