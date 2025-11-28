using Server.Models;

namespace Server.Repositories.Interfaces;

/// <summary>
/// Repository interface for attendant-related data operations
/// </summary>
public interface IAttendantRepository
{
    /// <summary>
    /// Retrieves an attendant by their email address
    /// </summary>
    /// <param name="email">The email address to search for</param>
    /// <returns>The attendant if found, null otherwise</returns>
    Task<Attendant?> GetByEmailAsync(string email);

    /// <summary>
    /// Creates a new attendant record
    /// </summary>
    /// <param name="attendant">The attendant to create</param>
    /// <returns>The created attendant with generated values</returns>
    Task<Attendant> CreateAsync(Attendant attendant);
}
