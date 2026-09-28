using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Tests;

public class UniversityRrExportViewModelTests
{
    [Fact]
    public async Task Constructor_PopulatesProgrammeOptions_WithAnAllProgrammesEntryFirst()
    {
        var apiClient = new FakeApiClient
        {
            ProgrammesToReturn = [new ProgrammeDto { Id = 5, Name = "BCA", DepartmentName = "CS" }],
        };

        var viewModel = new UniversityRrExportViewModel(apiClient, new FakeLookupCache());
        await Task.Delay(5); // let the constructor's fire-and-forget load settle

        Assert.Equal(2, viewModel.ProgrammeOptions.Count);
        Assert.Equal("", viewModel.ProgrammeOptions[0].Value);
        Assert.Equal("5", viewModel.ProgrammeOptions[1].Value);
    }

    [Fact]
    public async Task PreviewAsync_WithMissingScope_SetsErrorMessage_AndDoesNotCallTheServer()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new UniversityRrExportViewModel(apiClient, new FakeLookupCache()) { AcademicSession = "", Semester = "3" };

        await viewModel.PreviewCommand.ExecuteAsync(null);

        Assert.Equal("Academic Session and a valid Semester are both required.", viewModel.ErrorMessage);
        Assert.Null(apiClient.LastRrPreviewScope);
    }

    [Fact]
    public async Task PreviewAsync_WithValidScope_PopulatesRowsAndTotals()
    {
        var apiClient = new FakeApiClient
        {
            UniversityRrPreviewResultToReturn = new UniversityRrPreviewResult
            {
                Headers = ["SemesterBatch"],
                Rows = [new UniversityRrPreviewRowDto { StudentId = 1, StudentName = "A", Status = "ready" }],
                Totals = new UniversityRrTotalsDto { Total = 1, Ready = 1 },
            },
        };
        var viewModel = new UniversityRrExportViewModel(apiClient, new FakeLookupCache()) { AcademicSession = "2026-27", Semester = "3" };

        await viewModel.PreviewCommand.ExecuteAsync(null);

        Assert.Equal((("2026-27", 3, (int?) null)), apiClient.LastRrPreviewScope);
        Assert.Single(viewModel.Rows);
        Assert.Equal(1, viewModel.Totals!.Ready);
    }

    [Fact]
    public async Task PreviewAsync_ParsesOptionalProgrammeId()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new UniversityRrExportViewModel(apiClient, new FakeLookupCache())
        {
            AcademicSession = "2026-27", Semester = "3", ProgrammeId = "5",
        };

        await viewModel.PreviewCommand.ExecuteAsync(null);

        Assert.Equal(("2026-27", 3, (int?) 5), apiClient.LastRrPreviewScope);
    }

    [Fact]
    public async Task PreviewAsync_WhenMappingNotVerified_SetsShowMappingWarningTrue()
    {
        var apiClient = new FakeApiClient
        {
            UniversityRrPreviewResultToReturn = new UniversityRrPreviewResult { MappingVerified = false },
        };
        var viewModel = new UniversityRrExportViewModel(apiClient, new FakeLookupCache()) { AcademicSession = "2026-27", Semester = "3" };

        await viewModel.PreviewCommand.ExecuteAsync(null);

        Assert.False(viewModel.MappingVerified);
        Assert.True(viewModel.ShowMappingWarning);
    }

    [Fact]
    public async Task PreviewAsync_WhenMappingVerified_ShowMappingWarningIsFalse()
    {
        var apiClient = new FakeApiClient
        {
            UniversityRrPreviewResultToReturn = new UniversityRrPreviewResult { MappingVerified = true },
        };
        var viewModel = new UniversityRrExportViewModel(apiClient, new FakeLookupCache()) { AcademicSession = "2026-27", Semester = "3" };

        await viewModel.PreviewCommand.ExecuteAsync(null);

        Assert.True(viewModel.MappingVerified);
        Assert.False(viewModel.ShowMappingWarning);
    }

    [Fact]
    public void ShowMappingWarning_BeforeAnyPreviewOrExport_IsFalse()
    {
        var viewModel = new UniversityRrExportViewModel(new FakeApiClient(), new FakeLookupCache());

        Assert.False(viewModel.ShowMappingWarning);
    }

    [Fact]
    public async Task ExportAsync_OnSuccess_RaisesExportReady_AndSetsResultMessage()
    {
        var apiClient = new FakeApiClient
        {
            UniversityRrExportResultToReturn = ("RR_BCA_Semester3_2026-09-26.xlsx", [1, 2, 3], new UniversityRrTotalsDto { Total = 2 }, false),
        };
        var viewModel = new UniversityRrExportViewModel(apiClient, new FakeLookupCache()) { AcademicSession = "2026-27", Semester = "3" };
        (string Filename, byte[] Bytes)? raised = null;
        viewModel.ExportReady += (_, e) => raised = e;

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.NotNull(raised);
        Assert.Equal("RR_BCA_Semester3_2026-09-26.xlsx", raised!.Value.Filename);
        Assert.Contains("2 student(s)", viewModel.ResultMessage);
    }

    [Fact]
    public async Task ExportAsync_OnBlockedRows_SurfacesTheServerErrorMessage()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnUniversityRr = new Infrastructure.Api.ApiRequestException(new ApiErrorPayload
            {
                Code = "VALIDATION_ERROR",
                Message = "1 student row(s) are Blocked — resolve them before exporting.",
                FieldErrors = new Dictionary<string, string> { ["scope"] = "1 student row(s) are Blocked — resolve them before exporting." },
            }),
        };
        var viewModel = new UniversityRrExportViewModel(apiClient, new FakeLookupCache()) { AcademicSession = "2026-27", Semester = "3" };

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.Contains("Blocked", viewModel.ErrorMessage);
    }
}
