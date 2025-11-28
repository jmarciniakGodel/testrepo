using Server.Data;
using Server.Models;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

/// <summary>
/// Repository implementation for meeting attendance data operations
/// </summary>
public class MeetingAttendanceRepository : IMeetingAttendanceRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the MeetingAttendanceRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public MeetingAttendanceRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new meeting attendance record
    /// </summary>
    /// <param name="attendance">The attendance record to create</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task CreateAsync(MeetingAttendance attendance)
    {
        _context.MeetingAttendances.Add(attendance);
        await _context.SaveChangesAsync();
    }
}
