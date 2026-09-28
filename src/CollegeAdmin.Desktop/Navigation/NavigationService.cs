using System.Linq;
using System.Windows;
using CollegeAdmin.Application.Caching;
using CollegeAdmin.Application.Navigation;
using CollegeAdmin.Application.Notifications;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Desktop.Views;
using CollegeAdmin.Infrastructure.Api;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeAdmin.Desktop.Navigation;

/// <summary>
/// Most nav keys still resolve to a PlaceholderPageViewModel (no real module exists yet). "Settings"
/// (formerly "System") shows connectivity/version/diagnostics plus Credits (CLAUDE.md requirement),
/// folded in as a section per P1-15. "Website Content" and "College Structure" show tabbed
/// generic list screens, one tab per shipped module, grouped per docs/claude/05_MODULE_CATALOG.md.
/// "Administration" has its own dedicated screen (ban/unban/force-logout row actions don't fit the
/// generic list). The mapping is intentionally centralized here rather than scattered per-caller.
///
/// Every flat module's Create form goes through GenericCreateViewModel/GenericCreateWindow (a field
/// list + IApiClient.CreateGenericAsync) — Notices and Timetable keep dedicated Create forms because
/// they have real logic beyond "post these fields" (scope picker, conflict-detail parsing).
///
/// Known gap, not silently accepted: every tab's GenericListViewModel starts loading immediately on
/// construction, and nothing here caches pages across navigations — revisiting "Website Content"
/// re-fetches all 7 tabs' data every time. Acceptable for Phase 1's current List-only scope.
/// Final Convergence Phase P1-17 closed the one lookup with real, measured duplicate-fetch waste
/// (Programmes, independently re-fetched by 5 different call sites) via <see cref="lookupCache"/> —
/// see GetProgrammeOptionsAsync and the "Programmes" SimpleModule entry below. The other tabs'
/// full-list re-fetching is a different, larger problem (real content data, not a small lookup list)
/// and stays out of this scope, named not silently dropped.
/// </summary>
public sealed class NavigationService(IServiceProvider serviceProvider, IApiClient apiClient, ILookupCache lookupCache, IToastService toastService) : INavigationService
{
    public object? CurrentPage { get; private set; }
    public event EventHandler? CurrentPageChanged;

    // Department picker options, shared by every field factory that references a department (was
    // previously a raw free-text "Department ID" numeric field everywhere — an admin had no way to
    // know a department's id without looking it up first). Loaded once, eagerly, the first time
    // NavigateTo runs — by the time an admin actually opens a Create/Edit dialog (a deliberate
    // multi-click action), this cheap single GET has almost always already completed; if it
    // somehow hasn't, FormField.HasOptions gracefully falls back to a plain text box instead of a
    // broken empty dropdown, so there's no failure mode here, only a slower-than-ideal first open.
    private IReadOnlyList<FormFieldOption> _departmentOptions = [];
    private IReadOnlyList<FormFieldOption> _departmentOptionsWithBlank = [new("", "(General / not set)")];
    private bool _departmentOptionsLoadStarted;

    private void EnsureDepartmentOptionsLoading()
    {
        if (_departmentOptionsLoadStarted)
        {
            return;
        }
        _departmentOptionsLoadStarted = true;
        _ = LoadDepartmentOptionsAsync();
    }

    private async Task LoadDepartmentOptionsAsync()
    {
        try
        {
            var departments = await apiClient.GetDepartmentsAsync();
            _departmentOptions = departments.Select(d => new FormFieldOption(d.Id.ToString(), d.Name)).ToList();
            _departmentOptionsWithBlank = [new("", "(General / not set)"), .. _departmentOptions];
        }
        catch (ApiRequestException)
        {
            // Leave the lists as they were (possibly still empty) — see this field's own remarks.
        }
    }

    public void NavigateTo(string pageKey)
    {
        EnsureDepartmentOptionsLoading();
        CurrentPage = pageKey switch
        {
            "Dashboard" => new DashboardViewModel(apiClient, serviceProvider.GetRequiredService<Application.Auth.IAuthSessionService>()),
            "Settings" => serviceProvider.GetRequiredService<SettingsViewModel>(),
            "Help & Docs" => serviceProvider.GetRequiredService<HelpViewModel>(),
            "Website Content" => BuildWebsiteContentTabs(),
            "College Structure" => BuildCollegeStructureTabs(),
            "Administration" => serviceProvider.GetRequiredService<AdminsListViewModel>(),
            "Timetable" => new TimetableViewModel(BuildTimetableList(), apiClient, lookupCache),
            "Reports & Exports" => serviceProvider.GetRequiredService<BackgroundJobsViewModel>(),
            "Audit Log" => new AuditLogViewModel(apiClient),
            "Institution Overview" => new PrincipalOverviewViewModel(apiClient),
            "Academic Calendar" => BuildAcademicCalendarViewModel(),
            "Principal's Message" => new PrincipalSectionViewModel(apiClient),
            "Certifications" => new CertificationsViewModel(apiClient),
            "Submissions" => new SubmissionsViewModel(apiClient),
            "Blogs" => new BlogsViewModel(apiClient),
            "PYQ" => new PyqsViewModel(apiClient),
            "Academics" => BuildAcademicsTabs(),
            "Preferences" => BuildPreferencesTabs(),
            _ => CreatePlaceholder(pageKey),
        };
        CurrentPageChanged?.Invoke(this, EventArgs.Empty);
    }

