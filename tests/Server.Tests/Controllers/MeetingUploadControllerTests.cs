using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Controllers;
using Server.Data;
using Server.Repositories;
using Server.Repositories.Interfaces;
using Server.Services;
using Server.Services.Interfaces;

namespace Server.Tests.Controllers;

public class MeetingUploadControllerTests
{
    private readonly AppDbContext _context;
    private readonly IAttendantRepository _attendantRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingAttendanceRepository _attendanceRepository;
    private readonly ISummaryRepository _summaryRepository;
    private readonly IMeetingUploadService _meetingUploadService;
    private readonly ISummaryService _summaryService;
    private readonly MeetingUploadController _controller;

    public MeetingUploadControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _attendantRepository = new AttendantRepository(_context);
        _meetingRepository = new MeetingRepository(_context);
        _attendanceRepository = new MeetingAttendanceRepository(_context);
        _summaryRepository = new SummaryRepository(_context);

        _meetingUploadService = new MeetingUploadService(
            _attendantRepository,
            _meetingRepository,
            _attendanceRepository,
            _summaryRepository);

        _summaryService = new SummaryService(_summaryRepository);

        _controller = new MeetingUploadController(
            _summaryRepository,
            _meetingUploadService,
            _summaryService);
    }

    [Fact]
    public async Task UploadMeetingCsvs_NoFiles_ReturnsBadRequest()
    {
        // Arrange
        var files = new List<IFormFile>();

        // Act
        var result = await _controller.UploadMeetingCsvs(files);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UploadMeetingCsvs_ValidFiles_ReturnsOkWithSummary()
    {
        // Arrange
        var csvContent = @"Team Meeting,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45
Jane Smith,jane@example.com,30";

        var file = CreateFormFile(csvContent, "meeting.csv", "text/csv");
        var files = new List<IFormFile> { file };

        // Act
        var result = await _controller.UploadMeetingCsvs(files);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify data was saved
        var attendants = await _context.Attendants.ToListAsync();
        Assert.Equal(2, attendants.Count);
        Assert.Contains(attendants, a => a.Email == "john@example.com");
        Assert.Contains(attendants, a => a.Email == "jane@example.com");
    }

    [Fact]
    public async Task UploadMeetingCsvs_MultipleFiles_ProcessesAllFiles()
    {
        // Arrange
        var csv1 = @"Meeting 1,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45";

        var csv2 = @"Meeting 2,2024-01-16
Name,Email,Duration
Jane Smith,jane@example.com,60";

        var file1 = CreateFormFile(csv1, "meeting1.csv", "text/csv");
        var file2 = CreateFormFile(csv2, "meeting2.csv", "text/csv");
        var files = new List<IFormFile> { file1, file2 };

        // Act
        var result = await _controller.UploadMeetingCsvs(files);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var meetings = await _context.Meetings.ToListAsync();
        Assert.Equal(2, meetings.Count);
    }

    [Fact]
    public async Task UploadMeetingCsvs_DuplicateAttendant_ReusesExistingRecord()
    {
        // Arrange
        var csv1 = @"Meeting 1,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45";

        var csv2 = @"Meeting 2,2024-01-16
Name,Email,Duration
John Doe,john@example.com,60";

        var file1 = CreateFormFile(csv1, "meeting1.csv", "text/csv");
        var file2 = CreateFormFile(csv2, "meeting2.csv", "text/csv");
        var files = new List<IFormFile> { file1, file2 };

        // Act
        var result = await _controller.UploadMeetingCsvs(files);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var attendants = await _context.Attendants.ToListAsync();
        Assert.Single(attendants); // Should only create one attendant record
        Assert.Equal("john@example.com", attendants[0].Email);
        
        var attendances = await _context.MeetingAttendances.ToListAsync();
        Assert.Equal(2, attendances.Count); // But should have 2 attendance records
    }

    [Fact]
    public async Task UploadMeetingCsvs_InvalidMimeType_ReturnsBadRequest()
    {
        // Arrange
        var content = "Not a CSV";
        var file = CreateFormFile(content, "file.txt", "application/json");
        var files = new List<IFormFile> { file };

        // Act
        var result = await _controller.UploadMeetingCsvs(files);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAllSummaries_NoSummaries_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetAllSummaries();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Access the anonymous object value
        var responseValue = okResult.Value;
        var summariesProperty = responseValue?.GetType().GetProperty("summaries");
        var summaries = summariesProperty?.GetValue(responseValue) as IEnumerable<object>;
        
        Assert.NotNull(summaries);
        Assert.Empty(summaries);
    }

    [Fact]
    public async Task GetAllSummaries_WithSummaries_ReturnsList()
    {
        // Arrange
        var csv = @"Meeting 1,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45";

        var file = CreateFormFile(csv, "meeting.csv", "text/csv");
        await _controller.UploadMeetingCsvs(new List<IFormFile> { file });

        // Act
        var result = await _controller.GetAllSummaries();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Access the anonymous object value
        var responseValue = okResult.Value;
        var summariesProperty = responseValue?.GetType().GetProperty("summaries");
        var summaries = summariesProperty?.GetValue(responseValue) as IEnumerable<object>;
        
        Assert.NotNull(summaries);
        Assert.Single(summaries);
    }

    [Fact]
    public async Task GetSummaryById_ValidId_ReturnsSummary()
    {
        // Arrange
        var csv = @"Meeting 1,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45";

        var file = CreateFormFile(csv, "meeting.csv", "text/csv");
        await _controller.UploadMeetingCsvs(new List<IFormFile> { file });

        // Act
        var result = await _controller.GetSummaryById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSummaryById_InvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetSummaryById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
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
