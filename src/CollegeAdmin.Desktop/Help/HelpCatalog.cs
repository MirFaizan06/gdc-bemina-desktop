namespace CollegeAdmin.Desktop.Help;

/// <summary>The fixed content behind the in-app Help &amp; Docs page. Role ids match
/// AdminProfile.Role exactly (see gdc-bemina-website's UserController role validator: super_admin,
/// editor, dept_admin, committee_manager, principal) so "your role" defaulting in HelpViewModel is a
/// direct string comparison, no translation table needed. Category order here IS the display order
/// (HelpViewModel groups topics by first-encounter order) — keep related topics adjacent.</summary>
public static class HelpCatalog
{
    /// <summary>"general" is a pseudo-role (not a real AdminProfile.Role value) used purely as the
    /// filter id for topics with an empty Roles list.</summary>
    public static readonly IReadOnlyList<(string Id, string Label)> RoleFilters =
    [
        ("general", "General"),
        ("super_admin", "Super Admin"),
        ("editor", "Editor"),
        ("dept_admin", "Department Admin (HOD)"),
        ("committee_manager", "Committee Manager"),
        ("principal", "Principal"),
    ];

    public static readonly IReadOnlyList<HelpTopic> Topics =
    [
        // ───────────────────────────── Getting Started (General) ─────────────────────────────
        new HelpTopic(
            "getting-started", "Welcome to College Admin", "\U0001F44B", "Getting Started", [],
            "What this app is for, and how a typical session flows.",
            [
                "Sign in with your college email and password on the login screen.",
                "The app checks whether you already have a valid session and, if so, takes you straight to your Dashboard.",
                "Your Dashboard greets you by name and time of day, and shows a quick summary of the modules you have access to.",
                "Use the sidebar on the left to move between modules — only the modules your account has access to are shown.",
                "Logout (top-right) always ends your session cleanly, on this PC and on the server.",
            ],
            ["Not sure why a module is missing from your sidebar? See \"Understanding Roles & Permissions\" below."]),

        new HelpTopic(
            "navigating-the-app", "Navigating the App", "\U0001F9ED", "Getting Started", [],
            "How the sidebar, tabs, and page layout fit together.",
            [
                "The sidebar groups related modules — e.g. \"Website Content\" bundles Notices, News, Banners, FAQ, Documents, Events, and Gallery into one screen with tabs.",
                "Click a sidebar item once to open it — the page you were on is replaced, not stacked, so you're never several pages deep.",
                "Inside a tabbed module, click a tab header to switch between its sub-sections without leaving the page.",
                "The top bar always shows your name, a live Online/Offline connectivity indicator, and Logout.",
                "A banner appears at the top of the app when a new version is available — see \"Connectivity & Updates\" below.",
            ]),

        new HelpTopic(
            "lists-search-export", "Working with Lists, Search & Export", "\U0001F4CB", "Getting Started", [],
            "The shared list screen used by almost every module — learn it once, use it everywhere.",
            [
                "Most modules (Departments, Faculty, Notices, Committees, and more) share the exact same list screen, so this pattern applies almost everywhere in the app.",
                "Type in the Search box to filter instantly — it searches across the fields visible in that list, no need to press Enter.",
                "Use Previous / Next at the bottom to page through longer lists.",
                "Select one or more rows, then use the bulk action buttons that appear for the current selection.",
                "Click Export CSV to download the current filtered view as a spreadsheet — only rows matching your current search are exported.",
                "Where supported, tick \"Show Archived\" to reveal archived (soft-deleted) rows and restore them if needed.",
            ],
            [
                "Archiving hides a row from lists and the public site without permanently destroying it — look for \"Restore\" on an archived row.",
                "Delete is permanent only where a module doesn't support archiving — the confirmation dialog always tells you which kind of action you're about to take.",
            ]),

        new HelpTopic(
            "roles-and-permissions", "Understanding Roles & Permissions", "\U0001F510", "Getting Started", [],
            "Why your sidebar looks different from a colleague's, and who can do what.",
            [
                "Every admin account has one role: Super Admin, Editor, Department Admin (HOD), Committee Manager, or Principal.",
                "Your role — combined with specific capability grants — determines exactly which modules and actions you see.",
                "Super Admin accounts automatically have access to everything; every other role sees only the modules it's been explicitly granted.",
                "The sidebar only ever hides modules you can't use — it's a convenience, not the real security boundary. The server independently re-checks every action you take, every time.",
                "If you're missing access to something you need, contact a Super Admin to review your account's capability grants.",
            ],
            ["Use the role filter chips on this page to browse documentation written for other roles too, not just your own."]),

        new HelpTopic(
            "appearance-theme", "Appearance & Theme", "\U0001F3A8", "Getting Started", [],
            "Choosing how the app looks — a setting that lives only on this PC.",
            [
                "The first time the app runs on a PC, a \"Choose Your Theme\" screen offers three looks: Indigo Night, Emerald Fresh, and Slate Professional — each pairs a color palette with a font.",
                "Pick a card and select Continue. Indigo Night is pre-selected as the default if you don't change anything.",
                "To change your theme later, open Settings from the sidebar and use \"Change Theme...\" under Appearance.",
                "After picking a new theme, the app offers to restart immediately so the new look takes effect everywhere.",
                "Your theme choice is saved only on this PC, in a local file — it's never sent to the server and never affects any other user or device.",
            ]),

        new HelpTopic(
            "connectivity-updates", "Connectivity & Updates", "\U0001F4E1", "Getting Started", [],
            "How the app behaves online, offline, and when a new version is available.",
            [
                "The top bar's Online / Offline indicator reflects real, live traffic to the server — not a guess.",
                "If the connection drops, a toast notification appears and the app keeps working with whatever it already has loaded; actions that need the server retry or clearly fail rather than pretend to succeed.",
                "On launch, the app quietly checks for a newer release. If one exists, a banner appears at the top with a \"What's New\" toggle for the release notes.",
                "\"Update Now\" downloads and verifies the new installer (checked against a cryptographic hash) before launching it — the app closes itself once the installer takes over.",
                "You can keep working on your current version by dismissing the banner; it reappears on the next launch until you update.",
            ]),

        new HelpTopic(
            "settings-diagnostics", "Settings & Diagnostics", "⚙️", "Getting Started", [],
            "What's on the Settings page and when you'd use it.",
            [
                "Appearance lets you change your theme (see above).",
                "Connectivity shows whether the app can currently reach the server, and lets you force a fresh check.",
                "Diagnostics shows your exact app version and the server address the app is talking to — useful if you ever need to report a problem.",
                "Credits shows the developer and support contact for this app.",
            ]),

        // ───────────────────────────── Website Content ─────────────────────────────
        new HelpTopic(
            "website-content-basics", "Publishing Website Content", "\U0001F4F0", "Website Content",
            ["editor", "super_admin", "dept_admin", "committee_manager"],
            "Creating and managing Notices, News, Events, Banners, FAQ, and Documents.",
            [
                "Open Website Content from the sidebar — each content type (Notices, News, Banners, FAQ, Documents, Events) is its own tab.",
                "Click Add to open the creation form — required fields are marked, and any field with a small, fixed set of valid values (like Status or Type) uses a dropdown so you can't mistype it.",
                "For a field that needs a file (an image or a document), use Browse... — the file uploads to the server and is validated automatically.",
                "Save publishes immediately unless the content type has a Draft/Published status field — check that field if you want to hold something back before it goes live.",
                "Use Edit on an existing row to update it, or Archive to remove it from the public site without permanently deleting it.",
            ],
            ["Notices support scheduling — set Publish At / Expire At so a notice appears and disappears automatically without you needing to come back."]),

        new HelpTopic(
            "gallery-management", "Managing the Photo Gallery", "\U0001F5BC️", "Website Content",
            ["editor", "super_admin"],
            "Albums and the images inside them.",
            [
                "Open Website Content → Gallery to manage albums.",
                "Create an album, then use its Manage Images action to open the dedicated image manager for that album.",
                "Use Browse... to add a new image — it uploads and validates automatically, the same as any other file field in the app.",
                "Reorder or remove images directly from the image manager; changes reflect on the public gallery immediately.",
            ]),

        // ───────────────────────────── College Structure ─────────────────────────────
        new HelpTopic(
            "college-structure", "Managing Departments & Faculty", "\U0001F3E2", "College Structure",
            ["super_admin", "dept_admin"],
            "Keeping department and faculty records accurate.",
            [
                "Open College Structure → Departments to add or edit a department.",
                "Open College Structure → Faculty to manage individual faculty members — when a faculty member also has an admin account, link the two records so shared fields (phone, bio, CV) stay in sync automatically.",
                "Assign a faculty member to a Department using the dropdown — this feeds every other module that filters or displays faculty by department.",
                "Archive (rather than delete) a faculty member who has left, so their historical association with past timetables/notices isn't lost.",
            ]),

        new HelpTopic(
            "programmes-note", "Programmes & Courses", "\U0001F393", "College Structure",
            ["super_admin"],
            "Why you might not see an option to add a Programme.",
            [
                "Programmes (the academic courses the college offers) are managed under College Structure → Programmes.",
                "Creating, editing, or deleting a Programme is restricted to Super Admin accounts — this is deliberate: programmes are referenced by Students, Timetable, Subject Preferences, and official exports, and an accidental change would ripple widely.",
                "Every other module that needs to reference a Programme (Timetable, Subject Preferences, exports) uses a dropdown populated from this list — you never need to type a Programme ID by hand.",
            ]),

        new HelpTopic(
            "committees-management", "Managing Committees", "\U0001F91D", "Committees",
            ["super_admin", "committee_manager"],
            "Committee membership and structure.",
            [
                "Open Committees from the sidebar to see every committee.",
                "Add a committee, then manage its membership — each member has a role within the committee (e.g. Convenor, Co-Convenor, Member).",
                "Keep membership current — an outdated committee roster is one of the most common reasons a public committee page looks wrong.",
                "Archive a committee that's no longer active rather than deleting it, to preserve its history.",
            ]),

        // ───────────────────────────── Academics & Preferences ─────────────────────────────
        new HelpTopic(
            "academics-students", "Academics: Students & Admissions", "\U0001F9D1‍\U0001F393", "Academics & Preferences",
            ["super_admin", "dept_admin"],
            "Student records and the admissions-related links/banners shown on the public site.",
            [
                "Open Academics → Students to search and manage student records.",
                "Open Academics → Admission Links / Admission Banners to manage the promotional content shown on the public admissions pages.",
                "Student records carry a lifecycle status (e.g. draft, active) — check this field before assuming a record is fully live.",
            ]),

        new HelpTopic(
            "preferences-subjects", "Subject Preferences & Subjects Catalog", "\U0001F4DD", "Academics & Preferences",
            ["super_admin", "dept_admin"],
            "Running subject-choice windows for students.",
            [
                "Open Preferences → Subjects to maintain the catalog of subjects students can choose from.",
                "Open Preferences → Preference Windows to create a new choice window for a semester — set its open/close dates and which subjects are offered.",
                "Once a window closes, use the allotment tools to finalize which students received which subject.",
                "Students see their open windows, submitted choices, and finalized allotments on the public student dashboard automatically — no separate step is needed to publish results.",
            ]),

        new HelpTopic(
            "timetable-management", "Managing the Timetable", "⏰", "Academics & Preferences",
            ["super_admin", "dept_admin"],
            "Building and publishing class schedules.",
            [
                "Open Timetable from the sidebar.",
                "Add an entry for a Programme, Semester, Subject, Day, and time slot — the Programme field is a dropdown, never free text.",
                "The app warns you if a new entry conflicts with an existing one for the same programme/semester/day/time.",
                "A new or edited entry stays a draft until you use Publish on that row — only published entries appear on the public timetable and student dashboard.",
                "Use CSV export to download the current view for printing or sharing offline.",
            ]),

        // ───────────────────────────── Reports & Administration ─────────────────────────────
        new HelpTopic(
            "reports-jobs", "Reports & Background Jobs", "\U0001F4CA", "Reports & Administration",
            ["super_admin"],
            "Monitoring long-running exports and batch operations.",
            [
                "Open Reports & Exports to see every background job the server is running or has recently finished.",
                "Each job shows its current status, progress, and a result/error summary once it completes.",
                "Some exports (like the University RR export) run as a background job because they process a large amount of data — start the export from its own module, then check progress here.",
            ]),

        new HelpTopic(
            "administration-accounts", "Managing Admin Accounts", "\U0001F464", "Reports & Administration",
            ["super_admin"],
            "Creating accounts and handling account-level actions.",
            [
                "Open Administration from the sidebar to see every admin account.",
                "Ban an account to immediately block its ability to sign in — use this for accounts that need to be suspended rather than deleted.",
                "Force Logout ends every active session for an account immediately, without changing whether they can sign in again.",
                "Unban restores a previously banned account's ability to sign in.",
            ],
            ["Suspending access is almost always safer than deleting an account outright — deleting removes their historical audit-trail attribution."]),

        new HelpTopic(
            "audit-log", "Reading the Audit Log", "\U0001F9FE", "Reports & Administration",
            ["super_admin"],
            "Who did what, and when.",
            [
                "Open Audit Log from the sidebar.",
                "Every meaningful write action across the app (create, edit, archive, delete, key actions like bans or publishes) is recorded here automatically — you never need to remember to log anything yourself.",
                "Use search/filter to narrow down to a specific admin, module, or time period when investigating a change.",
                "The audit log is append-only — entries can never be edited or deleted from within the app, by design.",
            ]),

        // ───────────────────────────── Institution & Principal ─────────────────────────────
        new HelpTopic(
            "institution-overview", "Institution Overview", "\U0001F3DB️", "Institution & Principal",
            ["principal", "super_admin"],
            "A read-only, cross-module snapshot of the whole institution.",
            [
                "Open Institution Overview from the sidebar.",
                "This page aggregates key numbers across every module — departments, faculty, students, timetable coverage, and more — into one read-only summary.",
                "Nothing on this page can be edited directly; it's a dashboard for oversight, not a management screen. Use the specific module's own page to make changes.",
            ]),

        new HelpTopic(
            "principals-message", "Editing the Principal's Message", "\U0001F58B️", "Institution & Principal",
            ["principal", "super_admin"],
            "The public site's \"Principal's Message\" content.",
            [
                "Open Principal's Message from the sidebar.",
                "Edit the message text and, if applicable, the accompanying photo — the same file-upload picker used everywhere else in the app.",
                "Save publishes the update to the public site immediately.",
            ]),

        new HelpTopic(
            "academic-calendar", "Academic Calendar", "\U0001F4C5", "Institution & Principal",
            ["super_admin", "editor"],
            "Managing calendar events shown to the public.",
            [
                "Open Academic Calendar from the sidebar.",
                "Add an event with a title, date, and type (e.g. holiday, exam, event) — Type uses a dropdown so the public calendar's color-coding stays consistent.",
                "Edit or archive an event as plans change — archived events disappear from the public calendar but keep their history.",
            ]),

        // ───────────────────────────── Content Moderation ─────────────────────────────
        new HelpTopic(
            "content-moderation", "Moderating Certifications, Submissions & Blogs", "\U0001F6E1️", "Content Moderation",
            ["editor", "super_admin"],
            "Reviewing content submitted by students or the public before it goes live.",
            [
                "Certifications, Submissions (Contact / Grievance / Alumni), and Blogs each have their own sidebar item, but follow the same review pattern.",
                "New/pending items are highlighted so you can find what needs attention first.",
                "Approve, Reject (usually with a reason shown to the submitter), or Delete each item as appropriate.",
                "Once approved, content typically appears on the public site automatically — no separate publish step.",
            ]),

        new HelpTopic(
            "pyq-management", "Previous Year Questions (PYQ)", "\U0001F4C4", "Content Moderation",
            ["editor", "super_admin"],
            "Moderating public submissions and uploading papers directly as staff.",
            [
                "Open PYQ from the sidebar — the Pending/Approved/Rejected counters at the top give you an at-a-glance queue status.",
                "Use the Status filter to focus on just the papers that need a decision.",
                "Approve, Reject, or Recall a paper — Reject asks for a short reason that's shown to whoever submitted it.",
                "To add a paper yourself instead of waiting for a public submission, use the Add Paper form at the bottom — Semester and Type use dropdowns, and staff-added papers skip the moderation queue entirely since you're adding them directly.",
            ]),
    ];
}
