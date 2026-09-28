using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CollegeAdmin.Desktop.Theming;
using CollegeAdmin.Desktop.ViewModels;

namespace CollegeAdmin.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    /// <summary>Same "dialog + confirmation lives in code-behind, not the ViewModel" shape as
    /// GenericListView's delete confirmations — ThemeChooserWindow is a plain Window, and applying
    /// a new theme only takes effect for windows created after the swap (see ThemeService.Apply's
    /// remarks), so a relaunch is offered rather than pretending the change is instant.</summary>
    private void ChangeThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        var themeService = new ThemeService();
        var current = themeService.LoadPreset();

        var chooser = new ThemeChooserWindow { Owner = Window.GetWindow(this) };
        chooser.PreselectPreset(current);
        var result = chooser.ShowDialog();

        if (result != true || chooser.SelectedPreset.Id == current.Id)
        {
            return;
        }

        themeService.SavePreset(chooser.SelectedPreset);
        themeService.Apply(chooser.SelectedPreset);
        viewModel.RefreshThemeDisplay();

        var restart = MessageBox.Show(
            $"Theme changed to \"{chooser.SelectedPreset.Name}\". Restart College Admin now to see it applied?",
            "Restart Required",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (restart == MessageBoxResult.Yes)
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                Process.Start(exePath);
            }
            System.Windows.Application.Current.Shutdown();
        }
    }
}
