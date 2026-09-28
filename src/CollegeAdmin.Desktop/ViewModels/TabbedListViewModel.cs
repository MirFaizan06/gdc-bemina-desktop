using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>Groups several GenericListViewModel screens under one nav item as tabs (e.g. "Website
/// Content" -> Notices/News/Banners/FAQ/Documents/Events/Gallery), matching the module groupings in
/// docs/claude/05_MODULE_CATALOG.md without needing a separate tree-navigation control yet.</summary>
public sealed partial class TabbedListViewModel : ObservableObject
{
    public TabbedListViewModel(IEnumerable<GenericListViewModel> tabs)
    {
        Tabs = new ObservableCollection<GenericListViewModel>(tabs);
        SelectedTab = Tabs.FirstOrDefault();
    }

    public ObservableCollection<GenericListViewModel> Tabs { get; }

    [ObservableProperty]
    private GenericListViewModel? _selectedTab;
}
