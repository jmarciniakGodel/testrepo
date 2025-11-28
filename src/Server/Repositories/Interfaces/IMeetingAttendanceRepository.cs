using Server.Models;

namespace Server.Repositories.Interfaces;

/// <summary>
/// Repository interface for meeting attendance data operations
/// </summary>
public interface IMeetingAttendanceRepository
{
    /// <summary>
    /// Creates a new meeting attendance record
    /// </summary>
    /// <param name="attendance">The attendance record to create</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CreateAsync(MeetingAttendance attendance);
}
