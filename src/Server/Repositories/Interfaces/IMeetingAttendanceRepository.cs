using Server.Models;

namespace Server.Repositories.Interfaces;

public interface IMeetingAttendanceRepository
{
    Task CreateAsync(MeetingAttendance attendance);
}
