using System.IO;
using System.Text.Json;

namespace CollegeAdmin.Desktop.Theming;

/// <summary>Persists and applies the user's chosen theme preset. Deliberately local-only — a plain
/// JSON file under %LocalAppData%\CollegeAdmin, never sent to the server — per CLAUDE.md's "not a
/// new SaaS platform" direction and the user's own "just app related stuff, doesn't need backend
/// or live somewhere, just in PC" instruction. Plain class, not DI-registered: every call site
/// (App.xaml.cs at boot, SettingsView's code-behind later) just news one up, matching how
/// CsvExporter/other view-only utilities are already used directly in this codebase.</summary>
public sealed class ThemeService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CollegeAdmin");

    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "theme-settings.json");

    /// <summary>False only on a genuinely first run — used to decide whether the bootstrap theme
    /// chooser should appear at all.</summary>
    public bool HasSavedPreference() => File.Exists(SettingsPath);

    /// <summary>Never throws: a missing or corrupt local preference file is not a reason to fail
    /// boot, it just means "use the default" (Indigo Night, per the user's "keep current one as
    /// default" instruction).</summary>
    public ThemePreset LoadPreset()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var saved = JsonSerializer.Deserialize<SavedTheme>(json);
                if (!string.IsNullOrWhiteSpace(saved?.PresetId))
                {
                    return ThemeCatalog.FindById(saved.PresetId);
                }
            }
        }
        catch
        {
            // Corrupt/unreadable local file — fall through to the default below.
        }

        return ThemeCatalog.Default;
    }

    public void SavePreset(ThemePreset preset)
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new SavedTheme(preset.Id)));
    }

    /// <summary>Swaps the palette/font dictionaries at fixed indices 0/1 of the app-level
    /// MergedDictionaries (see App.xaml's own comment). Must run before any Window is constructed:
    /// every other style in Theme/Controls.xaml and Theme/Typography.xaml resolves its
    /// {StaticResource} colors/AppFont once, at the moment that XAML is parsed for the first
    /// element that uses it — swapping the dictionaries afterward would not restyle windows that
    /// already exist, which is also why SettingsView's "change theme" action asks to restart the
    /// app rather than trying to restyle live.</summary>
    public void Apply(ThemePreset preset)
    {
        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        dictionaries[0] = new System.Windows.ResourceDictionary { Source = new Uri(preset.PaletteUri, UriKind.Relative) };
        dictionaries[1] = new System.Windows.ResourceDictionary { Source = new Uri(preset.FontUri, UriKind.Relative) };
    }

    private sealed record SavedTheme(string PresetId);
}
