using System.Linq;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

public class StudentEditViewModelTests
{
    [Fact]
    public async Task SaveDraftAsync_CreateMode_PostsFinalizeFalse_AndRaisesSaved()
    {
        var apiClient = new FakeApiClient { StudentResultToReturn = new StudentResult { Student = new StudentDto { Id = 5, Lifecycle = "draft" } } };
        var viewModel = new StudentEditViewModel(apiClient);
        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveDraftCommand.ExecuteAsync(null);

        Assert.True(savedRaised);
        Assert.False(apiClient.LastSaveStudentFinalize);
        Assert.Equal("draft", viewModel.Lifecycle);
    }

    [Fact]
    public async Task FinalizeAsync_CreateMode_PostsFinalizeTrue()
    {
        var apiClient = new FakeApiClient { StudentResultToReturn = new StudentResult { Student = new StudentDto { Id = 5, Lifecycle = "active" } } };
        var viewModel = new StudentEditViewModel(apiClient);

        await viewModel.FinalizeCommand.ExecuteAsync(null);

        Assert.True(apiClient.LastSaveStudentFinalize);
        Assert.Equal("active", viewModel.Lifecycle);
        Assert.False(viewModel.IsDraft);
    }

    [Fact]
    public async Task SaveDraftAsync_CollectsFieldValues_BlankFieldsBecomeNull()
    {
        var apiClient = new FakeApiClient { StudentResultToReturn = new StudentResult { Student = new StudentDto { Id = 5 } } };
        var viewModel = new StudentEditViewModel(apiClient);
        var nameField = viewModel.Sections.SelectMany(s => s.Fields).First(f => f.Key == "name");
        nameField.Value = "Test Student";

        await viewModel.SaveDraftCommand.ExecuteAsync(null);

        Assert.Equal("Test Student", apiClient.LastSavedStudentFields!["name"]);
        Assert.Null(apiClient.LastSavedStudentFields["dob"]); // left blank -> null, not ""
    }

    /// <summary>Final Convergence Phase P1-19 (docs/claude/FINAL_COMPLETION_TRACKER.md §2):
    /// StudentDto has carried ProgrammeId since P0-4, but this form had zero UI for it — the exact
    /// gap named and deliberately deferred at the time, now closed by reusing FormField.Options.</summary>
    [Fact]
    public void Constructor_EditMode_PrefillsProgrammeIdFromTheExistingStudent()
    {
        var apiClient = new FakeApiClient();
        var options = new[] { new FormFieldOption("3", "BCA (Computer Science)") };
        var viewModel = new StudentEditViewModel(apiClient, new StudentDto { Id = 5, ProgrammeId = 3 }, options);

        var programmeField = viewModel.Sections.SelectMany(s => s.Fields).First(f => f.Key == "programmeId");

        Assert.Equal("3", programmeField.Value);
        Assert.True(programmeField.HasOptions);
        Assert.Single(programmeField.Options!);
    }

    [Fact]
    public async Task SaveDraftAsync_SubmitsTheSelectedProgrammeId()
    {
        var apiClient = new FakeApiClient { StudentResultToReturn = new StudentResult { Student = new StudentDto { Id = 5 } } };
        var options = new[] { new FormFieldOption("3", "BCA (Computer Science)") };
        var viewModel = new StudentEditViewModel(apiClient, existing: null, options);
        var programmeField = viewModel.Sections.SelectMany(s => s.Fields).First(f => f.Key == "programmeId");
        programmeField.Value = "3";

        await viewModel.SaveDraftCommand.ExecuteAsync(null);

        Assert.Equal("3", apiClient.LastSavedStudentFields!["programmeId"]);
    }

    [Fact]
    public async Task SaveDraftAsync_OnApiRequestExceptionWithFieldErrors_JoinsThem()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnSaveStudent = new ApiRequestException(new ApiErrorPayload
            {
                Code = "VALIDATION_ERROR",
                Message = "Please fix the highlighted fields.",
                FieldErrors = new Dictionary<string, string> { ["name"] = "Provide at least a name or a Board Registration Number." },
            }),
        };
        var viewModel = new StudentEditViewModel(apiClient);

        await viewModel.SaveDraftCommand.ExecuteAsync(null);

        Assert.Equal("Provide at least a name or a Board Registration Number.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveDraftAsync_SurfacesPossibleDuplicates_WithoutBlockingTheSave()
    {
        var apiClient = new FakeApiClient
        {
            StudentResultToReturn = new StudentResult
            {
                Student = new StudentDto { Id = 6, Lifecycle = "draft" },
                PossibleDuplicates = [new PossibleDuplicateDto { Field = "mobile", Student = new StudentDto { Id = 5, Name = "Existing Student" } }],
            },
        };
        var viewModel = new StudentEditViewModel(apiClient);
        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveDraftCommand.ExecuteAsync(null);

        Assert.True(savedRaised); // a soft duplicate warning never blocks the save
        Assert.Single(viewModel.DuplicateWarnings);
        Assert.Contains("Existing Student", viewModel.DuplicateWarnings[0]);
    }

    [Fact]
    public void EditMode_CanArchive_IsFalseWhenAlreadyArchived()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new StudentEditViewModel(apiClient, new StudentDto { Id = 5, Lifecycle = "archived" });

        Assert.False(viewModel.CanArchive);
    }

    [Fact]
    public void CreateMode_CanArchive_IsAlwaysFalse()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new StudentEditViewModel(apiClient);

        Assert.False(viewModel.CanArchive);
    }
}
