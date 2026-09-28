using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-20 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the desktop
/// consumer for the new `/programmes/{id}/papers` nested-resource API.</summary>
public class ProgrammePapersManagerViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsPapersForTheProgramme()
    {
        var apiClient = new FakeApiClient();
        apiClient.ProgrammePapers.Add(new ProgrammePaperDto { Id = 1, ProgrammeId = 5, Title = "Data Structures", PaperType = "Major" });
        var viewModel = new ProgrammePapersManagerViewModel(apiClient, 5, "BCA");
        await Task.Delay(1);

        Assert.Single(viewModel.Papers);
        Assert.Equal("Data Structures", viewModel.Papers[0].Title);
    }

    [Fact]
    public void CanAddPaper_RequiresNonBlankTitle()
    {
        var viewModel = new ProgrammePapersManagerViewModel(new FakeApiClient(), 5, "BCA");
        Assert.False(viewModel.AddPaperCommand.CanExecute(null));

        viewModel.NewTitle = "Data Structures";
        Assert.True(viewModel.AddPaperCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddPaperAsync_AddsThePaper_AndClearsTheTitleAndCode()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new ProgrammePapersManagerViewModel(apiClient, 5, "BCA")
        {
            NewTitle = "Data Structures",
            NewSemester = "1",
            NewPaperType = "Major",
            NewPaperCode = "CS101",
        };
        await Task.Delay(1);

        await viewModel.AddPaperCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Papers);
        Assert.Equal("Data Structures", apiClient.ProgrammePapers[0].Title);
        Assert.Equal("Major", apiClient.ProgrammePapers[0].PaperType);
        Assert.Equal("CS101", apiClient.ProgrammePapers[0].PaperCode);
        Assert.Equal("", viewModel.NewTitle);
        Assert.Equal("", viewModel.NewPaperCode);
    }

    [Fact]
    public async Task DeletePaperAsync_RemovesThePaper()
    {
        var apiClient = new FakeApiClient();
        apiClient.ProgrammePapers.Add(new ProgrammePaperDto { Id = 1, ProgrammeId = 5, Title = "Data Structures" });
        var viewModel = new ProgrammePapersManagerViewModel(apiClient, 5, "BCA");
        await Task.Delay(1);

        await viewModel.DeletePaperCommand.ExecuteAsync(viewModel.Papers[0]);

        Assert.Empty(viewModel.Papers);
        Assert.Empty(apiClient.ProgrammePapers);
    }

    [Fact]
    public async Task AddPaperAsync_OnApiRequestException_SetsErrorMessage()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnProgrammePapers = new ApiRequestException(new ApiErrorPayload
            {
                Code = "VALIDATION_ERROR",
                Message = "Validation failed.",
                FieldErrors = new Dictionary<string, string> { ["title"] = "Title is required." },
            }),
        };
        var viewModel = new ProgrammePapersManagerViewModel(apiClient, 5, "BCA") { NewTitle = "x" };
        await Task.Delay(1);

        await viewModel.AddPaperCommand.ExecuteAsync(null);

        Assert.Equal("Title is required.", viewModel.ErrorMessage);
    }
}
