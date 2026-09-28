namespace CollegeAdmin.Desktop;

/// <summary>Shared by ShellViewModel (top-bar welcome line) and DashboardViewModel (the big
/// landing-page header) so both say the same thing.</summary>
public static class TimeGreeting
{
    public static string Now() => DateTime.Now.Hour switch
    {
        < 12 => "Good morning",
        < 17 => "Good afternoon",
        _ => "Good evening",
    };

    public static string FirstName(string fullName) =>
        string.IsNullOrWhiteSpace(fullName) ? "" : fullName.Trim().Split(' ')[0];
}
