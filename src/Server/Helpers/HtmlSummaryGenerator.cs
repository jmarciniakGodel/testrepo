using System.Text;

namespace Server.Helpers;

/// <summary>
/// Represents summary data for meeting attendances
/// </summary>
public class SummaryData
{
    /// <summary>
    /// Gets or sets the attendance matrix mapping attendant emails to meeting attendance durations
    /// </summary>
    public Dictionary<string, Dictionary<string, TimeSpan>> AttendanceMatrix { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of meeting headers
    /// </summary>
    public List<string> MeetingHeaders { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of attendant email addresses
    /// </summary>
    public List<string> AttendantEmails { get; set; } = new();
}

/// <summary>
/// Static helper class for generating HTML summary tables
/// </summary>
public static class HtmlSummaryGenerator
{
    // HTML constants
    private const string TableOpenTag = "<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; font-family: Arial, sans-serif;'>";
    private const string TableCloseTag = "</table>";
    private const string TheadOpenTag = "<thead>";
    private const string TheadCloseTag = "</thead>";
    private const string TbodyOpenTag = "<tbody>";
    private const string TbodyCloseTag = "</tbody>";
    private const string TrOpenTag = "<tr>";
    private const string TrCloseTag = "</tr>";
    private const string HeaderRowOpenTag = "<tr style='background-color: #4CAF50; color: white;'>";
    private const string ThOpenTag = "<th>";
    private const string ThCloseTag = "</th>";
    private const string TdOpenTag = "<td>";
    private const string TdCloseTag = "</td>";
    private const string TdCenterOpenTag = "<td style='text-align: center;'>";

    // Duration format constants
    private const string EmptyDurationPlaceholder = "-";
    private const string MinutesFormat = "{0} min";
    private const string HoursFormat = "{0} hr";
    private const string HoursMinutesFormat = "{0} hr {1} min";
    private const int MinutesPerHour = 60;

    // Header text
    private const string AttendantEmailHeader = "Attendant Email";

    /// <summary>
    /// Generates an HTML table from summary data
    /// </summary>
    /// <param name="data">The summary data to convert to HTML</param>
    /// <returns>HTML string representation of the summary table</returns>
    public static string GenerateHtmlTable(SummaryData data)
    {
        var html = new StringBuilder();
        
        html.Append(TableOpenTag);
        AppendTableHeader(html, data);
        AppendTableBody(html, data);
        html.Append(TableCloseTag);
        
        return html.ToString();
    }

    /// <summary>
    /// Appends the table header section
    /// </summary>
    private static void AppendTableHeader(StringBuilder html, SummaryData data)
    {
        html.Append(TheadOpenTag);
        html.Append(HeaderRowOpenTag);
        html.Append(ThOpenTag).Append(AttendantEmailHeader).Append(ThCloseTag);
        
        foreach (var meeting in data.MeetingHeaders)
        {
            html.Append(ThOpenTag)
                .Append(System.Net.WebUtility.HtmlEncode(meeting))
                .Append(ThCloseTag);
        }
        
        html.Append(TrCloseTag);
        html.Append(TheadCloseTag);
    }

    /// <summary>
    /// Appends the table body section
    /// </summary>
    private static void AppendTableBody(StringBuilder html, SummaryData data)
    {
        html.Append(TbodyOpenTag);
        
        foreach (var email in data.AttendantEmails)
        {
            AppendAttendantRow(html, email, data);
        }
        
        html.Append(TbodyCloseTag);
    }

    /// <summary>
    /// Appends a single attendant row
    /// </summary>
    private static void AppendAttendantRow(StringBuilder html, string email, SummaryData data)
    {
        html.Append(TrOpenTag);
        html.Append(TdOpenTag)
            .Append(System.Net.WebUtility.HtmlEncode(email))
            .Append(TdCloseTag);
        
        foreach (var meeting in data.MeetingHeaders)
        {
            var duration = GetAttendanceDuration(data, email, meeting);
            var formattedDuration = FormatDuration(duration);
            
            html.Append(TdCenterOpenTag)
                .Append(formattedDuration)
                .Append(TdCloseTag);
        }
        
        html.Append(TrCloseTag);
    }

    /// <summary>
    /// Gets the attendance duration for a specific email and meeting
    /// </summary>
    private static TimeSpan GetAttendanceDuration(SummaryData data, string email, string meeting)
    {
        if (data.AttendanceMatrix.ContainsKey(email) && 
            data.AttendanceMatrix[email].ContainsKey(meeting))
        {
            return data.AttendanceMatrix[email][meeting];
        }
        
        return TimeSpan.Zero;
    }

    /// <summary>
    /// Formats a TimeSpan duration to a human-readable string
    /// </summary>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
            return EmptyDurationPlaceholder;
        
        var totalMinutes = (int)duration.TotalMinutes;
        
        if (totalMinutes < MinutesPerHour)
            return string.Format(MinutesFormat, totalMinutes);
        
        var hours = totalMinutes / MinutesPerHour;
        var minutes = totalMinutes % MinutesPerHour;
        
        if (minutes == 0)
            return string.Format(HoursFormat, hours);
        
        return string.Format(HoursMinutesFormat, hours, minutes);
    }
}
