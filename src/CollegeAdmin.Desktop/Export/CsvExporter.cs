using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace CollegeAdmin.Desktop.Export;

/// <summary>
/// Stage 12's export capability (docs/claude/12_EXPORT_REPORTING_CONNECTIVITY.md) — CSV only for
/// now, not XLSX/PDF: CSV needs no extra dependency and covers "current filtered view" (the only
/// export mode built so far; custom field selection / date range are not). Exports the row DTOs
/// already loaded into a GenericListViewModel via reflection — the same set of public properties
/// WPF's own DataGrid.AutoGenerateColumns already displays, so the CSV columns always match what
/// the admin is looking at on screen.
/// </summary>
public static class CsvExporter
{
    public static void Write(IReadOnlyList<object> rows, string filePath)
    {
        using var writer = new StreamWriter(filePath, append: false, Encoding.UTF8);
        if (rows.Count == 0)
        {
            return;
        }

        var properties = rows[0].GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        writer.WriteLine(string.Join(",", properties.Select(p => Escape(p.Name))));

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",", properties.Select(p => Escape(p.GetValue(row)?.ToString() ?? ""))));
        }
    }

    private static string Escape(string value) =>
        value.IndexOfAny([',', '"', '\n', '\r']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;

    /// <summary>The full interactive flow (SaveFileDialog -> background-threaded write -> audit
    /// call -> result MessageBox), shared by every view that exposes an Export CSV button
    /// (GenericListView and TimetableView) so the flow only exists in one place.</summary>
    public static async Task ExportInteractivelyAsync(IReadOnlyList<object> rows, string suggestedFileName, Func<int, Task> recordExport)
    {
        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = "CSV files (*.csv)|*.csv",
            DefaultExt = ".csv",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await Task.Run(() => Write(rows, dialog.FileName));
            await recordExport(rows.Count);
            MessageBox.Show($"Exported {rows.Count} row(s) to {dialog.FileName}.", "Export Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Could not write the export file: {ex.Message}", "Export Failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
