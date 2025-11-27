using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Server.Helpers;

public class CsvMeetingData
{
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<CsvAttendeeRecord> Attendees { get; set; } = new();
}

public class CsvAttendeeRecord
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

public static class CsvParser
{
    public static async Task<CsvMeetingData> ParseMeetingCsvAsync(Stream stream)
    {
        var meetingData = new CsvMeetingData();
        
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        
        // Reset stream for potential reuse
        if (stream.CanSeek)
            stream.Position = 0;
        
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

        while (await csv.ReadAsync())
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
        // Remove common time unit suffixes
        durationStr = durationStr.ToLower()
            .Replace("min", "")
            .Replace("mins", "")
            .Replace("minutes", "")
            .Replace("minute", "")
            .Trim();

        if (int.TryParse(durationStr, out var minutes))
        {
            return TimeSpan.FromMinutes(minutes);
        }

        // Try to parse as TimeSpan directly
        if (TimeSpan.TryParse(durationStr, out var timeSpan))
        {
            return timeSpan;
        }

        return TimeSpan.Zero;
    }
}
