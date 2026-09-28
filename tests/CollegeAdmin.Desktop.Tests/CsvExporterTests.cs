using System.IO;
using CollegeAdmin.Desktop.Export;

namespace CollegeAdmin.Desktop.Tests;

public class CsvExporterTests
{
    private sealed record Row(int Id, string Name, string? Notes);

    [Fact]
    public void Write_ProducesAHeaderRowAndOneLinePerRow()
    {
        var path = Path.Combine(Path.GetTempPath(), $"csv-export-test-{Guid.NewGuid()}.csv");
        try
        {
            CsvExporter.Write([new Row(1, "Alice", null), new Row(2, "Bob", "vip")], path);

            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length);
            Assert.Equal("Id,Name,Notes", lines[0]);
            Assert.Equal("1,Alice,", lines[1]);
            Assert.Equal("2,Bob,vip", lines[2]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Write_EscapesCommasQuotesAndNewlines()
    {
        var path = Path.Combine(Path.GetTempPath(), $"csv-export-test-{Guid.NewGuid()}.csv");
        try
        {
            CsvExporter.Write([new Row(1, "Smith, John", "Has \"quotes\" and\nnewline")], path);

            var content = File.ReadAllText(path);
            Assert.Contains("\"Smith, John\"", content);
            Assert.Contains("\"Has \"\"quotes\"\" and\nnewline\"", content);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Write_EmptyRows_WritesNothing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"csv-export-test-{Guid.NewGuid()}.csv");
        try
        {
            CsvExporter.Write([], path);

            Assert.Equal("", File.ReadAllText(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
