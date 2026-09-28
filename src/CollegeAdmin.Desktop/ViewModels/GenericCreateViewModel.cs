using System.Collections.ObjectModel;
using System.Linq;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Dynamic Create form for the flat content modules — one class replaces 8 near-identical
/// ViewModels the same way GenericListViewModel replaced 8 near-identical list screens. Client-
/// side validation only checks "required fields are non-empty"; every real business rule (slug
/// format, enum values, FK existence...) is enforced server-side and surfaces here as
/// ApiRequestException.FieldErrors/Message, per the Source of Truth rule — this form never
/// duplicates server validation logic, only reflects its result.
/// </summary>
public sealed partial class GenericCreateViewModel : ObservableObject
{
    private readonly Func<IReadOnlyDictionary<string, string?>, Task> _submit;
    private readonly IApiClient? _apiClient;

    public GenericCreateViewModel(string title, IEnumerable<FormField> fields, Func<IReadOnlyDictionary<string, string?>, Task> submit, IApiClient? apiClient = null)
    {
        Title = title;
        Fields = new ObservableCollection<FormField>(fields);
        _submit = submit;
        _apiClient = apiClient;
    }

    public string Title { get; }
    public ObservableCollection<FormField> Fields { get; }

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Final Convergence Phase P0-7: called by GenericCreateWindow's code-behind after the
    /// user picks a file for an IsFileUpload field — bytes are read there (a View-layer filesystem
    /// concern, matching StudentImportWindow's own established split), this method only uploads
    /// them and stores the resulting server path into the field's Value (the same string the submit
    /// pipeline already sends — no change needed there).</summary>
    public async Task UploadFieldAsync(FormField field, string fileName, byte[] bytes)
    {
        if (_apiClient is null || field.UploadModule is null)
        {
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        try
        {
            field.Value = await _apiClient.UploadFileAsync(field.UploadModule, fileName, bytes);
            field.SelectedFileName = fileName;
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ObservableProperty]
    private string? _errorMessage;

    public event EventHandler? Saved;

    [RelayCommand]
    private async Task SaveAsync()
    {
        var missing = Fields.Where(f => f.IsRequired && string.IsNullOrWhiteSpace(f.Value)).ToList();
        if (missing.Count > 0)
        {
            ErrorMessage = $"{string.Join(", ", missing.Select(f => f.Label))} {(missing.Count == 1 ? "is" : "are")} required.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var values = Fields.ToDictionary(f => f.Key, f => string.IsNullOrWhiteSpace(f.Value) ? null : f.Value);
            await _submit(values);
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
}
