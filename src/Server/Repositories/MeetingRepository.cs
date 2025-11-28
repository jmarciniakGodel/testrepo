using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

/// <summary>
/// Repository implementation for meeting-related data operations
/// </summary>
public class MeetingRepository : IMeetingRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the MeetingRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public MeetingRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new meeting record
    /// </summary>
    /// <param name="meeting">The meeting to create</param>
    /// <returns>The created meeting with generated values</returns>
    public async Task<Meeting> CreateAsync(Meeting meeting)
    {
        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();
        return meeting;
    }

    /// <summary>
    /// Retrieves all meetings for a specific summary
    /// </summary>
    /// <param name="summaryId">The summary identifier</param>
    /// <returns>Collection of meetings belonging to the summary</returns>
    public async Task<IEnumerable<Meeting>> GetBySummaryIdAsync(int summaryId)
    {
        return await _context.Meetings
            .Where(m => m.SummaryId == summaryId)
            .Include(m => m.MeetingAttendances)
            .ThenInclude(ma => ma.Attendant)
            .ToListAsync();
    }

    /// <summary>
    /// Updates an existing meeting record
    /// </summary>
    /// <param name="meeting">The meeting to update</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task UpdateAsync(Meeting meeting)
    {
        _context.Meetings.Update(meeting);
        await _context.SaveChangesAsync();
    }
}
