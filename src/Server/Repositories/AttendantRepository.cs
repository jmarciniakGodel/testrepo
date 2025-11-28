using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

/// <summary>
/// Repository implementation for attendant-related data operations
/// </summary>
public class AttendantRepository : IAttendantRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the AttendantRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public AttendantRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves an attendant by their email address
    /// </summary>
    /// <param name="email">The email address to search for</param>
    /// <returns>The attendant if found, null otherwise</returns>
    public async Task<Attendant?> GetByEmailAsync(string email)
    {
        return await _context.Attendants
            .FirstOrDefaultAsync(a => a.Email == email);
    }

    /// <summary>
    /// Creates a new attendant record
    /// </summary>
    /// <param name="attendant">The attendant to create</param>
    /// <returns>The created attendant with generated values</returns>
    public async Task<Attendant> CreateAsync(Attendant attendant)
    {
        _context.Attendants.Add(attendant);
        await _context.SaveChangesAsync();
        return attendant;
    }
}