    private TabbedListViewModel BuildWebsiteContentTabs()
    {
        GenericListViewModel? noticesTab = null;
        noticesTab = new GenericListViewModel(
            "Notices",
            "Notices you create will appear here.",
            async ct => (await apiClient.GetNoticesAsync(ct)).Cast<object>().ToList(),
            onAdd: () => OpenNoticeCreateDialog(noticesTab!),
            addLabel: "New Notice",
            onEdit: row => OpenNoticeEditDialogAsync(noticesTab!, row),
            onDelete: row => DeleteGenericRowAsync("notices", noticesTab!, row),
            onBulkDelete: rows => BulkDeleteGenericRowsAsync("notices", noticesTab!, rows),
            onExported: count => RecordExportAndToastAsync("notices", count));

        return new TabbedListViewModel(
        [
            noticesTab,
            SimpleModule("News", "News articles you publish will appear here.",
                async ct => (await apiClient.GetNewsAsync(ct)).Cast<object>().ToList(), "news", NewsFields, "New Article"),
            SimpleModule("Banners", "Homepage banners will appear here.",
                async ct => (await apiClient.GetBannersAsync(ct)).Cast<object>().ToList(), "banners", BannerFields, "New Banner"),
            SimpleModule("FAQ", "Frequently asked questions will appear here.",
                async ct => (await apiClient.GetFaqsAsync(ct)).Cast<object>().ToList(), "faq", FaqFields, "New FAQ"),
            SimpleModule("Documents", "IQAC/NAAC/NIRF documents will appear here.",
                async ct => (await apiClient.GetDocumentsAsync(ct)).Cast<object>().ToList(), "documents", DocumentFields, "New Document"),
            SimpleModule("Events", "Events you schedule will appear here.",
                async ct => (await apiClient.GetEventsAsync(ct)).Cast<object>().ToList(), "events", EventFields, "New Event"),
            BuildGalleryTab(),
        ]);
    }

    /// <summary>Gallery albums use SimpleModule's usual Create/Edit/Delete like every other flat
    /// module, plus one thing none of the others need: a nested "images" resource per album, via
    /// GenericListViewModel's third row action (onManage) rather than SimpleModule's shared helper.</summary>
    private GenericListViewModel BuildGalleryTab()
    {
        GenericListViewModel? tab = null;
        tab = new GenericListViewModel(
            "Gallery",
            "Photo albums will appear here.",
            async ct => (await apiClient.GetGalleryAlbumsAsync(ct)).Cast<object>().ToList(),
            onAdd: () => OpenGenericCreateDialog("New Album", GalleryFields(), "gallery", tab!),
            addLabel: "New Album",
            onEdit: row => OpenGenericEditDialogAsync("Gallery", GalleryFields, "gallery", tab!, row),
            onDelete: row => DeleteGenericRowAsync("gallery", tab!, row),
            onBulkDelete: rows => BulkDeleteGenericRowsAsync("gallery", tab!, rows),
            onManage: OpenGalleryImageManagerAsync,
            manageLabel: "Images",
            onExported: count => RecordExportAndToastAsync("gallery", count));
        return tab;
    }

    private Task OpenGalleryImageManagerAsync(object row)
    {
        var albumId = GetRowId(row);
        var albumTitle = (string?)row.GetType().GetProperty("Title")?.GetValue(row) ?? "Album";
        var viewModel = new GalleryImageManagerViewModel(apiClient, albumId, albumTitle);
        var window = new GalleryImageManagerWindow(viewModel);
        window.ShowDialog();
        return Task.CompletedTask;
    }

    // Phase 2 (docs/claude/PHASE2_EXECUTION_PLAN.md) — CLAUDE.md frames Student Management and
    // admissions as "tightly related", so they share a nav group and tab strip rather than getting
    // separate top-level entries.
    private TabbedListViewModel BuildAcademicsTabs() => new(
    [
        BuildStudentsTab(),
        SimpleModule("Admission Links", "PDF/link admission resources will appear here.",
            async ct => (await apiClient.GetAdmissionLinksAsync(ct)).Cast<object>().ToList(), "admission-links", AdmissionLinkFields, "New Link"),
        SimpleModule("Admission Banners", "Semester-pair admission banners will appear here.",
            async ct => (await apiClient.GetAdmissionBannersAsync(ct)).Cast<object>().ToList(), "admission-banners", AdmissionBannerFields, "New Banner"),
    ]);

    /// <summary>
    /// Students gets its own dedicated Create/Edit window (StudentEditViewModel), not SimpleModule's
    /// generic FormField dialog — docs/claude/phase2/03_STUDENT_MANAGER_UI.md is explicit that the
    /// flat single-list-of-fields shape is wrong for this module (no Draft/Finalize states, no
    /// sectioning, no soft duplicate-candidate handling). Edit re-fetches the full record via
    /// GetStudentsAsync's list row cast to StudentDto directly — the list already carries every core
    /// field this form edits, so no extra round-trip is needed the way GetGenericAsync's flat
    /// dictionary was for the generic modules.
    /// </summary>
    private GenericListViewModel BuildStudentsTab()
    {
        GenericListViewModel? tab = null;
        tab = new GenericListViewModel(
            "Students",
            "Students you add will appear here.",
            async ct => (await apiClient.GetStudentsAsync(ct)).Cast<object>().ToList(),
            onAdd: () => OpenStudentEditWindow(tab!, existing: null),
            addLabel: "New Student",
            onEdit: row => { OpenStudentEditWindow(tab!, existing: (Contracts.Api.StudentDto)row); return Task.CompletedTask; },
            onExported: count => RecordExportAndToastAsync("students", count),
            onImport: () => OpenStudentImportWindow(tab!),
            importLabel: "Import Excel");
        return tab;
    }

