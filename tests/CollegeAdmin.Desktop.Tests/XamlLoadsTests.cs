using System.Threading;
using CollegeAdmin.Desktop.Converters;
using CollegeAdmin.Desktop.Views;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>
/// Not OS UI automation (no window is shown, no input is simulated — both proved unreliable in
/// this sandbox, see docs/claude/18_DEVLOG.md Entry 5) — just constructing the compiled BAML on an
/// STA thread, which is what actually catches a bad {StaticResource} reference or a malformed
/// DataTemplate before a human ever opens the page. `dotnet build`'s XAML compiler does NOT catch
/// this class of bug: StaticResource lookups resolve at runtime, not compile time.
/// </summary>
public class XamlLoadsTests
{
    private static void RunOnStaThread(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                // No App.xaml.cs composition root runs in a test host, so App.xaml's merged
                // Theme/*.xaml dictionaries (Colors/Typography/Controls) never get attached to
                // Application.Resources the normal way — every View in the real app relies on
                // those for its {StaticResource ...} lookups. Load the same three dictionaries
                // App.xaml declares, onto a throwaway Application instance, so this test verifies
                // the same resource graph the real app has, not a bare/empty one.
                // Qualified explicitly — CollegeAdmin.Application (the project layer, referenced
                // transitively) and System.Windows.Application (the WPF class) both bring an
                // "Application" identifier into scope; the same ambiguity App.xaml.cs itself has
                // to work around.
                if (System.Windows.Application.Current is null)
                {
                    var app = new System.Windows.Application();
                    foreach (var source in new[] { "Theme/Colors.xaml", "Theme/Typography.xaml", "Theme/Controls.xaml" })
                    {
                        var dictionary = new System.Windows.ResourceDictionary
                        {
                            Source = new Uri($"pack://application:,,,/CollegeAdmin.Desktop;component/{source}"),
                        };
                        app.Resources.MergedDictionaries.Add(dictionary);
                    }

                    // App.xaml also registers these three converters directly (not via a merged
                    // Theme dictionary) — every shared/error/empty-state view binds Visibility
                    // through them.
                    app.Resources["BoolToVisibility"] = new BoolToVisibilityConverter();
                    app.Resources["InverseBoolToVisibility"] = new InverseBoolToVisibilityConverter();
                    app.Resources["NullOrEmptyToCollapsed"] = new NullOrEmptyToCollapsedConverter();
                }

                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught is not null)
        {
            throw caught;
        }
    }

    [Fact]
    public void TimetableView_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() => _ = new TimetableView());
    }

    /// <summary>Final Convergence Phase P1-18: the shared component behind ~15 modules, just given
    /// search/pagination/bulk-delete UI — a resource-resolution error here would break nearly every
    /// list screen in the app, so this is worth its own explicit check rather than relying on some
    /// other View's test to incidentally cover it.</summary>
    [Fact]
    public void GenericListView_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() =>
        {
            var view = new GenericListView
            {
                DataContext = new CollegeAdmin.Desktop.ViewModels.GenericListViewModel(
                    "Rows", "None yet.", _ => Task.FromResult<IReadOnlyList<object>>([]),
                    onDelete: _ => Task.CompletedTask),
            };
            _ = view;
        });
    }

    /// <summary>Final Convergence Phase P2 item 1: the archivable-and-viewing-archived state adds
    /// the "Show Archived" checkbox and swaps the row Delete/Restore buttons — a separate pass from
    /// the plain GenericListView test above since both are conditionally-visible paths that test
    /// would never exercise.</summary>
    [Fact]
    public void GenericListView_ArchivableAndShowingArchived_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() =>
        {
            var view = new GenericListView
            {
                DataContext = new CollegeAdmin.Desktop.ViewModels.GenericListViewModel(
                    "Rows", "None yet.", _ => Task.FromResult<IReadOnlyList<object>>([]),
                    onDelete: _ => Task.CompletedTask,
                    isArchivable: true,
                    onRestore: _ => Task.CompletedTask)
                {
                    ShowArchived = true,
                },
            };
            _ = view;
        });
    }

    /// <summary>Final Convergence Phase P1-16: the shell chrome itself — new this phase is the
    /// connectivity indicator (Ellipse + ConnectivityLabel) and the toast overlay (ItemsControl
    /// bound to Toasts, including its severity-triggered Border.Style), both always in the visual
    /// tree regardless of which page is current.</summary>
    [Fact]
    public void MainWindow_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() => _ = new CollegeAdmin.Desktop.MainWindow(
            new CollegeAdmin.Desktop.ViewModels.ShellViewModel(
                new FakeNavigationService(),
                new FakeAuthSessionService(),
                new FakeUpdateService(),
                new FakeConnectivityService(),
                new FakeToastService())));
    }

    /// <summary>Final Convergence Phase P2 item 10: the update banner's "What's New" toggle and
    /// expanded release-notes panel are both conditionally-visible paths the plain MainWindow test
    /// above never exercises (its default FakeUpdateService has no update available at all). The
    /// real startup version check is fire-and-forget/async, so the relevant ShellViewModel
    /// properties are set directly here rather than waiting on it.</summary>
    [Fact]
    public void MainWindow_WithUpdateBannerAndReleaseNotesExpanded_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() =>
        {
            var shell = new CollegeAdmin.Desktop.ViewModels.ShellViewModel(
                new FakeNavigationService(),
                new FakeAuthSessionService(),
                new FakeUpdateService(),
                new FakeConnectivityService(),
                new FakeToastService())
            {
                IsUpdateBannerVisible = true,
                UpdateBannerMessage = "A new version (1.2.0) is available. You're running 1.0.0.",
                UpdateReleaseNotes = "Fixed the thing.",
                IsReleaseNotesExpanded = true,
            };
            _ = new CollegeAdmin.Desktop.MainWindow(shell);
        });
    }

    [Fact]
    public void GalleryImageManagerWindow_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() => _ = new GalleryImageManagerWindow(
            new CollegeAdmin.Desktop.ViewModels.GalleryImageManagerViewModel(new FakeApiClient(), 1, "Test Album")));
    }

    [Fact]
    public void BackgroundJobsView_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() => _ = new BackgroundJobsView());
    }

    [Fact]
    public void StudentEditWindow_LoadsWithoutAResourceResolutionError_CreateMode()
    {
        RunOnStaThread(() => _ = new StudentEditWindow(
            new CollegeAdmin.Desktop.ViewModels.StudentEditViewModel(new FakeApiClient())));
    }

    [Fact]
    public void StudentEditWindow_LoadsWithoutAResourceResolutionError_EditMode()
    {
        // Edit mode renders the extra lifecycle badge and Archive button the create-mode test
        // above never exercises — both are conditionally visible, so both code paths need their
        // own pass to actually catch a bad {StaticResource} reference in either one.
        RunOnStaThread(() => _ = new StudentEditWindow(
            new CollegeAdmin.Desktop.ViewModels.StudentEditViewModel(new FakeApiClient(),
                new CollegeAdmin.Contracts.Api.StudentDto { Id = 1, Name = "Test Student", Lifecycle = "draft" })));
    }

    [Fact]
    public void StudentImportWindow_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() => _ = new StudentImportWindow(
            new CollegeAdmin.Desktop.ViewModels.StudentImportViewModel(new FakeApiClient())));
    }

    [Fact]
    public void PreferenceWindowDetailWindow_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() => _ = new PreferenceWindowDetailWindow(
            new CollegeAdmin.Desktop.ViewModels.PreferenceWindowDetailViewModel(new FakeApiClient(),
                new CollegeAdmin.Contracts.Api.PreferenceWindowDto { Id = 1, Status = "draft" })));
    }

    [Fact]
    public void UniversityRrExportWindow_LoadsWithoutAResourceResolutionError()
    {
        RunOnStaThread(() => _ = new UniversityRrExportWindow(
            new CollegeAdmin.Desktop.ViewModels.UniversityRrExportViewModel(new FakeApiClient(), new FakeLookupCache())));
    }
}
