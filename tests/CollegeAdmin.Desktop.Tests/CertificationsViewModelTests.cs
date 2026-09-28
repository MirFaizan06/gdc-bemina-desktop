using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): the
/// desktop consumer for the new CertificationsController.</summary>
public class CertificationsViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsApplicationsStatsAndTypes()
    {
        var apiClient = new FakeApiClient
        {
            CertificationsToReturn = new CertificationsListResult
            {
                Applications = [new CertificateApplicationDto { Id = 1, StudentName = "A Student", Status = "pending" }],
                Stats = new CertificationStatsDto { Total = 1, Pending = 1 },
                Types = [new CertificateTypeDto { Id = 1, Code = "bonafide", Name = "Bona Fide", IsActive = true }],
            },
        };
        var viewModel = new CertificationsViewModel(apiClient);
        await Task.Delay(20);

        Assert.Single(viewModel.Applications);
        Assert.Single(viewModel.Types);
        Assert.Equal(1, viewModel.Stats.Pending);
    }

    [Fact]
    public async Task ChangingStatusFilter_ReQueriesTheServer()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new CertificationsViewModel(apiClient);
        await Task.Delay(20);

        viewModel.StatusFilter = "approved";
        await Task.Delay(20);

        Assert.Equal("approved", apiClient.LastCertificationsStatusRequested);
    }

    [Fact]
    public async Task ApproveAsync_CallsTheApiAndRefreshes()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new CertificationsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.ApproveCommand.ExecuteAsync(new CertificateApplicationDto { Id = 5 });

        Assert.Equal(5, apiClient.LastApprovedCertificationId);
        Assert.Contains("#5", viewModel.StatusMessage);
    }

    [Fact]
    public async Task RejectAsync_SubmitsTheReason()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new CertificationsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.RejectAsync(new CertificateApplicationDto { Id = 7 }, "Missing documents.");

        Assert.Equal((7, "Missing documents."), apiClient.LastRejectedCertification);
    }

    [Fact]
    public void CanAddType_RequiresCodeNameAndPrefix()
    {
        var viewModel = new CertificationsViewModel(new FakeApiClient());
        Assert.False(viewModel.AddTypeCommand.CanExecute(null));

        viewModel.NewTypeCode = "tc";
        viewModel.NewTypeName = "Transfer Certificate";
        viewModel.NewTypePrefix = "TC";
        Assert.True(viewModel.AddTypeCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddTypeAsync_AddsTheType_AndClearsTheForm()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new CertificationsViewModel(apiClient)
        {
            NewTypeCode = "tc",
            NewTypeName = "Transfer Certificate",
            NewTypePrefix = "TC",
        };
        await Task.Delay(20);

        await viewModel.AddTypeCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Types);
        Assert.Equal(("tc", "Transfer Certificate", "TC"), apiClient.LastCreatedCertificateType);
        Assert.Equal("", viewModel.NewTypeCode);
    }

    [Fact]
    public async Task ToggleTypeAsync_CallsTheApi()
    {
        var apiClient = new FakeApiClient();
        apiClient.CertificateTypes.Add(new CertificateTypeDto { Id = 3, IsActive = true });
        var viewModel = new CertificationsViewModel(apiClient);
        await Task.Delay(20);

        await viewModel.ToggleTypeCommand.ExecuteAsync(new CertificateTypeDto { Id = 3, IsActive = true });

        Assert.Equal(3, apiClient.LastToggledCertificateTypeId);
    }
}
