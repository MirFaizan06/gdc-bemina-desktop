using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Admin account Edit — name/role/active only (AdminsController::update()'s accepted fields).
/// Ban/unban/timeout/force-logout/reset-password stay on AdminsListView's row actions, unchanged;
/// this is specifically the "fix a typo in someone's name" / "promote to dept_admin" / "deactivate
/// without banning" case that had no UI at all before. Pre-filled directly from the AdminSummary
/// row already in memory (the list already carries every field this form needs — no extra GET).
/// </summary>
public sealed partial class AdminEditViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly int _adminId;

    public AdminEditViewModel(IApiClient apiClient, AdminSummary admin)
    {
        _apiClient = apiClient;
        _adminId = admin.Id;
        Email = admin.Email;
        Name = admin.Name;
        SelectedRole = admin.Role;
        IsActive = admin.IsActive;
    }

    public string Email { get; }

    // Mirrors admins.role's ENUM in database/schema.sql + admin_principal_migration.sql.
    public string[] Roles { get; } = ["super_admin", "editor", "committee_manager", "dept_admin", "principal"];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = "";

    [ObservableProperty]
    private string _selectedRole = "editor";

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public event EventHandler? Saved;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _apiClient.UpdateAdminAsync(_adminId, Name.Trim(), SelectedRole, IsActive);
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 }
                ? string.Join(" ", ex.FieldErrors.Values)
                : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !IsBusy && Name.Trim().Length > 0;
}
