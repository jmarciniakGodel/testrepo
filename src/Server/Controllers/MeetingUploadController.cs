using Microsoft.AspNetCore.Mvc;
using Server.Services.Interfaces;

namespace Server.Controllers;

/// <summary>
/// API controller for handling meeting CSV file uploads and summary operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MeetingUploadController : ControllerBase
{
    // Error codes constants
    private const string ErrorCodeNoFiles = "NO_FILES";
    private const string ErrorCodeValidationError = "VALIDATION_ERROR";
    private const string ErrorCodeTypeMismatch = "TYPE_MISMATCH";
    private const string ErrorCodeEmptyFile = "EMPTY_FILE";
    private const string ErrorCodeNoAttendees = "NO_ATTENDEES";
    private const string ErrorCodeInvalidEmail = "INVALID_EMAIL";
    private const string ErrorCodeInvalidFormat = "INVALID_FORMAT";
    private const string ErrorCodeServerError = "SERVER_ERROR";

    // Error messages constants
    private const string ErrorMessageNoFiles = "No files uploaded";
    private const string HintSelectFiles = "Please select at least one CSV file to upload";
    private const string HintValidCsv = "Please ensure you're uploading valid Teams attendance CSV files.";
    private const string HintTypeMismatch = "The file extension does not match the actual file content. Please upload a genuine CSV file.";
    private const string HintEmptyFile = "The uploaded file is empty. Please provide a file with valid meeting data.";
    private const string HintNoAttendees = "The CSV file must contain at least one attendee with a valid email address.";
    private const string HintInvalidEmail = "One or more email addresses in the CSV are invalid. Please check the email format.";
    private const string HintInvalidFormat = "The CSV file does not have the required Teams attendance format. Please ensure all required fields are present.";
    private const string HintServerError = "An unexpected error occurred while processing the files.";

    private readonly IMeetingUploadService _meetingUploadService;
    private readonly ISummaryService _summaryService;

    /// <summary>
    /// Initializes a new instance of the MeetingUploadController
    /// </summary>
    /// <param name="meetingUploadService">The meeting upload service</param>
    /// <param name="summaryService">The summary service</param>
    public MeetingUploadController(
        IMeetingUploadService meetingUploadService,
        ISummaryService summaryService)
    {
        _meetingUploadService = meetingUploadService;
        _summaryService = summaryService;
    }

    /// <summary>
    /// Uploads and processes meeting CSV files
    /// </summary>
    /// <param name="files">Collection of CSV files to upload</param>
    /// <returns>Summary information including ID and HTML table</returns>
    [HttpPost]
    public async Task<IActionResult> UploadMeetingCsvs([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new 
            { 
                error = ErrorMessageNoFiles,
                errorCode = ErrorCodeNoFiles,
                hint = HintSelectFiles
            });
        }

        try
        {
            var (summaryId, htmlTable) = await _meetingUploadService.ProcessMeetingFilesAsync(files);

            return Ok(new
            {
                summaryId = summaryId,
                htmlTable = htmlTable
            });
        }
        catch (ArgumentException ex)
        {
            var (errorCode, hint) = ExtractErrorCodeAndHint(ex.Message);

            return BadRequest(new 
            { 
                error = ex.Message,
                errorCode = errorCode,
                hint = hint
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                error = ex.Message,
                errorCode = ErrorCodeServerError,
                hint = HintServerError
            });
        }
    }

    /// <summary>
    /// Extracts error code and hint from exception message
    /// </summary>
    /// <param name="message">The exception message</param>
    /// <returns>Tuple containing error code and hint</returns>
    private (string ErrorCode, string Hint) ExtractErrorCodeAndHint(string message)
    {
        if (message.Contains("TYPE_MISMATCH"))
        {
            return (ErrorCodeTypeMismatch, HintTypeMismatch);
        }

        if (message.Contains("EMPTY_FILE") || message.Contains("EMPTY_CONTENT"))
        {
            return (ErrorCodeEmptyFile, HintEmptyFile);
        }

        if (message.Contains("NO_ATTENDEES"))
        {
            return (ErrorCodeNoAttendees, HintNoAttendees);
        }

        if (message.Contains("INVALID_EMAIL"))
        {
            return (ErrorCodeInvalidEmail, HintInvalidEmail);
        }

        if (message.Contains("MISSING") || message.Contains("INVALID_HEADER"))
        {
            return (ErrorCodeInvalidFormat, HintInvalidFormat);
        }

        return (ErrorCodeValidationError, HintValidCsv);
    }

    /// <summary>
    /// Retrieves all summaries with pagination and optional filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 5)</param>
    /// <param name="search">Optional search query for meeting titles</param>
    /// <param name="number">Optional summary number to filter by</param>
    /// <param name="sortDesc">Sort by created date descending (default: true)</param>
    /// <returns>Paginated list of summaries</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllSummaries(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 5, 
        [FromQuery] string? search = null,
        [FromQuery] int? number = null,
        [FromQuery] bool sortDesc = true)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 5;

            var (summaries, totalCount) = await _summaryService.GetPagedSummariesAsync(page, pageSize, search, number, sortDesc);
            var summaryList = summaries.Select(s => new
            {
                id = s.Id,
                createdAt = s.CreatedAt,
                meetingCount = s.Meetings.Count,
                htmlTable = s.HtmlTable
            });

            return Ok(new
            {
                summaries = summaryList,
                page = page,
                pageSize = pageSize,
                totalCount = totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a specific summary by its identifier
    /// </summary>
    /// <param name="id">The summary identifier</param>
    /// <returns>Detailed summary information</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSummaryById(int id)
    {
        try
        {
            var summary = await _summaryService.GetSummaryByIdAsync(id);
            
            if (summary == null)
            {
                return NotFound(new { error = $"Summary with ID {id} not found" });
            }

            return Ok(new
            {
                id = summary.Id,
                createdAt = summary.CreatedAt,
                meetingCount = summary.Meetings.Count,
                htmlTable = summary.HtmlTable,
                meetings = summary.Meetings.Select(m => new
                {
                    id = m.Id,
                    title = m.Title,
                    date = m.Date,
                    attendeeCount = m.MeetingAttendances.Count
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Downloads the Excel file for a specific summary
    /// </summary>
    /// <param name="id">The summary identifier</param>
    /// <returns>Excel file as a downloadable attachment</returns>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadSummaryExcel(int id)
    {
        try
        {
            var result = await _summaryService.GetSummaryExcelAsync(id);
            
            if (result == null)
            {
                return NotFound(new { error = $"Summary with ID {id} not found" });
            }

            var (data, fileName) = result.Value;
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
