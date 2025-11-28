using Server.Helpers;

namespace Server.Tests.Helpers;

public class XlsxGeneratorTests
{
    [Fact]
    public void GenerateXlsxFromSummary_ValidData_GeneratesValidXlsx()
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
        var xlsxData = XlsxGenerator.GenerateXlsxFromSummary(data);

        // Assert
        Assert.NotNull(xlsxData);
        Assert.NotEmpty(xlsxData);
        // XLSX files have a specific header signature
        Assert.Equal(0x50, xlsxData[0]); // 'P'
        Assert.Equal(0x4B, xlsxData[1]); // 'K' (ZIP file signature)
    }

    [Fact]
    public void GenerateXlsxFromSummary_EmptyData_GeneratesValidXlsx()
    {
        // Arrange
        var data = new SummaryData
        {
            MeetingHeaders = new List<string>(),
            AttendantEmails = new List<string>(),
            AttendanceMatrix = new Dictionary<string, Dictionary<string, TimeSpan>>()
        };

        // Act
        var xlsxData = XlsxGenerator.GenerateXlsxFromSummary(data);

        // Assert
        Assert.NotNull(xlsxData);
        Assert.NotEmpty(xlsxData);
    }
}
