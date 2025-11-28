using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class SummaryRepository : ISummaryRepository
{
    private readonly AppDbContext _context;

    public SummaryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Summary> CreateAsync(Summary summary)
    {
        _context.Summaries.Add(summary);
        await _context.SaveChangesAsync();
        return summary;
    }

    public async Task<Summary?> GetByIdAsync(int id)
    {
        return await _context.Summaries
            .Include(s => s.Meetings)
            .ThenInclude(m => m.MeetingAttendances)
            .ThenInclude(ma => ma.Attendant)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Summary>> GetAllAsync()
    {
        return await _context.Summaries
            .Include(s => s.Meetings)
            .ThenInclude(m => m.MeetingAttendances)
            .ThenInclude(ma => ma.Attendant)
            .ToListAsync();
    }
}
