using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class AttendantRepository : IAttendantRepository
{
    private readonly AppDbContext _context;

    public AttendantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Attendant?> GetByEmailAsync(string email)
    {
        return await _context.Attendants
            .FirstOrDefaultAsync(a => a.Email == email);
    }

    public async Task<Attendant> CreateAsync(Attendant attendant)
    {
        _context.Attendants.Add(attendant);
        await _context.SaveChangesAsync();
        return attendant;
    }
}