    private async void OpenStudentEditWindow(GenericListViewModel listTab, Contracts.Api.StudentDto? existing)
    {
        // Final Convergence Phase P1-19: reuses the same GetProgrammeOptionsAsync() helper
        // Preference Windows/Timetable/RR export already use — one Programme-lookup path, not a
        // second one duplicated for Students.
        var programmeOptions = await GetProgrammeOptionsAsync(includeBlankOption: true, blankLabel: "(Not set)");
        var viewModel = new StudentEditViewModel(apiClient, existing, programmeOptions);
        var window = new StudentEditWindow(viewModel);
        if (window.ShowDialog() == true)
        {
            _ = listTab.RefreshAsync();
        }
    }

    /// <summary>Not gated on ShowDialog()'s result the way the other dialogs are — a confirmed
    /// import doesn't close the window itself (the admin may keep reviewing/exporting from it), so
    /// the list is refreshed unconditionally once the window closes, on the assumption a confirm may
    /// have happened. A no-op refresh on an untouched/cancelled import is harmless.</summary>
    private void OpenStudentImportWindow(GenericListViewModel listTab)
    {
        var viewModel = new StudentImportViewModel(apiClient);
        var window = new StudentImportWindow(viewModel);
        window.ShowDialog();
        _ = listTab.RefreshAsync();
    }

    // Phase 2 Slice 3 (docs/claude/phase2/06_PREFERENCE_SYSTEM.md) — Windows reuses SimpleModule's
    // plain list/create/edit shape (its create/edit request-response shape happens to match
    // SimpleContentController's exactly, even though PreferenceWindowsController isn't built on that
    // base class — it has real logic beyond flat CRUD) plus a third row action ("Monitor") opening
    // the dedicated Open/Close/Summary/Submissions detail window, the same way Gallery's "Manage"
    // opens its image manager. No Delete action — the controller has no destroy() endpoint by design
    // (a window with real submissions should never simply vanish).
    private TabbedListViewModel BuildPreferencesTabs()
    {
        GenericListViewModel? windowsTab = null;
        windowsTab = new GenericListViewModel(
            "Preference Windows",
            "Preference windows you create will appear here.",
            async ct => (await apiClient.GetPreferenceWindowsAsync(ct)).Cast<object>().ToList(),
            onAdd: () => OpenPreferenceWindowCreateDialogAsync(windowsTab!),
            addLabel: "New Window",
            onManage: row => OpenPreferenceWindowDetail(windowsTab!, (Contracts.Api.PreferenceWindowDto) row),
            manageLabel: "Monitor",
            // Reuses the Import toolbar slot Slice 2 built for Excel Import — same "second, opt-in
            // toolbar action" shape, just relabeled for this screen (Student Management -> Reports/
            // Exports -> University RR, per docs/claude/phase2/University_RR_Export_Claude_Addendum.md).
            onImport: OpenUniversityRrExportWindow,
            importLabel: "University RR");

        return new TabbedListViewModel(
        [
            windowsTab,
            SimpleModule("Subjects", "Subjects you add will appear here.",
                null, "subjects", SubjectFields, "New Subject",
                archivableLoader: async (archived, ct) => (await apiClient.GetSubjectsAsync(ct, archived)).Cast<object>().ToList()),
        ]);
    }

    private void OpenUniversityRrExportWindow()
    {
        var viewModel = new UniversityRrExportViewModel(apiClient, lookupCache);
        new UniversityRrExportWindow(viewModel).ShowDialog();
    }

    private Task OpenPreferenceWindowDetail(GenericListViewModel listTab, Contracts.Api.PreferenceWindowDto window)
    {
        var viewModel = new PreferenceWindowDetailViewModel(apiClient, window);
        var detailWindow = new PreferenceWindowDetailWindow(viewModel);
        detailWindow.ShowDialog();
        _ = listTab.RefreshAsync(); // status may have changed (opened/closed)
        return Task.CompletedTask;
    }

    /// <summary>Final Convergence Phase P0-4: fetches the programme picker's options right before
    /// the dialog opens (this module's list is small; no caching layer exists yet — that's a
    /// separate, later tracker item — so a fresh fetch each time is the simplest correct choice).</summary>
    private async void OpenPreferenceWindowCreateDialogAsync(GenericListViewModel listTab)
    {
        var programmeOptions = await GetProgrammeOptionsAsync(includeBlankOption: true, blankLabel: "(All programmes)");
        OpenGenericCreateDialog("New Preference Window", PreferenceWindowFields(programmeOptions), "preference-windows", listTab);
    }

