using System.Text.Json;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

public class TimetableEntryCreateViewModelTests
{
    private static TimetableEntryCreateViewModel ValidViewModel(FakeApiClient apiClient) => new(apiClient, new FakeLookupCache())
    {
        AcademicYear = "2026-27",
        ProgrammeId = "1",
        Semester = "1",
        Subject = "Data Structures",
        SelectedDay = "mon",
        StartTime = "09:00",
        EndTime = "10:00",
    };

    [Fact]
    public async Task LoadProgrammeOptionsAsync_PopulatesProgrammeOptionsFromTheApi()
    {
        var apiClient = new FakeApiClient
        {
            ProgrammesToReturn = [
                new ProgrammeDto { Id = 1, Name = "BCA", DepartmentName = "Computer Applications" },
                new ProgrammeDto { Id = 2, Name = "BSc", DepartmentName = "Science" },
            ],
        };
        var viewModel = new TimetableEntryCreateViewModel(apiClient, new FakeLookupCache());

        await viewModel.LoadProgrammeOptionsAsync();

        Assert.Equal(2, viewModel.ProgrammeOptions.Count);
        Assert.Equal("1", viewModel.ProgrammeOptions[0].Value);
        Assert.Equal("BCA (Computer Applications)", viewModel.ProgrammeOptions[0].Label);
    }

    [Fact]
    public void CanSave_RequiresAllCoreFieldsAndValidTimeFormat()
    {
        var viewModel = ValidViewModel(new FakeApiClient());
        Assert.True(viewModel.SaveCommand.CanExecute(null));

        viewModel.StartTime = "not-a-time";
        Assert.False(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveAsync_OnConflict_JoinsAllConflictMessagesFromDetails()
    {
        // Mirrors the exact shape TimetableController::describeConflicts() sends.
        var detailsJson = """
            [{"type":"faculty","message":"The assigned faculty is already teaching \"X\" on mon 09:00:00-10:00:00."},
             {"type":"room","message":"The room is already booked for \"Y\" on mon 09:00:00-10:00:00."}]
            """;
        var details = JsonSerializer.Deserialize<JsonElement>(detailsJson);

        var apiClient = new FakeApiClient
        {
            ThrowOnCreateTimetableEntry = new ApiRequestException(new ApiErrorPayload
            {
                Code = "CONFLICT",
                Message = "This slot conflicts with an existing timetable entry.",
                Details = details,
            }),
        };
        var viewModel = ValidViewModel(apiClient);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("faculty is already teaching", viewModel.ErrorMessage);
        Assert.Contains("room is already booked", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_Success_RaisesSaved_WithParsedValues()
    {
        var apiClient = new FakeApiClient();
        var viewModel = ValidViewModel(apiClient);
        viewModel.FacultyId = "5";
        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(savedRaised);
        Assert.Equal(5, apiClient.LastCreatedTimetableEntry!.FacultyId);
        Assert.Equal(1, apiClient.LastCreatedTimetableEntry.ProgrammeId);
    }

    [Fact]
    public void InitializeForEdit_PrefillsFields_AndTrimsTimeColumnsToHoursMinutes()
    {
        var viewModel = new TimetableEntryCreateViewModel(new FakeApiClient(), new FakeLookupCache());
        var fields = new Dictionary<string, string?>
        {
            ["academicYear"] = "2026-27",
            ["programmeId"] = "3",
            ["semester"] = "2",
            ["subject"] = "Algorithms",
            ["dayOfWeek"] = "tue",
            ["startTime"] = "09:00:00", // server TIME columns include seconds
            ["endTime"] = "10:00:00",
        };

        viewModel.InitializeForEdit(9, fields);

        Assert.Equal("Edit Timetable Entry", viewModel.WindowTitle);
        Assert.Equal("Algorithms", viewModel.Subject);
        Assert.Equal("tue", viewModel.SelectedDay);
        Assert.Equal("09:00", viewModel.StartTime); // seconds trimmed so the HH:MM regex still validates
        Assert.Equal("10:00", viewModel.EndTime);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveAsync_AfterInitializeForEdit_CallsUpdateNotCreate()
    {
        var apiClient = new FakeApiClient();
        var viewModel = ValidViewModel(apiClient);
        viewModel.InitializeForEdit(9, new Dictionary<string, string?> { ["subject"] = "Existing" });
        // ValidViewModel's field values remain (InitializeForEdit only overwrites what's in `fields`).

        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(savedRaised);
        Assert.Null(apiClient.LastCreatedTimetableEntry); // Create was never called
        Assert.Equal("timetable", apiClient.LastGenericUpdateEndpoint);
        Assert.Equal(9, apiClient.LastGenericUpdateId);
    }

    [Fact]
    public async Task SaveAsync_EditMode_OnConflict_JoinsAllConflictMessagesFromDetails()
    {
        var detailsJson = """[{"type":"room","message":"The room is already booked for \"Y\" on mon 09:00:00-10:00:00."}]""";
        var details = JsonSerializer.Deserialize<JsonElement>(detailsJson);
        var apiClient = new FakeApiClient
        {
            ThrowOnUpdateGeneric = new ApiRequestException(new ApiErrorPayload
            {
                Code = "CONFLICT",
                Message = "This slot conflicts with an existing timetable entry.",
                Details = details,
            }),
        };
        var viewModel = ValidViewModel(apiClient);
        viewModel.InitializeForEdit(9, new Dictionary<string, string?> { ["subject"] = "Existing" });

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("room is already booked", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_DepartmentScope_SendsParsedDepartmentId()
    {
        var apiClient = new FakeApiClient();
        var viewModel = ValidViewModel(apiClient);
        viewModel.SelectedScope = "Department";
        viewModel.DepartmentId = "3";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(3, apiClient.LastCreatedTimetableEntry!.DepartmentId);
        Assert.Null(apiClient.LastCreatedTimetableEntry.CommitteeId);
    }

    [Fact]
    public async Task SaveAsync_GeneralScope_SendsNullDepartmentAndCommitteeIds()
    {
        var apiClient = new FakeApiClient();
        var viewModel = ValidViewModel(apiClient);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Null(apiClient.LastCreatedTimetableEntry!.DepartmentId);
        Assert.Null(apiClient.LastCreatedTimetableEntry.CommitteeId);
    }

    [Fact]
    public void InitializeForEdit_DepartmentScopedEntry_DerivesDepartmentScope()
    {
        var viewModel = new TimetableEntryCreateViewModel(new FakeApiClient(), new FakeLookupCache());
        var fields = new Dictionary<string, string?> { ["departmentId"] = "4", ["committeeId"] = null };

        viewModel.InitializeForEdit(9, fields);

        Assert.Equal("Department", viewModel.SelectedScope);
        Assert.Equal("4", viewModel.DepartmentId);
        Assert.True(viewModel.IsDepartmentScope);
    }
}
