namespace CollegeAdmin.Desktop.Theming;

/// <summary>The fixed set of built-in theme presets. Adding a fourth preset later means: a new
/// Theme/Palettes/*.xaml (copy an existing one, edit the "varying" keys — see EmeraldPalette.xaml's
/// remarks), optionally a new Theme/Fonts/*.xaml, and one more entry here.</summary>
public static class ThemeCatalog
{
    public static readonly ThemePreset Indigo = new(
        Id: "indigo",
        Name: "Indigo Night",
        Description: "Deep indigo & violet — the original College Admin look.",
        PaletteUri: "Theme/Colors.xaml",
        FontUri: "Theme/Fonts/SegoeFont.xaml",
        PrimaryHex: "#6366F1",
        SidebarHex: "#1E1B4B",
        FontPreviewName: "Segoe UI");

    public static readonly ThemePreset Emerald = new(
        Id: "emerald",
        Name: "Emerald Fresh",
        Description: "Calm emerald green with a modern variable font.",
        PaletteUri: "Theme/Palettes/EmeraldPalette.xaml",
        FontUri: "Theme/Fonts/VariableFont.xaml",
        PrimaryHex: "#059669",
        SidebarHex: "#064E3B",
        FontPreviewName: "Segoe UI Variable Text, Segoe UI");

    public static readonly ThemePreset Slate = new(
        Id: "slate",
        Name: "Slate Professional",
        Description: "Cool slate blue with a crisp Trebuchet font.",
        PaletteUri: "Theme/Palettes/SlatePalette.xaml",
        FontUri: "Theme/Fonts/TrebuchetFont.xaml",
        PrimaryHex: "#2563EB",
        SidebarHex: "#0F172A",
        FontPreviewName: "Trebuchet MS, Segoe UI");

    public static readonly ThemePreset Default = Indigo;

    public static IReadOnlyList<ThemePreset> All { get; } = [Indigo, Emerald, Slate];

    public static ThemePreset FindById(string? id) =>
        All.FirstOrDefault(p => p.Id == id) ?? Default;
}
