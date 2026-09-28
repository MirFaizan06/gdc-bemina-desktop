using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>
/// Exercises the exact ViewModel logic a real click-through of LoginWindow would exercise:
/// CanExecute gating on both fields being non-empty, successful login raising LoginSucceeded,
/// and a failed login surfacing ErrorMessage instead of throwing past the ViewModel. Written
/// after OS-level UI input simulation (SendKeys/mouse-click against the running app) proved
/// unreliable in this sandboxed session due to Windows' foreground-focus-stealing protection —
/// see docs/claude/18_DEVLOG.md Stage 3 entry. This is arguably the better verification anyway:
/// deterministic and safe to run in CI, unlike driving a real GUI window.
/// </summary>
public class LoginViewModelTests
{
    [Fact]
    public void LoginCommand_CannotExecute_WhenEmailOrPasswordAreEmpty()
    {
        var viewModel = new LoginViewModel(new FakeAuthSessionService());

        Assert.False(viewModel.LoginCommand.CanExecute(null));

        viewModel.Email = "admin@test.local";
        Assert.False(viewModel.LoginCommand.CanExecute(null)); // password still empty

        viewModel.Password = "secret";
        Assert.True(viewModel.LoginCommand.CanExecute(null));
    }

    [Fact]
    public async Task LoginCommand_OnSuccess_CallsAuthServiceAndRaisesLoginSucceeded()
    {
        var authService = new FakeAuthSessionService();
        var viewModel = new LoginViewModel(authService)
        {
            Email = "  admin@test.local  ", // deliberately padded — the ViewModel must trim it
            Password = "TestPassword123!",
        };

        var succeededRaised = false;
        viewModel.LoginSucceeded += (_, _) => succeededRaised = true;

        await viewModel.LoginCommand.ExecuteAsync(null);

        Assert.Equal("admin@test.local", authService.LastLoginEmail);
        Assert.Equal("TestPassword123!", authService.LastLoginPassword);
        Assert.True(succeededRaised);
        Assert.Null(viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task LoginCommand_OnApiRequestException_SetsErrorMessage_AndDoesNotRaiseLoginSucceeded()
    {
        var authService = new FakeAuthSessionService
        {
            ThrowOnLogin = new ApiRequestException(new ApiErrorPayload
            {
                Code = "AUTHENTICATION_ERROR",
                Message = "Incorrect email or password.",
            }),
        };
        var viewModel = new LoginViewModel(authService) { Email = "admin@test.local", Password = "wrong" };

        var succeededRaised = false;
        viewModel.LoginSucceeded += (_, _) => succeededRaised = true;

        await viewModel.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Incorrect email or password.", viewModel.ErrorMessage);
        Assert.False(succeededRaised);
        Assert.False(viewModel.IsBusy);
    }
}
