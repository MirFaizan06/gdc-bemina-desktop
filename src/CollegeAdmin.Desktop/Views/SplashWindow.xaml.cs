using System.Windows;

namespace CollegeAdmin.Desktop.Views;

/// <summary>Reused twice in the boot sequence (App.xaml.cs): once before login (with
/// "Checking for updates..."/"Starting...") and once briefly after login succeeds
/// ("Preparing your dashboard...") — just a status-text swap, not two separate windows.</summary>
public partial class SplashWindow : Window
{
    public SplashWindow() => InitializeComponent();

    public void SetStatus(string status) => StatusTextBlock.Text = status;
}
