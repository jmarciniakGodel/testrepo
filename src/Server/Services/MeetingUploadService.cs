using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Server.Data;
using Server.Helpers;
using Server.Models;
using Server.Repositories.Interfaces;
using Server.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Server.Services;

/// <summary>
/// Service implementation for meeting upload business operations
/// </summary>
public class MeetingUploadService : IMeetingUploadService
{
    private readonly IAttendantRepository _attendantRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingAttendanceRepository _attendanceRepository;
    private readonly ISummaryRepository _summaryRepository;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MeetingUploadService> _logger;

    /// <summary>
    /// Initializes a new instance of the MeetingUploadService
    /// </summary>
    /// <param name="attendantRepository">The attendant repository</param>
    /// <param name="meetingRepository">The meeting repository</param>
    /// <param name="attendanceRepository">The meeting attendance repository</param>
    /// <param name="summaryRepository">The summary repository</param>
    /// <param name="dbContext">The database context for transaction management</param>
    /// <param name="logger">Logger instance</param>
    public MeetingUploadService(
        IAttendantRepository attendantRepository,
        IMeetingRepository meetingRepository,
        IMeetingAttendanceRepository attendanceRepository,
        ISummaryRepository summaryRepository,
        AppDbContext dbContext,
        ILogger<MeetingUploadService> logger)
    {
        _attendantRepository = attendantRepository;
        _meetingRepository = meetingRepository;
        _attendanceRepository = attendanceRepository;
        _summaryRepository = summaryRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Processes uploaded meeting CSV files and creates a summary
    /// </summary>
    /// <param name="files">Collection of CSV files to process</param>
    /// <returns>Tuple containing the summary ID and HTML table representation</returns>
    /// <exception cref="ArgumentException">Thrown when invalid file format is detected</exception>
    public async Task<(int SummaryId, string HtmlTable)> ProcessMeetingFilesAsync(IEnumerable<IFormFile> files)
    {
        // Start a transaction to ensure atomicity (if supported by database)
        // For InMemory database in tests, this will be ignored
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            // Try to start a transaction, but don't fail if not supported (e.g., InMemory database)
            IDbContextTransaction? transaction = null;
            try
            {
                if (_dbContext.Database.IsRelational())
                {
                    transaction = await _dbContext.Database.BeginTransactionAsync();
                }
            }
            catch (InvalidOperationException)
            {
                // Transaction not supported by database provider
                transaction = null;
            }
            catch (NotSupportedException)
            {
                // Transaction not supported by database provider
                transaction = null;
            }
            
            try
            {
                var meetings = new List<Meeting>();
                var summaryData = new SummaryData();
                var filesList = files.ToList();

                // Dictionary to cache parsed results to avoid double parsing
                var parsedFiles = new Dictionary<string, CsvMeetingData>();

                // Phase 1: Validate ALL files first (fail-fast approach)
                _logger.LogInformation("Starting validation of {FileCount} files", filesList.Count);
                
                foreach (var file in filesList)
                {
                    _logger.LogInformation("Validating file: {FileName}, Extension: {Extension}, ContentType: {ContentType}", 
                        file.FileName, Path.GetExtension(file.FileName), file.ContentType);

                    // Validate MIME type
                    if (!FileValidator.IsValidCsvMimeType(file.ContentType))
                    {
                        _logger.LogWarning("Invalid MIME type for {FileName}: {ContentType}", file.FileName, file.ContentType);
                        throw new ArgumentException($"Invalid file type for {file.FileName}. Expected CSV, got {file.ContentType}.");
                    }

                    // Validate file content with detailed error reporting
                    using var validationStream = file.OpenReadStream();
                    var validationResult = await FileValidator.ValidateCsvFileAsync(validationStream, file.FileName);
                    
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("File validation failed for {FileName}: Code={ErrorCode}, Message={ErrorMessage}, DetectedType={DetectedType}, OriginalExtension={OriginalExtension}",
                            file.FileName, validationResult.ErrorCode, validationResult.ErrorMessage, 
                            validationResult.DetectedType, validationResult.OriginalExtension);
                        
                        throw new ArgumentException(
                            $"File validation failed for {file.FileName}: {validationResult.ErrorMessage} " +
                            $"(Error Code: {validationResult.ErrorCode}, Detected Type: {validationResult.DetectedType ?? "unknown"})");
                    }

                    _logger.LogInformation("File {FileName} passed content validation. Detected type: {DetectedType}", 
                        file.FileName, validationResult.DetectedType);

                    // Parse and validate CSV structure and content (cache the result)
                    validationStream.Position = 0;
                    var parseResult = await CsvParser.ParseAndValidateMeetingCsvAsync(validationStream);
                    
                    if (!parseResult.Success)
                    {
                        _logger.LogWarning("CSV parsing failed for {FileName}: Code={ErrorCode}, Message={ErrorMessage}",
                            file.FileName, parseResult.ErrorCode, parseResult.ErrorMessage);
                        
                        throw new ArgumentException(
                            $"CSV validation failed for {file.FileName}: {parseResult.ErrorMessage} " +
                            $"(Error Code: {parseResult.ErrorCode})");
                    }

                    // Cache parsed data to avoid re-parsing
                    parsedFiles[file.FileName] = parseResult.Data!;

                    _logger.LogInformation("File {FileName} passed CSV validation. Title: {Title}, Attendees: {AttendeeCount}",
                        file.FileName, parseResult.Data!.Title, parseResult.Data.Attendees.Count);
                }

                _logger.LogInformation("All files passed validation. Processing data...");

                // Phase 2: Process files (only after ALL validations pass)
                foreach (var file in filesList)
                {
                    // Use cached parsed data (no need to re-parse)
                    var meetingData = parsedFiles[file.FileName];

                    // Create meeting
                    var meeting = new Meeting
                    {
                        Title = meetingData.Title,
                        Date = meetingData.Date
                    };

                    meeting = await _meetingRepository.CreateAsync(meeting);
                    meetings.Add(meeting);

                    _logger.LogInformation("Created meeting: {MeetingId} - {MeetingTitle}", meeting.Id, meeting.Title);

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
                            _logger.LogInformation("Created new attendant: {Email}", attendant.Email);
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

                _logger.LogInformation("Created summary: {SummaryId}", summary.Id);

                // Update meetings with summary reference
                foreach (var meeting in meetings)
                {
                    meeting.SummaryId = summary.Id;
                    await _meetingRepository.UpdateAsync(meeting);
                }

                // Commit transaction - all operations succeeded (if transaction is supported)
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                    await transaction.DisposeAsync();
                    _logger.LogInformation("Transaction committed successfully. Summary ID: {SummaryId}", summary.Id);
                }
                else
                {
                    _logger.LogInformation("Processing completed successfully (no transaction support). Summary ID: {SummaryId}", summary.Id);
                }

                return (summary.Id, htmlTable);
            }
            catch (Exception ex)
            {
                // Rollback transaction on any error (if transaction is supported)
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                    await transaction.DisposeAsync();
                    _logger.LogError(ex, "Transaction rolled back due to error during file processing");
                }
                else
                {
                    _logger.LogError(ex, "Error during file processing (no transaction support)");
                }
                
                // Re-throw the exception to propagate to controller
                throw;
            }
        });
    }
}
