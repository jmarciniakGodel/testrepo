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

    [Fact]
    public async Task ParseMeetingCsvAsync_MicrosoftTeamsFormat_ParsesCorrectly()
    {
        // Arrange
        var csvContent = @"1. Summary
Meeting title	Test Meeting with Teams
Attended participants	2
Start time	11/26/25, 4:16:54 PM
End time	11/26/25, 4:18:08 PM
Meeting duration	1m 14s
Average attendance time	1m 13s

2. Participants
Name	First Join	Last Leave	In-Meeting Duration	Email	Participant ID (UPN)	Role
John Doe	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	2m 30s	john@example.com	john@example.com	Organizer
Jane Smith	11/26/25, 4:17:00 PM	11/26/25, 4:18:05 PM	1m 5s	jane@example.com	jane@example.com	Presenter

3. In-Meeting Activities
Name	Join Time	Leave Time	Duration	Email	Role
John Doe	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	2m 30s	john@example.com	Organizer";
        
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseMeetingCsvAsync(stream);

        // Assert
        Assert.Equal("Test Meeting with Teams", result.Title);
        Assert.Equal(new DateTime(2025, 11, 26, 16, 16, 54), result.Date);
        Assert.Equal(2, result.Attendees.Count);
        Assert.Equal("john@example.com", result.Attendees[0].Email);
        Assert.Equal("John Doe", result.Attendees[0].Name);
        Assert.Equal(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(30), result.Attendees[0].Duration);
        Assert.Equal("jane@example.com", result.Attendees[1].Email);
        Assert.Equal(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(5), result.Attendees[1].Duration);
    }

    [Fact]
    public async Task ParseMeetingCsvAsync_MicrosoftTeamsFormatUTF16_ParsesCorrectly()
    {
        // Arrange - simulate UTF-16LE encoded content with BOM
        var csvContent = @"1. Summary
Meeting title	Meeting with Jan Kowalski
Attended participants	1
Start time	11/26/25, 4:16:54 PM
End time	11/26/25, 4:18:08 PM
Meeting duration	1m 14s
Average attendance time	1m 13s

2. Participants
Name	First Join	Last Leave	In-Meeting Duration	Email	Participant ID (UPN)	Role
Jan Kowalski	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	j.kowalski@gmail.com	j.kowalski@gmail.com	Organizer

3. In-Meeting Activities
Name	Join Time	Leave Time	Duration	Email	Role
Jan Kowalski	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	j.kowalski@gmail.com	Organizer";
        
        // Create UTF-16LE with BOM - manually prepend the BOM
        var encoding = new System.Text.UnicodeEncoding(false, false); // Little endian, no auto-BOM
        var preamble = encoding.GetPreamble(); // Get BOM bytes
        var contentBytes = encoding.GetBytes(csvContent);
        
        // Combine BOM + content
        var bytes = new byte[] { 0xFF, 0xFE }.Concat(contentBytes).ToArray();
        var stream = new MemoryStream(bytes);

        // Act
        var result = await CsvParser.ParseMeetingCsvAsync(stream);

        // Assert
        Assert.Equal("Meeting with Jan Kowalski", result.Title);
        Assert.Single(result.Attendees);
        Assert.Equal("j.kowalski@gmail.com", result.Attendees[0].Email);
        Assert.Equal("Jan Kowalski", result.Attendees[0].Name);
        Assert.Equal(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(13), result.Attendees[0].Duration);
    }
}