    private async Task<IReadOnlyList<FormFieldOption>> GetProgrammeOptionsAsync(bool includeBlankOption = false, string blankLabel = "")
    {
        var programmes = await lookupCache.GetOrFetchAsync("programmes", ct => apiClient.GetProgrammesAsync(ct));
        var options = new List<FormFieldOption>();
        if (includeBlankOption)
        {
            options.Add(new FormFieldOption("", blankLabel));
        }
        options.AddRange(programmes.Select(p => new FormFieldOption(p.Id.ToString(), $"{p.Name} ({p.DepartmentName})")));
        return options;
    }

    // Small fixed-vocabulary dropdowns shared by every field factory below — no async fetch needed,
    // these are the exact same strings the corresponding PHP controller's validate() already
    // enforces. Previously every one of these was a free-text box the admin had to type correctly
    // by hand (e.g. "published", "draft") with no hint of the valid values.
    private static readonly IReadOnlyList<FormFieldOption> PublishedDraftOptions =
        [new("published", "Published"), new("draft", "Draft")];

    private static readonly IReadOnlyList<FormFieldOption> SubjectTypeOptions =
        [new("Minor", "Minor"), new("Skill", "Skill"), new("MDC", "MDC"), new("AECC", "AECC"), new("VAC", "VAC")];

    private static readonly IReadOnlyList<FormFieldOption> CalendarEventTypeOptions =
    [
        new("holiday", "Holiday"), new("exam", "Exam"), new("result", "Result"), new("admission", "Admission"),
        new("academic", "Academic"), new("sports", "Sports"), new("cultural", "Cultural"),
        new("administrative", "Administrative"), new("other", "Other"),
    ];

    private static IEnumerable<FormField> PreferenceWindowFields(IReadOnlyList<FormFieldOption> programmeOptions) =>
    [
        new("academicSession", "Academic Session (e.g. 2026-27)", isRequired: true),
        new("semester", "Semester", isRequired: true),
        new("programmeId", "Programme (blank = applies to every programme)", options: programmeOptions),
        new("subjectType", "Subject Type", isRequired: true, options: SubjectTypeOptions),
        new("minChoices", "Minimum Choices (default 1)"),
        new("maxChoices", "Maximum Choices (default 5)"),
    ];

    private IEnumerable<FormField> SubjectFields() =>
    [
        new("subjectType", "Subject Type", isRequired: true, options: SubjectTypeOptions),
        new("code", "Code", isRequired: true),
        new("name", "Name", isRequired: true),
        new("departmentId", "Department (optional)", options: _departmentOptions),
        new("quota", "Quota (blank = uncapped)"),
    ];

    private TabbedListViewModel BuildCollegeStructureTabs() => new(
    [
        SimpleModule("Departments", "Departments you create will appear here.",
            null, "departments", DepartmentFields, "New Department",
            archivableLoader: async (archived, ct) => (await apiClient.GetDepartmentsAsync(ct, archived)).Cast<object>().ToList()),
        SimpleModule("Faculty", "Faculty profiles will appear here.",
            null, "faculty", FacultyFields, "New Faculty",
            archivableLoader: async (archived, ct) => (await apiClient.GetFacultyAsync(ct, archived)).Cast<object>().ToList()),
        SimpleModule("Committees", "Committees you create will appear here.",
            null, "committees", CommitteeFields, "New Committee",
            archivableLoader: async (archived, ct) => (await apiClient.GetCommitteesAsync(ct, archived)).Cast<object>().ToList()),
        // Final Convergence Phase P0-4: Programmes/Courses master data — previously manageable only
        // through the legacy web admin panel despite being a hard FK dependency of Students,
        // Timetable, Preference Windows, Allotments, and RR export. Final Convergence Phase P2 item
        // 1: only the common (non-archived) fetch goes through the lookup cache — the archived view
        // is a rare, deliberate admin action, not a hot path worth caching, and caching it under the
        // same "programmes" key would corrupt the non-archived cache entry.
        SimpleModule("Programmes", "Programmes/courses you create will appear here.",
            null, "programmes", ProgrammeFields, "New Programme",
            archivableLoader: async (archived, ct) => archived
                ? (await apiClient.GetProgrammesAsync(ct, archived: true)).Cast<object>().ToList()
                : (await lookupCache.GetOrFetchAsync("programmes", c => apiClient.GetProgrammesAsync(c), ct)).Cast<object>().ToList(),
            onManageFactory: tab => OpenProgrammePapersManagerAsync, manageLabel: "Papers"),
    ]);

    /// <summary>Final Convergence Phase P1-20: a paper only ever makes sense in the context of one
    /// specific programme — same nested-resource shape as OpenGalleryImageManagerAsync.</summary>
    private Task OpenProgrammePapersManagerAsync(object row)
    {
        var programmeId = GetRowId(row);
        var programmeName = (string?)row.GetType().GetProperty("Name")?.GetValue(row) ?? "Programme";
        var viewModel = new ProgrammePapersManagerViewModel(apiClient, programmeId, programmeName);
        var window = new ProgrammePapersManagerWindow(viewModel);
        window.ShowDialog();
        return Task.CompletedTask;
    }

