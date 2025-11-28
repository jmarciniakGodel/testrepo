using Server.Models;

namespace Server.Repositories.Interfaces;

/// <summary>
/// Repository interface for summary-related data operations with pagination support
/// </summary>
public interface ISummaryRepository
{
    /// <summary>
    /// Creates a new summary record
    /// </summary>
    /// <param name="summary">The summary to create</param>
    /// <returns>The created summary with generated values</returns>
    Task<Summary> CreateAsync(Summary summary);

    /// <summary>
    /// Retrieves a single summary by its identifier
    /// </summary>
    /// <param name="id">The summary identifier</param>
    /// <returns>The summary if found, null otherwise</returns>
    Task<Summary?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves all summaries from the database
    /// </summary>
    /// <returns>Collection of all summaries</returns>
    Task<IEnumerable<Summary>> GetAllAsync();

    /// <summary>
    /// Retrieves summaries with pagination and optional search filtering
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="searchQuery">Optional search query to filter results</param>
    /// <returns>Tuple containing the summaries and total count</returns>
    Task<(IEnumerable<Summary> Summaries, int TotalCount)> GetPagedAsync(int page, int pageSize, string? searchQuery = null);
}
