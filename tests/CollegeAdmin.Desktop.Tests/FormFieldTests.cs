using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P0-4: FormField.Options is what lets GenericCreateWindow render
/// a real ComboBox picker (e.g. Programmes) instead of a free-text box, without touching any of the
/// dozens of existing plain-text fields. These tests cover the property itself, not the XAML
/// rendering (not unit-testable here) — see the live HTTP/manual-verification notes in
/// docs/claude/18_DEVLOG.md for how the rendering was actually checked.</summary>
public class FormFieldTests
{
    [Fact]
    public void PlainField_HasNoOptions()
    {
        var field = new FormField("name", "Name", isRequired: true);

        Assert.False(field.HasOptions);
        Assert.Null(field.Options);
    }

    [Fact]
    public void FieldWithOptions_HasOptionsTrue_AndExposesThem()
    {
        var options = new List<FormFieldOption> { new("1", "BCA"), new("2", "BSc") };

        var field = new FormField("programmeId", "Programme", options: options);

        Assert.True(field.HasOptions);
        Assert.Equal(2, field.Options!.Count);
        Assert.Equal("BCA", field.Options[0].Label);
    }

    [Fact]
    public void FieldWithEmptyOptionsList_HasOptionsFalse()
    {
        // An empty (not null) list must still fall back to the plain TextBox — HasOptions checks
        // Count > 0, not just "is the list non-null", so a fetch that returned zero rows doesn't
        // render a permanently-empty, unusable ComboBox.
        var field = new FormField("programmeId", "Programme", options: []);

        Assert.False(field.HasOptions);
    }

    [Fact]
    public void Value_IsIndependentOfOptions_SubmitPipelineUnaffected()
    {
        var field = new FormField("programmeId", "Programme", options: [new("1", "BCA")]) { Value = "1" };

        Assert.Equal("1", field.Value);
    }

    // --- Final Convergence Phase P0-7: file-upload field state ---

    [Fact]
    public void PlainField_IsNotAFileUpload_AndIsPlainTextIsTrue()
    {
        var field = new FormField("name", "Name");

        Assert.False(field.IsFileUpload);
        Assert.True(field.IsPlainText);
    }

    [Fact]
    public void FieldWithUploadModule_IsFileUpload_AndIsNotPlainText()
    {
        var field = new FormField("image", "Image", uploadModule: "news");

        Assert.True(field.IsFileUpload);
        Assert.False(field.IsPlainText);
        Assert.Equal("news", field.UploadModule);
    }

    [Fact]
    public void DisplayFileName_WithNoValueAndNoSelection_ShowsPlaceholder()
    {
        var field = new FormField("image", "Image", uploadModule: "news");

        Assert.Equal("(no file selected)", field.DisplayFileName);
    }

    [Fact]
    public void DisplayFileName_InEditModeWithExistingValue_ShowsItsBasename()
    {
        var field = new FormField("image", "Image", uploadModule: "news") { Value = "assets/uploads/news/existing.png" };

        Assert.Equal("existing.png", field.DisplayFileName);
    }

    [Fact]
    public void DisplayFileName_AfterAFreshSelection_PrefersSelectedFileNameOverValue()
    {
        var field = new FormField("image", "Image", uploadModule: "news") { Value = "assets/uploads/news/existing.png" };
        field.SelectedFileName = "brand-new.png";

        Assert.Equal("brand-new.png", field.DisplayFileName);
    }

    // --- Final Convergence Phase P1-13: checkbox field state ---

    [Fact]
    public void CheckboxField_DefaultsValueToZero_AndIsNotPlainText()
    {
        var field = new FormField("isAdmissionsCommittee", "Is Admissions Committee", isCheckbox: true);

        Assert.Equal("0", field.Value);
        Assert.False(field.CheckedValue);
        Assert.True(field.IsCheckbox);
        Assert.False(field.IsPlainText);
    }

    [Fact]
    public void CheckboxField_SettingCheckedValueTrue_SetsValueToOne()
    {
        var field = new FormField("isAdmissionsCommittee", "Is Admissions Committee", isCheckbox: true)
        {
            CheckedValue = true,
        };

        Assert.Equal("1", field.Value);
    }

    [Fact]
    public void CheckboxField_SettingValueDirectly_KeepsCheckedValueInSync()
    {
        var field = new FormField("isAdmissionsCommittee", "Is Admissions Committee", isCheckbox: true)
        {
            Value = "1",
        };

        Assert.True(field.CheckedValue);
    }
}
