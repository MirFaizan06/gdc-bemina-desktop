using CommunityToolkit.Mvvm.ComponentModel;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>One selectable choice in an Options-backed FormField (see FormField.Options) — Value is
/// what actually gets submitted (e.g. a programme id as a string), Label is what the ComboBox shows.</summary>
public sealed record FormFieldOption(string Value, string Label);

/// <summary>One bindable field in a GenericCreateViewModel's dynamically-built form.</summary>
public sealed partial class FormField : ObservableObject
{
    public FormField(string key, string label, bool isRequired = false, bool isMultiline = false,
        IReadOnlyList<FormFieldOption>? options = null, string? uploadModule = null, bool isCheckbox = false)
    {
        Key = key;
        Label = label;
        IsRequired = isRequired;
        IsMultiline = isMultiline;
        Options = options;
        UploadModule = uploadModule;
        IsCheckbox = isCheckbox;
        if (isCheckbox)
        {
            Value = "0";
        }
    }

    public string Key { get; }
    public string Label { get; }
    public bool IsRequired { get; }
    public bool IsMultiline { get; }

    /// <summary>Final Convergence Phase P0-4: when set, GenericCreateWindow renders a ComboBox bound
    /// to these choices instead of a free-text TextBox — e.g. a real Programme picker instead of a
    /// raw numeric id field. Null (the default) keeps every existing plain-text field unchanged.</summary>
    public IReadOnlyList<FormFieldOption>? Options { get; }

    public bool HasOptions => Options is { Count: > 0 };

    /// <summary>Final Convergence Phase P0-7: when set, names the /api/v1/uploads module this field
    /// uploads to (e.g. "news", "gallery") — GenericCreateWindow renders a real file-picker + Browse
    /// button instead of a free-text path box. Null (the default) keeps every existing field
    /// unchanged. Mutually exclusive with Options in practice (a field is either a picker of
    /// existing choices or a file upload, never both).</summary>
    public string? UploadModule { get; }

    public bool IsFileUpload => UploadModule is not null;

    /// <summary>Final Convergence Phase P1-13: a plain boolean flag (e.g. Committee's
    /// is_admissions_committee) rendered as a real CheckBox — Value stays "1"/"0", exactly what the
    /// server's own boolean-casting (CaseConverter::castBooleans) already expects on write, so the
    /// submit pipeline needs no changes.</summary>
    public bool IsCheckbox { get; }

    /// <summary>Two-way bridge between the CheckBox's bool IsChecked and Value's "1"/"0" string.</summary>
    public bool CheckedValue
    {
        get => Value == "1";
        set => Value = value ? "1" : "0";
    }

    /// <summary>True for the ordinary, vast-majority case (no Options, no UploadModule, no
    /// IsCheckbox) — lets the XAML template pick the plain TextBox without an OR-combining converter.</summary>
    public bool IsPlainText => !HasOptions && !IsFileUpload && !IsCheckbox;

    /// <summary>Set by GenericCreateWindow's code-behind after a successful upload, purely for
    /// display — Value itself (the server path) is what actually gets submitted.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayFileName))]
    private string? _selectedFileName;

    /// <summary>What the Browse box actually shows: the just-picked filename if any, else the
    /// existing server path's own filename in edit mode (Value is pre-filled there but
    /// SelectedFileName starts null since no new file has been picked yet), else a placeholder.</summary>
    public string DisplayFileName =>
        SelectedFileName
        ?? (string.IsNullOrWhiteSpace(Value) ? "(no file selected)" : System.IO.Path.GetFileName(Value));

    /// <summary>What the form actually displays — computed here (not with a Run+Visibility XAML
    /// trick) since Run is an Inline, not a FrameworkElement, and has no Visibility property.</summary>
    public string DisplayLabel => IsRequired ? $"{Label} *" : Label;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayFileName))]
    [NotifyPropertyChangedFor(nameof(CheckedValue))]
    private string _value = "";
}
