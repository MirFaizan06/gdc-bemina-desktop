using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Final Convergence Phase P1-10/§2b (docs/claude/FINAL_COMPLETION_TRACKER.md §2b): a small,
/// reusable "pick a committee to forward to" prompt for Submissions moderation (Contact/Grievance).
/// </summary>
public sealed partial class ForwardToCommitteeViewModel : ObservableObject
{
    public ForwardToCommitteeViewModel(IReadOnlyList<CommitteeDto> committees)
    {
        foreach (var committee in committees)
        {
            Committees.Add(committee);
        }
    }

    public ObservableCollection<CommitteeDto> Committees { get; } = [];

    [ObservableProperty]
    private CommitteeDto? _selectedCommittee;

    [ObservableProperty]
    private string _note = "";
}
