using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using Server.Repositories;
using Server.Services;

namespace Server.Tests.Integration;

public class FileUploadIntegrationTests
{
    [Fact]
    public async Task ProcessMeetingFilesAsync_ValidSimpleCsv_Success()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        var attendantRepository = new AttendantRepository(context);
        var meetingRepository = new MeetingRepository(context);
        var attendanceRepository = new MeetingAttendanceRepository(context);
        var summaryRepository = new SummaryRepository(context);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<MeetingUploadService>();

        var service = new MeetingUploadService(
            attendantRepository,
            meetingRepository,
            attendanceRepository,
            summaryRepository,
            context,
            logger);

        var csvContent = @"Team Meeting,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45
Jane Smith,jane@example.com,30";

        var file = CreateFormFile(csvContent, "meeting.csv", "text/csv");
        var files = new List<IFormFile> { file };

        // Act
        var result = await service.ProcessMeetingFilesAsync(files);

        // Assert
        Assert.True(result.SummaryId > 0);
        Assert.NotNull(result.HtmlTable);

        var attendants = await context.Attendants.ToListAsync();
        Assert.Equal(2, attendants.Count);
    }

    [Fact]
    public async Task ProcessMeetingFilesAsync_JsonMasqueradingAsCsv_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        var attendantRepository = new AttendantRepository(context);
        var meetingRepository = new MeetingRepository(context);
        var attendanceRepository = new MeetingAttendanceRepository(context);
        var summaryRepository = new SummaryRepository(context);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MeetingUploadService>();

        var service = new MeetingUploadService(
            attendantRepository,
            meetingRepository,
            attendanceRepository,
            summaryRepository,
            context,
            logger);

        var jsonContent = @"{
  ""summaryId"": 1,
  ""htmlTable"": ""<table border='1'>...</table>""
}";

        var file = CreateFormFile(jsonContent, "response.csv", "text/csv");
        var files = new List<IFormFile> { file };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ProcessMeetingFilesAsync(files));
        
        Assert.Contains("TYPE_MISMATCH", exception.Message);
        Assert.Contains("JSON", exception.Message);

        // Verify no data was saved (transaction rollback)
        var attendants = await context.Attendants.ToListAsync();
        Assert.Empty(attendants);
        
        var meetings = await context.Meetings.ToListAsync();
        Assert.Empty(meetings);
        
        var summaries = await context.Summaries.ToListAsync();
        Assert.Empty(summaries);
    }

    [Fact]
    public async Task ProcessMeetingFilesAsync_EmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        var attendantRepository = new AttendantRepository(context);
        var meetingRepository = new MeetingRepository(context);
        var attendanceRepository = new MeetingAttendanceRepository(context);
        var summaryRepository = new SummaryRepository(context);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MeetingUploadService>();

        var service = new MeetingUploadService(
            attendantRepository,
            meetingRepository,
            attendanceRepository,
            summaryRepository,
            context,
            logger);

        var file = CreateFormFile("", "empty.csv", "text/csv");
        var files = new List<IFormFile> { file };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ProcessMeetingFilesAsync(files));
        
        Assert.Contains("EMPTY_FILE", exception.Message);
    }

    [Fact]
    public async Task ProcessMeetingFilesAsync_NoAttendees_ThrowsArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        var attendantRepository = new AttendantRepository(context);
        var meetingRepository = new MeetingRepository(context);
        var attendanceRepository = new MeetingAttendanceRepository(context);
        var summaryRepository = new SummaryRepository(context);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MeetingUploadService>();

        var service = new MeetingUploadService(
            attendantRepository,
            meetingRepository,
            attendanceRepository,
            summaryRepository,
            context,
            logger);

        var csvContent = @"Team Meeting,2024-01-15
Name,Email,Duration
,,";

        var file = CreateFormFile(csvContent, "empty.csv", "text/csv");
        var files = new List<IFormFile> { file };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ProcessMeetingFilesAsync(files));
        
        Assert.Contains("NO_ATTENDEES", exception.Message);
    }

    [Fact]
    public async Task ProcessMeetingFilesAsync_ValidTeamsCsv_Success()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        var attendantRepository = new AttendantRepository(context);
        var meetingRepository = new MeetingRepository(context);
        var attendanceRepository = new MeetingAttendanceRepository(context);
        var summaryRepository = new SummaryRepository(context);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MeetingUploadService>();

        var service = new MeetingUploadService(
            attendantRepository,
            meetingRepository,
            attendanceRepository,
            summaryRepository,
            context,
            logger);

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

        var file = CreateFormFile(csvContent, "teams_meeting.csv", "text/csv");
        var files = new List<IFormFile> { file };

        // Act
        var result = await service.ProcessMeetingFilesAsync(files);

        // Assert
        Assert.True(result.SummaryId > 0);
        Assert.NotNull(result.HtmlTable);

        var attendants = await context.Attendants.ToListAsync();
        Assert.Single(attendants);
        Assert.Equal("j.nowak@gmail.com", attendants[0].Email);
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
