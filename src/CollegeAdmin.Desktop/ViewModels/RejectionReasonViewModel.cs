using CommunityToolkit.Mvvm.ComponentModel;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): a small,
/// reusable "enter a reason" prompt — Certifications/Blogs/PYQ moderation all need to collect a
/// rejection reason before rejecting something, and no such prompt existed anywhere in this
/// codebase before now (AdminsListViewModel's own Ban action hardcodes a fixed reason string with
/// no prompt at all). Built once here rather than duplicated per module.
/// </summary>
public sealed partial class RejectionReasonViewModel : ObservableObject
{
    public RejectionReasonViewModel(string title, string prompt)
    {
        Title = title;
        Prompt = prompt;
    }

    public string Title { get; }
    public string Prompt { get; }

    [ObservableProperty]
    private string _reason = "";
}
