using System.Linq;
using System.Text.Json;
using CollegeAdmin.Contracts.Api;

namespace CollegeAdmin.Infrastructure.Api;

/// <summary>
/// Default <see cref="IApiClient"/> implementation. Registered as a typed client with
/// <see cref="AuthHeaderHandler"/> attached (see DependencyInjection.AddInfrastructure), so every
/// call here is automatically bearer-authenticated — no method here touches auth headers itself.
/// </summary>
public sealed class ApiClient(HttpClient httpClient) : IApiClient
{
    private const int PageSize = 200;

    /// <summary>
    /// Final Convergence Phase P1-5 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): every list endpoint
    /// caps at 200 rows per page server-side (e.g. StudentsController::index()); a single
    /// perPage=200 request used to be treated as "the whole list", silently truncating any export
    /// or screen once a college's real data passed 200 rows. This loops page-by-page using the
    /// envelope's own `meta.total`, stopping only once every row has actually been fetched (or the
    /// server returns an empty page, as a safety net against a miscounted total).
    /// </summary>
    private async Task<List<TItem>> FetchAllPagesAsync<TData, TItem>(string baseUrl, Func<TData, IReadOnlyList<TItem>> selectItems, CancellationToken cancellationToken)
    {
        var separator = baseUrl.Contains('?') ? '&' : '?';
        var all = new List<TItem>();
        var page = 1;
        while (true)
        {
            var (data, meta) = await ApiEnvelopeHttp.GetPageAsync<TData>(httpClient, $"{baseUrl}{separator}page={page}&perPage={PageSize}", cancellationToken);
            var items = selectItems(data);
            if (items.Count == 0)
            {
                break;
            }

            all.AddRange(items);
            if (meta?.Total is not int total || all.Count >= total)
            {
                break;
            }

            page++;
        }

        return all;
    }

    public Task<PingResult> PingAsync(CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.GetAsync<PingResult>(httpClient, "ping", cancellationToken);

    public async Task<AdminProfile> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<MeResult>(httpClient, "auth/me", cancellationToken);
        return result.Admin;
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<LogoutResult>(httpClient, "auth/logout", null, cancellationToken);

    public async Task<int> LogoutAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<LogoutAllResult>(httpClient, "auth/logout-all", null, cancellationToken);
        return result.RevokedSessions;
    }

    public Task<CreditsResult> GetCreditsAsync(CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.GetAsync<CreditsResult>(httpClient, "credits", cancellationToken);

    public async Task<IReadOnlyList<NoticeDto>> GetNoticesAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<NoticesListResult>(httpClient, "notices", cancellationToken);
        return result.Notices;
    }

