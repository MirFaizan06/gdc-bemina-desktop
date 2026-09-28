using CommunityToolkit.Mvvm.ComponentModel;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>One instance per nav group until that module ships for real (Stage 9+).</summary>
public sealed partial class PlaceholderPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "";

    public void SetTitle(string title) => Title = title;
}
