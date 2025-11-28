using Server.Helpers;

namespace Server.Tests.Helpers;

public class CsvParserEnhancedTests
{
    [Fact]
    public async Task ParseAndValidateMeetingCsvAsync_EmptyContent_ReturnsError()
    {
        // Arrange
        var csvContent = "";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseAndValidateMeetingCsvAsync(stream);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("EMPTY_CONTENT", result.ErrorCode);
    }

    [Fact]
    public async Task ParseAndValidateMeetingCsvAsync_NoAttendees_ReturnsError()
    {
        // Arrange - has header but no attendee rows
        var csvContent = @"Team Meeting,2024-01-15
Name,Email,Duration
,,";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseAndValidateMeetingCsvAsync(stream);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_ATTENDEES", result.ErrorCode);
    }

    [Fact]
    public async Task ParseAndValidateMeetingCsvAsync_InvalidEmail_ReturnsError()
    {
        // Arrange
        var csvContent = @"Team Meeting,2024-01-15
Name,Email,Duration
John Doe,not-an-email,45";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseAndValidateMeetingCsvAsync(stream);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_EMAIL_FORMAT", result.ErrorCode);
    }

    [Fact]
    public async Task ParseAndValidateMeetingCsvAsync_ValidSimpleFormat_ReturnsSuccess()
    {
        // Arrange
        var csvContent = @"Team Meeting,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseAndValidateMeetingCsvAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Team Meeting", result.Data.Title);
        Assert.Single(result.Data.Attendees);
    }

    [Fact]
    public async Task ParseAndValidateMeetingCsvAsync_TeamsFormatMissingSection_ReturnsError()
    {
        // Arrange - missing Participants section
        var csvContent = @"1. Summary
Meeting title	Test Meeting
Start time	11/26/25, 4:16:54 PM";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseAndValidateMeetingCsvAsync(stream);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("MISSING_PARTICIPANTS_SECTION", result.ErrorCode);
    }

    [Fact]
    public async Task ParseAndValidateMeetingCsvAsync_TeamsFormatInvalidDate_ReturnsError()
    {
        // Arrange
        var csvContent = @"1. Summary
Meeting title	Test Meeting
Start time	invalid-date
2. Participants
Name	First Join	Last Leave	In-Meeting Duration	Email	Participant ID (UPN)	Role
John Doe	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	john@example.com	john@example.com	Organizer";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseAndValidateMeetingCsvAsync(stream);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_DATE_FORMAT", result.ErrorCode);
    }

    [Fact]
    public async Task ParseAndValidateMeetingCsvAsync_TeamsFormatValid_ReturnsSuccess()
    {
        // Arrange
        var csvContent = @"1. Summary
Meeting title	Meeting with Jan Nowak
Attended participants	1
Start time	11/26/25, 4:16:54 PM
End time	11/26/25, 4:18:08 PM
Meeting duration	1m 14s
Average attendance time	1m 13s

2. Participants
Name	First Join	Last Leave	In-Meeting Duration	Email	Participant ID (UPN)	Role
Jan Nowak	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	j.nowak@gmail.com	j.nowak@gmail.com	Organizer

3. In-Meeting Activities
Name	Join Time	Leave Time	Duration	Email	Role
Jan Nowak	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	j.nowak@gmail.com	Organizer";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await CsvParser.ParseAndValidateMeetingCsvAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Meeting with Jan Nowak", result.Data.Title);
        Assert.Single(result.Data.Attendees);
        Assert.Equal("j.nowak@gmail.com", result.Data.Attendees[0].Email);
    }
}
