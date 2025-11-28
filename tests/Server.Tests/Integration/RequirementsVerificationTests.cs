using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using Server.Repositories;
using Server.Services;

namespace Server.Tests.Integration;

/// <summary>
/// Integration tests that verify the requirements from the issue
/// </summary>
public class RequirementsVerificationTests
{
    /// <summary>
    /// Verifies that JSON files masquerading as CSV are rejected (from issue reference file: response_1764256961991.csv)
    /// </summary>
    [Fact]
    public async Task RejectJsonFileWithCsvExtension()
    {
        // Arrange - This is the actual JSON content from the issue
        var jsonContent = @"{
  ""summaryId"": 1,
  ""htmlTable"": ""<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; font-family: Arial, sans-serif;'>\r\n  <thead>\r\n    <tr style='background-color: #4CAF50; color: white;'>\r\n      <th>Attendant Email</th>\r\n      <th>1. Summary (2025-11-27)</th>\r\n    </tr>\r\n  </thead>\r\n  <tbody>\r\n    <tr>\r\n      <td>4:16:54 PM</td>\r\n      <td style='text-align: center;'>-</td>\r\n    </tr>\r\n    <tr>\r\n      <td>4:18:08 PM</td>\r\n      <td style='text-align: center;'>-</td>\r\n    </tr>\r\n    <tr>\r\n      <td>4:16:55 PM\t11/26/25</td>\r\n      <td style='text-align: center;'>-</td>\r\n    </tr>\r\n  </tbody>\r\n</table>\r\n""
}";

