namespace CollegeAdmin.Domain.Tests;

/// <summary>
/// Stage 2 (desktop bootstrap) has no domain types yet — real permission/model tests
/// arrive with Stage 6 (RBAC/account foundation). This exists so `dotnet test` proves
/// the CollegeAdmin.Domain.Tests -&gt; CollegeAdmin.Domain project reference and the xUnit
/// runner are both wired correctly, per the Stage 2 exit criterion in
/// docs/claude/PHASE1_EXECUTION_PLAN.md. Replace/delete once real Domain tests exist.
/// </summary>
public class SolutionWiringTests
{
    [Fact]
    public void TestRunnerAndDomainReferenceAreWired()
    {
        Assert.True(typeof(CollegeAdmin.Domain.AssemblyMarker).Assembly is not null);
    }
}
