using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the
/// desktop consumer for the new PrincipalSectionController — the public Principal's-message
/// website content.</summary>
public class PrincipalSectionViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsTheCurrentMessage()
    {
        var apiClient = new FakeApiClient
        {
            PrincipalMessageToReturn = new PrincipalMessageDto { Name = "Dr. Jane Doe", Designation = "Principal", Message = "Welcome." },
        };
        var viewModel = new PrincipalSectionViewModel(apiClient);
        await Task.Delay(20);

        Assert.Equal("Dr. Jane Doe", viewModel.Name);
        Assert.Equal("Principal", viewModel.Designation);
        Assert.Equal("Welcome.", viewModel.Message);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task SaveAsync_SubmitsTheCurrentFieldValues()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new PrincipalSectionViewModel(apiClient);
        await Task.Delay(20);
        viewModel.Name = "Dr. New Name";
        viewModel.Designation = "Officiating Principal";
        viewModel.Message = "An updated message.";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(("Dr. New Name", "Officiating Principal", null, "An updated message."), apiClient.LastPrincipalSectionUpdate);
        Assert.Equal("Saved.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task UploadPhotoAsync_SetsPhotoFromTheUploadedPath()
    {
        var apiClient = new FakeApiClient { UploadPathToReturn = "assets/uploads/principal/abc123.webp" };
        var viewModel = new PrincipalSectionViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.UploadPhotoAsync("photo.jpg", [1, 2, 3]);

        Assert.Equal("assets/uploads/principal/abc123.webp", viewModel.Photo);
        Assert.Equal("photo.jpg", viewModel.DisplayPhotoName);
    }

    [Fact]
    public async Task SaveAsync_OnApiRequestExceptionWithFieldErrors_JoinsThem()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnPrincipalSection = new ApiRequestException(new ApiErrorPayload
            {
                Code = "VALIDATION_ERROR",
                Message = "Please fix the highlighted fields.",
                FieldErrors = new Dictionary<string, string> { ["name"] = "Name must be 150 characters or fewer." },
            }),
        };
        var viewModel = new PrincipalSectionViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Name must be 150 characters or fewer.", viewModel.ErrorMessage);
    }
}
