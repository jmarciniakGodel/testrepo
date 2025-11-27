using Server.Helpers;

namespace Server.Tests.Helpers;

public class CsvParserTests
{
    [Fact]
    public async Task ParseMeetingCsvAsync_ValidCsv_ParsesCorrectly()
    {
        // Arrange
        var csvContent = @"Team Meeting,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45
Jane Smith,jane@example.com,30";
        
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseMeetingCsvAsync(stream);

        // Assert
        Assert.Equal("Team Meeting", result.Title);
        Assert.Equal(new DateTime(2024, 1, 15), result.Date);
        Assert.Equal(2, result.Attendees.Count);
        Assert.Equal("john@example.com", result.Attendees[0].Email);
        Assert.Equal("John Doe", result.Attendees[0].Name);
        Assert.Equal(TimeSpan.FromMinutes(45), result.Attendees[0].Duration);
    }

    [Fact]
    public async Task ParseMeetingCsvAsync_MultipleAttendees_ParsesAll()
    {
        // Arrange
        var csvContent = @"Sprint Planning,2024-01-20
Name,Email,Duration
Alice,alice@example.com,60
Bob,bob@example.com,60
Charlie,charlie@example.com,45";
        
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseMeetingCsvAsync(stream);

        // Assert
        Assert.Equal(3, result.Attendees.Count);
        Assert.Contains(result.Attendees, a => a.Email == "alice@example.com");
        Assert.Contains(result.Attendees, a => a.Email == "bob@example.com");
        Assert.Contains(result.Attendees, a => a.Email == "charlie@example.com");
    }

    [Fact]
    public async Task ParseMeetingCsvAsync_EmptyEmail_SkipsRow()
    {
        // Arrange
        var csvContent = @"Test Meeting,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45
NoEmail,,30
Jane Smith,jane@example.com,60";
        
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseMeetingCsvAsync(stream);

        // Assert
        Assert.Equal(2, result.Attendees.Count);
        Assert.DoesNotContain(result.Attendees, a => string.IsNullOrWhiteSpace(a.Email));
    }
}
