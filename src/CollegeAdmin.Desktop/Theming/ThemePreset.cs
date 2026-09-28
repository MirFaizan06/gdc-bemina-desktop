namespace CollegeAdmin.Desktop.Theming;

/// <summary>One selectable look: a color palette + a font family, applied together. PaletteUri/
/// FontUri are pack-relative paths to the ResourceDictionary files ThemeService merges into
/// Application.Resources; PrimaryHex/SidebarHex/FontPreviewName are plain strings (not resources)
/// so ThemeChooserWindow can preview all three presets side by side without actually applying any
/// of them.</summary>
public sealed record ThemePreset(
    string Id,
    string Name,
    string Description,
    string PaletteUri,
    string FontUri,
    string PrimaryHex,
    string SidebarHex,
    string FontPreviewName);
