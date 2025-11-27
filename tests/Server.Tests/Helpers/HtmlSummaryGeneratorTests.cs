using Server.Helpers;

namespace Server.Tests.Helpers;

public class HtmlSummaryGeneratorTests
{
    [Fact]
    public void GenerateHtmlTable_ValidData_GeneratesCorrectHtml()
    {
        // Arrange
        var data = new SummaryData
        {
            MeetingHeaders = new List<string> { "Meeting1 (2024-01-01)", "Meeting2 (2024-01-02)" },
            AttendantEmails = new List<string> { "john@example.com", "jane@example.com" },
            AttendanceMatrix = new Dictionary<string, Dictionary<string, TimeSpan>>
            {
                {
                    "john@example.com",
                    new Dictionary<string, TimeSpan>
                    {
                        { "Meeting1 (2024-01-01)", TimeSpan.FromMinutes(45) },
                        { "Meeting2 (2024-01-02)", TimeSpan.FromMinutes(90) }
                    }
                },
                {
                    "jane@example.com",
                    new Dictionary<string, TimeSpan>
                    {
                        { "Meeting1 (2024-01-01)", TimeSpan.FromMinutes(30) }
                    }
                }
            }
        };

        // Act
        var html = HtmlSummaryGenerator.GenerateHtmlTable(data);

        // Assert
        Assert.Contains("john@example.com", html);
        Assert.Contains("jane@example.com", html);
        Assert.Contains("Meeting1 (2024-01-01)", html);
        Assert.Contains("Meeting2 (2024-01-02)", html);
        Assert.Contains("45 min", html);
        Assert.Contains("1 hr 30 min", html);
        Assert.Contains("30 min", html);
        Assert.Contains("<table", html);
        Assert.Contains("</table>", html);
    }

    [Fact]
    public void GenerateHtmlTable_EmptyAttendance_ShowsDash()
    {
        // Arrange
        var data = new SummaryData
        {
            MeetingHeaders = new List<string> { "Meeting1 (2024-01-01)" },
            AttendantEmails = new List<string> { "john@example.com" },
            AttendanceMatrix = new Dictionary<string, Dictionary<string, TimeSpan>>()
        };

        // Act
        var html = HtmlSummaryGenerator.GenerateHtmlTable(data);

        // Assert
        Assert.Contains("-", html);
    }
}
