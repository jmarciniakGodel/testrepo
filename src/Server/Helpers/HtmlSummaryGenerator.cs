using System.Text;

namespace Server.Helpers;

public class SummaryData
{
    public Dictionary<string, Dictionary<string, TimeSpan>> AttendanceMatrix { get; set; } = new();
    public List<string> MeetingHeaders { get; set; } = new();
    public List<string> AttendantEmails { get; set; } = new();
}

public static class HtmlSummaryGenerator
{
    public static string GenerateHtmlTable(SummaryData data)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; font-family: Arial, sans-serif;'>");
        
        // Header row
        html.AppendLine("  <thead>");
        html.AppendLine("    <tr style='background-color: #4CAF50; color: white;'>");
        html.AppendLine("      <th>Attendant Email</th>");
        
        foreach (var meeting in data.MeetingHeaders)
        {
            html.AppendLine($"      <th>{System.Net.WebUtility.HtmlEncode(meeting)}</th>");
        }
        
        html.AppendLine("    </tr>");
        html.AppendLine("  </thead>");
        
        // Body rows
        html.AppendLine("  <tbody>");
        
        foreach (var email in data.AttendantEmails)
        {
            html.AppendLine("    <tr>");
            html.AppendLine($"      <td>{System.Net.WebUtility.HtmlEncode(email)}</td>");
            
            foreach (var meeting in data.MeetingHeaders)
            {
                var duration = TimeSpan.Zero;
                
                if (data.AttendanceMatrix.ContainsKey(email) && 
                    data.AttendanceMatrix[email].ContainsKey(meeting))
                {
                    duration = data.AttendanceMatrix[email][meeting];
                }
                
                var formattedDuration = FormatDuration(duration);
                html.AppendLine($"      <td style='text-align: center;'>{formattedDuration}</td>");
            }
            
            html.AppendLine("    </tr>");
        }
        
        html.AppendLine("  </tbody>");
        html.AppendLine("</table>");
        
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
