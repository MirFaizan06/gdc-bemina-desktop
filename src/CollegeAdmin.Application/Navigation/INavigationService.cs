namespace CollegeAdmin.Application.Navigation;

/// <summary>
/// Maps a nav key to a ViewModel instance for the shell's content area. Implemented in
/// CollegeAdmin.Desktop (it needs DI to construct ViewModels) — this interface is what
/// ShellViewModel depends on, keeping it framework-testable without a real WPF host.
/// </summary>
public interface INavigationService
{
    object? CurrentPage { get; }
    event EventHandler? CurrentPageChanged;
    void NavigateTo(string pageKey);
}
