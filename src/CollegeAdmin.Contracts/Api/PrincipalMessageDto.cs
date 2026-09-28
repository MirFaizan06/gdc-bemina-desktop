namespace CollegeAdmin.Contracts.Api;

/// <summary>Final Convergence Phase P1-10/§2b — mirrors PrincipalSectionController's row shape.</summary>
public sealed record PrincipalMessageDto
{
    public string? Name { get; init; }
    public string? Designation { get; init; }
    public string? Photo { get; init; }
    public string? Message { get; init; }
    public string? UpdatedAt { get; init; }
}

public sealed record PrincipalMessageResult { public PrincipalMessageDto PrincipalMessage { get; init; } = new(); }
