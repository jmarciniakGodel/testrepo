using Server.Data;
using Server.Models;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class MeetingAttendanceRepository : IMeetingAttendanceRepository
{
    private readonly AppDbContext _context;

    public MeetingAttendanceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(MeetingAttendance attendance)
    {
        _context.MeetingAttendances.Add(attendance);
        await _context.SaveChangesAsync();
    }
}
