using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Desktop.Theming;

namespace CollegeAdmin.Desktop.Views;

/// <summary>Bootstrap-only ("choose your look" on first run, or re-opened from Settings' "Change
/// Theme" action) and re-usable for both call sites: the caller reads <see cref="SelectedPreset"/>
/// after a true DialogResult and decides itself whether/how to persist and apply it.</summary>
public partial class ThemeChooserWindow : Window
{
    public ThemePreset SelectedPreset { get; private set; } = ThemeCatalog.Default;

    public ThemeChooserWindow() => InitializeComponent();

    /// <summary>Pre-selects the given preset's card (used when re-opening from Settings so the
    /// user sees their current theme already highlighted, not always defaulting back to Indigo).</summary>
    public void PreselectPreset(ThemePreset preset)
    {
        var option = preset.Id switch
        {
            "emerald" => EmeraldOption,
            "slate" => SlateOption,
            _ => IndigoOption,
        };
        option.IsChecked = true;
    }

    private void ThemeOption_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string id })
        {
            SelectedPreset = ThemeCatalog.FindById(id);
        }
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
