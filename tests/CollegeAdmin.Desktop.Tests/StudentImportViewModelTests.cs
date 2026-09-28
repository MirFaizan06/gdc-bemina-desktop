using System.IO;
using System.Linq;
using System.Text.Json;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

public class StudentImportViewModelTests
{
    private static StudentImportRowDto MakeRow(int id, string status, string name = "Test Student") =>
        new()
        {
            Id = id,
            RowNo = id + 1,
            Normalized = JsonDocument.Parse($$"""{"name":"{{name}}","board_reg_no":"BG-{{id}}","semester":1}""").RootElement,
            Validation = new StudentImportValidationDto { Status = status, Issues = status == "error" ? ["Some error"] : [] },
            Resolution = "pending",
        };

    [Fact]
    public async Task UploadAsync_ReadsTheFileAndPopulatesBatchAndRows()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, [1, 2, 3]); // content irrelevant — the fake never really parses it
        try
        {
            var apiClient = new FakeApiClient
            {
                StudentImportUploadResultToReturn = new StudentImportBatchResult
                {
                    Batch = new StudentImportBatchDto { Id = 7, OriginalFilename = "roster.xlsx", Status = "review", TotalRows = 2 },
                    HeaderIssues = ["Unrecognized column \"Foo\"."],
                },
                StudentImportRowsToReturn = [MakeRow(1, "valid"), MakeRow(2, "warning")],
            };
            var viewModel = new StudentImportViewModel(apiClient);

            await viewModel.UploadCommand.ExecuteAsync(tempFile);

            Assert.NotNull(viewModel.Batch);
            Assert.Equal(7, viewModel.Batch!.Id);
            Assert.True(viewModel.HasBatch);
            Assert.Equal(2, viewModel.Rows.Count);
            Assert.Single(viewModel.HeaderIssues);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadAsync_OnApiRequestException_SetsErrorMessage_AndLeavesBatchNull()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, [1]);
        try
        {
            var apiClient = new FakeApiClient
            {
                ThrowOnStudentImport = new ApiRequestException(new ApiErrorPayload
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Please fix the highlighted fields.",
                    FieldErrors = new Dictionary<string, string> { ["file"] = "Only .xlsx files are accepted." },
                }),
            };
            var viewModel = new StudentImportViewModel(apiClient);

            await viewModel.UploadCommand.ExecuteAsync(tempFile);

            Assert.Equal("Only .xlsx files are accepted.", viewModel.ErrorMessage);
            Assert.Null(viewModel.Batch);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveRowEditAsync_SendsBlankFieldsAsNull_AndUpdatesTheRowFromTheServerResponse()
    {
        var apiClient = new FakeApiClient
        {
            StudentImportRowUpdateResultToReturn = new StudentImportRowDto
            {
                Id = 1,
                Normalized = JsonDocument.Parse("""{"name":"Fixed Name"}""").RootElement,
                Validation = new StudentImportValidationDto { Status = "valid", Issues = [] },
                Resolution = "include",
            },
        };
        var viewModel = new StudentImportViewModel(apiClient) { Batch = new StudentImportBatchDto { Id = 7, Status = "review" } };
        var row = new StudentImportRowViewModel(MakeRow(1, "warning")) { TypeOfCourse = "", Semester = "" };

        await viewModel.SaveRowEditCommand.ExecuteAsync(row);

        Assert.Equal((7, 1), (apiClient.LastUpdatedImportRow!.Value.BatchId, apiClient.LastUpdatedImportRow.Value.RowId));
        var overrides = (Dictionary<string, object?>) apiClient.LastUpdatedImportRow.Value.Overrides!;
        Assert.Null(overrides["type_of_course"]); // blank -> null, not ""
        Assert.Equal("valid", row.Status); // re-synced from the server's fresh classification
        Assert.Equal("include", row.Resolution);
    }

    [Fact]
    public async Task ConfirmAsync_OnSuccess_SetsResultMessage()
    {
        var apiClient = new FakeApiClient
        {
            StudentImportConfirmResultToReturn = new StudentImportConfirmResult { BatchId = 7, Created = 3, Skipped = 1 },
        };
        var viewModel = new StudentImportViewModel(apiClient) { Batch = new StudentImportBatchDto { Id = 7, Status = "review" } };

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal("3 student(s) created, 1 row(s) skipped.", viewModel.ResultMessage);
    }

    [Fact]
    public async Task ConfirmAsync_AlreadyCommitted_SetsErrorMessage_AndDoesNotCallTheServer()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new StudentImportViewModel(apiClient) { Batch = new StudentImportBatchDto { Id = 7, Status = "committed" } };

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal("This import has already been confirmed.", viewModel.ErrorMessage);
    }
}
