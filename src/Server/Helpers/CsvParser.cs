using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Helpers;

/// <summary>
/// Represents meeting data parsed from CSV
/// </summary>
public class CsvMeetingData
{
    /// <summary>
    /// Gets or sets the meeting title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the meeting date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the list of attendees
    /// </summary>
    public List<CsvAttendeeRecord> Attendees { get; set; } = new();
}

/// <summary>
/// Represents the result of CSV parsing with validation details
/// </summary>
public class CsvParseResult
{
    public bool Success { get; set; }
    public CsvMeetingData? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents an attendee record from CSV
/// </summary>
public class CsvAttendeeRecord
{
    /// <summary>
    /// Gets or sets the attendee name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attendee email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of attendance
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Static helper class for parsing CSV meeting files
/// </summary>
public static class CsvParser
{
    // Error codes
    private const string ErrorCodeEmptyContent = "EMPTY_CONTENT";
    private const string ErrorCodeParseError = "PARSE_ERROR";
    private const string ErrorCodeMissingSummary = "MISSING_SUMMARY_SECTION";
    private const string ErrorCodeMissingParticipants = "MISSING_PARTICIPANTS_SECTION";
    private const string ErrorCodeMissingTitle = "MISSING_MEETING_TITLE";
    private const string ErrorCodeInvalidDate = "INVALID_DATE_FORMAT";
    private const string ErrorCodeMissingStartTime = "MISSING_START_TIME";
    private const string ErrorCodeInvalidHeader = "INVALID_HEADER";
    private const string ErrorCodeInvalidEmailFormat = "INVALID_EMAIL_FORMAT";
    private const string ErrorCodeNoAttendees = "NO_ATTENDEES";
    private const string ErrorCodeInsufficientData = "INSUFFICIENT_DATA";
    private const string ErrorCodeMissingTitleField = "MISSING_TITLE";

    // Section markers for Teams format
    private const string TeamsSummarySectionMarker = "1. Summary";
    private const string TeamsParticipantsSectionMarker = "2. Participants";
    private const string TeamsNextSectionMarker = "3.";

    // Field names for Teams format
    private const string TeamsMeetingTitleField = "Meeting title\t";
    private const string TeamsStartTimeField = "Start time\t";
    private const string TeamsParticipantIdField = "Participant ID (UPN)";

    // Required fields for Teams format
    private static readonly string[] TeamsRequiredFields = { "Name", "Email", "In-Meeting Duration" };

    // Required fields for simple format (case-insensitive)
    private const string SimpleFormatNameField = "name";
    private const string SimpleFormatEmailField = "email";

    // Minimum line count for simple format
    private const int MinimumLinesForSimpleFormat = 3;

    // Duration parsing regex pattern
    private const string DurationRegexPattern = @"(?:(\d+)h)?\s*(?:(\d+)m)?\s*(?:(\d+)s)?";

    // More comprehensive email regex that matches most valid email formats
    // Based on simplified RFC 5322 compliant pattern
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses a meeting CSV file from a stream
    /// </summary>
    /// <param name="stream">The stream containing CSV data</param>
    /// <returns>Parsed meeting data</returns>
    public static async Task<CsvMeetingData> ParseMeetingCsvAsync(Stream stream)
    {
        var result = await ParseAndValidateMeetingCsvAsync(stream);
        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Failed to parse CSV");
        }
        return result.Data!;
    }

