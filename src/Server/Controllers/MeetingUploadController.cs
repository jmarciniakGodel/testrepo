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
            return BadRequest(new { error = "No files uploaded" });
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
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all summaries with pagination and optional filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10)</param>
    /// <param name="search">Optional search query</param>
    /// <returns>Paginated list of summaries</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllSummaries([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var (summaries, totalCount) = await _summaryService.GetPagedSummariesAsync(page, pageSize, search);
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
