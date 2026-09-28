using CollegeAdmin.Contracts.Api;

namespace CollegeAdmin.Infrastructure.Api;

/// <summary>
/// Typed client for /api/v1 business endpoints — grows one method per endpoint as later stages
/// add them. Every call here is automatically bearer-authenticated by AuthHeaderHandler using
/// whatever token AuthSessionService currently holds; callers never attach the header themselves.
/// Login/refresh are NOT here — see AuthSessionService for why they use a separate, unauthenticated
/// path instead.
/// </summary>
public interface IApiClient
{
    Task<PingResult> PingAsync(CancellationToken cancellationToken = default);
    Task<AdminProfile> GetMeAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<int> LogoutAllAsync(CancellationToken cancellationToken = default);
    Task<CreditsResult> GetCreditsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NoticeDto>> GetNoticesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminSummary>> GetAdminsAsync(CancellationToken cancellationToken = default);
    Task<int> BanAdminAsync(int adminId, string reason, CancellationToken cancellationToken = default);
    Task UpdateAdminAsync(int adminId, string name, string role, bool isActive, CancellationToken cancellationToken = default);
    Task UnbanAdminAsync(int adminId, CancellationToken cancellationToken = default);
    Task<int> ForceLogoutAdminAsync(int adminId, CancellationToken cancellationToken = default);
    /// <summary>`archived: true` (Final Convergence Phase P2 item 1) requests only archived rows —
    /// default (false) matches this endpoint's normal "active only" list.</summary>
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default, bool archived = false);
    Task<IReadOnlyList<NewsDto>> GetNewsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BannerDto>> GetBannersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FaqDto>> GetFaqsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventDto>> GetEventsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GalleryAlbumDto>> GetGalleryAlbumsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FacultyMemberDto>> GetFacultyAsync(CancellationToken cancellationToken = default, bool archived = false);
    Task<IReadOnlyList<CommitteeDto>> GetCommitteesAsync(CancellationToken cancellationToken = default, bool archived = false);

    /// <summary>Final Convergence Phase P0-4 — the canonical Programme list, used both by the
    /// dedicated Programmes management screen and by every picker (Preference Windows, Timetable,
    /// University RR) that used to take a raw free-text Programme ID. `archived` per P2 item 1.</summary>
    Task<IReadOnlyList<ProgrammeDto>> GetProgrammesAsync(CancellationToken cancellationToken = default, bool archived = false);

    /// <summary>Final Convergence Phase P0-7 — uploads a real file for one content module (notices/
    /// news/events/gallery/banners/documents) via /api/v1/uploads, returning the server-assigned
    /// path to store in that module's own image/file field. Replaces the untyped path-string
    /// TextBox every one of those modules previously exposed.</summary>
    Task<string> UploadFileAsync(string module, string filename, byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary>Deletes a previously-uploaded file (e.g. when a field's selection is replaced).</summary>
    Task DeleteUploadAsync(string module, string path, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-1 — the fully-built, capability-gated backend audit
    /// endpoint had zero desktop consumer at all until now. Filters are optional (server-side).</summary>
    Task<IReadOnlyList<ActivityLogEntryDto>> GetActivityLogAsync(string? action = null, string? entity = null, int? adminId = null, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-14 — the universal "at a glance" Dashboard landing page,
    /// scoped to whatever the caller can actually see (the server omits a domain entirely rather
    /// than returning a misleading zero for one the caller has no grant for).</summary>
    Task<DashboardStatsDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-10/§2b — Academic Calendar. Create/update/delete reuse
    /// the generic endpoint-agnostic methods above (academic-calendar's response shape matches every
    /// other flat module); this is only for the list, since its envelope also carries the current
    /// academic year and year-picker options alongside the events themselves.</summary>
    Task<CalendarEventsResult> GetCalendarEventsAsync(string? academicYear = null, CancellationToken cancellationToken = default);

    Task<CalendarSyncResult> SyncCalendarHolidaysAsync(string academicYear, bool clearFirst, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-10/§2b — the public Principal's-message website
    /// content (name/designation/photo/message). A single-row settings-shaped resource, same
    /// show/update shape as Settings.</summary>
    Task<PrincipalMessageDto> GetPrincipalSectionAsync(CancellationToken cancellationToken = default);

    Task<PrincipalMessageDto> UpdatePrincipalSectionAsync(string? name, string? designation, string? photo, string? message, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-10/§2b — Certifications, admin review only (student-
    /// facing application/upload/download/QR-verification pages stay web-based).</summary>
    Task<CertificationsListResult> GetCertificationsAsync(string? status = null, CancellationToken cancellationToken = default);

    Task<CertificateApplicationDto> ApproveCertificationAsync(int id, CancellationToken cancellationToken = default);

    Task<CertificateApplicationDto> RejectCertificationAsync(int id, string reason, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CertificateTypeDto>> CreateCertificateTypeAsync(string code, string name, string prefix, string? description, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CertificateTypeDto>> ToggleCertificateTypeAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CertificateTypeDto>> UpdateCertificateTypeOffsetAsync(int id, int numberOffset, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-10/§2b — Contact/Grievance/Alumni submissions, admin
    /// handling only (public submission forms stay on the legacy website). Committee-scoped access
    /// is enforced server-side; these calls simply surface whatever the server allows.</summary>
    Task<IReadOnlyList<ContactMessageDto>> GetContactMessagesAsync(CancellationToken cancellationToken = default);
    Task<ContactMessageDto> MarkContactReadAsync(int id, CancellationToken cancellationToken = default);
    Task<ContactMessageDto> ReplyContactAsync(int id, string replyBody, CancellationToken cancellationToken = default);
    Task<ContactMessageDto> ForwardContactAsync(int id, int committeeId, string? forwardNote, CancellationToken cancellationToken = default);
    Task DeleteContactAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GrievanceDto>> GetGrievancesAsync(CancellationToken cancellationToken = default);
    Task<GrievanceDto> MarkGrievanceReadAsync(int id, CancellationToken cancellationToken = default);
    Task<GrievanceDto> ReplyGrievanceAsync(int id, string replyBody, CancellationToken cancellationToken = default);
    Task<GrievanceDto> ForwardGrievanceAsync(int id, int committeeId, string? forwardNote, CancellationToken cancellationToken = default);
    Task<GrievanceDto> UpdateGrievanceStatusAsync(int id, string status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlumniDto>> GetAlumniAsync(CancellationToken cancellationToken = default);
    Task<AlumniDto> MarkAlumnusReadAsync(int id, CancellationToken cancellationToken = default);
    Task<AlumniDto> ToggleAlumnusShowcaseAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteAlumnusAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-10/§2b — Blogs, admin moderation only (public
    /// submission/self-service-edit stay on the legacy website).</summary>
    Task<BlogsListResult> GetBlogsAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<BlogPostDto> ApproveBlogAsync(int id, CancellationToken cancellationToken = default);
    Task<BlogPostDto> RejectBlogAsync(int id, string reason, CancellationToken cancellationToken = default);
    Task DeleteBlogAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-10/§2b — PYQ, moderation + staff upload (public
    /// submission and the IP-ban/NSFW-filter path stay on the legacy website).</summary>
    Task<PyqsListResult> GetPyqsAsync(string? status = null, string? search = null, CancellationToken cancellationToken = default);
    Task<PyqDto> CreatePyqAsync(IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default);
    Task<PyqDto> UpdatePyqAsync(int id, IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default);
    Task<PyqDto> ApprovePyqAsync(int id, CancellationToken cancellationToken = default);
    Task<PyqDto> RejectPyqAsync(int id, string? reason, CancellationToken cancellationToken = default);
    Task<PyqDto> RecallPyqAsync(int id, CancellationToken cancellationToken = default);
    Task DeletePyqAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-20 — a programme's papers, nested the same way Gallery's
    /// images are nested under an album.</summary>
    Task<IReadOnlyList<ProgrammePaperDto>> GetProgrammePapersAsync(int programmeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProgrammePaperDto>> AddProgrammePaperAsync(int programmeId, IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProgrammePaperDto>> DeleteProgrammePaperAsync(int programmeId, int paperId, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-8 — swaps an existing gallery image's position with its
    /// immediate neighbor ("up" or "down"); a no-op (not an error) if already at that edge.</summary>
    Task<IReadOnlyList<GalleryImageDto>> MoveGalleryImageAsync(int albumId, int imageId, string direction, CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P1-3 — the read-only cross-module "Institution Overview"
    /// a Principal-role admin lands on, gated on the new `principal.overview` capability.</summary>
    Task<PrincipalOverviewResult> GetPrincipalOverviewAsync(CancellationToken cancellationToken = default);

    Task<NoticeDto> CreateNoticeAsync(
        string title, string? body, string? link, string? category, int? departmentId, int? committeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TimetableEntryDto>> GetTimetableEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Final Convergence Phase P0-5 — toggles an entry between draft and published.</summary>
    Task<TimetableEntryDto> PublishTimetableEntryAsync(int id, CancellationToken cancellationToken = default);

    Task<TimetableEntryDto> CreateTimetableEntryAsync(
        string academicYear, int programmeId, int semester, string? section, string subject,
        int? facultyId, string? room, string dayOfWeek, string startTime, string endTime,
        int? departmentId, int? committeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generic create for the flat modules (News/Banners/FAQ/Documents/Events/Gallery/Faculty/
    /// Committees) — posts field values to `endpoint` and discards the typed response (the caller
    /// re-fetches the list, which already deserializes correctly-typed rows; this only needs to
    /// know "did it succeed", so one method replaces 8 near-identical typed Create methods the
    /// same way GenericListViewModel replaced 8 near-identical List screens).
    /// </summary>
    Task CreateGenericAsync(string endpoint, IReadOnlyDictionary<string, string?> fields, CancellationToken cancellationToken = default);

    /// <summary>Fetches a single record's raw field values (unwrapped from the server's
    /// {responseKeySingular: {...}} envelope) so an Edit dialog can be pre-filled — every server
    /// column comes back as a string, matching what FormField/CreateGenericAsync already work with.</summary>
    Task<IReadOnlyDictionary<string, string?>> GetGenericAsync(string endpoint, int id, CancellationToken cancellationToken = default);

    /// <summary>Generic update, mirroring CreateGenericAsync — every SimpleContentController/
    /// DepartmentScopedContentController route accepts POST {endpoint}/{id} with any subset of
    /// fillable() fields (unset ones are left unchanged server-side).</summary>
    Task UpdateGenericAsync(string endpoint, int id, IReadOnlyDictionary<string, string?> fields, CancellationToken cancellationToken = default);

    /// <summary>Generic delete — every module's destroy route is POST {endpoint}/{id}/delete. For
    /// Departments/Faculty/Committees/Subjects/Programmes (Final Convergence Phase P2 item 1) this
    /// same call now archives instead of hard-deleting server-side — the route/verb didn't change,
    /// only what it does for those 5 endpoints specifically.</summary>
    Task DeleteGenericAsync(string endpoint, int id, CancellationToken cancellationToken = default);

    /// <summary>Un-archives a row previously archived via DeleteGenericAsync — only meaningful for
    /// the same 5 archivable endpoints; POST {endpoint}/{id}/restore.</summary>
    Task RestoreGenericAsync(string endpoint, int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GalleryImageDto>> GetGalleryImagesAsync(int albumId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GalleryImageDto>> AddGalleryImageAsync(
        int albumId, string imagePath, string? caption, int sortOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GalleryImageDto>> DeleteGalleryImageAsync(int albumId, int imageId, CancellationToken cancellationToken = default);

    /// <summary>Records that an export happened (module + row count) — see ExportsController::audit
    /// for why this is enough: the export itself reuses already-authorized, already-fetched data,
    /// so there is nothing left to authorize here, only to log.</summary>
    Task RecordExportAsync(string module, int rowCount, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackgroundJobDto>> GetJobsAsync(CancellationToken cancellationToken = default);

    Task<RunJobResult> RunJobAsync(string type, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentDto>> GetStudentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Student Manager create/edit (docs/claude/phase2/PHASE2_IMPLEMENTATION_PLAN.md Slice 1) — unlike
    /// the flat modules' CreateGenericAsync/UpdateGenericAsync, fields is object-valued rather than
    /// string-valued so a nested section (residentialAddress/bankAccount/priorEducation/
    /// priorSubjects/...) can be posted as a real nested JSON object/array, not a flattened string.
    /// Returns the saved record plus any soft duplicate-candidate warnings
    /// (StudentsController::findDuplicateCandidates) for the caller to surface, never to block on.
    /// </summary>
    Task<StudentResult> CreateStudentAsync(IReadOnlyDictionary<string, object?> fields, bool finalize, CancellationToken cancellationToken = default);

    Task<StudentResult> UpdateStudentAsync(int id, IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default);

    /// <summary>Strict-validates and flips lifecycle draft -> active (StudentsController::finalize).</summary>
    Task<StudentDto> FinalizeStudentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes into lifecycle=archived — never a hard DELETE (StudentsController::archive).</summary>
    Task<StudentDto> ArchiveStudentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The full canonical record — core row + every related-table section — for the Student
    /// Detail workspace's tabs, in one call (StudentsController::showFull).</summary>
    Task<StudentFullDto> GetStudentFullAsync(int id, CancellationToken cancellationToken = default);

    Task<StudentHistoryResult> GetStudentHistoryAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Excel Import Pipeline (docs/claude/phase2/04_EXCEL_IMPORT_PIPELINE.md, Slice 2).
    /// Uploads as base64 inside the ordinary JSON body — see StudentImportsController's own remarks
    /// on why (no multipart path exists anywhere in this API, and building one wasn't worth it for
    /// file sizes this small). Never writes to `students` — only stages for review.</summary>
    Task<StudentImportBatchResult> UploadStudentImportAsync(string filename, byte[] fileBytes, CancellationToken cancellationToken = default);

    Task<StudentImportBatchDto> GetStudentImportBatchAsync(int batchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentImportRowDto>> GetStudentImportRowsAsync(int batchId, CancellationToken cancellationToken = default);

    /// <summary>`normalizedOverrides` is merged (server-side, recursively) into the row's existing
    /// normalized values and reclassified — pass only the fields actually being corrected, not the
    /// whole row. `resolution` is optional (null leaves it unchanged).</summary>
    Task<StudentImportRowDto> UpdateStudentImportRowAsync(int batchId, int rowId, object? normalizedOverrides, string? resolution, CancellationToken cancellationToken = default);

    /// <summary>Transactional commit — creates real students for every included, non-error row.
    /// Never partial: either the whole batch commits or the server marks it 'failed' and nothing
    /// is created.</summary>
    Task<StudentImportConfirmResult> ConfirmStudentImportAsync(int batchId, CancellationToken cancellationToken = default);

    /// <summary>Generalized Preference Windows (docs/claude/phase2/06_PREFERENCE_SYSTEM.md, Slice 3).</summary>
    Task<IReadOnlyList<PreferenceWindowDto>> GetPreferenceWindowsAsync(CancellationToken cancellationToken = default);

    Task<PreferenceWindowDto> CreatePreferenceWindowAsync(IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default);

    Task<PreferenceWindowDto> OpenPreferenceWindowAsync(int windowId, CancellationToken cancellationToken = default);

    Task<PreferenceWindowDto> ClosePreferenceWindowAsync(int windowId, CancellationToken cancellationToken = default);

    Task<PreferenceWindowSummaryDto> GetPreferenceWindowSummaryAsync(int windowId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PreferenceSubmissionDto>> GetPreferenceWindowSubmissionsAsync(int windowId, CancellationToken cancellationToken = default);

    Task InvalidateSubmissionAsync(int submissionId, string reason, CancellationToken cancellationToken = default);

    Task ReopenSubmissionAsync(int submissionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default, bool archived = false);

    /// <summary>Allotment Engine (docs/claude/phase2/07_ALLOTMENT_ENGINE.md, Slice 4). The window
    /// must be Closed first (server-enforced).</summary>
    Task<RunAllotmentResult> RunAllotmentAsync(int windowId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AllotmentRunDto>> GetAllotmentRunsAsync(int windowId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AllotmentResultDto>> GetAllotmentResultsAsync(int runId, CancellationToken cancellationToken = default);

    Task<int> FinalizeAllotmentRunAsync(int runId, CancellationToken cancellationToken = default);

    /// <summary>Never a silent edit — supersedes the old row and inserts a fresh one, returns the new row's id.</summary>
    Task<int> OverrideAllotmentResultAsync(int resultId, int? newSubjectId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Only works on a Proposed (not yet finalized) result.</summary>
    Task CancelAllotmentResultAsync(int resultId, CancellationToken cancellationToken = default);

    /// <summary>University RR Export (docs/claude/phase2/University_RR_Export_Claude_Addendum.md, Slice 5).</summary>
    Task<UniversityRrPreviewResult> PreviewUniversityRrAsync(string academicSession, int semester, int? programmeId, CancellationToken cancellationToken = default);

    /// <summary>Returns the final XLSX as bytes, decoded from the server's base64 response — the
    /// caller handles saving it to disk (a View-layer concern, matching StudentImportWindow's own
    /// file-handling split).</summary>
    Task<(string Filename, byte[] Bytes, UniversityRrTotalsDto Totals, bool MappingVerified)> ExportUniversityRrAsync(string academicSession, int semester, int? programmeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdmissionLinkDto>> GetAdmissionLinksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdmissionBannerDto>> GetAdmissionBannersAsync(CancellationToken cancellationToken = default);
}
