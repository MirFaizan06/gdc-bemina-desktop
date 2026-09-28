using CollegeAdmin.Application.Auth;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

public sealed partial class LoginViewModel(IAuthSessionService authSessionService) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _email = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Raised on a successful login — App.xaml.cs swaps to MainWindow. A real
    /// navigation service arrives in Stage 4; this is intentionally minimal for now.</summary>
    public event EventHandler? LoginSucceeded;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            await authSessionService.LoginAsync(Email.Trim(), Password);
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiRequestException ex)
        {
            // Direct catch of an Infrastructure exception type here is a deliberate, small
            // exception to "ViewModels don't touch Infrastructure directly" — Stage 5 builds the
            // general error/retry/toast mapping (docs/claude/09_ERROR_RETRY_TOAST_SYSTEM.md);
            // until then this is the minimum needed to show a real login error instead of none.
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLogin() => !IsBusy && Email.Trim().Length > 0 && Password.Length > 0;
}
