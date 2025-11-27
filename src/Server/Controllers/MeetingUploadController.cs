using Microsoft.AspNetCore.Mvc;
using Server.Helpers;
using Server.Models;
using Server.Repositories.Interfaces;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeetingUploadController : ControllerBase
{
    private readonly IAttendantRepository _attendantRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingAttendanceRepository _attendanceRepository;
    private readonly ISummaryRepository _summaryRepository;

    public MeetingUploadController(
        IAttendantRepository attendantRepository,
        IMeetingRepository meetingRepository,
        IMeetingAttendanceRepository attendanceRepository,
        ISummaryRepository summaryRepository)
    {
        _attendantRepository = attendantRepository;
        _meetingRepository = meetingRepository;
        _attendanceRepository = attendanceRepository;
        _summaryRepository = summaryRepository;
    }

    [HttpPost]
    public async Task<IActionResult> UploadMeetingCsvs([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { error = "No files uploaded" });
        }

        try
        {
            var meetings = new List<Meeting>();
            var summaryData = new SummaryData();

            // Process each file
            foreach (var file in files)
            {
                // Validate MIME type
                if (!FileValidator.IsValidCsvMimeType(file.ContentType))
                {
                    return BadRequest(new { error = $"Invalid file type for {file.FileName}. Expected CSV." });
                }

                // Validate file content
                using var stream = file.OpenReadStream();
                if (!await FileValidator.IsValidCsvFileAsync(stream))
                {
                    return BadRequest(new { error = $"Invalid CSV file format for {file.FileName}" });
                }

                // Parse CSV
                stream.Position = 0;
                var meetingData = await CsvParser.ParseMeetingCsvAsync(stream);

                // Create meeting
                var meeting = new Meeting
                {
                    Title = meetingData.Title,
                    Date = meetingData.Date
                };

                meeting = await _meetingRepository.CreateAsync(meeting);
                meetings.Add(meeting);

                // Process attendees
                foreach (var attendeeRecord in meetingData.Attendees)
                {
                    // Find or create attendant
                    var attendant = await _attendantRepository.GetByEmailAsync(attendeeRecord.Email);
                    if (attendant == null)
                    {
                        attendant = await _attendantRepository.CreateAsync(new Attendant
                        {
                            Email = attendeeRecord.Email,
                            Name = attendeeRecord.Name
                        });
                    }

                    // Create attendance record
                    await _attendanceRepository.CreateAsync(new MeetingAttendance
                    {
                        MeetingId = meeting.Id,
                        AttendantId = attendant.Id,
                        Duration = attendeeRecord.Duration
                    });

                    // Update summary data
                    var meetingKey = $"{meeting.Title} ({meeting.Date:yyyy-MM-dd})";
                    
                    if (!summaryData.MeetingHeaders.Contains(meetingKey))
                    {
                        summaryData.MeetingHeaders.Add(meetingKey);
                    }

                    if (!summaryData.AttendantEmails.Contains(attendant.Email))
                    {
                        summaryData.AttendantEmails.Add(attendant.Email);
                    }

                    if (!summaryData.AttendanceMatrix.ContainsKey(attendant.Email))
                    {
                        summaryData.AttendanceMatrix[attendant.Email] = new Dictionary<string, TimeSpan>();
                    }

                    if (!summaryData.AttendanceMatrix[attendant.Email].ContainsKey(meetingKey))
                    {
                        summaryData.AttendanceMatrix[attendant.Email][meetingKey] = TimeSpan.Zero;
                    }

                    summaryData.AttendanceMatrix[attendant.Email][meetingKey] += attendeeRecord.Duration;
                }
            }

            // Generate HTML summary
            var htmlTable = HtmlSummaryGenerator.GenerateHtmlTable(summaryData);

            // Generate XLSX
            var xlsxData = XlsxGenerator.GenerateXlsxFromSummary(summaryData);

            // Create summary record
            var summary = await _summaryRepository.CreateAsync(new Summary
            {
                CreatedAt = DateTime.UtcNow,
                HtmlTable = htmlTable,
                XlsxData = xlsxData
            });

            // Update meetings with summary reference
            foreach (var meeting in meetings)
            {
                meeting.SummaryId = summary.Id;
            }

            return Ok(new
            {
                summaryId = summary.Id,
                htmlTable = htmlTable
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
