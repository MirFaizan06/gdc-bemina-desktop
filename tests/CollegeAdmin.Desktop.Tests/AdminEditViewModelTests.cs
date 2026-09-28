using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

public class AdminEditViewModelTests
{
    private static AdminSummary SampleAdmin() => new()
    {
        Id = 11,
        Name = "Jane Doe",
        Email = "jane@example.test",
        Role = "editor",
        IsActive = true,
    };

    [Fact]
    public void Constructor_PrefillsFromTheAdminSummaryRow()
    {
        var viewModel = new AdminEditViewModel(new FakeApiClient(), SampleAdmin());

        Assert.Equal("jane@example.test", viewModel.Email);
        Assert.Equal("Jane Doe", viewModel.Name);
        Assert.Equal("editor", viewModel.SelectedRole);
        Assert.True(viewModel.IsActive);
    }

    [Fact]
    public void CanSave_RequiresNonBlankName()
    {
        var viewModel = new AdminEditViewModel(new FakeApiClient(), SampleAdmin());
        Assert.True(viewModel.SaveCommand.CanExecute(null));

        viewModel.Name = "   ";
        Assert.False(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveAsync_SendsUpdatedNameRoleAndActiveState()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new AdminEditViewModel(apiClient, SampleAdmin())
        {
            Name = "Jane A. Doe",
            SelectedRole = "dept_admin",
            IsActive = false,
        };

        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(savedRaised);
        Assert.Equal(11, apiClient.LastUpdatedAdminId);
        Assert.Equal("Jane A. Doe", apiClient.LastUpdatedAdminName);
        Assert.Equal("dept_admin", apiClient.LastUpdatedAdminRole);
        Assert.False(apiClient.LastUpdatedAdminIsActive);
    }

    [Fact]
    public async Task SaveAsync_OnApiRequestException_SetsErrorMessage_AndDoesNotRaiseSaved()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnUpdateAdmin = new ApiRequestException(new ApiErrorPayload
            {
                Code = "AUTHORIZATION_ERROR",
                Message = "You do not have permission to perform this action.",
            }),
        };
        var viewModel = new AdminEditViewModel(apiClient, SampleAdmin());
        var savedRaised = false;
        viewModel.Saved += (_, _) => savedRaised = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(savedRaised);
        Assert.Equal("You do not have permission to perform this action.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }
}
