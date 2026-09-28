using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

public class NoticeCreateViewModelTests
{
    [Fact]
    public void CanSave_RequiresTitle()
    {
        var viewModel = new NoticeCreateViewModel(new FakeApiClient());

        Assert.False(viewModel.SaveCommand.CanExecute(null));

        viewModel.Title = "A notice";
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("Department")]
    [InlineData("Committee")]
    public void CanSave_ScopedNotice_RequiresValidNumericId(string scope)
    {
        var viewModel = new NoticeCreateViewModel(new FakeApiClient()) { Title = "A notice", SelectedScope = scope };

        // No ID entered yet for the selected scope -> cannot save.
        Assert.False(viewModel.SaveCommand.CanExecute(null));

        if (scope == "Department") viewModel.DepartmentId = "not-a-number";
        else viewModel.CommitteeId = "not-a-number";
        Assert.False(viewModel.SaveCommand.CanExecute(null));

        if (scope == "Department") viewModel.DepartmentId = "1";
        else viewModel.CommitteeId = "1";
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveAsync_GeneralScope_SendsNullDepartmentAndCommitteeIds()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new NoticeCreateViewModel(apiClient) { Title = "General notice" };

        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(savedRaised);
        Assert.NotNull(apiClient.LastGenericCreateFields);
        Assert.Equal("notices", apiClient.LastGenericCreateEndpoint);
        Assert.Null(apiClient.LastGenericCreateFields!["departmentId"]);
        Assert.Null(apiClient.LastGenericCreateFields["committeeId"]);
    }

    [Fact]
    public async Task SaveAsync_DepartmentScope_SendsParsedDepartmentId()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new NoticeCreateViewModel(apiClient)
        {
            Title = "Dept notice",
            SelectedScope = "Department",
            DepartmentId = "42",
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("42", apiClient.LastGenericCreateFields!["departmentId"]);
        Assert.Null(apiClient.LastGenericCreateFields["committeeId"]);
    }

    [Fact]
    public async Task SaveAsync_SendsMarqueePublishExpireStatusAndFilePath()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new NoticeCreateViewModel(apiClient)
        {
            Title = "Notice",
            IsMarquee = true,
            PublishAt = "2026-10-01 09:00:00",
            ExpireAt = "2026-10-31 23:59:59",
            Status = "draft",
            FilePath = "assets/uploads/notices/abc.pdf",
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("1", apiClient.LastGenericCreateFields!["isMarquee"]);
        Assert.Equal("2026-10-01 09:00:00", apiClient.LastGenericCreateFields["publishAt"]);
        Assert.Equal("2026-10-31 23:59:59", apiClient.LastGenericCreateFields["expireAt"]);
        Assert.Equal("draft", apiClient.LastGenericCreateFields["status"]);
        Assert.Equal("assets/uploads/notices/abc.pdf", apiClient.LastGenericCreateFields["filePath"]);
    }

    [Fact]
    public async Task UploadFileAsync_OnSuccess_SetsFilePathToServerPath()
    {
        var apiClient = new FakeApiClient { UploadPathToReturn = "assets/uploads/notices/real-server-path.pdf" };
        var viewModel = new NoticeCreateViewModel(apiClient);

        await viewModel.UploadFileAsync("myfile.pdf", [1, 2, 3]);

        Assert.Equal("assets/uploads/notices/real-server-path.pdf", viewModel.FilePath);
        Assert.Equal("myfile.pdf", viewModel.FilePathFileName);
        Assert.Equal(("notices", "myfile.pdf", new byte[] { 1, 2, 3 }), apiClient.LastUpload);
    }

    [Fact]
    public void InitializeForEdit_PrefillsFieldsAndDerivesScope()
    {
        var viewModel = new NoticeCreateViewModel(new FakeApiClient());
        var fields = new Dictionary<string, string?>
        {
            ["title"] = "Existing Notice",
            ["body"] = "Some body",
            ["departmentId"] = "5",
            ["committeeId"] = null,
        };

        viewModel.InitializeForEdit(7, fields);

        Assert.Equal("Edit Notice", viewModel.WindowTitle);
        Assert.Equal("Existing Notice", viewModel.Title);
        Assert.Equal("Some body", viewModel.Body);
        Assert.Equal("Department", viewModel.SelectedScope);
        Assert.Equal("5", viewModel.DepartmentId);
    }

    [Fact]
    public async Task SaveAsync_AfterInitializeForEdit_CallsUpdateNotCreate()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new NoticeCreateViewModel(apiClient);
        viewModel.InitializeForEdit(7, new Dictionary<string, string?> { ["title"] = "Existing Notice" });

        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(savedRaised);
        Assert.Null(apiClient.LastGenericCreateFields); // Create was never called
        Assert.Equal("notices", apiClient.LastGenericUpdateEndpoint);
        Assert.Equal(7, apiClient.LastGenericUpdateId);
        Assert.Equal("Existing Notice", apiClient.LastGenericUpdateFields!["title"]);
    }

    [Fact]
    public async Task SaveAsync_OnApiRequestException_SetsErrorMessage_AndDoesNotRaiseSaved()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnCreateGeneric = new ApiRequestException(new ApiErrorPayload
            {
                Code = "AUTHORIZATION_ERROR",
                Message = "You do not have permission to perform this action.",
            }),
        };
        var viewModel = new NoticeCreateViewModel(apiClient) { Title = "Notice" };
        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(savedRaised);
        Assert.Equal("You do not have permission to perform this action.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }
}