        var context = CreateTestContext();
        var service = CreateService(context);
        var file = CreateFormFile(jsonContent, "response_1764256961991.csv", "text/csv");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ProcessMeetingFilesAsync(new List<IFormFile> { file }));

        // Verify the error message contains the expected information
        Assert.Contains("TYPE_MISMATCH", exception.Message);
        Assert.Contains("JSON", exception.Message);
        
        // Verify no data was persisted (atomic rollback)
        Assert.Empty(await context.Attendants.ToListAsync());
        Assert.Empty(await context.Meetings.ToListAsync());
        Assert.Empty(await context.Summaries.ToListAsync());
        Assert.Empty(await context.MeetingAttendances.ToListAsync());
    }

    /// <summary>
    /// Verifies that valid Teams CSV is accepted (from issue reference: Meeting with Jan Nowak - Attendance report 11-26-25.csv)
    /// </summary>
    [Fact]
    public async Task AcceptValidTeamsCsv()
    {
        // Arrange - This mimics the actual Teams CSV format from the issue
        var validTeamsCsv = @"1. Summary
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

        var context = CreateTestContext();
        var service = CreateService(context);
        var file = CreateFormFile(validTeamsCsv, "Meeting with Jan Nowak - Attendance report 11-26-25.csv", "text/csv");

        // Act
        var result = await service.ProcessMeetingFilesAsync(new List<IFormFile> { file });

        // Assert
        Assert.True(result.SummaryId > 0);
        Assert.NotNull(result.HtmlTable);

        // Verify data was persisted correctly
        var attendants = await context.Attendants.ToListAsync();
        Assert.Single(attendants);
        Assert.Equal("j.nowak@gmail.com", attendants[0].Email);
        Assert.Equal("Jan Nowak", attendants[0].Name);

        var meetings = await context.Meetings.ToListAsync();
        Assert.Single(meetings);
        Assert.Equal("Meeting with Jan Nowak", meetings[0].Title);

        var attendances = await context.MeetingAttendances.ToListAsync();
        Assert.Single(attendances);
        Assert.Equal(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(13), attendances[0].Duration);
    }

    /// <summary>
    /// Verifies that empty CSV files are rejected with clear error message
    /// </summary>
    [Fact]
    public async Task RejectEmptyFile()
    {
        // Arrange
        var context = CreateTestContext();
        var service = CreateService(context);
        var file = CreateFormFile("", "empty.csv", "text/csv");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ProcessMeetingFilesAsync(new List<IFormFile> { file }));

        Assert.Contains("EMPTY_FILE", exception.Message);
        
        // Verify no data was persisted
        Assert.Empty(await context.Attendants.ToListAsync());
        Assert.Empty(await context.Meetings.ToListAsync());
    }

    /// <summary>
    /// Verifies that Teams CSV without attendees is rejected
    /// </summary>
    [Fact]
    public async Task RejectTeamsCsvWithoutAttendees()
    {
        // Arrange
        var invalidTeamsCsv = @"1. Summary
Meeting title	Empty Meeting
Attended participants	0
Start time	11/26/25, 4:16:54 PM
End time	11/26/25, 4:18:08 PM
Meeting duration	1m 14s
Average attendance time	0s

2. Participants
Name	First Join	Last Leave	In-Meeting Duration	Email	Participant ID (UPN)	Role

3. In-Meeting Activities
Name	Join Time	Leave Time	Duration	Email	Role";

        var context = CreateTestContext();
        var service = CreateService(context);
        var file = CreateFormFile(invalidTeamsCsv, "empty_meeting.csv", "text/csv");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ProcessMeetingFilesAsync(new List<IFormFile> { file }));

        Assert.Contains("NO_ATTENDEES", exception.Message);
        
        // Verify no data was persisted (atomic rollback)
        Assert.Empty(await context.Attendants.ToListAsync());
        Assert.Empty(await context.Meetings.ToListAsync());
        Assert.Empty(await context.Summaries.ToListAsync());
    }

    /// <summary>
    /// Verifies that Teams CSV with invalid email is rejected
    /// </summary>
    [Fact]
    public async Task RejectTeamsCsvWithInvalidEmail()
    {
        // Arrange
        var invalidEmailCsv = @"1. Summary
Meeting title	Meeting with Invalid Email
Attended participants	1
Start time	11/26/25, 4:16:54 PM
End time	11/26/25, 4:18:08 PM
Meeting duration	1m 14s
Average attendance time	1m 13s

2. Participants
Name	First Join	Last Leave	In-Meeting Duration	Email	Participant ID (UPN)	Role
John Doe	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	not-an-email	not-an-email	Organizer

3. In-Meeting Activities
Name	Join Time	Leave Time	Duration	Email	Role
John Doe	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	not-an-email	Organizer";

        var context = CreateTestContext();
        var service = CreateService(context);
        var file = CreateFormFile(invalidEmailCsv, "invalid_email.csv", "text/csv");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ProcessMeetingFilesAsync(new List<IFormFile> { file }));

        Assert.Contains("INVALID_EMAIL", exception.Message);
        
        // Verify no data was persisted (atomic rollback)
        Assert.Empty(await context.Attendants.ToListAsync());
        Assert.Empty(await context.Meetings.ToListAsync());
    }

    /// <summary>
    /// Verifies that when multiple files are uploaded and one is invalid, NO data is persisted (atomic transaction)
    /// </summary>
    [Fact]
    public async Task RejectMultipleFilesIfOneIsInvalid_NoDataPersisted()
    {
        // Arrange
        var validCsv = @"1. Summary
Meeting title	Valid Meeting
Attended participants	1
Start time	11/26/25, 4:16:54 PM
End time	11/26/25, 4:18:08 PM
Meeting duration	1m 14s
Average attendance time	1m 13s

2. Participants
Name	First Join	Last Leave	In-Meeting Duration	Email	Participant ID (UPN)	Role
John Doe	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	john@example.com	john@example.com	Organizer

3. In-Meeting Activities
Name	Join Time	Leave Time	Duration	Email	Role
John Doe	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	john@example.com	Organizer";

        var invalidJson = @"{""error"": ""This is JSON not CSV""}";

        var context = CreateTestContext();
        var service = CreateService(context);
        
        var files = new List<IFormFile>
        {
            CreateFormFile(validCsv, "valid.csv", "text/csv"),
            CreateFormFile(invalidJson, "invalid.csv", "text/csv")
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ProcessMeetingFilesAsync(files));

        Assert.Contains("TYPE_MISMATCH", exception.Message);
        Assert.Contains("JSON", exception.Message);
        
        // Critical: Verify NO data from the valid file was persisted (atomic transaction behavior)
        Assert.Empty(await context.Attendants.ToListAsync());
        Assert.Empty(await context.Meetings.ToListAsync());
        Assert.Empty(await context.Summaries.ToListAsync());
        Assert.Empty(await context.MeetingAttendances.ToListAsync());
    }

    private static AppDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static MeetingUploadService CreateService(AppDbContext context)
    {
        var attendantRepository = new AttendantRepository(context);
        var meetingRepository = new MeetingRepository(context);
        var attendanceRepository = new MeetingAttendanceRepository(context);
        var summaryRepository = new SummaryRepository(context);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MeetingUploadService>();

        return new MeetingUploadService(
            attendantRepository,
            meetingRepository,
            attendanceRepository,
            summaryRepository,
            context,
            logger);
    }

    private static IFormFile CreateFormFile(string content, string fileName, string contentType)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        
        return new FormFile(stream, 0, bytes.Length, "files", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
