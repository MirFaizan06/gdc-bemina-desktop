namespace CollegeAdmin.Desktop.Help;

/// <summary>One documentation/tutorial entry. Content is authored entirely in HelpCatalog.cs — this
/// is a local, static, offline reference (no server call, no backend dependency), matching the
/// user's own "just app related stuff, doesn't need backend" framing for this kind of feature.
/// Roles is empty for a "General" topic (relevant to every admin regardless of role); otherwise it
/// lists which of the five admin roles (see AdminProfile.Role) the topic is scoped to — a topic can
/// list more than one role when the underlying module is genuinely shared (e.g. Website Content is
/// used by Editors, Department Admins, and Committee Managers alike).</summary>
public sealed record HelpTopic(
    string Id,
    string Title,
    string Icon,
    string Category,
    IReadOnlyList<string> Roles,
    string Summary,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string>? Tips = null);
