using System.Linq;

namespace CollegeAdmin.Contracts.Api;

/// <summary>One row from admin_capability_grants — the same capability+scope model the server's
/// Authorization::check() enforces, mirrored here so the desktop can filter its own nav to match
/// (see ShellViewModel) rather than showing every module to every role and relying only on a
/// server-side 403 after the fact.</summary>
public sealed record CapabilityGrantDto
{
    public string Capability { get; init; } = "";
    public string ScopeType { get; init; } = "";
    public int? ScopeId { get; init; }
}

/// <summary>Mirrors SessionService::presentAdmin() on the server — never carries password_hash etc.
/// Capabilities is deliberately empty for super_admin (the server has no grant rows for that role,
/// since Authorization::check() bypasses the grants table entirely for it) — HasCapability below is
/// the one place that distinction is resolved, so callers never need to special-case Role themselves.</summary>
public sealed record AdminProfile
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public int? FacultyId { get; init; }
    public int? DepartmentId { get; init; }
    public IReadOnlyList<CapabilityGrantDto> Capabilities { get; init; } = [];

    /// <summary>True if this admin can act on `capability` in ANY scope — the same "do they hold it
    /// at all" question Authorization::hasAnyGrant() answers server-side, used here for nav
    /// visibility (a coarser question than a specific scoped check, which is what the server still
    /// re-verifies for every actual read/write regardless of what the sidebar shows).</summary>
    public bool HasCapability(string capability) =>
        Role == "super_admin" || Capabilities.Any(g => g.Capability == capability);
}
