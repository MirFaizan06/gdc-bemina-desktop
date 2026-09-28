using System.Collections.ObjectModel;
using CollegeAdmin.Application.Auth;
using CollegeAdmin.Desktop.Help;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>One step within a HelpTopic, pre-numbered so HelpView's numbered-step badges don't need
/// WPF's ItemsControl.AlternationIndex machinery — the content itself is static, so numbering it
/// once here is simpler than a template-side counter.</summary>
public sealed record NumberedStep(int Number, string Text);

/// <summary>Per-topic expand/collapse state, mirroring the same "wrap static content in a small
/// ObservableObject for its own UI-only state" pattern GenericListViewModel's row actions already
/// use — HelpTopic itself stays a plain immutable record with no WPF dependency.</summary>
public sealed partial class HelpTopicItem : ObservableObject
{
    public HelpTopic Topic { get; }
    public IReadOnlyList<NumberedStep> NumberedSteps { get; }

    [ObservableProperty]
    private bool _isExpanded;

    public HelpTopicItem(HelpTopic topic)
    {
        Topic = topic;
        NumberedSteps = topic.Steps.Select((step, index) => new NumberedStep(index + 1, step)).ToList();
    }

    [RelayCommand]
    private void Toggle() => IsExpanded = !IsExpanded;
}

public sealed record HelpCategoryGroup(string Category, IReadOnlyList<HelpTopicItem> Topics);

/// <summary>Backs the role-filter chip row — IsSelected is recomputed whenever
/// HelpViewModel.SelectedRoleFilter changes, rather than each chip owning its own selection state,
/// so exactly one chip is ever selected at a time (see HelpViewModel.SyncRoleChipSelection).</summary>
public sealed partial class RoleFilterChip : ObservableObject
{
    public string Id { get; }
    public string Label { get; }

    [ObservableProperty]
    private bool _isSelected;

    public RoleFilterChip(string id, string label)
    {
        Id = id;
        Label = label;
    }
}

/// <summary>
/// Final Convergence: in-app Help &amp; Documentation. Entirely local/offline content (HelpCatalog) —
/// no API call, matching the user's own "just app related stuff, doesn't need backend" framing.
/// Content is split into a "General" view (topics relevant to every admin regardless of role) and a
/// per-role scoped view (chips for each of the five AdminProfile.Role values) — defaulting to the
/// signed-in admin's own role so the page opens already relevant to them, while still letting anyone
/// browse documentation written for a different role (e.g. a Super Admin reviewing what a
/// Committee Manager sees).
/// </summary>
public sealed partial class HelpViewModel : ObservableObject
{
    private readonly IReadOnlyList<HelpTopic> _allTopics = HelpCatalog.Topics;

    public HelpViewModel(IAuthSessionService authSessionService)
    {
        RoleChips = new ObservableCollection<RoleFilterChip>(
            HelpCatalog.RoleFilters.Select(r => new RoleFilterChip(r.Id, r.Label)));

        var adminRole = authSessionService.CurrentAdmin?.Role;
        _selectedRoleFilter = RoleChips.Any(chip => chip.Id == adminRole) ? adminRole! : "general";

        SyncRoleChipSelection();
        Refresh();
    }

    public ObservableCollection<RoleFilterChip> RoleChips { get; }

    public ObservableCollection<HelpCategoryGroup> Groups { get; } = [];

    /// <summary>Backs the "no topics match" empty state. EqualsToVisibilityConverter only compares
    /// strings (Groups.Count is an int, so a direct "compare Count to 0" binding would silently
    /// never match) — a plain bool avoids that mismatch entirely.</summary>
    [ObservableProperty]
    private bool _hasResults = true;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedRoleFilter;

    partial void OnSearchTextChanged(string value) => Refresh();

    partial void OnSelectedRoleFilterChanged(string value)
    {
        SyncRoleChipSelection();
        Refresh();
    }

    [RelayCommand]
    private void SelectRole(string roleId) => SelectedRoleFilter = roleId;

    private void SyncRoleChipSelection()
    {
        foreach (var chip in RoleChips)
        {
            chip.IsSelected = chip.Id == SelectedRoleFilter;
        }
    }

    /// <summary>Re-run on every search/role change — the catalog is small (a few dozen topics, all
    /// already in memory), so a full re-filter is simpler and fast enough rather than maintaining an
    /// incremental diff.</summary>
    private void Refresh()
    {
        var query = SearchText.Trim();

        IEnumerable<HelpTopic> filtered = _allTopics.Where(topic =>
            SelectedRoleFilter == "general" ? topic.Roles.Count == 0 : topic.Roles.Contains(SelectedRoleFilter));

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(topic =>
                topic.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                topic.Summary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                topic.Steps.Any(step => step.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        Groups.Clear();
        foreach (var group in filtered.GroupBy(topic => topic.Category))
        {
            Groups.Add(new HelpCategoryGroup(group.Key, group.Select(topic => new HelpTopicItem(topic)).ToList()));
        }

        HasResults = Groups.Count > 0;
    }
}
