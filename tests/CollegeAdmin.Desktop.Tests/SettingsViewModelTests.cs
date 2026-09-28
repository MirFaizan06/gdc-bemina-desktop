using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.Theming;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;
using Microsoft.Extensions.Configuration;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-15 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the
/// "Settings" nav item (formerly "System"/Credits-only) — now also connectivity status (a real
/// ping round-trip), app version, and basic diagnostics (API base URL).</summary>
public class SettingsViewModelTests
{
    private static IConfiguration MakeConfiguration(string baseUrl = "http://localhost:8099/api/v1/") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = baseUrl })
            .Build();

    [Fact]
    public async Task LoadAsync_PopulatesCreditsAndDiagnostics_AndChecksConnectivity()
    {
        var apiClient = new FakeApiClient
        {
            CreditsToReturn = new CreditsResult { Company = "Tech Bytes Design", Developer = "Mir Faizan", BusinessEmail = "mirfaizan8803@gmail.com" },
            PingToReturn = new PingResult { Status = "ok", Time = "2026-09-27T00:00:00Z" },
        };

        var viewModel = new SettingsViewModel(apiClient, MakeConfiguration("http://localhost:8099/api/v1/"), new ThemeService(), new FakeAuthSessionService());
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Tech Bytes Design", viewModel.Company);
        Assert.Equal("Mir Faizan", viewModel.Developer);
        Assert.Equal("mirfaizan8803@gmail.com", viewModel.BusinessEmail);
        Assert.Equal("http://localhost:8099/api/v1/", viewModel.ApiBaseUrl);
        Assert.False(string.IsNullOrEmpty(viewModel.AppVersion));
        Assert.True(viewModel.IsConnected);
        Assert.NotNull(viewModel.LastSuccessfulCheckAt);
        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_OnCreditsApiFailure_SetsErrorMessage_NotAnUnhandledException()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnCredits = new ApiRequestException(new ApiErrorPayload { Code = "SERVER_ERROR", Message = "Failed.", Retryable = true }),
        };

        var viewModel = new SettingsViewModel(apiClient, MakeConfiguration(), new ThemeService(), new FakeAuthSessionService());
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Failed.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task CheckConnectivityAsync_OnPingFailure_SetsNotConnected_WithoutThrowing()
    {
        var apiClient = new FakeApiClient
        {
            ThrowOnPing = new ApiRequestException(new ApiErrorPayload { Code = "NETWORK_ERROR", Message = "Could not reach the server.", Retryable = true }),
        };

        var viewModel = new SettingsViewModel(apiClient, MakeConfiguration(), new ThemeService(), new FakeAuthSessionService());
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsConnected);
        Assert.Null(viewModel.LastSuccessfulCheckAt);
        Assert.False(viewModel.IsCheckingConnectivity);
    }
}
