using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

public class GenericCreateViewModelTests
{
    [Fact]
    public async Task SaveAsync_MissingRequiredField_SetsErrorMessage_AndDoesNotSubmit()
    {
        var apiClient = new FakeApiClient();
        var fields = new[] { new FormField("title", "Title", isRequired: true), new FormField("body", "Body") };
        var viewModel = new GenericCreateViewModel("New News", fields, values => apiClient.CreateGenericAsync("news", values));

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Title", viewModel.ErrorMessage);
        Assert.Null(apiClient.LastGenericCreateEndpoint);
    }

    [Fact]
    public async Task SaveAsync_AllRequiredFieldsFilled_SubmitsExactKeyValuePairs()
    {
        var apiClient = new FakeApiClient();
        var fields = new[]
        {
            new FormField("title", "Title", isRequired: true) { Value = "Hello" },
            new FormField("excerpt", "Excerpt") { Value = "" }, // optional, left blank
        };
        var viewModel = new GenericCreateViewModel("New News", fields, values => apiClient.CreateGenericAsync("news", values));

        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(savedRaised);
        Assert.Equal("news", apiClient.LastGenericCreateEndpoint);
        Assert.Equal("Hello", apiClient.LastGenericCreateFields!["title"]);
        Assert.Null(apiClient.LastGenericCreateFields["excerpt"]); // blank optional -> null, not ""
    }

    [Fact]
    public async Task SaveAsync_OnApiRequestExceptionWithFieldErrors_JoinsThem()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnCreateGeneric = new ApiRequestException(new ApiErrorPayload
            {
                Code = "VALIDATION_ERROR",
                Message = "Please fix the highlighted fields.",
                FieldErrors = new Dictionary<string, string> { ["slug"] = "Slug is required." },
            }),
        };
        var fields = new[] { new FormField("title", "Title", isRequired: true) { Value = "X" } };
        var viewModel = new GenericCreateViewModel("New News", fields, values => apiClient.CreateGenericAsync("news", values));

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Slug is required.", viewModel.ErrorMessage);
    }

    // --- Final Convergence Phase P0-7: UploadFieldAsync ---

    [Fact]
    public async Task UploadFieldAsync_OnSuccess_SetsFieldValueToTheServerPath_AndSelectedFileName()
    {
        var apiClient = new FakeApiClient { UploadPathToReturn = "assets/uploads/news/server-name.png" };
        var field = new FormField("image", "Image", uploadModule: "news");
        var viewModel = new GenericCreateViewModel("New News", [field], values => apiClient.CreateGenericAsync("news", values), apiClient);

        await viewModel.UploadFieldAsync(field, "my-photo.png", [9, 9, 9]);

        Assert.Equal("assets/uploads/news/server-name.png", field.Value);
        Assert.Equal("my-photo.png", field.SelectedFileName);
        Assert.Equal(("news", "my-photo.png", new byte[] { 9, 9, 9 }), apiClient.LastUpload);
    }

    [Fact]
    public async Task UploadFieldAsync_WithNoApiClientInjected_DoesNothing_NoCrash()
    {
        var apiClient = new FakeApiClient();
        var field = new FormField("image", "Image", uploadModule: "news");
        // Deliberately omit the apiClient parameter — matches every ViewModel construction site that
        // never uploads (e.g. plain-text-only modules).
        var viewModel = new GenericCreateViewModel("New News", [field], values => apiClient.CreateGenericAsync("news", values));

        await viewModel.UploadFieldAsync(field, "x.png", [1]);

        Assert.Equal("", field.Value);
        Assert.Null(apiClient.LastUpload);
    }

    [Fact]
    public async Task UploadFieldAsync_OnFailure_SetsErrorMessage_LeavesFieldValueUnchanged()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnUpload = new ApiRequestException(new ApiErrorPayload
            {
                Code = "VALIDATION_ERROR",
                Message = "The file content does not match its extension.",
            }),
        };
        var field = new FormField("image", "Image", uploadModule: "news") { Value = "assets/uploads/news/old.png" };
        var viewModel = new GenericCreateViewModel("New News", [field], values => apiClient.CreateGenericAsync("news", values), apiClient);

        await viewModel.UploadFieldAsync(field, "evil.png", [1]);

        Assert.Equal("The file content does not match its extension.", viewModel.ErrorMessage);
        Assert.Equal("assets/uploads/news/old.png", field.Value); // unchanged — bad upload never clobbers the existing file
    }
}
