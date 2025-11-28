using Server.Models;

namespace Server.Repositories.Interfaces;

/// <summary>
/// Repository interface for meeting-related data operations
/// </summary>
public interface IMeetingRepository
{
    /// <summary>
    /// Creates a new meeting record
    /// </summary>
    /// <param name="meeting">The meeting to create</param>
    /// <returns>The created meeting with generated values</returns>
    Task<Meeting> CreateAsync(Meeting meeting);

    /// <summary>
    /// Retrieves all meetings for a specific summary
    /// </summary>
    /// <param name="summaryId">The summary identifier</param>
    /// <returns>Collection of meetings belonging to the summary</returns>
    Task<IEnumerable<Meeting>> GetBySummaryIdAsync(int summaryId);

    /// <summary>
    /// Updates an existing meeting record
    /// </summary>
    /// <param name="meeting">The meeting to update</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task UpdateAsync(Meeting meeting);
}
