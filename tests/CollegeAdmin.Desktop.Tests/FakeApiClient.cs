using System.Linq;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Minimal fake covering only what ViewModel tests in this project actually exercise —
/// unused members throw so a test that accidentally depends on one fails loudly, not silently.</summary>
internal sealed class FakeApiClient : IApiClient
{
    public NoticeDto? LastCreatedNotice { get; private set; }
    public Exception? ThrowOnCreateNotice { get; set; }

    public Task<NoticeDto> CreateNoticeAsync(
        string title, string? body, string? link, string? category, int? departmentId, int? committeeId,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnCreateNotice is not null)
        {
            throw ThrowOnCreateNotice;
        }

        var notice = new NoticeDto { Title = title, DepartmentId = departmentId, CommitteeId = committeeId };
        LastCreatedNotice = notice;
        return Task.FromResult(notice);
    }

    public List<BackgroundJobDto> Jobs { get; } = [];
    public Exception? ThrowOnJobs { get; set; }
    public string? LastRunJobType { get; private set; }
    public RunJobResult? RunJobResultToReturn { get; set; }

    public Task<IReadOnlyList<BackgroundJobDto>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnJobs is not null)
        {
            throw ThrowOnJobs;
        }

        return Task.FromResult<IReadOnlyList<BackgroundJobDto>>(Jobs);
    }

    public Task<RunJobResult> RunJobAsync(string type, CancellationToken cancellationToken = default)
    {
        if (ThrowOnJobs is not null)
        {
            throw ThrowOnJobs;
        }

        LastRunJobType = type;
        return Task.FromResult(RunJobResultToReturn ?? new RunJobResult { Jobs = Jobs, Processed = 1, Failed = 0 });
    }

    public Task<IReadOnlyList<StudentDto>> GetStudentsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<AdmissionLinkDto>> GetAdmissionLinksAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<AdmissionBannerDto>> GetAdmissionBannersAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public StudentResult? StudentResultToReturn { get; set; }
    public Exception? ThrowOnSaveStudent { get; set; }
    public IReadOnlyDictionary<string, object?>? LastSavedStudentFields { get; private set; }
    public bool? LastSaveStudentFinalize { get; private set; }

    public Task<StudentResult> CreateStudentAsync(IReadOnlyDictionary<string, object?> fields, bool finalize, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSaveStudent is not null) throw ThrowOnSaveStudent;
        LastSavedStudentFields = fields;
        LastSaveStudentFinalize = finalize;
        return Task.FromResult(StudentResultToReturn ?? new StudentResult());
    }

    public Task<StudentResult> UpdateStudentAsync(int id, IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSaveStudent is not null) throw ThrowOnSaveStudent;
        LastSavedStudentFields = fields;
        return Task.FromResult(StudentResultToReturn ?? new StudentResult());
    }

    public Task<StudentDto> FinalizeStudentAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<StudentDto> ArchiveStudentAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<StudentFullDto> GetStudentFullAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<StudentHistoryResult> GetStudentHistoryAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public StudentImportBatchResult? StudentImportUploadResultToReturn { get; set; }
    public List<StudentImportRowDto> StudentImportRowsToReturn { get; set; } = [];
    public StudentImportRowDto? StudentImportRowUpdateResultToReturn { get; set; }
    public StudentImportConfirmResult? StudentImportConfirmResultToReturn { get; set; }
    public Exception? ThrowOnStudentImport { get; set; }
    public (int BatchId, int RowId, object? Overrides, string? Resolution)? LastUpdatedImportRow { get; private set; }

    public Task<StudentImportBatchResult> UploadStudentImportAsync(string filename, byte[] fileBytes, CancellationToken cancellationToken = default)
    {
        if (ThrowOnStudentImport is not null) throw ThrowOnStudentImport;
        return Task.FromResult(StudentImportUploadResultToReturn ?? new StudentImportBatchResult());
    }

    public Task<StudentImportBatchDto> GetStudentImportBatchAsync(int batchId, CancellationToken cancellationToken = default) =>
        Task.FromResult(StudentImportUploadResultToReturn?.Batch ?? new StudentImportBatchDto { Id = batchId });

    public Task<IReadOnlyList<StudentImportRowDto>> GetStudentImportRowsAsync(int batchId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StudentImportRowDto>>(StudentImportRowsToReturn);

    public Task<StudentImportRowDto> UpdateStudentImportRowAsync(int batchId, int rowId, object? normalizedOverrides, string? resolution, CancellationToken cancellationToken = default)
    {
        if (ThrowOnStudentImport is not null) throw ThrowOnStudentImport;
        LastUpdatedImportRow = (batchId, rowId, normalizedOverrides, resolution);
        return Task.FromResult(StudentImportRowUpdateResultToReturn ?? new StudentImportRowDto { Id = rowId });
    }

    public Task<StudentImportConfirmResult> ConfirmStudentImportAsync(int batchId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnStudentImport is not null) throw ThrowOnStudentImport;
        return Task.FromResult(StudentImportConfirmResultToReturn ?? new StudentImportConfirmResult { BatchId = batchId });
    }

    public List<PreferenceSubmissionDto> PreferenceSubmissionsToReturn { get; set; } = [];
    public PreferenceWindowSummaryDto PreferenceWindowSummaryToReturn { get; set; } = new();
    public PreferenceWindowDto? PreferenceWindowActionResultToReturn { get; set; }
    public Exception? ThrowOnPreferenceAction { get; set; }
    public (int SubmissionId, string Reason)? LastInvalidatedSubmission { get; private set; }
    public int? LastReopenedSubmissionId { get; private set; }

    public Task<IReadOnlyList<PreferenceWindowDto>> GetPreferenceWindowsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PreferenceWindowDto> CreatePreferenceWindowAsync(IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<PreferenceWindowDto> OpenPreferenceWindowAsync(int windowId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPreferenceAction is not null) throw ThrowOnPreferenceAction;
        return Task.FromResult(PreferenceWindowActionResultToReturn ?? new PreferenceWindowDto { Id = windowId, Status = "open" });
    }

    public Task<PreferenceWindowDto> ClosePreferenceWindowAsync(int windowId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPreferenceAction is not null) throw ThrowOnPreferenceAction;
        return Task.FromResult(PreferenceWindowActionResultToReturn ?? new PreferenceWindowDto { Id = windowId, Status = "closed" });
    }

    public Task<PreferenceWindowSummaryDto> GetPreferenceWindowSummaryAsync(int windowId, CancellationToken cancellationToken = default) =>
        Task.FromResult(PreferenceWindowSummaryToReturn);

    public Task<IReadOnlyList<PreferenceSubmissionDto>> GetPreferenceWindowSubmissionsAsync(int windowId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PreferenceSubmissionDto>>(PreferenceSubmissionsToReturn);

    public Task InvalidateSubmissionAsync(int submissionId, string reason, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPreferenceAction is not null) throw ThrowOnPreferenceAction;
        LastInvalidatedSubmission = (submissionId, reason);
        return Task.CompletedTask;
    }

    public Task ReopenSubmissionAsync(int submissionId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPreferenceAction is not null) throw ThrowOnPreferenceAction;
        LastReopenedSubmissionId = submissionId;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default, bool archived = false) => throw new NotImplementedException();

    public RunAllotmentResult RunAllotmentResultToReturn { get; set; } = new();
    public List<AllotmentRunDto> AllotmentRunsToReturn { get; set; } = [];
    public List<AllotmentResultDto> AllotmentResultsToReturn { get; set; } = [];
    public int FinalizeAllotmentRunResultToReturn { get; set; }
    public int OverrideAllotmentResultResultToReturn { get; set; }
    public (int ResultId, int? SubjectId, string Reason)? LastOverriddenResult { get; private set; }
    public int? LastCancelledResultId { get; private set; }

    public Task<RunAllotmentResult> RunAllotmentAsync(int windowId, CancellationToken cancellationToken = default) =>
        Task.FromResult(RunAllotmentResultToReturn);

    public Task<IReadOnlyList<AllotmentRunDto>> GetAllotmentRunsAsync(int windowId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AllotmentRunDto>>(AllotmentRunsToReturn);

    public Task<IReadOnlyList<AllotmentResultDto>> GetAllotmentResultsAsync(int runId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AllotmentResultDto>>(AllotmentResultsToReturn);

    public Task<int> FinalizeAllotmentRunAsync(int runId, CancellationToken cancellationToken = default) =>
        Task.FromResult(FinalizeAllotmentRunResultToReturn);

    public Task<int> OverrideAllotmentResultAsync(int resultId, int? newSubjectId, string reason, CancellationToken cancellationToken = default)
    {
        LastOverriddenResult = (resultId, newSubjectId, reason);
        return Task.FromResult(OverrideAllotmentResultResultToReturn);
    }

    public Task CancelAllotmentResultAsync(int resultId, CancellationToken cancellationToken = default)
    {
        LastCancelledResultId = resultId;
        return Task.CompletedTask;
    }

    public UniversityRrPreviewResult UniversityRrPreviewResultToReturn { get; set; } = new();
    public (string Filename, byte[] Bytes, UniversityRrTotalsDto Totals, bool MappingVerified) UniversityRrExportResultToReturn { get; set; } = ("RR_Test.xlsx", [], new UniversityRrTotalsDto(), false);
    public Exception? ThrowOnUniversityRr { get; set; }
    public (string Session, int Semester, int? ProgrammeId)? LastRrPreviewScope { get; private set; }
    public (string Session, int Semester, int? ProgrammeId)? LastRrExportScope { get; private set; }

    public Task<UniversityRrPreviewResult> PreviewUniversityRrAsync(string academicSession, int semester, int? programmeId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnUniversityRr is not null) throw ThrowOnUniversityRr;
        LastRrPreviewScope = (academicSession, semester, programmeId);
        return Task.FromResult(UniversityRrPreviewResultToReturn);
    }

    public Task<(string Filename, byte[] Bytes, UniversityRrTotalsDto Totals, bool MappingVerified)> ExportUniversityRrAsync(string academicSession, int semester, int? programmeId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnUniversityRr is not null) throw ThrowOnUniversityRr;
        LastRrExportScope = (academicSession, semester, programmeId);
        return Task.FromResult(UniversityRrExportResultToReturn);
    }

    public BackupGeneratedResult BackupGeneratedResultToReturn { get; set; } = new() { Token = "test-token", Filename = "backup.zip", SizeBytes = 1024, ModuleCount = 1, FileCount = 1, GeneratedAt = "2026-09-28T00:00:00Z" };
    public byte[] BackupBytesToReturn { get; set; } = [1, 2, 3];
    public IReadOnlyList<BackupSummaryDto> RecentBackupsToReturn { get; set; } = [];
    public Exception? ThrowOnBackup { get; set; }
    public string? LastDownloadedBackupToken { get; private set; }

    public Task<BackupGeneratedResult> GenerateBackupAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnBackup is not null) throw ThrowOnBackup;
        return Task.FromResult(BackupGeneratedResultToReturn);
    }

    public Task<byte[]> DownloadBackupAsync(string token, CancellationToken cancellationToken = default)
    {
        if (ThrowOnBackup is not null) throw ThrowOnBackup;
        LastDownloadedBackupToken = token;
        return Task.FromResult(BackupBytesToReturn);
    }

    public Task<IReadOnlyList<BackupSummaryDto>> GetRecentBackupsAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnBackup is not null) throw ThrowOnBackup;
        return Task.FromResult(RecentBackupsToReturn);
    }

    public string? LastExportedModule { get; private set; }
    public int? LastExportedRowCount { get; private set; }

    public Task RecordExportAsync(string module, int rowCount, CancellationToken cancellationToken = default)
    {
        LastExportedModule = module;
        LastExportedRowCount = rowCount;
        return Task.CompletedTask;
    }

    public PingResult PingToReturn { get; set; } = new() { Status = "ok", Time = "2026-01-01T00:00:00Z" };
    public Exception? ThrowOnPing { get; set; }

    public Task<PingResult> PingAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnPing is not null) throw ThrowOnPing;
        return Task.FromResult(PingToReturn);
    }
    public Task<AdminProfile> GetMeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task LogoutAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<int> LogoutAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public CreditsResult CreditsToReturn { get; set; } = new();
    public Exception? ThrowOnCredits { get; set; }

    public Task<CreditsResult> GetCreditsAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnCredits is not null) throw ThrowOnCredits;
        return Task.FromResult(CreditsToReturn);
    }
    public Task<IReadOnlyList<NoticeDto>> GetNoticesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<AdminSummary>> GetAdminsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<int> BanAdminAsync(int adminId, string reason, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public int? LastUpdatedAdminId { get; private set; }
    public string? LastUpdatedAdminName { get; private set; }
    public string? LastUpdatedAdminRole { get; private set; }
    public bool? LastUpdatedAdminIsActive { get; private set; }
    public Exception? ThrowOnUpdateAdmin { get; set; }

    public Task UpdateAdminAsync(int adminId, string name, string role, bool isActive, CancellationToken cancellationToken = default)
    {
        if (ThrowOnUpdateAdmin is not null)
        {
            throw ThrowOnUpdateAdmin;
        }

        LastUpdatedAdminId = adminId;
        LastUpdatedAdminName = name;
        LastUpdatedAdminRole = role;
        LastUpdatedAdminIsActive = isActive;
        return Task.CompletedTask;
    }
    public Task UnbanAdminAsync(int adminId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<int> ForceLogoutAdminAsync(int adminId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default, bool archived = false) => throw new NotImplementedException();
    public Task<IReadOnlyList<NewsDto>> GetNewsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<BannerDto>> GetBannersAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<FaqDto>> GetFaqsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<EventDto>> GetEventsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<GalleryAlbumDto>> GetGalleryAlbumsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<FacultyMemberDto>> GetFacultyAsync(CancellationToken cancellationToken = default, bool archived = false) => throw new NotImplementedException();
    public List<CommitteeDto> CommitteesToReturn { get; } = [];
    public Task<IReadOnlyList<CommitteeDto>> GetCommitteesAsync(CancellationToken cancellationToken = default, bool archived = false) => Task.FromResult<IReadOnlyList<CommitteeDto>>(CommitteesToReturn);
    public Task<IReadOnlyList<TimetableEntryDto>> GetTimetableEntriesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<TimetableEntryDto> PublishTimetableEntryAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    // Final Convergence Phase P0-4: several ViewModels now fetch this in their constructor to
    // populate a programme picker (fire-and-forget, non-blocking) — defaulting to an empty list
    // (rather than throwing, unlike most of this fake's unused members) keeps every existing test
    // that constructs those ViewModels exactly as stable as it was before this field existed.
    // Tests that specifically care about the picker's contents set ProgrammesToReturn first.
    public IReadOnlyList<ProgrammeDto> ProgrammesToReturn { get; set; } = [];

    public Task<IReadOnlyList<ProgrammeDto>> GetProgrammesAsync(CancellationToken cancellationToken = default, bool archived = false) =>
        Task.FromResult(ProgrammesToReturn);

    public string UploadPathToReturn { get; set; } = "assets/uploads/test/fake.png";
    public (string Module, string Filename, byte[] Bytes)? LastUpload { get; private set; }
    public Exception? ThrowOnUpload { get; set; }

    public Task<string> UploadFileAsync(string module, string filename, byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (ThrowOnUpload is not null) throw ThrowOnUpload;
        LastUpload = (module, filename, bytes);
        return Task.FromResult(UploadPathToReturn);
    }

    public Task DeleteUploadAsync(string module, string path, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public IReadOnlyList<ActivityLogEntryDto> ActivityLogEntriesToReturn { get; set; } = [];
    public (string? Action, string? Entity, int? AdminId)? LastActivityLogFilter { get; private set; }

    public Task<IReadOnlyList<ActivityLogEntryDto>> GetActivityLogAsync(string? action = null, string? entity = null, int? adminId = null, CancellationToken cancellationToken = default)
    {
        LastActivityLogFilter = (action, entity, adminId);
        return Task.FromResult(ActivityLogEntriesToReturn);
    }

    public PrincipalOverviewResult PrincipalOverviewToReturn { get; set; } = new();
    public Exception? ThrowOnPrincipalOverview { get; set; }

    public Task<PrincipalOverviewResult> GetPrincipalOverviewAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnPrincipalOverview is not null) throw ThrowOnPrincipalOverview;
        return Task.FromResult(PrincipalOverviewToReturn);
    }

    public DashboardStatsDto DashboardToReturn { get; set; } = new();
    public Exception? ThrowOnDashboard { get; set; }

    public Task<DashboardStatsDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnDashboard is not null) throw ThrowOnDashboard;
        return Task.FromResult(DashboardToReturn);
    }

    public IReadOnlyDictionary<string, string?>? LastGenericCreateFields { get; private set; }
    public string? LastGenericCreateEndpoint { get; private set; }
    public Exception? ThrowOnCreateGeneric { get; set; }

    public Task CreateGenericAsync(string endpoint, IReadOnlyDictionary<string, string?> fields, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCreateGeneric is not null)
        {
            throw ThrowOnCreateGeneric;
        }

        LastGenericCreateEndpoint = endpoint;
        LastGenericCreateFields = fields;
        return Task.CompletedTask;
    }

    public IReadOnlyDictionary<string, string?> GenericShowResult { get; set; } = new Dictionary<string, string?>();
    public Exception? ThrowOnGetGeneric { get; set; }

    public Task<IReadOnlyDictionary<string, string?>> GetGenericAsync(string endpoint, int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnGetGeneric is not null)
        {
            throw ThrowOnGetGeneric;
        }

        return Task.FromResult(GenericShowResult);
    }

    public string? LastGenericUpdateEndpoint { get; private set; }
    public int? LastGenericUpdateId { get; private set; }
    public IReadOnlyDictionary<string, string?>? LastGenericUpdateFields { get; private set; }
    public Exception? ThrowOnUpdateGeneric { get; set; }

    public Task UpdateGenericAsync(string endpoint, int id, IReadOnlyDictionary<string, string?> fields, CancellationToken cancellationToken = default)
    {
        if (ThrowOnUpdateGeneric is not null)
        {
            throw ThrowOnUpdateGeneric;
        }

        LastGenericUpdateEndpoint = endpoint;
        LastGenericUpdateId = id;
        LastGenericUpdateFields = fields;
        return Task.CompletedTask;
    }

    public string? LastGenericDeleteEndpoint { get; private set; }
    public int? LastGenericDeleteId { get; private set; }
    public Exception? ThrowOnDeleteGeneric { get; set; }

    public Task DeleteGenericAsync(string endpoint, int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnDeleteGeneric is not null)
        {
            throw ThrowOnDeleteGeneric;
        }

        LastGenericDeleteEndpoint = endpoint;
        LastGenericDeleteId = id;
        return Task.CompletedTask;
    }

    public string? LastGenericRestoreEndpoint { get; private set; }
    public int? LastGenericRestoreId { get; private set; }
    public Exception? ThrowOnRestoreGeneric { get; set; }

    public Task RestoreGenericAsync(string endpoint, int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnRestoreGeneric is not null)
        {
            throw ThrowOnRestoreGeneric;
        }

        LastGenericRestoreEndpoint = endpoint;
        LastGenericRestoreId = id;
        return Task.CompletedTask;
    }

    public List<GalleryImageDto> GalleryImages { get; } = [];
    public Exception? ThrowOnGalleryImages { get; set; }

    public Task<IReadOnlyList<GalleryImageDto>> GetGalleryImagesAsync(int albumId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnGalleryImages is not null)
        {
            throw ThrowOnGalleryImages;
        }

        return Task.FromResult<IReadOnlyList<GalleryImageDto>>(GalleryImages);
    }

    public Task<IReadOnlyList<GalleryImageDto>> AddGalleryImageAsync(
        int albumId, string imagePath, string? caption, int sortOrder, CancellationToken cancellationToken = default)
    {
        if (ThrowOnGalleryImages is not null)
        {
            throw ThrowOnGalleryImages;
        }

        GalleryImages.Add(new GalleryImageDto { Id = GalleryImages.Count + 1, AlbumId = albumId, ImagePath = imagePath, Caption = caption, SortOrder = sortOrder });
        return Task.FromResult<IReadOnlyList<GalleryImageDto>>(GalleryImages);
    }

    public Task<IReadOnlyList<GalleryImageDto>> DeleteGalleryImageAsync(int albumId, int imageId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnGalleryImages is not null)
        {
            throw ThrowOnGalleryImages;
        }

        GalleryImages.RemoveAll(i => i.Id == imageId);
        return Task.FromResult<IReadOnlyList<GalleryImageDto>>(GalleryImages);
    }

    public Task<IReadOnlyList<GalleryImageDto>> MoveGalleryImageAsync(int albumId, int imageId, string direction, CancellationToken cancellationToken = default)
    {
        if (ThrowOnGalleryImages is not null)
        {
            throw ThrowOnGalleryImages;
        }

        var ordered = GalleryImages.OrderBy(i => i.SortOrder).ToList();
        var index = ordered.FindIndex(i => i.Id == imageId);
        var neighborIndex = direction == "up" ? index - 1 : index + 1;
        if (index >= 0 && neighborIndex >= 0 && neighborIndex < ordered.Count)
        {
            var target = ordered[index];
            var neighbor = ordered[neighborIndex];
            GalleryImages[GalleryImages.FindIndex(i => i.Id == target.Id)] = target with { SortOrder = neighbor.SortOrder };
            GalleryImages[GalleryImages.FindIndex(i => i.Id == neighbor.Id)] = neighbor with { SortOrder = target.SortOrder };
        }

        return Task.FromResult<IReadOnlyList<GalleryImageDto>>(GalleryImages.OrderBy(i => i.SortOrder).ToList());
    }

    public PyqsListResult PyqsToReturn { get; set; } = new();
    public Exception? ThrowOnPyqs { get; set; }
    public (string? Status, string? Search)? LastPyqsFilterRequested { get; private set; }
    public IReadOnlyDictionary<string, object?>? LastCreatedPyqFields { get; private set; }
    public int? LastApprovedPyqId { get; private set; }
    public (int Id, string? Reason)? LastRejectedPyq { get; private set; }
    public int? LastRecalledPyqId { get; private set; }
    public int? LastDeletedPyqId { get; private set; }

    public Task<PyqsListResult> GetPyqsAsync(string? status = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPyqs is not null) throw ThrowOnPyqs;
        LastPyqsFilterRequested = (status, search);
        return Task.FromResult(PyqsToReturn);
    }

    public Task<PyqDto> CreatePyqAsync(IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPyqs is not null) throw ThrowOnPyqs;
        LastCreatedPyqFields = fields;
        return Task.FromResult(new PyqDto { Id = 1, Status = "approved" });
    }

    public Task<PyqDto> UpdatePyqAsync(int id, IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPyqs is not null) throw ThrowOnPyqs;
        return Task.FromResult(new PyqDto { Id = id });
    }

    public Task<PyqDto> ApprovePyqAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPyqs is not null) throw ThrowOnPyqs;
        LastApprovedPyqId = id;
        return Task.FromResult(new PyqDto { Id = id, Status = "approved" });
    }

    public Task<PyqDto> RejectPyqAsync(int id, string? reason, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPyqs is not null) throw ThrowOnPyqs;
        LastRejectedPyq = (id, reason);
        return Task.FromResult(new PyqDto { Id = id, Status = "rejected", RejectionReason = reason });
    }

    public Task<PyqDto> RecallPyqAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPyqs is not null) throw ThrowOnPyqs;
        LastRecalledPyqId = id;
        return Task.FromResult(new PyqDto { Id = id, Status = "pending" });
    }

    public Task DeletePyqAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPyqs is not null) throw ThrowOnPyqs;
        LastDeletedPyqId = id;
        return Task.CompletedTask;
    }

    public BlogsListResult BlogsToReturn { get; set; } = new();
    public Exception? ThrowOnBlogs { get; set; }
    public string? LastBlogsStatusRequested { get; private set; }
    public int? LastApprovedBlogId { get; private set; }
    public (int Id, string Reason)? LastRejectedBlog { get; private set; }
    public int? LastDeletedBlogId { get; private set; }

    public Task<BlogsListResult> GetBlogsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        if (ThrowOnBlogs is not null) throw ThrowOnBlogs;
        LastBlogsStatusRequested = status;
        return Task.FromResult(BlogsToReturn);
    }

    public Task<BlogPostDto> ApproveBlogAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnBlogs is not null) throw ThrowOnBlogs;
        LastApprovedBlogId = id;
        return Task.FromResult(new BlogPostDto { Id = id, Status = "published" });
    }

    public Task<BlogPostDto> RejectBlogAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        if (ThrowOnBlogs is not null) throw ThrowOnBlogs;
        LastRejectedBlog = (id, reason);
        return Task.FromResult(new BlogPostDto { Id = id, Status = "rejected", RejectionReason = reason });
    }

    public Task DeleteBlogAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnBlogs is not null) throw ThrowOnBlogs;
        LastDeletedBlogId = id;
        return Task.CompletedTask;
    }

    public List<ContactMessageDto> ContactMessages { get; } = [];
    public Exception? ThrowOnSubmissions { get; set; }
    public (int Id, string ReplyBody)? LastContactReply { get; private set; }
    public (int Id, int CommitteeId, string? Note)? LastContactForward { get; private set; }
    public int? LastDeletedContactId { get; private set; }

    public Task<IReadOnlyList<ContactMessageDto>> GetContactMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        return Task.FromResult<IReadOnlyList<ContactMessageDto>>(ContactMessages);
    }

    public Task<ContactMessageDto> MarkContactReadAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        return Task.FromResult(new ContactMessageDto { Id = id, IsRead = true });
    }

    public Task<ContactMessageDto> ReplyContactAsync(int id, string replyBody, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        LastContactReply = (id, replyBody);
        return Task.FromResult(new ContactMessageDto { Id = id, ReplyBody = replyBody });
    }

    public Task<ContactMessageDto> ForwardContactAsync(int id, int committeeId, string? forwardNote, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        LastContactForward = (id, committeeId, forwardNote);
        return Task.FromResult(new ContactMessageDto { Id = id, ForwardedToCommitteeId = committeeId, ForwardNote = forwardNote });
    }

    public Task DeleteContactAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        LastDeletedContactId = id;
        return Task.CompletedTask;
    }

    public List<GrievanceDto> Grievances { get; } = [];
    public (int Id, string ReplyBody)? LastGrievanceReply { get; private set; }
    public (int Id, int CommitteeId, string? Note)? LastGrievanceForward { get; private set; }
    public (int Id, string Status)? LastGrievanceStatusUpdate { get; private set; }

    public Task<IReadOnlyList<GrievanceDto>> GetGrievancesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        return Task.FromResult<IReadOnlyList<GrievanceDto>>(Grievances);
    }

    public Task<GrievanceDto> MarkGrievanceReadAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        return Task.FromResult(new GrievanceDto { Id = id, IsRead = true });
    }

    public Task<GrievanceDto> ReplyGrievanceAsync(int id, string replyBody, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        LastGrievanceReply = (id, replyBody);
        return Task.FromResult(new GrievanceDto { Id = id, ReplyBody = replyBody });
    }

    public Task<GrievanceDto> ForwardGrievanceAsync(int id, int committeeId, string? forwardNote, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        LastGrievanceForward = (id, committeeId, forwardNote);
        return Task.FromResult(new GrievanceDto { Id = id, ForwardedToCommitteeId = committeeId, ForwardNote = forwardNote });
    }

    public Task<GrievanceDto> UpdateGrievanceStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        LastGrievanceStatusUpdate = (id, status);
        return Task.FromResult(new GrievanceDto { Id = id, Status = status });
    }

    public List<AlumniDto> AlumniList { get; } = [];
    public int? LastAlumnusShowcaseToggled { get; private set; }
    public int? LastDeletedAlumnusId { get; private set; }

    public Task<IReadOnlyList<AlumniDto>> GetAlumniAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        return Task.FromResult<IReadOnlyList<AlumniDto>>(AlumniList);
    }

    public Task<AlumniDto> MarkAlumnusReadAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        return Task.FromResult(new AlumniDto { Id = id, IsRead = true });
    }

    public Task<AlumniDto> ToggleAlumnusShowcaseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        LastAlumnusShowcaseToggled = id;
        return Task.FromResult(new AlumniDto { Id = id, Showcase = true });
    }

    public Task DeleteAlumnusAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSubmissions is not null) throw ThrowOnSubmissions;
        LastDeletedAlumnusId = id;
        return Task.CompletedTask;
    }

    public CertificationsListResult CertificationsToReturn { get; set; } = new();
    public Exception? ThrowOnCertifications { get; set; }
    public string? LastCertificationsStatusRequested { get; private set; }
    public int? LastApprovedCertificationId { get; private set; }
    public (int Id, string Reason)? LastRejectedCertification { get; private set; }
    public List<CertificateTypeDto> CertificateTypes { get; } = [];
    public (string Code, string Name, string Prefix)? LastCreatedCertificateType { get; private set; }
    public int? LastToggledCertificateTypeId { get; private set; }
    public (int Id, int Offset)? LastCertificateTypeOffsetUpdate { get; private set; }

    public Task<CertificationsListResult> GetCertificationsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCertifications is not null)
        {
            throw ThrowOnCertifications;
        }

        LastCertificationsStatusRequested = status;
        return Task.FromResult(CertificationsToReturn);
    }

    public Task<CertificateApplicationDto> ApproveCertificationAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCertifications is not null)
        {
            throw ThrowOnCertifications;
        }

        LastApprovedCertificationId = id;
        return Task.FromResult(new CertificateApplicationDto { Id = id, Status = "approved" });
    }

    public Task<CertificateApplicationDto> RejectCertificationAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCertifications is not null)
        {
            throw ThrowOnCertifications;
        }

        LastRejectedCertification = (id, reason);
        return Task.FromResult(new CertificateApplicationDto { Id = id, Status = "rejected", RejectionReason = reason });
    }

    public Task<IReadOnlyList<CertificateTypeDto>> CreateCertificateTypeAsync(string code, string name, string prefix, string? description, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCertifications is not null)
        {
            throw ThrowOnCertifications;
        }

        LastCreatedCertificateType = (code, name, prefix);
        CertificateTypes.Add(new CertificateTypeDto { Id = CertificateTypes.Count + 1, Code = code, Name = name, Prefix = prefix, Description = description, IsActive = false });
        return Task.FromResult<IReadOnlyList<CertificateTypeDto>>(CertificateTypes);
    }

    public Task<IReadOnlyList<CertificateTypeDto>> ToggleCertificateTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCertifications is not null)
        {
            throw ThrowOnCertifications;
        }

        LastToggledCertificateTypeId = id;
        var index = CertificateTypes.FindIndex(t => t.Id == id);
        if (index >= 0)
        {
            CertificateTypes[index] = CertificateTypes[index] with { IsActive = !CertificateTypes[index].IsActive };
        }
        return Task.FromResult<IReadOnlyList<CertificateTypeDto>>(CertificateTypes);
    }

    public Task<IReadOnlyList<CertificateTypeDto>> UpdateCertificateTypeOffsetAsync(int id, int numberOffset, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCertifications is not null)
        {
            throw ThrowOnCertifications;
        }

        LastCertificateTypeOffsetUpdate = (id, numberOffset);
        return Task.FromResult<IReadOnlyList<CertificateTypeDto>>(CertificateTypes);
    }

    public PrincipalMessageDto PrincipalMessageToReturn { get; set; } = new();
    public Exception? ThrowOnPrincipalSection { get; set; }
    public (string? Name, string? Designation, string? Photo, string? Message)? LastPrincipalSectionUpdate { get; private set; }

    public Task<PrincipalMessageDto> GetPrincipalSectionAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnPrincipalSection is not null)
        {
            throw ThrowOnPrincipalSection;
        }

        return Task.FromResult(PrincipalMessageToReturn);
    }

    public Task<PrincipalMessageDto> UpdatePrincipalSectionAsync(string? name, string? designation, string? photo, string? message, CancellationToken cancellationToken = default)
    {
        if (ThrowOnPrincipalSection is not null)
        {
            throw ThrowOnPrincipalSection;
        }

        LastPrincipalSectionUpdate = (name, designation, photo, message);
        PrincipalMessageToReturn = new PrincipalMessageDto { Name = name, Designation = designation, Photo = photo, Message = message };
        return Task.FromResult(PrincipalMessageToReturn);
    }

    public CalendarEventsResult CalendarEventsToReturn { get; set; } = new();
    public Exception? ThrowOnCalendar { get; set; }
    public string? LastCalendarAcademicYearRequested { get; private set; }

    public Task<CalendarEventsResult> GetCalendarEventsAsync(string? academicYear = null, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCalendar is not null)
        {
            throw ThrowOnCalendar;
        }

        LastCalendarAcademicYearRequested = academicYear;
        return Task.FromResult(CalendarEventsToReturn);
    }

    public CalendarSyncResult CalendarSyncResultToReturn { get; set; } = new();
    public (string AcademicYear, bool ClearFirst)? LastCalendarSyncRequest { get; private set; }

    public Task<CalendarSyncResult> SyncCalendarHolidaysAsync(string academicYear, bool clearFirst, CancellationToken cancellationToken = default)
    {
        if (ThrowOnCalendar is not null)
        {
            throw ThrowOnCalendar;
        }

        LastCalendarSyncRequest = (academicYear, clearFirst);
        return Task.FromResult(CalendarSyncResultToReturn);
    }

    public List<ProgrammePaperDto> ProgrammePapers { get; } = [];
    public Exception? ThrowOnProgrammePapers { get; set; }

    public Task<IReadOnlyList<ProgrammePaperDto>> GetProgrammePapersAsync(int programmeId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnProgrammePapers is not null)
        {
            throw ThrowOnProgrammePapers;
        }

        return Task.FromResult<IReadOnlyList<ProgrammePaperDto>>(ProgrammePapers);
    }

    public Task<IReadOnlyList<ProgrammePaperDto>> AddProgrammePaperAsync(int programmeId, IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        if (ThrowOnProgrammePapers is not null)
        {
            throw ThrowOnProgrammePapers;
        }

        ProgrammePapers.Add(new ProgrammePaperDto
        {
            Id = ProgrammePapers.Count + 1,
            ProgrammeId = programmeId,
            Semester = int.TryParse(fields.GetValueOrDefault("semester")?.ToString(), out var s) ? s : 1,
            Title = fields.GetValueOrDefault("title")?.ToString() ?? "",
            PaperType = fields.GetValueOrDefault("paperType")?.ToString() ?? "",
            PaperCode = fields.GetValueOrDefault("paperCode")?.ToString(),
            Status = fields.GetValueOrDefault("status")?.ToString() ?? "draft",
        });
        return Task.FromResult<IReadOnlyList<ProgrammePaperDto>>(ProgrammePapers);
    }

    public Task<IReadOnlyList<ProgrammePaperDto>> DeleteProgrammePaperAsync(int programmeId, int paperId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnProgrammePapers is not null)
        {
            throw ThrowOnProgrammePapers;
        }

        ProgrammePapers.RemoveAll(p => p.Id == paperId);
        return Task.FromResult<IReadOnlyList<ProgrammePaperDto>>(ProgrammePapers);
    }

    public Exception? ThrowOnCreateTimetableEntry { get; set; }
    public TimetableEntryDto? LastCreatedTimetableEntry { get; private set; }

    public Task<TimetableEntryDto> CreateTimetableEntryAsync(
        string academicYear, int programmeId, int semester, string? section, string subject,
        int? facultyId, string? room, string dayOfWeek, string startTime, string endTime,
        int? departmentId, int? committeeId,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnCreateTimetableEntry is not null)
        {
            throw ThrowOnCreateTimetableEntry;
        }

        var entry = new TimetableEntryDto
        {
            AcademicYear = academicYear,
            ProgrammeId = programmeId,
            Semester = semester,
            Section = section,
            Subject = subject,
            FacultyId = facultyId,
            Room = room,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            DepartmentId = departmentId,
            CommitteeId = committeeId,
        };
        LastCreatedTimetableEntry = entry;
        return Task.FromResult(entry);
    }
}
