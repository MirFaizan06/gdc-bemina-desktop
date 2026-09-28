using CollegeAdmin.Application.Auth;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.Theming;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-15 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): the "System" nav
/// item's replacement — renamed "Settings" and now hosts connectivity status, app version, and
/// basic diagnostics (API base URL, last successful request time) alongside the existing Credits
/// content (CLAUDE.md's developer-credit requirement), which previously had this screen to itself.
/// Supersedes the standalone CreditsViewModel/CreditsView rather than wrapping them, since both
/// screens share the same load-on-construct/LoadingView/ErrorStateView shape and folding avoids
/// maintaining two near-identical ViewModels for what is now one screen.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly ThemeService _themeService;

    public SettingsViewModel(IApiClient apiClient, IConfiguration configuration, ThemeService themeService, IAuthSessionService authSessionService)
    {
        _apiClient = apiClient;
        _themeService = themeService;
        ApiBaseUrl = configuration["Api:BaseUrl"] ?? "(not configured)";
        AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        // Convenience only, matching ShellViewModel's own nav-hiding philosophy — the real
        // enforcement is server-side (BackupsController's raw role check). Hiding the section here
        // just stops a non-Super-Admin from seeing a button they'd be 403'd on anyway.
        IsSuperAdmin = authSessionService.CurrentAdmin?.Role == "super_admin";
        RefreshThemeDisplay();
        _ = LoadAsync();
    }

    [ObservableProperty]
    private bool _isSuperAdmin;

    [ObservableProperty]
    private string _currentThemeName = "";

    [ObservableProperty]
    private string _currentThemePrimaryHex = "#6366F1";

    /// <summary>Re-reads the saved theme preference — called on construction and again by
    /// SettingsView's code-behind after the user picks a different theme, since that save happens
    /// entirely outside this ViewModel (ThemeChooserWindow is a plain Window, not MVVM-bound).</summary>
    public void RefreshThemeDisplay()
    {
        var preset = _themeService.LoadPreset();
        CurrentThemeName = preset.Name;
        CurrentThemePrimaryHex = preset.PrimaryHex;
    }

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _company = "";

    [ObservableProperty]
    private string _developer = "";

    [ObservableProperty]
    private string _businessEmail = "";

    [ObservableProperty]
    private string _appVersion = "";

    [ObservableProperty]
    private string _apiBaseUrl = "";

    [ObservableProperty]
    private bool _isCheckingConnectivity;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private DateTime? _lastSuccessfulCheckAt;

    // ─── Backup (download-only — see FINAL_COMPLETION_TRACKER.md; restore is a deliberately
    // separate, not-yet-built feature) ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isGeneratingBackup;

    [ObservableProperty]
    private string? _backupStatusMessage;

    [ObservableProperty]
    private string? _backupErrorMessage;

    /// <summary>The actual SaveFileDialog/disk-write is a View-layer concern handled by
    /// SettingsView's code-behind (matching ExportButton_Click's own established split) — this just
    /// does the two API calls and keeps the bindable status text current while they run.</summary>
    public async Task<(BackupGeneratedResult Meta, byte[] Bytes)?> GenerateAndFetchBackupAsync()
    {
        IsGeneratingBackup = true;
        BackupErrorMessage = null;
        BackupStatusMessage = "Generating backup on the server...";
        try
        {
            var meta = await _apiClient.GenerateBackupAsync();
            BackupStatusMessage = $"Backup ready ({meta.ModuleCount} modules, {meta.FileCount} files, {FormatSize(meta.SizeBytes)}). Downloading...";
            var bytes = await _apiClient.DownloadBackupAsync(meta.Token);
            BackupStatusMessage = "Backup downloaded. Choose where to save it.";
            return (meta, bytes);
        }
        catch (ApiRequestException ex)
        {
            BackupErrorMessage = ex.Message;
            BackupStatusMessage = null;
            return null;
        }
        finally
        {
            IsGeneratingBackup = false;
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B",
    };

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var credits = await _apiClient.GetCreditsAsync();
            Company = credits.Company;
            Developer = credits.Developer;
            BusinessEmail = credits.BusinessEmail;
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }

        await CheckConnectivityAsync();
    }

    /// <summary>A real round-trip to GET /api/v1/ping, not a cached/assumed value — the
    /// "connectivity status" the tracker asks for is meaningless if it isn't live. Never throws:
    /// a failed ping is a legitimate, expected outcome on this screen (that's the whole point of
    /// showing it), not an error state.</summary>
    [RelayCommand]
    private async Task CheckConnectivityAsync()
    {
        IsCheckingConnectivity = true;
        try
        {
            await _apiClient.PingAsync();
            IsConnected = true;
            LastSuccessfulCheckAt = DateTime.Now;
        }
        catch (ApiRequestException)
        {
            IsConnected = false;
        }
        finally
        {
            IsCheckingConnectivity = false;
        }
    }
}
