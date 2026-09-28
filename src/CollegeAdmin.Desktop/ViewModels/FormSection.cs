namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>A titled group of FormFields — used by StudentEditViewModel's sectioned stepper
/// (docs/claude/phase2/03_STUDENT_MANAGER_UI.md: "large forms must be sectioned, not one endless
/// form"), unlike GenericCreateViewModel's flat single-list form which suits the smaller modules.</summary>
public sealed class FormSection(string title, IReadOnlyList<FormField> fields)
{
    public string Title { get; } = title;
    public IReadOnlyList<FormField> Fields { get; } = fields;
}
