using ClosedXML.Excel;

namespace Server.Helpers;

public static class XlsxGenerator
{
    public static byte[] GenerateXlsxFromSummary(SummaryData data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Meeting Attendance Summary");

        // Add header row
        worksheet.Cell(1, 1).Value = "Attendant Email";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;

        for (int i = 0; i < data.MeetingHeaders.Count; i++)
        {
            worksheet.Cell(1, i + 2).Value = data.MeetingHeaders[i];
            worksheet.Cell(1, i + 2).Style.Font.Bold = true;
            worksheet.Cell(1, i + 2).Style.Fill.BackgroundColor = XLColor.LightGreen;
        }

        // Add data rows
        int rowIndex = 2;
        foreach (var email in data.AttendantEmails)
        {
            worksheet.Cell(rowIndex, 1).Value = email;

            for (int i = 0; i < data.MeetingHeaders.Count; i++)
            {
                var meeting = data.MeetingHeaders[i];
                var duration = TimeSpan.Zero;

                if (data.AttendanceMatrix.ContainsKey(email) &&
                    data.AttendanceMatrix[email].ContainsKey(meeting))
                {
                    duration = data.AttendanceMatrix[email][meeting];
                }

                var formattedDuration = FormatDuration(duration);
                worksheet.Cell(rowIndex, i + 2).Value = formattedDuration;
                worksheet.Cell(rowIndex, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            rowIndex++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Add borders to all cells
        var usedRange = worksheet.RangeUsed();
        if (usedRange != null)
        {
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        // Save to byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
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
