using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

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
    /// <summary>
    /// Parses a meeting CSV file from a stream
    /// </summary>
    /// <param name="stream">The stream containing CSV data</param>
    /// <returns>Parsed meeting data</returns>
    public static async Task<CsvMeetingData> ParseMeetingCsvAsync(Stream stream)
    {
        // Detect encoding
        var encoding = DetectEncoding(stream);
        stream.Position = 0;
        
        using var reader = new StreamReader(stream, encoding);
        var content = await reader.ReadToEndAsync();
        
        // Reset stream for potential reuse
        if (stream.CanSeek)
            stream.Position = 0;
        
        // Try to detect format type
        if (IsMicrosoftTeamsFormat(content))
        {
            return ParseMicrosoftTeamsFormat(content);
        }
        else
        {
            return ParseSimpleFormat(content);
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
        return content.Contains("1. Summary") || 
               content.Contains("2. Participants") || 
               (content.Contains("Meeting title\t") && content.Contains("Participant ID (UPN)"));
    }

    private static CsvMeetingData ParseMicrosoftTeamsFormat(string content)
    {
        var meetingData = new CsvMeetingData();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Parse title from "Meeting title" row
        var titleLine = lines.FirstOrDefault(l => l.StartsWith("Meeting title\t"));
        if (titleLine != null)
        {
            var parts = titleLine.Split('\t');
            if (parts.Length >= 2)
            {
                meetingData.Title = parts[1].Trim();
            }
        }
        
        // Parse date from "Start time" row
        var startTimeLine = lines.FirstOrDefault(l => l.StartsWith("Start time\t"));
        if (startTimeLine != null)
        {
            var parts = startTimeLine.Split('\t');
            if (parts.Length >= 2)
            {
                var dateStr = parts[1].Trim();
                if (DateTime.TryParse(dateStr, out var parsedDate))
                {
                    meetingData.Date = parsedDate;
                }
                else
                {
                    meetingData.Date = DateTime.Now;
                }
            }
        }
        
        // Find the "2. Participants" section
        var participantsSectionIdx = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("2. Participants"))
            {
                participantsSectionIdx = i;
                break;
            }
        }
        
        if (participantsSectionIdx >= 0 && participantsSectionIdx + 2 < lines.Length)
        {
            // The next line should be the header, skip it and parse data rows
            var headerIdx = participantsSectionIdx + 1;
            
            for (int i = headerIdx + 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Stop if we hit the next section
                if (line.StartsWith("3.") || string.IsNullOrWhiteSpace(line))
                    break;
                
                var parts = line.Split('\t');
                if (parts.Length >= 5)
                {
                    var name = parts[0].Trim();
                    var email = parts[4].Trim();
                    var durationStr = parts[3].Trim();
                    
                    var duration = ParseDuration(durationStr);
                    
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        meetingData.Attendees.Add(new CsvAttendeeRecord
                        {
                            Name = name,
                            Email = email,
                            Duration = duration
                        });
                    }
                }
            }
        }
        
        if (string.IsNullOrWhiteSpace(meetingData.Title))
        {
            meetingData.Title = "Untitled Meeting";
        }
        
        if (meetingData.Date == default)
        {
            meetingData.Date = DateTime.Now;
        }
        
        return meetingData;
    }

    private static CsvMeetingData ParseSimpleFormat(string content)
    {
        var meetingData = new CsvMeetingData();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 3)
            throw new InvalidOperationException("CSV file does not contain enough data");

        // Parse title and date from first line (expected format: "Meeting Title,Date")
        var titleLine = lines[0].Trim();
        var titleParts = titleLine.Split(',');
        
        if (titleParts.Length >= 2)
        {
            meetingData.Title = titleParts[0].Trim().Trim('"');
            
            // Try to parse date from various formats
            var dateString = titleParts[1].Trim().Trim('"');
            if (DateTime.TryParse(dateString, out var parsedDate))
            {
                meetingData.Date = parsedDate;
            }
            else
            {
                meetingData.Date = DateTime.Now;
            }
        }
        else
        {
            meetingData.Title = titleLine.Trim('"');
            meetingData.Date = DateTime.Now;
        }

        // Parse attendees starting from line 3 (skip title at line 1 and header at line 2)
        using var csvReader = new StringReader(string.Join('\n', lines.Skip(2)));
        using var csv = new CsvReader(csvReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        while (csv.Read())
        {
            try
            {
                var name = csv.GetField<string>(0)?.Trim() ?? "";
                var email = csv.GetField<string>(1)?.Trim() ?? "";
                var durationStr = csv.GetField<string>(2)?.Trim() ?? "0";

                // Parse duration (assuming format like "30 min" or just "30")
                var duration = ParseDuration(durationStr);

                if (!string.IsNullOrWhiteSpace(email))
                {
                    meetingData.Attendees.Add(new CsvAttendeeRecord
                    {
                        Name = name,
                        Email = email,
                        Duration = duration
                    });
                }
            }
            catch
            {
                // Skip malformed rows
                continue;
            }
        }

        return meetingData;
    }

    private static TimeSpan ParseDuration(string durationStr)
    {
        if (string.IsNullOrWhiteSpace(durationStr))
            return TimeSpan.Zero;

        // Handle formats like "1m 13s"
        var match = System.Text.RegularExpressions.Regex.Match(durationStr, @"(?:(\d+)h)?\s*(?:(\d+)m)?\s*(?:(\d+)s)?");
        if (match.Success)
        {
            int hours = 0, minutes = 0, seconds = 0;
            
            if (!string.IsNullOrEmpty(match.Groups[1].Value))
                hours = int.Parse(match.Groups[1].Value);
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
                minutes = int.Parse(match.Groups[2].Value);
            if (!string.IsNullOrEmpty(match.Groups[3].Value))
                seconds = int.Parse(match.Groups[3].Value);
            
            if (hours > 0 || minutes > 0 || seconds > 0)
                return new TimeSpan(hours, minutes, seconds);
        }

        // Remove common time unit suffixes
        durationStr = durationStr.ToLower()
            .Replace("min", "")
            .Replace("mins", "")
            .Replace("minutes", "")
            .Replace("minute", "")
            .Trim();

        if (int.TryParse(durationStr, out var minutesOnly))
        {
            return TimeSpan.FromMinutes(minutesOnly);
        }

        // Try to parse as TimeSpan directly
        if (TimeSpan.TryParse(durationStr, out var timeSpan))
        {
            return timeSpan;
        }

        return TimeSpan.Zero;
    }
}
