using Server.Models;
using Server.Repositories.Interfaces;
using Server.Services.Interfaces;

namespace Server.Services;

/// <summary>
/// Service implementation for summary-related business operations
/// </summary>
public class SummaryService : ISummaryService
{
    private readonly ISummaryRepository _summaryRepository;

    /// <summary>
    /// Initializes a new instance of the SummaryService
    /// </summary>
    /// <param name="summaryRepository">The summary repository</param>
    public SummaryService(ISummaryRepository summaryRepository)
    {
        _summaryRepository = summaryRepository;
    }

    /// <summary>
    /// Retrieves a single summary by its identifier
    /// </summary>
    /// <param name="id">The summary identifier</param>
    /// <returns>The summary if found, null otherwise</returns>
    public async Task<Summary?> GetSummaryByIdAsync(int id)
    {
        return await _summaryRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Retrieves summaries with pagination and optional search filtering
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="searchQuery">Optional search query to filter results</param>
    /// <returns>Tuple containing the summaries and total count</returns>
    public async Task<(IEnumerable<Summary> Summaries, int TotalCount)> GetPagedSummariesAsync(int page, int pageSize, string? searchQuery = null)
    {
        return await _summaryRepository.GetPagedAsync(page, pageSize, searchQuery);
    }

    /// <summary>
    /// Retrieves the Excel file data for a specific summary
    /// </summary>
    /// <param name="id">The summary identifier</param>
    /// <returns>Tuple containing Excel data and filename, or null if not found</returns>
    public async Task<(byte[] Data, string FileName)?> GetSummaryExcelAsync(int id)
    {
        var summary = await _summaryRepository.GetByIdAsync(id);
        if (summary == null)
        {
            return null;
        }

        var fileName = $"meeting-summary-{summary.CreatedAt:yyyyMMdd-HHmmss}.xlsx";
        return (summary.XlsxData, fileName);
    }
}