    /// <summary>Final Convergence Phase P1-10/§2b: the same deferred-closure trick as SimpleModule's
    /// onManage — `viewModel` doesn't exist yet while the loader closure below is created (it's the
    /// AcademicCalendarViewModel wrapping THIS list), but the closure only reads
    /// `viewModel?.AcademicYearFilter` when actually invoked later, by which point it's assigned.
    /// The very first load (before AcademicCalendarViewModel's own constructor sets the filter from
    /// the server's current-academic-year default) passes null, which the server itself treats as
    /// "use the current academic year" — a reasonable default, not an error state.</summary>
    private AcademicCalendarViewModel BuildAcademicCalendarViewModel()
    {
        AcademicCalendarViewModel? viewModel = null;
        GenericListViewModel? calendarTab = null;
        calendarTab = new GenericListViewModel(
            "Academic Calendar",
            "Calendar events you create will appear here.",
            async ct => (await apiClient.GetCalendarEventsAsync(viewModel?.AcademicYearFilter, ct)).Events.Cast<object>().ToList(),
            onAdd: () => OpenGenericCreateDialog("New Calendar Event", CalendarFields(), "academic-calendar", calendarTab!),
            addLabel: "New Event",
            onEdit: row => OpenGenericEditDialogAsync("Academic Calendar", CalendarFields, "academic-calendar", calendarTab!, row),
            onDelete: row => DeleteGenericRowAsync("academic-calendar", calendarTab!, row),
            onBulkDelete: rows => BulkDeleteGenericRowsAsync("academic-calendar", calendarTab!, rows),
            onExported: count => RecordExportAndToastAsync("academic-calendar", count));
        viewModel = new AcademicCalendarViewModel(calendarTab, apiClient);
        return viewModel;
    }

    private static IEnumerable<FormField> CalendarFields() =>
    [
        new("title", "Title", isRequired: true),
        new("description", "Description", isMultiline: true),
        new("eventType", "Type", isRequired: true, options: CalendarEventTypeOptions),
        new("startDate", "Start Date (YYYY-MM-DD)", isRequired: true),
        new("endDate", "End Date (YYYY-MM-DD)", isRequired: true),
        new("academicYear", "Academic Year (e.g. 2026-27)", isRequired: true),
        new("linkedEventId", "Linked Event ID (optional)"),
        new("status", "Status", isRequired: true, options: PublishedDraftOptions),
    ];

    private GenericListViewModel BuildTimetableList()
    {
        GenericListViewModel? timetableTab = null;
        timetableTab = new GenericListViewModel(
            "Timetable",
            "Timetable entries you create will appear here.",
            async ct => (await apiClient.GetTimetableEntriesAsync(ct)).Cast<object>().ToList(),
            onAdd: () => _ = OpenTimetableEntryCreateDialogAsync(timetableTab!),
            addLabel: "New Entry",
            onEdit: row => OpenTimetableEntryEditDialogAsync(timetableTab!, row),
            onDelete: row => DeleteGenericRowAsync("timetable", timetableTab!, row),
            onBulkDelete: rows => BulkDeleteGenericRowsAsync("timetable", timetableTab!, rows),
            onManage: row => PublishTimetableEntryAsync(timetableTab!, row),
            manageLabel: "Publish/Unpublish",
            onExported: count => RecordExportAndToastAsync("timetable", count));
        return timetableTab;
    }

    // ---- Dedicated dialogs (real logic beyond "post these fields") ----

    /// <summary>No Owner is set on any dialog in this file (WPF's default MainWindow heuristic is
    /// unreliable across the login->shell transition this app does) — dialogs just center on
    /// screen instead.</summary>
    private void OpenNoticeCreateDialog(GenericListViewModel noticesTab)
    {
        var createViewModel = serviceProvider.GetRequiredService<NoticeCreateViewModel>();
        var window = new NoticeCreateWindow(createViewModel);
        if (window.ShowDialog() == true)
        {
            _ = noticesTab.RefreshAsync();
        }
    }

    private async Task OpenTimetableEntryCreateDialogAsync(GenericListViewModel timetableTab)
    {
        var createViewModel = serviceProvider.GetRequiredService<TimetableEntryCreateViewModel>();
        await createViewModel.LoadProgrammeOptionsAsync();
        var window = new TimetableEntryCreateWindow(createViewModel);
        if (window.ShowDialog() == true)
        {
            _ = timetableTab.RefreshAsync();
        }
    }

