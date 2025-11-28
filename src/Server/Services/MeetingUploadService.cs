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
    // Log messages
    private const string LogStartingValidation = "Starting validation of {FileCount} files";
    private const string LogValidatingFile = "Validating file: {FileName}, Extension: {Extension}, ContentType: {ContentType}";
    private const string LogInvalidMimeType = "Invalid MIME type for {FileName}: {ContentType}";
    private const string LogValidationFailed = "File validation failed for {FileName}: Code={ErrorCode}, Message={ErrorMessage}, DetectedType={DetectedType}, OriginalExtension={OriginalExtension}";
    private const string LogPassedContentValidation = "File {FileName} passed content validation. Detected type: {DetectedType}";
    private const string LogCsvParsingFailed = "CSV parsing failed for {FileName}: Code={ErrorCode}, Message={ErrorMessage}";
    private const string LogPassedCsvValidation = "File {FileName} passed CSV validation. Title: {Title}, Attendees: {AttendeeCount}";
    private const string LogAllFilesPassedValidation = "All files passed validation. Processing data...";
    private const string LogCreatedMeeting = "Created meeting: {MeetingId} - {MeetingTitle}";
    private const string LogCreatedAttendant = "Created new attendant: {Email}";
    private const string LogCreatedSummary = "Created summary: {SummaryId}";
    private const string LogTransactionCommitted = "Transaction committed successfully. Summary ID: {SummaryId}";
    private const string LogProcessingCompletedNoTransaction = "Processing completed successfully (no transaction support). Summary ID: {SummaryId}";
    private const string LogTransactionRolledBack = "Transaction rolled back due to error during file processing";
    private const string LogErrorNoTransaction = "Error during file processing (no transaction support)";

    // Error messages
    private const string ErrorInvalidFileType = "Invalid file type for {0}. Expected CSV, got {1}.";
    private const string ErrorFileValidationFailed = "File validation failed for {0}: {1} (Error Code: {2}, Detected Type: {3})";
    private const string ErrorCsvValidationFailed = "CSV validation failed for {0}: {1} (Error Code: {2})";

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
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => await ProcessFilesWithTransactionAsync(files));
    }

    /// <summary>
    /// Processes files within a transaction context
    /// </summary>
    private async Task<(int SummaryId, string HtmlTable)> ProcessFilesWithTransactionAsync(IEnumerable<IFormFile> files)
    {
        var transaction = await TryBeginTransactionAsync();
        
        try
        {
            var filesList = files.ToList();
            var parsedFiles = new Dictionary<string, CsvMeetingData>();

            // Phase 1: Validate ALL files first (fail-fast approach)
            await ValidateAllFilesAsync(filesList, parsedFiles);

            _logger.LogInformation(LogAllFilesPassedValidation);

            // Phase 2: Process validated files
            var summaryData = new SummaryData();
            var meetings = await ProcessValidatedFilesAsync(filesList, parsedFiles, summaryData);

            // Generate summary and Excel
            var (summaryId, htmlTable) = await CreateSummaryAsync(summaryData, meetings);

            // Commit transaction
            await CommitTransactionAsync(transaction, summaryId);

            return (summaryId, htmlTable);
        }
        catch (Exception ex)
        {
            await RollbackTransactionAsync(transaction, ex);
            throw;
        }
    }

    /// <summary>
    /// Attempts to begin a database transaction
    /// </summary>
    private async Task<IDbContextTransaction?> TryBeginTransactionAsync()
    {
        try
        {
            if (_dbContext.Database.IsRelational())
            {
                return await _dbContext.Database.BeginTransactionAsync();
            }
        }
        catch (InvalidOperationException)
        {
            // Transaction not supported by database provider
        }
        catch (NotSupportedException)
        {
            // Transaction not supported by database provider
        }
        
        return null;
    }

    /// <summary>
    /// Validates all uploaded files
    /// </summary>
    private async Task ValidateAllFilesAsync(List<IFormFile> filesList, Dictionary<string, CsvMeetingData> parsedFiles)
    {
        _logger.LogInformation(LogStartingValidation, filesList.Count);
        
        foreach (var file in filesList)
        {
            _logger.LogInformation(LogValidatingFile, 
                file.FileName, Path.GetExtension(file.FileName), file.ContentType);

            await ValidateSingleFileAsync(file, parsedFiles);
        }
    }

    /// <summary>
    /// Validates a single file
    /// </summary>
    private async Task ValidateSingleFileAsync(IFormFile file, Dictionary<string, CsvMeetingData> parsedFiles)
    {
        // Validate MIME type
        ValidateMimeType(file);

        // Validate file content
        using var validationStream = file.OpenReadStream();
        await ValidateFileContentAsync(file, validationStream);

        // Parse and validate CSV structure
        validationStream.Position = 0;
        var parseResult = await CsvParser.ParseAndValidateMeetingCsvAsync(validationStream);
        
        if (!parseResult.Success)
        {
            _logger.LogWarning(LogCsvParsingFailed, file.FileName, parseResult.ErrorCode, parseResult.ErrorMessage);
            throw new ArgumentException(string.Format(ErrorCsvValidationFailed, 
                file.FileName, parseResult.ErrorMessage, parseResult.ErrorCode));
        }

        // Cache parsed data
        parsedFiles[file.FileName] = parseResult.Data!;

        _logger.LogInformation(LogPassedCsvValidation, 
            file.FileName, parseResult.Data!.Title, parseResult.Data.Attendees.Count);
    }

    /// <summary>
    /// Validates file MIME type
    /// </summary>
    private void ValidateMimeType(IFormFile file)
    {
        if (!FileValidator.IsValidCsvMimeType(file.ContentType))
        {
            _logger.LogWarning(LogInvalidMimeType, file.FileName, file.ContentType);
            throw new ArgumentException(string.Format(ErrorInvalidFileType, file.FileName, file.ContentType));
        }
    }

    /// <summary>
    /// Validates file content
    /// </summary>
    private async Task ValidateFileContentAsync(IFormFile file, Stream validationStream)
    {
        var validationResult = await FileValidator.ValidateCsvFileAsync(validationStream, file.FileName);
        
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(LogValidationFailed,
                file.FileName, validationResult.ErrorCode, validationResult.ErrorMessage, 
                validationResult.DetectedType, validationResult.OriginalExtension);
            
            throw new ArgumentException(string.Format(ErrorFileValidationFailed,
                file.FileName, validationResult.ErrorMessage, 
                validationResult.ErrorCode, validationResult.DetectedType ?? "unknown"));
        }

        _logger.LogInformation(LogPassedContentValidation, 
            file.FileName, validationResult.DetectedType);
    }

    /// <summary>
    /// Processes validated files and creates meetings
    /// </summary>
    private async Task<List<Meeting>> ProcessValidatedFilesAsync(
        List<IFormFile> filesList, 
        Dictionary<string, CsvMeetingData> parsedFiles, 
        SummaryData summaryData)
    {
        var meetings = new List<Meeting>();

        foreach (var file in filesList)
        {
            var meetingData = parsedFiles[file.FileName];
            var meeting = await CreateMeetingAsync(meetingData);
            meetings.Add(meeting);

            _logger.LogInformation(LogCreatedMeeting, meeting.Id, meeting.Title);

            await ProcessAttendeesAsync(meeting, meetingData, summaryData);
        }

        return meetings;
    }

    /// <summary>
    /// Creates a meeting from parsed data
    /// </summary>
    private async Task<Meeting> CreateMeetingAsync(CsvMeetingData meetingData)
    {
        var meeting = new Meeting
        {
            Title = meetingData.Title,
            Date = meetingData.Date
        };

        return await _meetingRepository.CreateAsync(meeting);
    }

    /// <summary>
    /// Processes attendees for a meeting
    /// </summary>
    private async Task ProcessAttendeesAsync(Meeting meeting, CsvMeetingData meetingData, SummaryData summaryData)
    {
        foreach (var attendeeRecord in meetingData.Attendees)
        {
            var attendant = await GetOrCreateAttendantAsync(attendeeRecord);
            await CreateAttendanceRecordAsync(meeting.Id, attendant.Id, attendeeRecord.Duration);
            UpdateSummaryData(summaryData, meeting, attendant, attendeeRecord.Duration);
        }
    }

    /// <summary>
    /// Gets an existing attendant or creates a new one
    /// </summary>
    private async Task<Attendant> GetOrCreateAttendantAsync(CsvAttendeeRecord attendeeRecord)
    {
        var attendant = await _attendantRepository.GetByEmailAsync(attendeeRecord.Email);
        
        if (attendant == null)
        {
            attendant = await _attendantRepository.CreateAsync(new Attendant
            {
                Email = attendeeRecord.Email,
                Name = attendeeRecord.Name
            });
            _logger.LogInformation(LogCreatedAttendant, attendant.Email);
        }

        return attendant;
    }

    /// <summary>
    /// Creates an attendance record
    /// </summary>
    private async Task CreateAttendanceRecordAsync(int meetingId, int attendantId, TimeSpan duration)
    {
        await _attendanceRepository.CreateAsync(new MeetingAttendance
        {
            MeetingId = meetingId,
            AttendantId = attendantId,
            Duration = duration
        });
    }

    /// <summary>
    /// Updates summary data with attendance information
    /// </summary>
    private void UpdateSummaryData(SummaryData summaryData, Meeting meeting, Attendant attendant, TimeSpan duration)
    {
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

        summaryData.AttendanceMatrix[attendant.Email][meetingKey] += duration;
    }

    /// <summary>
    /// Creates a summary and links meetings to it
    /// </summary>
    private async Task<(int SummaryId, string HtmlTable)> CreateSummaryAsync(SummaryData summaryData, List<Meeting> meetings)
    {
        var htmlTable = HtmlSummaryGenerator.GenerateHtmlTable(summaryData);
        var xlsxData = XlsxGenerator.GenerateXlsxFromSummary(summaryData);

        var summary = await _summaryRepository.CreateAsync(new Summary
        {
            CreatedAt = DateTime.UtcNow,
            HtmlTable = htmlTable,
            XlsxData = xlsxData
        });

        _logger.LogInformation(LogCreatedSummary, summary.Id);

        await LinkMeetingsToSummaryAsync(meetings, summary.Id);

        return (summary.Id, htmlTable);
    }

    /// <summary>
    /// Links meetings to a summary
    /// </summary>
    private async Task LinkMeetingsToSummaryAsync(List<Meeting> meetings, int summaryId)
    {
        foreach (var meeting in meetings)
        {
            meeting.SummaryId = summaryId;
            await _meetingRepository.UpdateAsync(meeting);
        }
    }

    /// <summary>
    /// Commits the transaction if it exists
    /// </summary>
    private async Task CommitTransactionAsync(IDbContextTransaction? transaction, int summaryId)
    {
        if (transaction != null)
        {
            await transaction.CommitAsync();
            await transaction.DisposeAsync();
            _logger.LogInformation(LogTransactionCommitted, summaryId);
        }
        else
        {
            _logger.LogInformation(LogProcessingCompletedNoTransaction, summaryId);
        }
    }

    /// <summary>
    /// Rolls back the transaction if it exists
    /// </summary>
    private async Task RollbackTransactionAsync(IDbContextTransaction? transaction, Exception ex)
    {
        if (transaction != null)
        {
            await transaction.RollbackAsync();
            await transaction.DisposeAsync();
            _logger.LogError(ex, LogTransactionRolledBack);
        }
        else
        {
            _logger.LogError(ex, LogErrorNoTransaction);
        }
    }
}
