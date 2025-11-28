using Server.Models;

namespace Server.Services.Interfaces;

/// <summary>
/// Service interface for summary-related business operations
/// </summary>
public interface ISummaryService
{
    /// <summary>
    /// Retrieves a single summary by its identifier
    /// </summary>
    /// <param name="id">The summary identifier</param>
    /// <returns>The summary if found, null otherwise</returns>
    Task<Summary?> GetSummaryByIdAsync(int id);

    /// <summary>
    /// Retrieves summaries with pagination and optional search filtering
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="searchQuery">Optional search query to filter results</param>
    /// <returns>Tuple containing the summaries and total count</returns>
    Task<(IEnumerable<Summary> Summaries, int TotalCount)> GetPagedSummariesAsync(int page, int pageSize, string? searchQuery = null);

    /// <summary>
    /// Retrieves the Excel file data for a specific summary
    /// </summary>
    /// <param name="id">The summary identifier</param>
    /// <returns>Tuple containing Excel data and filename, or null if not found</returns>
    Task<(byte[] Data, string FileName)?> GetSummaryExcelAsync(int id);
}
