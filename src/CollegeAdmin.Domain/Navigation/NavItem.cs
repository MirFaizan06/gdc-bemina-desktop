namespace CollegeAdmin.Domain.Navigation;

/// <summary>Static nav entry. Visibility/permission gating is added in Stage 6 — every item is
/// shown to every signed-in user for now (nav visibility is never the authorization boundary;
/// the server enforces that regardless of what the sidebar shows).</summary>
public sealed record NavItem(string Key, string Label, string Group);
