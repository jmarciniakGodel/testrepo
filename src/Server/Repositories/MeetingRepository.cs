using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class MeetingRepository : IMeetingRepository
{
    private readonly AppDbContext _context;

    public MeetingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Meeting> CreateAsync(Meeting meeting)
    {
        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();
        return meeting;
    }

    public async Task<IEnumerable<Meeting>> GetBySummaryIdAsync(int summaryId)
    {
        return await _context.Meetings
            .Where(m => m.SummaryId == summaryId)
            .Include(m => m.MeetingAttendances)
            .ThenInclude(ma => ma.Attendant)
            .ToListAsync();
    }

    public async Task UpdateAsync(Meeting meeting)
    {
        _context.Meetings.Update(meeting);
        await _context.SaveChangesAsync();
    }
}