    /// <summary>
    /// Parses and validates a meeting CSV file with detailed error reporting
    /// </summary>
    /// <param name="stream">The stream containing CSV data</param>
    /// <returns>Parse result with validation details</returns>
    public static async Task<CsvParseResult> ParseAndValidateMeetingCsvAsync(Stream stream)
    {
        try
        {
            var encoding = DetectEncoding(stream);
            stream.Position = 0;
            
            using var reader = new StreamReader(stream, encoding);
            var content = await reader.ReadToEndAsync();
            
            ResetStreamPosition(stream);
            
            if (string.IsNullOrWhiteSpace(content))
            {
                return CreateErrorResult(ErrorCodeEmptyContent, "CSV file is empty or contains only whitespace");
            }
            
            return IsMicrosoftTeamsFormat(content) 
                ? ParseMicrosoftTeamsFormat(content) 
                : ParseSimpleFormat(content);
        }
        catch (Exception ex)
        {
            return CreateErrorResult(ErrorCodeParseError, $"Error parsing CSV: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an error parse result
    /// </summary>
    private static CsvParseResult CreateErrorResult(string errorCode, string errorMessage)
    {
        return new CsvParseResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a success parse result
    /// </summary>
    private static CsvParseResult CreateSuccessResult(CsvMeetingData data)
    {
        return new CsvParseResult
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// Resets stream position if seeking is supported
    /// </summary>
    private static void ResetStreamPosition(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    private static Encoding DetectEncoding(Stream stream)
    {
        var buffer = new byte[4];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        stream.Position = 0;

        if (bytesRead >= 2)
        {
            // Check for UTF-16 LE BOM
            if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                return Encoding.Unicode;
            
            // Check for UTF-16 BE BOM
            if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                return Encoding.BigEndianUnicode;
        }

        if (bytesRead >= 3)
        {
            // Check for UTF-8 BOM
            if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                return Encoding.UTF8;
        }

        // Default to UTF-8
        return Encoding.UTF8;
    }

    private static bool IsMicrosoftTeamsFormat(string content)
    {
        // Microsoft Teams format has sections like "1. Summary", "2. Participants", etc.
        return content.Contains(TeamsSummarySectionMarker) || 
               content.Contains(TeamsParticipantsSectionMarker) || 
               (content.Contains(TeamsMeetingTitleField) && content.Contains(TeamsParticipantIdField));
    }

    private static CsvParseResult ParseMicrosoftTeamsFormat(string content)
    {
        var meetingData = new CsvMeetingData();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Validate required sections
        var sectionValidation = ValidateTeamsSections(content);
        if (!sectionValidation.Success)
        {
            return sectionValidation;
        }

        // Parse meeting title
        var titleResult = ParseTeamsMeetingTitle(lines);
        if (!titleResult.Success)
        {
            return titleResult;
        }
        meetingData.Title = titleResult.Data!.Title;

        // Parse meeting date
        var dateResult = ParseTeamsStartTime(lines);
        if (!dateResult.Success)
        {
            return dateResult;
        }
        meetingData.Date = dateResult.Data!.Date;

        // Parse participants
        var participantsResult = ParseTeamsParticipants(lines);
        if (!participantsResult.Success)
        {
            return participantsResult;
        }
        meetingData.Attendees = participantsResult.Data!.Attendees;

        return CreateSuccessResult(meetingData);
    }

    /// <summary>
    /// Validates that required Teams sections exist
    /// </summary>
    private static CsvParseResult ValidateTeamsSections(string content)
    {
        if (!content.Contains(TeamsSummarySectionMarker))
        {
            return CreateErrorResult(ErrorCodeMissingSummary, "Teams CSV must contain '1. Summary' section");
        }

        if (!content.Contains(TeamsParticipantsSectionMarker))
        {
            return CreateErrorResult(ErrorCodeMissingParticipants, "Teams CSV must contain '2. Participants' section");
        }

        return new CsvParseResult { Success = true };
    }

    /// <summary>
    /// Parses the meeting title from Teams format
    /// </summary>
    private static CsvParseResult ParseTeamsMeetingTitle(string[] lines)
    {
        var titleLine = lines.FirstOrDefault(l => l.StartsWith(TeamsMeetingTitleField));
        if (titleLine == null)
        {
            return CreateErrorResult(ErrorCodeMissingTitle, "Teams CSV must contain a valid 'Meeting title' field");
        }

        var parts = titleLine.Split('\t');
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            return CreateErrorResult(ErrorCodeMissingTitle, "Teams CSV must contain a valid 'Meeting title' field");
        }

        return CreateSuccessResult(new CsvMeetingData { Title = parts[1].Trim() });
    }

    /// <summary>
    /// Parses the start time from Teams format
    /// </summary>
    private static CsvParseResult ParseTeamsStartTime(string[] lines)
    {
        var startTimeLine = lines.FirstOrDefault(l => l.StartsWith(TeamsStartTimeField));
        if (startTimeLine == null)
        {
            return CreateErrorResult(ErrorCodeMissingStartTime, "Teams CSV must contain 'Start time' field");
        }

        var parts = startTimeLine.Split('\t');
        if (parts.Length < 2)
        {
            return CreateErrorResult(ErrorCodeMissingStartTime, "Teams CSV must contain 'Start time' field");
        }

        var dateStr = parts[1].Trim();
        if (!DateTime.TryParse(dateStr, out var parsedDate))
        {
            return CreateErrorResult(ErrorCodeInvalidDate, $"Invalid date format in 'Start time' field: {dateStr}");
        }

        return CreateSuccessResult(new CsvMeetingData { Date = parsedDate });
    }

    /// <summary>
    /// Parses participants from Teams format
    /// </summary>
    private static CsvParseResult ParseTeamsParticipants(string[] lines)
    {
        var participantsSectionIdx = FindParticipantsSection(lines);
        if (participantsSectionIdx < 0)
        {
            return CreateErrorResult(ErrorCodeMissingParticipants, "Teams CSV must contain '2. Participants' section");
        }

        if (participantsSectionIdx + 2 >= lines.Length)
        {
            return CreateErrorResult(ErrorCodeNoAttendees, "Teams CSV must contain participant data");
        }

        var headerIdx = participantsSectionIdx + 1;
        var headerValidation = ValidateTeamsHeader(lines[headerIdx]);
        if (!headerValidation.Success)
        {
            return headerValidation;
        }

        var attendees = new List<CsvAttendeeRecord>();
        var parseResult = ParseTeamsAttendeeRows(lines, headerIdx, attendees);
        if (!parseResult.Success)
        {
            return parseResult;
        }

        if (attendees.Count == 0)
        {
            return CreateErrorResult(ErrorCodeNoAttendees, "Teams CSV must contain at least one valid attendee with email address");
        }

        return CreateSuccessResult(new CsvMeetingData { Attendees = attendees });
    }

    /// <summary>
    /// Finds the participants section index in lines array
    /// </summary>
    private static int FindParticipantsSection(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith(TeamsParticipantsSectionMarker))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Validates Teams format header contains required fields
    /// </summary>
    private static CsvParseResult ValidateTeamsHeader(string headerLine)
    {
        foreach (var field in TeamsRequiredFields)
        {
            if (!headerLine.Contains(field))
            {
                return CreateErrorResult(ErrorCodeInvalidHeader, $"Participants section header must contain '{field}' field");
            }
        }
        return new CsvParseResult { Success = true };
    }

    /// <summary>
    /// Parses attendee rows from Teams format
    /// </summary>
    private static CsvParseResult ParseTeamsAttendeeRows(string[] lines, int headerIdx, List<CsvAttendeeRecord> attendees)
    {
        for (int i = headerIdx + 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // Stop if we hit the next section
            if (line.StartsWith(TeamsNextSectionMarker) || string.IsNullOrWhiteSpace(line))
                break;
            
            var parts = line.Split('\t');
            if (parts.Length < 5)
                continue;

            var name = parts[0].Trim();
            var email = parts[4].Trim();
            var durationStr = parts[3].Trim();
            
            // Validate email format
            if (!string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
            {
                return CreateErrorResult(ErrorCodeInvalidEmailFormat, $"Invalid email format: {email}");
            }
            
            if (!string.IsNullOrWhiteSpace(email))
            {
                attendees.Add(new CsvAttendeeRecord
                {
                    Name = name,
                    Email = email,
                    Duration = ParseDuration(durationStr)
                });
            }
        }

        return new CsvParseResult { Success = true };
    }

    private static CsvParseResult ParseSimpleFormat(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < MinimumLinesForSimpleFormat)
        {
            return CreateErrorResult(ErrorCodeInsufficientData, 
                "CSV file does not contain enough data (minimum 3 lines required: title, header, and at least one attendee)");
        }

        var meetingData = new CsvMeetingData();

        // Parse title and date from first line
        var titleResult = ParseSimpleFormatTitleAndDate(lines[0]);
        if (!titleResult.Success)
        {
            return titleResult;
        }
        meetingData.Title = titleResult.Data!.Title;
        meetingData.Date = titleResult.Data.Date;

        // Validate header
        var headerValidation = ValidateSimpleFormatHeader(lines[1]);
        if (!headerValidation.Success)
        {
            return headerValidation;
        }

        // Parse attendees
        var attendeesResult = ParseSimpleFormatAttendees(lines);
        if (!attendeesResult.Success)
        {
            return attendeesResult;
        }
        meetingData.Attendees = attendeesResult.Data!.Attendees;

        return CreateSuccessResult(meetingData);
    }

    /// <summary>
    /// Parses title and date from simple format first line
    /// </summary>
    private static CsvParseResult ParseSimpleFormatTitleAndDate(string titleLine)
    {
        var trimmedLine = titleLine.Trim();
        var titleParts = trimmedLine.Split(',');
        
        var title = titleParts.Length >= 1 ? titleParts[0].Trim().Trim('"') : trimmedLine.Trim('"');
        var date = DateTime.Now;

        if (titleParts.Length >= 2)
        {
            var dateString = titleParts[1].Trim().Trim('"');
            if (!DateTime.TryParse(dateString, out date))
            {
                return CreateErrorResult(ErrorCodeInvalidDate, $"Invalid date format: {dateString}");
            }
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return CreateErrorResult(ErrorCodeMissingTitleField, "CSV must contain a valid meeting title");
        }

        return CreateSuccessResult(new CsvMeetingData { Title = title, Date = date });
    }

    /// <summary>
    /// Validates simple format header
    /// </summary>
    private static CsvParseResult ValidateSimpleFormatHeader(string headerLine)
    {
        var header = headerLine.Trim().ToLower();
        if (!header.Contains(SimpleFormatNameField) || !header.Contains(SimpleFormatEmailField))
        {
            return CreateErrorResult(ErrorCodeInvalidHeader, "CSV header must contain 'Name' and 'Email' fields");
        }
        return new CsvParseResult { Success = true };
    }

    /// <summary>
    /// Parses attendees from simple format
    /// </summary>
    private static CsvParseResult ParseSimpleFormatAttendees(string[] lines)
    {
        var attendees = new List<CsvAttendeeRecord>();

        using var csvReader = new StringReader(string.Join('\n', lines.Skip(2)));
        using var csv = new CsvReader(csvReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        while (csv.Read())
        {
            if (!TryParseSimpleAttendeeRow(csv, out var attendee, out var error))
            {
                if (error != null)
                {
                    return error;
                }
                // Skip malformed rows but continue
                continue;
            }

            if (attendee != null)
            {
                attendees.Add(attendee);
            }
        }

        if (attendees.Count == 0)
        {
            return CreateErrorResult(ErrorCodeNoAttendees, "CSV must contain at least one valid attendee with email address");
        }

        return CreateSuccessResult(new CsvMeetingData { Attendees = attendees });
    }

    /// <summary>
    /// Tries to parse a single attendee row from simple format
    /// </summary>
    private static bool TryParseSimpleAttendeeRow(CsvReader csv, out CsvAttendeeRecord? attendee, out CsvParseResult? error)
    {
        attendee = null;
        error = null;

        try
        {
            var name = csv.GetField<string>(0)?.Trim() ?? "";
            var email = csv.GetField<string>(1)?.Trim() ?? "";
            var durationStr = csv.GetField<string>(2)?.Trim() ?? "0";

            // Validate email format
            if (!string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
            {
                error = CreateErrorResult(ErrorCodeInvalidEmailFormat, $"Invalid email format: {email}");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                attendee = new CsvAttendeeRecord
                {
                    Name = name,
                    Email = email,
                    Duration = ParseDuration(durationStr)
                };
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static TimeSpan ParseDuration(string durationStr)
    {
        if (string.IsNullOrWhiteSpace(durationStr))
            return TimeSpan.Zero;

        // Try parsing duration with time units (e.g., "1h 13m 5s")
        var durationWithUnits = TryParseDurationWithUnits(durationStr);
        if (durationWithUnits.HasValue)
            return durationWithUnits.Value;

        // Try parsing as minutes only
        var minutesOnly = TryParseMinutesOnly(durationStr);
        if (minutesOnly.HasValue)
            return minutesOnly.Value;

        // Try to parse as TimeSpan directly
        if (TimeSpan.TryParse(durationStr, out var timeSpan))
            return timeSpan;

        return TimeSpan.Zero;
    }

    /// <summary>
    /// Tries to parse duration string with time units (h, m, s)
    /// </summary>
    private static TimeSpan? TryParseDurationWithUnits(string durationStr)
    {
        var match = Regex.Match(durationStr, DurationRegexPattern);
        if (!match.Success)
            return null;

        int hours = 0, minutes = 0, seconds = 0;
        
        if (!string.IsNullOrEmpty(match.Groups[1].Value))
            hours = int.Parse(match.Groups[1].Value);
        if (!string.IsNullOrEmpty(match.Groups[2].Value))
            minutes = int.Parse(match.Groups[2].Value);
        if (!string.IsNullOrEmpty(match.Groups[3].Value))
            seconds = int.Parse(match.Groups[3].Value);
        
        if (hours > 0 || minutes > 0 || seconds > 0)
            return new TimeSpan(hours, minutes, seconds);

        return null;
    }

    /// <summary>
    /// Tries to parse duration as minutes only
    /// </summary>
    private static TimeSpan? TryParseMinutesOnly(string durationStr)
    {
        // Remove common time unit suffixes
        var cleaned = durationStr.ToLower()
            .Replace("min", "")
            .Replace("mins", "")
            .Replace("minutes", "")
            .Replace("minute", "")
            .Trim();

        if (int.TryParse(cleaned, out var minutesOnly))
        {
            return TimeSpan.FromMinutes(minutesOnly);
        }

        return null;
    }
}
