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
    /// <summary>
    /// Generates an HTML table from summary data
    /// </summary>
    /// <param name="data">The summary data to convert to HTML</param>
    /// <returns>HTML string representation of the summary table</returns>
    public static string GenerateHtmlTable(SummaryData data)
    {
        var html = new StringBuilder();
        
        html.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; font-family: Arial, sans-serif;'>");
        
        // Header row
        html.Append("<thead>");
        html.Append("<tr style='background-color: #4CAF50; color: white;'>");
        html.Append("<th>Attendant Email</th>");
        
        foreach (var meeting in data.MeetingHeaders)
        {
            html.Append($"<th>{System.Net.WebUtility.HtmlEncode(meeting)}</th>");
        }
        
        html.Append("</tr>");
        html.Append("</thead>");
        
        // Body rows
        html.Append("<tbody>");
        
        foreach (var email in data.AttendantEmails)
        {
            html.Append("<tr>");
            html.Append($"<td>{System.Net.WebUtility.HtmlEncode(email)}</td>");
            
            foreach (var meeting in data.MeetingHeaders)
            {
                var duration = TimeSpan.Zero;
                
                if (data.AttendanceMatrix.ContainsKey(email) && 
                    data.AttendanceMatrix[email].ContainsKey(meeting))
                {
                    duration = data.AttendanceMatrix[email][meeting];
                }
                
                var formattedDuration = FormatDuration(duration);
                html.Append($"<td style='text-align: center;'>{formattedDuration}</td>");
            }
            
            html.Append("</tr>");
        }
        
        html.Append("</tbody>");
        html.Append("</table>");
        
        return html.ToString();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
            return "-";
        
        var totalMinutes = (int)duration.TotalMinutes;
        
        if (totalMinutes < 60)
            return $"{totalMinutes} min";
        
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        
        if (minutes == 0)
            return $"{hours} hr";
        
        return $"{hours} hr {minutes} min";
    }
}