    private async Task OpenNoticeEditDialogAsync(GenericListViewModel noticesTab, object row)
    {
        var id = GetRowId(row);
        IReadOnlyDictionary<string, string?> current;
        try
        {
            current = await apiClient.GetGenericAsync("notices", id);
        }
        catch (ApiRequestException ex)
        {
            MessageBox.Show($"Could not load this notice: {ex.Message}", "Edit", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var editViewModel = serviceProvider.GetRequiredService<NoticeCreateViewModel>();
        editViewModel.InitializeForEdit(id, current);
        var window = new NoticeCreateWindow(editViewModel);
        if (window.ShowDialog() == true)
        {
            _ = noticesTab.RefreshAsync();
        }
    }

    private async Task OpenTimetableEntryEditDialogAsync(GenericListViewModel timetableTab, object row)
    {
        var id = GetRowId(row);
        IReadOnlyDictionary<string, string?> current;
        try
        {
            current = await apiClient.GetGenericAsync("timetable", id);
        }
        catch (ApiRequestException ex)
        {
            MessageBox.Show($"Could not load this entry: {ex.Message}", "Edit", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var editViewModel = serviceProvider.GetRequiredService<TimetableEntryCreateViewModel>();
        await editViewModel.LoadProgrammeOptionsAsync();
        editViewModel.InitializeForEdit(id, current);
        var window = new TimetableEntryCreateWindow(editViewModel);
        if (window.ShowDialog() == true)
        {
            _ = timetableTab.RefreshAsync();
        }
    }

    // ---- Generic "flat fields" modules ----

    private GenericListViewModel SimpleModule(
        string title,
        string emptyDescription,
        Func<CancellationToken, Task<IReadOnlyList<object>>>? loader,
        string endpoint,
        Func<IEnumerable<FormField>> fieldsFactory,
        string addLabel,
        Func<GenericListViewModel, Func<object, Task>>? onManageFactory = null,
        string manageLabel = "Manage",
        // Final Convergence Phase P2 item 1 (docs/claude/FINAL_COMPLETION_TRACKER.md §4): exactly
        // one of `loader`/`archivableLoader` is supplied by every call site. archivableLoader takes
        // (archived, ct) so it can honor the tab's own ShowArchived toggle, using the same
        // deferred-closure trick onManageFactory already needs (tab?.ShowArchived is null, not the
        // still-null tab itself, on the very first synchronous load during construction —
        // null-coalesces to false, which is the correct default anyway). Non-null also flips
        // isArchivable/onRestore on for this module.
        Func<bool, CancellationToken, Task<IReadOnlyList<object>>>? archivableLoader = null)
    {
        GenericListViewModel? tab = null;
        tab = new GenericListViewModel(
            title,
            emptyDescription,
            archivableLoader is null ? loader! : (ct => archivableLoader(tab?.ShowArchived ?? false, ct)),
            onAdd: () => OpenGenericCreateDialog(addLabel, fieldsFactory(), endpoint, tab!),
            addLabel: addLabel,
            onEdit: row => OpenGenericEditDialogAsync(title, fieldsFactory, endpoint, tab!, row),
            onDelete: row => DeleteGenericRowAsync(endpoint, tab!, row),
            onBulkDelete: rows => BulkDeleteGenericRowsAsync(endpoint, tab!, rows),
            // Final Convergence Phase P1-20: a factory, not a plain delegate, because it needs
            // `tab` itself (to refresh the list after the manage dialog closes) — the same reason
            // BuildGalleryTab() couldn't just pass a closure captured before `tab` exists. Wrapped
            // in an outer lambda so `onManageFactory(tab!)` isn't evaluated until Manage is actually
            // clicked — evaluating it eagerly here, before this very assignment completes, would
            // capture `tab` as still-null. Optional and null for every module except Programmes today.
            onManage: onManageFactory is null ? null : row => onManageFactory(tab!)(row),
            manageLabel: manageLabel,
            onExported: count => RecordExportAndToastAsync(endpoint, count),
            isArchivable: archivableLoader is not null,
            onRestore: archivableLoader is null ? null : row => RestoreGenericRowAsync(endpoint, tab!, row));
        return tab;
    }

    /// <summary>Final Convergence Phase P1-16: a toast for the one GenericListViewModel action that
    /// previously had no feedback at all beyond the CSV file appearing on disk.</summary>
    private Task RecordExportAndToastAsync(string endpoint, int count)
    {
        toastService.Show($"Exported {count} row{(count == 1 ? "" : "s")}.", ToastSeverity.Success);
        return apiClient.RecordExportAsync(endpoint, count);
    }

    /// <summary>Final Convergence Phase P1-17: the only endpoint currently cached — see this class's
    /// own doc comment and 18_DEVLOG.md Entry 49 for why Departments/Subjects/capability-bootstrap
    /// aren't wired in yet. A single explicit check rather than a generic endpoint-to-cache-key
    /// registry, since there is exactly one entry — extend this, not invent a registry, if/when a
    /// second lookup gets cached.</summary>
    private void InvalidateLookupCacheFor(string endpoint)
    {
        if (endpoint == "programmes")
        {
            lookupCache.Invalidate("programmes");
        }
    }

    private void OpenGenericCreateDialog(string dialogTitle, IEnumerable<FormField> fields, string endpoint, GenericListViewModel listTab)
    {
        var createViewModel = new GenericCreateViewModel(dialogTitle, fields, values => apiClient.CreateGenericAsync(endpoint, values), apiClient);
        var window = new GenericCreateWindow(createViewModel);
        if (window.ShowDialog() == true)
        {
            InvalidateLookupCacheFor(endpoint);
            _ = listTab.RefreshAsync();
        }
    }

    /// <summary>Pre-fills the same field set Create uses from a fresh GET of the single record
    /// (not from the list row DTO, which only carries a handful of columns for display) — see
    /// IApiClient.GetGenericAsync for why. Submits through UpdateGenericAsync instead of Create.</summary>
    private async Task OpenGenericEditDialogAsync(
        string moduleTitle, Func<IEnumerable<FormField>> fieldsFactory, string endpoint, GenericListViewModel listTab, object row)
    {
        var id = GetRowId(row);
        IReadOnlyDictionary<string, string?> current;
        try
        {
            current = await apiClient.GetGenericAsync(endpoint, id);
        }
        catch (ApiRequestException ex)
        {
            MessageBox.Show($"Could not load this record: {ex.Message}", "Edit", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var fields = fieldsFactory().ToList();
        foreach (var field in fields)
        {
            if (current.TryGetValue(field.Key, out var value) && value is not null)
            {
                field.Value = value;
            }
        }

        var editViewModel = new GenericCreateViewModel($"Edit {moduleTitle}", fields, values => apiClient.UpdateGenericAsync(endpoint, id, values), apiClient);
        var window = new GenericCreateWindow(editViewModel);
        if (window.ShowDialog() == true)
        {
            InvalidateLookupCacheFor(endpoint);
            _ = listTab.RefreshAsync();
        }
    }

    private async Task DeleteGenericRowAsync(string endpoint, GenericListViewModel listTab, object row)
    {
        try
        {
            await apiClient.DeleteGenericAsync(endpoint, GetRowId(row));
            InvalidateLookupCacheFor(endpoint);
            await listTab.RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            MessageBox.Show($"Could not delete this record: {ex.Message}", "Delete", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Final Convergence Phase P1-18: unlike DeleteGenericRowAsync (which the single-row
    /// Delete button still uses unchanged), this deletes every selected row first and refreshes the
    /// list exactly once afterward — looping the single-delete path per row would mean N server
    /// refreshes for one bulk action.</summary>
    private async Task BulkDeleteGenericRowsAsync(string endpoint, GenericListViewModel listTab, IReadOnlyList<object> rows)
    {
        foreach (var row in rows)
        {
            try
            {
                await apiClient.DeleteGenericAsync(endpoint, GetRowId(row));
            }
            catch (ApiRequestException ex)
            {
                MessageBox.Show($"Could not delete one of the selected records: {ex.Message}", "Bulk Delete", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        InvalidateLookupCacheFor(endpoint);
        await listTab.RefreshAsync();
    }

    /// <summary>Final Convergence Phase P2 item 1: no confirmation needed (see GenericListViewModel's
    /// own RestoreAsync remarks) — restoring is safe and non-destructive.</summary>
    private async Task RestoreGenericRowAsync(string endpoint, GenericListViewModel listTab, object row)
    {
        try
        {
            await apiClient.RestoreGenericAsync(endpoint, GetRowId(row));
            InvalidateLookupCacheFor(endpoint);
            await listTab.RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            MessageBox.Show($"Could not restore this record: {ex.Message}", "Restore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Final Convergence Phase P0-5: the backend publish endpoint toggles status itself
    /// (draft-&gt;published or back), so there's nothing to choose here beyond which row.</summary>
    private async Task PublishTimetableEntryAsync(GenericListViewModel timetableTab, object row)
    {
        try
        {
            await apiClient.PublishTimetableEntryAsync(GetRowId(row));
            await timetableTab.RefreshAsync();
        }
        catch (ApiRequestException ex)
        {
            MessageBox.Show($"Could not publish/unpublish this entry: {ex.Message}", "Publish", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Every row DTO across every generic module exposes an int Id — reflection here
    /// avoids needing a shared interface just for this one property on records that otherwise have
    /// nothing else in common.</summary>
    private static int GetRowId(object row) =>
        (int)(row.GetType().GetProperty("Id")?.GetValue(row)
              ?? throw new InvalidOperationException($"{row.GetType().Name} has no Id property."));

    // Field lists mirror each controller's fillable() + validate() — see the corresponding
    // App\Api\Controllers\*Controller.php for the server-side rules these forms defer to.
    private static IEnumerable<FormField> NewsFields() =>
    [
        new("title", "Title", isRequired: true),
        new("slug", "Slug", isRequired: true),
        new("excerpt", "Excerpt"),
        new("content", "Content", isMultiline: true),
        // Final Convergence Phase P0-7: was entirely absent from this form despite `image` being a
        // real, publicly-rendered column (news.image) — a desktop-created article could never carry
        // one until now.
        new("image", "Image", uploadModule: "news"),
        // Final Convergence Phase P1-8: news.published_at is real and fillable
        // (NewsController::fillable()) but had no desktop field at all.
        new("publishedAt", "Published At (YYYY-MM-DD HH:MM:SS)"),
        new("status", "Status", options: PublishedDraftOptions),
    ];

    private static IEnumerable<FormField> BannerFields() =>
    [
        new("title", "Title"),
        new("subtitle", "Subtitle"),
        new("mediaPath", "Image", isRequired: true, uploadModule: "banners"),
        new("mediaType", "Media Type (image/video)"),
        new("link", "Link"),
        new("sortOrder", "Sort Order"),
        new("status", "Status", options: PublishedDraftOptions),
    ];

    private static IEnumerable<FormField> FaqFields() =>
    [
        new("question", "Question", isRequired: true),
        new("answer", "Answer", isRequired: true, isMultiline: true),
        // Final Convergence Phase P1-8 (docs/claude/FINAL_COMPLETION_TRACKER.md §3): faqs.sort_order
        // and faqs.status are real, already-fillable columns (FaqController::fillable()) that had no
        // desktop field at all — every desktop-created FAQ silently got NULL status and default
        // ordering with no way to change either.
        new("sortOrder", "Sort Order"),
        new("status", "Status", options: PublishedDraftOptions),
    ];

    private static IEnumerable<FormField> DocumentFields() =>
    [
        new("type", "Type (iqac/naac/nirf)", isRequired: true),
        new("title", "Title", isRequired: true),
        new("category", "Category"),
        // Final Convergence Phase P0-7: was a raw free-text path with no upload mechanism at all.
        new("filePath", "File", isRequired: true, uploadModule: "documents"),
        // Final Convergence Phase (Section 28's own LEGACY CONFLICT finding): the public site's
        // IQAC/NAAC/NIRF pages filter/sort by academic_year, but this field never existed on the
        // desktop form — every desktop-created document silently broke that public filter until now.
        new("academicYear", "Academic Year (e.g. 2026-27)"),
    ];

    private IEnumerable<FormField> EventFields() =>
    [
        new("title", "Title", isRequired: true),
        new("slug", "Slug", isRequired: true),
        new("eventDate", "Event Date (YYYY-MM-DD HH:MM:SS)", isRequired: true),
        new("location", "Location"),
        new("description", "Description", isMultiline: true),
        // Final Convergence Phase P0-7: events.image is real and publicly rendered but had no
        // desktop field at all.
        new("image", "Image", uploadModule: "events"),
        new("departmentId", "Department (blank = general/institution-wide)", options: _departmentOptionsWithBlank),
    ];

    private IEnumerable<FormField> GalleryFields() =>
    [
        new("title", "Title", isRequired: true),
        new("slug", "Slug", isRequired: true),
        new("coverImage", "Cover Image", uploadModule: "gallery"),
        new("departmentId", "Department (blank = general/institution-wide)", options: _departmentOptionsWithBlank),
        new("status", "Status", options: PublishedDraftOptions),
    ];

    private static IEnumerable<FormField> DepartmentFields() =>
    [
        new("name", "Name", isRequired: true),
        new("status", "Status", options: PublishedDraftOptions),
        // Final Convergence Phase P1-2: previously unsettable through the new API/desktop at all —
        // changing a department's HOD required the legacy admin panel. Free-text by id, matching
        // this codebase's existing convention for referencing another entity in this class of form
        // (e.g. Subjects'/Faculty's own departmentId fields) — a real picker wasn't specifically
        // requested for this field the way Programme's was.
        new("hodFacultyId", "HOD Faculty ID (optional)"),
    ];

    private IEnumerable<FormField> FacultyFields() =>
    [
        new("name", "Name", isRequired: true),
        new("slug", "Slug", isRequired: true),
        new("designation", "Designation", isRequired: true),
        new("departmentId", "Department (optional)", options: _departmentOptionsWithBlank),
        // P2 item 5: links this row to another Faculty row (a separate row per department, same
        // person) — legacy's own multi-department mechanism, now ported to the new API/desktop.
        // Free-text ID (no picker: unlike departmentId, this references a specific PERSON, not a
        // small fixed list — a real "pick a faculty member" search box is future scope).
        new("linkedFacultyId", "Linked Faculty ID (optional — same person, other department)"),
        new("email", "Email"),
    ];

    private static IEnumerable<FormField> CommitteeFields() =>
    [
        new("name", "Name", isRequired: true),
        new("slug", "Slug", isRequired: true),
        new("academicYear", "Academic Year"),
        // Final Convergence Phase P1-13: the explicit flag that replaces the legacy web panel's
        // fragile name/slug LIKE '%admission%' authorization match — see
        // Committee::isConvenorOfAdmissionsCommittee() and docs/claude/FINAL_COMPLETION_TRACKER.md §2.
        new("isAdmissionsCommittee", "Is Admissions Committee", isCheckbox: true),
    ];

    private static readonly IReadOnlyList<FormFieldOption> ProgrammeLevelOptions =
    [
        new("UG", "Under-Graduate (UG)"), new("PG", "Post-Graduate (PG)"), new("Certificate", "Certificate"),
        new("Diploma", "Diploma"), new("PhD", "Ph.D / Doctoral"),
    ];

    private IEnumerable<FormField> ProgrammeFields() =>
    [
        new("departmentId", "Department", isRequired: true, options: _departmentOptions),
        new("name", "Name (e.g. BCA)", isRequired: true),
        new("fullName", "Full Name (e.g. Bachelor of Computer Applications)", isRequired: true),
        new("level", "Level", isRequired: true, options: ProgrammeLevelOptions),
        new("durationYears", "Duration (Years)", isRequired: true),
        new("durationSemesters", "Duration (Semesters)", isRequired: true),
        new("seats", "Seats (optional)"),
        new("annualFee", "Annual Fee (optional)"),
        new("eligibility", "Eligibility (optional)", isMultiline: true),
        new("description", "Description (optional)", isMultiline: true),
        new("status", "Status", options: PublishedDraftOptions),
    ];

    private static IEnumerable<FormField> AdmissionLinkFields() =>
    [
        new("title", "Title", isRequired: true),
        new("url", "URL"),
        new("filePath", "File Path"),
        new("status", "Status", options: PublishedDraftOptions),
        new("sortOrder", "Sort Order"),
    ];

    private static IEnumerable<FormField> AdmissionBannerFields() =>
    [
        new("pairKey", "Pair Key (e.g. 1-2)", isRequired: true),
        new("label", "Label (e.g. 1st & 2nd Semester)", isRequired: true),
        new("sortOrder", "Sort Order"),
        new("enabled", "Enabled (true/false)"),
    ];

    private PlaceholderPageViewModel CreatePlaceholder(string pageKey)
    {
        var page = serviceProvider.GetRequiredService<PlaceholderPageViewModel>();
        page.SetTitle(pageKey);
        return page;
    }
}
