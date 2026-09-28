using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// The reference Create form — Stage 9's canonical pattern extended to authoring, not just
/// viewing. Scope picker mirrors NoticesController's server-side rule (general / department /
/// committee, mutually exclusive) so a validation error here means the same thing it means on the
/// server, not a client-side guess at the rule.
/// </summary>
public sealed partial class NoticeCreateViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private int? _editingId;

    public NoticeCreateViewModel(IApiClient apiClient) => _apiClient = apiClient;

    [ObservableProperty]
    private string _windowTitle = "New Notice";

    public string[] ScopeOptions { get; } = ["General", "Department", "Committee"];

    /// <summary>Switches this form into edit mode, pre-filled from a fresh GET of the record (not
    /// the list row DTO — see IApiClient.GetGenericAsync). Save then calls UpdateGenericAsync
    /// against NoticesController::update() instead of CreateGenericAsync.</summary>
    public void InitializeForEdit(int id, IReadOnlyDictionary<string, string?> fields)
    {
        _editingId = id;
        WindowTitle = "Edit Notice";
        Title = fields.GetValueOrDefault("title") ?? "";
        Body = fields.GetValueOrDefault("body") ?? "";
        Link = fields.GetValueOrDefault("link") ?? "";
        Category = fields.GetValueOrDefault("category") ?? "";
        DepartmentId = fields.GetValueOrDefault("departmentId") ?? "";
        CommitteeId = fields.GetValueOrDefault("committeeId") ?? "";
        SelectedScope = DepartmentId.Length > 0 ? "Department" : CommitteeId.Length > 0 ? "Committee" : "General";
        IsMarquee = fields.GetValueOrDefault("isMarquee") is "1" or "true";
        PublishAt = fields.GetValueOrDefault("publishAt") ?? "";
        ExpireAt = fields.GetValueOrDefault("expireAt") ?? "";
        Status = fields.GetValueOrDefault("status") ?? "published";
        FilePath = fields.GetValueOrDefault("filePath") ?? "";
        SaveCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(IsDepartmentScope), nameof(IsCommitteeScope))]
    private string _selectedScope = "General";

    public bool IsDepartmentScope => SelectedScope == "Department";
    public bool IsCommitteeScope => SelectedScope == "Committee";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _title = "";

    [ObservableProperty]
    private string _body = "";

    [ObservableProperty]
    private string _link = "";

    [ObservableProperty]
    private string _category = "";

    // Final Convergence Phase P0-7: these 4 fields were fillable server-side (and, for isMarquee/
    // publishAt/expireAt/status, already accepted by NoticesController::store()) but had no desktop
    // form field at all — a desktop-created notice could never be marquee, scheduled, drafted, or
    // carry a file. filePath additionally needed a backend fix (store()/update() didn't accept it
    // at all before this same slice) — see NoticesController.php.
    [ObservableProperty]
    private bool _isMarquee;

    [ObservableProperty]
    private string _publishAt = "";

    [ObservableProperty]
    private string _expireAt = "";

    [ObservableProperty]
    private string _status = "published";

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string? _filePathFileName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _departmentId = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _committeeId = "";

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
            int? deptId = IsDepartmentScope && int.TryParse(DepartmentId, out var d) ? d : null;
            int? commId = IsCommitteeScope && int.TryParse(CommitteeId, out var c) ? c : null;

            var fields = new Dictionary<string, string?>
            {
                ["title"] = Title.Trim(),
                ["body"] = string.IsNullOrWhiteSpace(Body) ? null : Body,
                ["link"] = string.IsNullOrWhiteSpace(Link) ? null : Link,
                ["category"] = string.IsNullOrWhiteSpace(Category) ? null : Category,
                ["departmentId"] = deptId?.ToString(),
                ["committeeId"] = commId?.ToString(),
                ["isMarquee"] = IsMarquee ? "1" : "0",
                ["publishAt"] = string.IsNullOrWhiteSpace(PublishAt) ? null : PublishAt.Trim(),
                ["expireAt"] = string.IsNullOrWhiteSpace(ExpireAt) ? null : ExpireAt.Trim(),
                ["status"] = string.IsNullOrWhiteSpace(Status) ? "published" : Status.Trim(),
                ["filePath"] = string.IsNullOrWhiteSpace(FilePath) ? null : FilePath,
            };

            if (_editingId is int id)
            {
                await _apiClient.UpdateGenericAsync("notices", id, fields);
            }
            else
            {
                await _apiClient.CreateGenericAsync("notices", fields);
            }

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Called by the View's code-behind after it reads the picked file's bytes (a View-
    /// layer filesystem concern) — uploads via /api/v1/uploads (module "notices") then stores the
    /// resulting server path into FilePath.</summary>
    public async Task UploadFileAsync(string fileName, byte[] bytes)
    {
        ErrorMessage = null;
        try
        {
            FilePath = await _apiClient.UploadFileAsync("notices", fileName, bytes);
            FilePathFileName = fileName;
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
    }

    private bool CanSave() =>
        !IsBusy
        && Title.Trim().Length > 0
        && (!IsDepartmentScope || int.TryParse(DepartmentId, out _))
        && (!IsCommitteeScope || int.TryParse(CommitteeId, out _));
}