    public async Task<IReadOnlyList<AdminSummary>> GetAdminsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<AdminsListResult>(httpClient, "admins", cancellationToken);
        return result.Admins;
    }

    public async Task<int> BanAdminAsync(int adminId, string reason, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<BanResult>(httpClient, $"admins/{adminId}/ban", new { reason }, cancellationToken);
        return result.RevokedSessions;
    }

    public Task UpdateAdminAsync(int adminId, string name, string role, bool isActive, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"admins/{adminId}", new { name, role, isActive }, cancellationToken);

    public Task UnbanAdminAsync(int adminId, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<BanResult>(httpClient, $"admins/{adminId}/unban", null, cancellationToken);

    public async Task<int> ForceLogoutAdminAsync(int adminId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<ForceLogoutResult>(httpClient, $"admins/{adminId}/force-logout", null, cancellationToken);
        return result.RevokedSessions;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default, bool archived = false)
    {
        var result = await ApiEnvelopeHttp.GetAsync<DepartmentsListResult>(httpClient, archived ? "departments?archived=1" : "departments", cancellationToken);
        return result.Departments;
    }

    public async Task<IReadOnlyList<NewsDto>> GetNewsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<NewsListResult>(httpClient, "news", cancellationToken);
        return result.NewsItems;
    }

    public async Task<IReadOnlyList<BannerDto>> GetBannersAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<BannersListResult>(httpClient, "banners", cancellationToken);
        return result.Banners;
    }

    public async Task<IReadOnlyList<FaqDto>> GetFaqsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<FaqsListResult>(httpClient, "faq", cancellationToken);
        return result.Faqs;
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<DocumentsListResult>(httpClient, "documents", cancellationToken);
        return result.Documents;
    }

    public async Task<IReadOnlyList<EventDto>> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<EventsListResult>(httpClient, "events", cancellationToken);
        return result.Events;
    }

    public async Task<IReadOnlyList<GalleryAlbumDto>> GetGalleryAlbumsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<GalleryAlbumsListResult>(httpClient, "gallery", cancellationToken);
        return result.Albums;
    }

    public async Task<IReadOnlyList<FacultyMemberDto>> GetFacultyAsync(CancellationToken cancellationToken = default, bool archived = false)
    {
        var result = await ApiEnvelopeHttp.GetAsync<FacultyListResult>(httpClient, archived ? "faculty?archived=1" : "faculty", cancellationToken);
        return result.Faculty;
    }

    public async Task<IReadOnlyList<CommitteeDto>> GetCommitteesAsync(CancellationToken cancellationToken = default, bool archived = false)
    {
        var result = await ApiEnvelopeHttp.GetAsync<CommitteesListResult>(httpClient, archived ? "committees?archived=1" : "committees", cancellationToken);
        return result.Committees;
    }

    public async Task<IReadOnlyList<ProgrammeDto>> GetProgrammesAsync(CancellationToken cancellationToken = default, bool archived = false)
    {
        var result = await ApiEnvelopeHttp.GetAsync<ProgrammesListResult>(httpClient, archived ? "programmes?archived=1" : "programmes", cancellationToken);
        return result.Programmes;
    }

    public async Task<string> UploadFileAsync(string module, string filename, byte[] bytes, CancellationToken cancellationToken = default)
    {
        var payload = new { module, filename, fileBase64 = Convert.ToBase64String(bytes) };
        var result = await ApiEnvelopeHttp.PostAsync<UploadResult>(httpClient, "uploads", payload, cancellationToken);
        return result.Path;
    }

    public Task DeleteUploadAsync(string module, string path, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, "uploads/delete", new { module, path }, cancellationToken);

    public async Task<IReadOnlyList<ActivityLogEntryDto>> GetActivityLogAsync(string? action = null, string? entity = null, int? adminId = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { "perPage=200" };
        if (!string.IsNullOrWhiteSpace(action)) query.Add($"action={Uri.EscapeDataString(action)}");
        if (!string.IsNullOrWhiteSpace(entity)) query.Add($"entity={Uri.EscapeDataString(entity)}");
        if (adminId is not null) query.Add($"adminId={adminId}");

        var result = await ApiEnvelopeHttp.GetAsync<ActivityLogListResult>(httpClient, $"activity-log?{string.Join('&', query)}", cancellationToken);
        return result.Entries;
    }

    public Task<PrincipalOverviewResult> GetPrincipalOverviewAsync(CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.GetAsync<PrincipalOverviewResult>(httpClient, "principal/overview", cancellationToken);

    public async Task<DashboardStatsDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<DashboardResult>(httpClient, "dashboard", cancellationToken);
        return result.Stats;
    }

    public async Task<NoticeDto> CreateNoticeAsync(
        string title, string? body, string? link, string? category, int? departmentId, int? committeeId,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            title,
            body,
            link,
            category,
            departmentId,
            committeeId,
        };
        var result = await ApiEnvelopeHttp.PostAsync<NoticeResult>(httpClient, "notices", payload, cancellationToken);
        return result.Notice;
    }

    public async Task<IReadOnlyList<TimetableEntryDto>> GetTimetableEntriesAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<TimetableEntriesListResult>(httpClient, "timetable", cancellationToken);
        return result.Entries;
    }

    public async Task<TimetableEntryDto> CreateTimetableEntryAsync(
        string academicYear, int programmeId, int semester, string? section, string subject,
        int? facultyId, string? room, string dayOfWeek, string startTime, string endTime,
        int? departmentId, int? committeeId,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            academicYear,
            programmeId,
            semester,
            section,
            subject,
            facultyId,
            room,
            dayOfWeek,
            startTime,
            endTime,
            departmentId,
            committeeId,
        };
        var result = await ApiEnvelopeHttp.PostAsync<TimetableEntryResult>(httpClient, "timetable", payload, cancellationToken);
        return result.Entry;
    }

    /// <summary>Final Convergence Phase P0-5 — the backend `timetable/{id}/publish` endpoint (toggles
    /// draft&lt;-&gt;published) already existed with no desktop consumer at all until now.</summary>
    public async Task<TimetableEntryDto> PublishTimetableEntryAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<TimetableEntryResult>(httpClient, $"timetable/{id}/publish", null, cancellationToken);
        return result.Entry;
    }

    public Task CreateGenericAsync(string endpoint, IReadOnlyDictionary<string, string?> fields, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, endpoint, fields, cancellationToken);

    public async Task<IReadOnlyDictionary<string, string?>> GetGenericAsync(string endpoint, int id, CancellationToken cancellationToken = default)
    {
        var data = await ApiEnvelopeHttp.GetAsync<JsonElement>(httpClient, $"{endpoint}/{id}", cancellationToken);
        // The server always wraps a single record as {responseKeySingular: {...}} — the wrapper
        // key differs per module (e.g. "newsItem" vs "banner"), so unwrap positionally rather than
        // needing to know each controller's exact key here.
        var row = data.EnumerateObject().First().Value;
        var result = new Dictionary<string, string?>();
        foreach (var prop in row.EnumerateObject())
        {
            result[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => prop.Value.ToString(),
            };
        }
        return result;
    }

    public Task UpdateGenericAsync(string endpoint, int id, IReadOnlyDictionary<string, string?> fields, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"{endpoint}/{id}", fields, cancellationToken);

    public Task DeleteGenericAsync(string endpoint, int id, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"{endpoint}/{id}/delete", null, cancellationToken);

    public Task RestoreGenericAsync(string endpoint, int id, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"{endpoint}/{id}/restore", null, cancellationToken);

    public async Task<IReadOnlyList<GalleryImageDto>> GetGalleryImagesAsync(int albumId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<GalleryImagesResult>(httpClient, $"gallery/{albumId}/images", cancellationToken);
        return result.Images;
    }

    public async Task<IReadOnlyList<GalleryImageDto>> AddGalleryImageAsync(
        int albumId, string imagePath, string? caption, int sortOrder, CancellationToken cancellationToken = default)
    {
        var payload = new { imagePath, caption, sortOrder };
        var result = await ApiEnvelopeHttp.PostAsync<GalleryImagesResult>(httpClient, $"gallery/{albumId}/images", payload, cancellationToken);
        return result.Images;
    }

    public async Task<IReadOnlyList<GalleryImageDto>> DeleteGalleryImageAsync(int albumId, int imageId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<GalleryImagesResult>(httpClient, $"gallery/{albumId}/images/{imageId}/delete", null, cancellationToken);
        return result.Images;
    }

    public async Task<IReadOnlyList<GalleryImageDto>> MoveGalleryImageAsync(int albumId, int imageId, string direction, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<GalleryImagesResult>(httpClient, $"gallery/{albumId}/images/{imageId}/move", new { direction }, cancellationToken);
        return result.Images;
    }

    public Task<CalendarEventsResult> GetCalendarEventsAsync(string? academicYear = null, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(academicYear) ? "academic-calendar" : $"academic-calendar?academicYear={Uri.EscapeDataString(academicYear)}";
        return ApiEnvelopeHttp.GetAsync<CalendarEventsResult>(httpClient, url, cancellationToken);
    }

    public Task<CalendarSyncResult> SyncCalendarHolidaysAsync(string academicYear, bool clearFirst, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<CalendarSyncResult>(httpClient, "academic-calendar/sync-holidays", new { academicYear, clearFirst }, cancellationToken);

    public async Task<PrincipalMessageDto> GetPrincipalSectionAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<PrincipalMessageResult>(httpClient, "principal-section", cancellationToken);
        return result.PrincipalMessage;
    }

    public async Task<PrincipalMessageDto> UpdatePrincipalSectionAsync(string? name, string? designation, string? photo, string? message, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<PrincipalMessageResult>(httpClient, "principal-section", new { name, designation, photo, message }, cancellationToken);
        return result.PrincipalMessage;
    }

    public Task<CertificationsListResult> GetCertificationsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(status) ? "certifications" : $"certifications?status={Uri.EscapeDataString(status)}";
        return ApiEnvelopeHttp.GetAsync<CertificationsListResult>(httpClient, url, cancellationToken);
    }

    public async Task<CertificateApplicationDto> ApproveCertificationAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<CertificationApplicationResult>(httpClient, $"certifications/{id}/approve", null, cancellationToken);
        return result.Application;
    }

    public async Task<CertificateApplicationDto> RejectCertificationAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<CertificationApplicationResult>(httpClient, $"certifications/{id}/reject", new { reason }, cancellationToken);
        return result.Application;
    }

    public async Task<IReadOnlyList<CertificateTypeDto>> CreateCertificateTypeAsync(string code, string name, string prefix, string? description, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<CertificateTypesResult>(httpClient, "certification-types", new { code, name, prefix, description }, cancellationToken);
        return result.Types;
    }

    public async Task<IReadOnlyList<CertificateTypeDto>> ToggleCertificateTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<CertificateTypesResult>(httpClient, $"certification-types/{id}/toggle", null, cancellationToken);
        return result.Types;
    }

    public async Task<IReadOnlyList<CertificateTypeDto>> UpdateCertificateTypeOffsetAsync(int id, int numberOffset, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<CertificateTypesResult>(httpClient, $"certification-types/{id}/offset", new { numberOffset }, cancellationToken);
        return result.Types;
    }

    public async Task<IReadOnlyList<ContactMessageDto>> GetContactMessagesAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<ContactMessagesResult>(httpClient, "submissions/contact", cancellationToken);
        return result.Messages;
    }

    public async Task<ContactMessageDto> MarkContactReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<ContactMessageResult>(httpClient, $"submissions/contact/{id}/read", null, cancellationToken);
        return result.Message;
    }

    public async Task<ContactMessageDto> ReplyContactAsync(int id, string replyBody, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<ContactMessageResult>(httpClient, $"submissions/contact/{id}/reply", new { replyBody }, cancellationToken);
        return result.Message;
    }

    public async Task<ContactMessageDto> ForwardContactAsync(int id, int committeeId, string? forwardNote, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<ContactMessageResult>(httpClient, $"submissions/contact/{id}/forward", new { committeeId, forwardNote }, cancellationToken);
        return result.Message;
    }

    public Task DeleteContactAsync(int id, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"submissions/contact/{id}/delete", null, cancellationToken);

    public async Task<IReadOnlyList<GrievanceDto>> GetGrievancesAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<GrievancesResult>(httpClient, "submissions/grievances", cancellationToken);
        return result.Grievances;
    }

    public async Task<GrievanceDto> MarkGrievanceReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<GrievanceResult>(httpClient, $"submissions/grievances/{id}/read", null, cancellationToken);
        return result.Grievance;
    }

    public async Task<GrievanceDto> ReplyGrievanceAsync(int id, string replyBody, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<GrievanceResult>(httpClient, $"submissions/grievances/{id}/reply", new { replyBody }, cancellationToken);
        return result.Grievance;
    }

    public async Task<GrievanceDto> ForwardGrievanceAsync(int id, int committeeId, string? forwardNote, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<GrievanceResult>(httpClient, $"submissions/grievances/{id}/forward", new { committeeId, forwardNote }, cancellationToken);
        return result.Grievance;
    }

    public async Task<GrievanceDto> UpdateGrievanceStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<GrievanceResult>(httpClient, $"submissions/grievances/{id}/status", new { status }, cancellationToken);
        return result.Grievance;
    }

    public async Task<IReadOnlyList<AlumniDto>> GetAlumniAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<AlumniListResult>(httpClient, "submissions/alumni", cancellationToken);
        return result.Alumni;
    }

    public async Task<AlumniDto> MarkAlumnusReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<AlumnusResult>(httpClient, $"submissions/alumni/{id}/read", null, cancellationToken);
        return result.Alumnus;
    }

    public async Task<AlumniDto> ToggleAlumnusShowcaseAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<AlumnusResult>(httpClient, $"submissions/alumni/{id}/showcase", null, cancellationToken);
        return result.Alumnus;
    }

    public Task DeleteAlumnusAsync(int id, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"submissions/alumni/{id}/delete", null, cancellationToken);

    public Task<BlogsListResult> GetBlogsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(status) ? "blogs" : $"blogs?status={Uri.EscapeDataString(status)}";
        return ApiEnvelopeHttp.GetAsync<BlogsListResult>(httpClient, url, cancellationToken);
    }

    public async Task<BlogPostDto> ApproveBlogAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<BlogPostResult>(httpClient, $"blogs/{id}/approve", null, cancellationToken);
        return result.Post;
    }

    public async Task<BlogPostDto> RejectBlogAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<BlogPostResult>(httpClient, $"blogs/{id}/reject", new { reason }, cancellationToken);
        return result.Post;
    }

    public Task DeleteBlogAsync(int id, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"blogs/{id}/delete", null, cancellationToken);

    public Task<PyqsListResult> GetPyqsAsync(string? status = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var url = query.Count > 0 ? $"pyqs?{string.Join('&', query)}" : "pyqs";
        return ApiEnvelopeHttp.GetAsync<PyqsListResult>(httpClient, url, cancellationToken);
    }

    public async Task<PyqDto> CreatePyqAsync(IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<PyqResult>(httpClient, "pyqs", fields, cancellationToken);
        return result.Paper;
    }

    public async Task<PyqDto> UpdatePyqAsync(int id, IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<PyqResult>(httpClient, $"pyqs/{id}", fields, cancellationToken);
        return result.Paper;
    }

    public async Task<PyqDto> ApprovePyqAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<PyqResult>(httpClient, $"pyqs/{id}/approve", null, cancellationToken);
        return result.Paper;
    }

    public async Task<PyqDto> RejectPyqAsync(int id, string? reason, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<PyqResult>(httpClient, $"pyqs/{id}/reject", new { reason }, cancellationToken);
        return result.Paper;
    }

    public async Task<PyqDto> RecallPyqAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<PyqResult>(httpClient, $"pyqs/{id}/recall", null, cancellationToken);
        return result.Paper;
    }

    public Task DeletePyqAsync(int id, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"pyqs/{id}/delete", null, cancellationToken);

    public async Task<IReadOnlyList<ProgrammePaperDto>> GetProgrammePapersAsync(int programmeId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<ProgrammePapersResult>(httpClient, $"programmes/{programmeId}/papers", cancellationToken);
        return result.Papers;
    }

    public async Task<IReadOnlyList<ProgrammePaperDto>> AddProgrammePaperAsync(int programmeId, IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<ProgrammePapersResult>(httpClient, $"programmes/{programmeId}/papers", fields, cancellationToken);
        return result.Papers;
    }

    public async Task<IReadOnlyList<ProgrammePaperDto>> DeleteProgrammePaperAsync(int programmeId, int paperId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<ProgrammePapersResult>(httpClient, $"programmes/{programmeId}/papers/{paperId}/delete", null, cancellationToken);
        return result.Papers;
    }

    public Task RecordExportAsync(string module, int rowCount, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, "exports/audit", new { module, rowCount }, cancellationToken);

    public async Task<IReadOnlyList<BackgroundJobDto>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<JobsListResult>(httpClient, "jobs", cancellationToken);
        return result.Jobs;
    }

    public Task<RunJobResult> RunJobAsync(string type, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<RunJobResult>(httpClient, "jobs/run", new { type }, cancellationToken);

    public async Task<IReadOnlyList<StudentDto>> GetStudentsAsync(CancellationToken cancellationToken = default) =>
        await FetchAllPagesAsync<StudentsListResult, StudentDto>("students", r => r.Students, cancellationToken);

    public Task<StudentResult> CreateStudentAsync(IReadOnlyDictionary<string, object?> fields, bool finalize, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>(fields) { ["finalize"] = finalize };
        return ApiEnvelopeHttp.PostAsync<StudentResult>(httpClient, "students", body, cancellationToken);
    }

    public Task<StudentResult> UpdateStudentAsync(int id, IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<StudentResult>(httpClient, $"students/{id}", fields, cancellationToken);

    public async Task<StudentDto> FinalizeStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<StudentResult>(httpClient, $"students/{id}/finalize", null, cancellationToken);
        return result.Student;
    }

    public async Task<StudentDto> ArchiveStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<StudentResult>(httpClient, $"students/{id}/archive", null, cancellationToken);
        return result.Student;
    }

    public async Task<StudentFullDto> GetStudentFullAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<StudentFullResult>(httpClient, $"students/{id}/full", cancellationToken);
        return result.Student;
    }

    public Task<StudentHistoryResult> GetStudentHistoryAsync(int id, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.GetAsync<StudentHistoryResult>(httpClient, $"students/{id}/history", cancellationToken);

    public Task<StudentImportBatchResult> UploadStudentImportAsync(string filename, byte[] fileBytes, CancellationToken cancellationToken = default)
    {
        var body = new { filename, fileBase64 = Convert.ToBase64String(fileBytes) };
        return ApiEnvelopeHttp.PostAsync<StudentImportBatchResult>(httpClient, "student-imports", body, cancellationToken);
    }

    public async Task<StudentImportBatchDto> GetStudentImportBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<StudentImportBatchOnlyResult>(httpClient, $"student-imports/{batchId}", cancellationToken);
        return result.Batch;
    }

    public async Task<IReadOnlyList<StudentImportRowDto>> GetStudentImportRowsAsync(int batchId, CancellationToken cancellationToken = default) =>
        await FetchAllPagesAsync<StudentImportRowsResult, StudentImportRowDto>($"student-imports/{batchId}/rows", r => r.Rows, cancellationToken);

    public async Task<StudentImportRowDto> UpdateStudentImportRowAsync(int batchId, int rowId, object? normalizedOverrides, string? resolution, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?> { ["normalized"] = normalizedOverrides, ["resolution"] = resolution };
        var result = await ApiEnvelopeHttp.PostAsync<StudentImportRowResult>(httpClient, $"student-imports/{batchId}/rows/{rowId}", body, cancellationToken);
        return result.Row;
    }

    public Task<StudentImportConfirmResult> ConfirmStudentImportAsync(int batchId, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<StudentImportConfirmResult>(httpClient, $"student-imports/{batchId}/confirm", null, cancellationToken);

    public async Task<IReadOnlyList<PreferenceWindowDto>> GetPreferenceWindowsAsync(CancellationToken cancellationToken = default) =>
        await FetchAllPagesAsync<PreferenceWindowsListResult, PreferenceWindowDto>("preference-windows", r => r.Windows, cancellationToken);

    public async Task<PreferenceWindowDto> CreatePreferenceWindowAsync(IReadOnlyDictionary<string, object?> fields, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<PreferenceWindowResult>(httpClient, "preference-windows", fields, cancellationToken);
        return result.Window;
    }

    public async Task<PreferenceWindowDto> OpenPreferenceWindowAsync(int windowId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<PreferenceWindowResult>(httpClient, $"preference-windows/{windowId}/open", null, cancellationToken);
        return result.Window;
    }

    public async Task<PreferenceWindowDto> ClosePreferenceWindowAsync(int windowId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<PreferenceWindowResult>(httpClient, $"preference-windows/{windowId}/close", null, cancellationToken);
        return result.Window;
    }

    public async Task<PreferenceWindowSummaryDto> GetPreferenceWindowSummaryAsync(int windowId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<PreferenceWindowSummaryResult>(httpClient, $"preference-windows/{windowId}/summary", cancellationToken);
        return result.Summary;
    }

    public async Task<IReadOnlyList<PreferenceSubmissionDto>> GetPreferenceWindowSubmissionsAsync(int windowId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<PreferenceSubmissionsResult>(httpClient, $"preference-windows/{windowId}/submissions", cancellationToken);
        return result.Submissions;
    }

    public Task InvalidateSubmissionAsync(int submissionId, string reason, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"preference-submissions/{submissionId}/invalidate", new { reason }, cancellationToken);

    public Task ReopenSubmissionAsync(int submissionId, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"preference-submissions/{submissionId}/reopen", null, cancellationToken);

    public async Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default, bool archived = false)
    {
        var result = await ApiEnvelopeHttp.GetAsync<SubjectsListResult>(httpClient, archived ? "subjects?archived=1" : "subjects", cancellationToken);
        return result.Subjects;
    }

    public Task<RunAllotmentResult> RunAllotmentAsync(int windowId, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<RunAllotmentResult>(httpClient, $"preference-windows/{windowId}/allotment-runs", null, cancellationToken);

    public async Task<IReadOnlyList<AllotmentRunDto>> GetAllotmentRunsAsync(int windowId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<AllotmentRunsListResult>(httpClient, $"preference-windows/{windowId}/allotment-runs", cancellationToken);
        return result.Runs;
    }

    public async Task<IReadOnlyList<AllotmentResultDto>> GetAllotmentResultsAsync(int runId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<AllotmentResultsListResult>(httpClient, $"allotment-runs/{runId}/results", cancellationToken);
        return result.Results;
    }

    public async Task<int> FinalizeAllotmentRunAsync(int runId, CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.PostAsync<Dictionary<string, int>>(httpClient, $"allotment-runs/{runId}/finalize", null, cancellationToken);
        return result["finalized"];
    }

    public async Task<int> OverrideAllotmentResultAsync(int resultId, int? newSubjectId, string reason, CancellationToken cancellationToken = default)
    {
        var body = new { subjectId = newSubjectId, reason };
        var result = await ApiEnvelopeHttp.PostAsync<Dictionary<string, int>>(httpClient, $"allotment-results/{resultId}/override", body, cancellationToken);
        return result["newAllotmentId"];
    }

    public Task CancelAllotmentResultAsync(int resultId, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<object>(httpClient, $"allotment-results/{resultId}/cancel", null, cancellationToken);

    public Task<UniversityRrPreviewResult> PreviewUniversityRrAsync(string academicSession, int semester, int? programmeId, CancellationToken cancellationToken = default)
    {
        var query = $"reports/university-rr/preview?academicSession={Uri.EscapeDataString(academicSession)}&semester={semester}"
            + (programmeId is not null ? $"&programmeId={programmeId}" : "");
        return ApiEnvelopeHttp.GetAsync<UniversityRrPreviewResult>(httpClient, query, cancellationToken);
    }

    public async Task<(string Filename, byte[] Bytes, UniversityRrTotalsDto Totals, bool MappingVerified)> ExportUniversityRrAsync(string academicSession, int semester, int? programmeId, CancellationToken cancellationToken = default)
    {
        var body = new { academicSession, semester, programmeId };
        var result = await ApiEnvelopeHttp.PostAsync<UniversityRrExportResult>(httpClient, "reports/university-rr/export", body, cancellationToken);
        return (result.Filename, Convert.FromBase64String(result.FileBase64), result.Totals, result.MappingVerified);
    }

    public async Task<IReadOnlyList<AdmissionLinkDto>> GetAdmissionLinksAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<AdmissionLinksListResult>(httpClient, "admission-links", cancellationToken);
        return result.AdmissionLinks;
    }

    public async Task<IReadOnlyList<AdmissionBannerDto>> GetAdmissionBannersAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<AdmissionBannersListResult>(httpClient, "admission-banners", cancellationToken);
        return result.AdmissionBanners;
    }

    public Task<BackupGeneratedResult> GenerateBackupAsync(CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.PostAsync<BackupGeneratedResult>(httpClient, "backups", body: null, cancellationToken);

    public Task<byte[]> DownloadBackupAsync(string token, CancellationToken cancellationToken = default) =>
        ApiEnvelopeHttp.GetBytesAsync(httpClient, $"backups/{Uri.EscapeDataString(token)}/download", cancellationToken);

    public async Task<IReadOnlyList<BackupSummaryDto>> GetRecentBackupsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ApiEnvelopeHttp.GetAsync<BackupsListResult>(httpClient, "backups", cancellationToken);
        return result.Backups;
    }
}
