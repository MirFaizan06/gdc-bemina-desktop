using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CollegeAdmin.Application.Caching;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// University RR Export's desktop workflow (docs/claude/phase2/University_RR_Export_Claude_Addendum.md,
/// PHASE2_IMPLEMENTATION_PLAN.md Slice 5): Select Scope -> Preview (Ready/Warning/Blocked per row) ->
/// Export. The server always re-validates on export and refuses while any row is Blocked — this
/// ViewModel doesn't duplicate that rule client-side, it just surfaces the server's own rejection.
/// </summary>
public sealed partial class UniversityRrExportViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly ILookupCache _lookupCache;

    public UniversityRrExportViewModel(IApiClient apiClient, ILookupCache lookupCache)
    {
        _apiClient = apiClient;
        _lookupCache = lookupCache;
        _ = LoadProgrammeOptionsAsync();
    }

    /// <summary>Final Convergence Phase P0-4: replaces the raw numeric "Programme ID" scope filter
    /// with a real picker. Non-blocking fetch on construction — this is an optional export filter,
    /// not a required field, so the dialog is usable immediately either way.</summary>
    public ObservableCollection<FormFieldOption> ProgrammeOptions { get; } = [new("", "(All programmes)")];

    private async Task LoadProgrammeOptionsAsync()
    {
        // Final Convergence Phase P1-17: Programmes is independently re-fetched by 5 different call
        // sites across the app (see NavigationService's own doc comment) — this cache closes that.
        var programmes = await _lookupCache.GetOrFetchAsync("programmes", ct => _apiClient.GetProgrammesAsync(ct));
        foreach (var p in programmes)
        {
            ProgrammeOptions.Add(new FormFieldOption(p.Id.ToString(), $"{p.Name} ({p.DepartmentName})"));
        }
    }

    [ObservableProperty]
    private string _academicSession = "";

    [ObservableProperty]
    private string _semester = "";

    [ObservableProperty]
    private string _programmeId = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _resultMessage;

    [ObservableProperty]
    private UniversityRrTotalsDto? _totals;

    /// <summary>Final Convergence Phase P0-2: true only once a human has verified the 4 ADR-019
    /// field mappings against real university RR data (UniversityRrMapper::MAPPING_VERIFIED) —
    /// starts false/hidden until a preview or export has actually run.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMappingWarning))]
    private bool _mappingVerified;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMappingWarning))]
    private bool _hasResult;

    public bool ShowMappingWarning => HasResult && !MappingVerified;

    public ObservableCollection<UniversityRrPreviewRowDto> Rows { get; } = [];

    /// <summary>Set by the code-behind once a Save dialog gives it a target path — the actual write
    /// happens in code-behind (File.WriteAllBytesAsync is a View-layer file-system concern, same
    /// split StudentImportWindow uses for reading).</summary>
    public event EventHandler<(string Filename, byte[] Bytes)>? ExportReady;

    private bool TryParseScope(out string session, out int semester, out int? programmeId)
    {
        session = AcademicSession.Trim();
        programmeId = int.TryParse(ProgrammeId, out var pid) ? pid : null;
        var semesterParsed = int.TryParse(Semester, out semester);
        if (session == "" || !semesterParsed || semester < 1)
        {
            ErrorMessage = "Academic Session and a valid Semester are both required.";
            return false;
        }
        return true;
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        ErrorMessage = null;
        ResultMessage = null;
        if (!TryParseScope(out var session, out var semester, out var programmeId))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiClient.PreviewUniversityRrAsync(session, semester, programmeId);
            Totals = result.Totals;
            MappingVerified = result.MappingVerified;
            HasResult = true;
            Rows.Clear();
            foreach (var row in result.Rows)
            {
                Rows.Add(row);
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        ErrorMessage = null;
        ResultMessage = null;
        if (!TryParseScope(out var session, out var semester, out var programmeId))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var (filename, bytes, totals, mappingVerified) = await _apiClient.ExportUniversityRrAsync(session, semester, programmeId);
            Totals = totals;
            MappingVerified = mappingVerified;
            HasResult = true;
            ExportReady?.Invoke(this, (filename, bytes));
            ResultMessage = $"Exported {totals.Total} student(s) to {filename}.";
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
