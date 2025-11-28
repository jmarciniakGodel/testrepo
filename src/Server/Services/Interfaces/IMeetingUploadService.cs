using Microsoft.AspNetCore.Http;
using Server.Models;

namespace Server.Services.Interfaces;

/// <summary>
/// Service interface for meeting upload business operations
/// </summary>
public interface IMeetingUploadService
{
    /// <summary>
    /// Processes uploaded meeting CSV files and creates a summary
    /// </summary>
    /// <param name="files">Collection of CSV files to process</param>
    /// <returns>Tuple containing the summary ID and HTML table representation</returns>
    Task<(int SummaryId, string HtmlTable)> ProcessMeetingFilesAsync(IEnumerable<IFormFile> files);
}
