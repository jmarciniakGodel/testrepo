using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

/// <summary>
/// Repository implementation for summary-related data operations with pagination support
/// </summary>
public class SummaryRepository : ISummaryRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the SummaryRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public SummaryRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new summary record
    /// </summary>
    /// <param name="summary">The summary to create</param>
    /// <returns>The created summary with generated values</returns>
    public async Task<Summary> CreateAsync(Summary summary)
    {
        _context.Summaries.Add(summary);
        await _context.SaveChangesAsync();
        return summary;
    }

    /// <summary>
    /// Retrieves a single summary by its identifier
    /// </summary>
    /// <param name="id">The summary identifier</param>
    /// <returns>The summary if found, null otherwise</returns>
    public async Task<Summary?> GetByIdAsync(int id)
    {
        return await _context.Summaries
            .Include(s => s.Meetings)
            .ThenInclude(m => m.MeetingAttendances)
            .ThenInclude(ma => ma.Attendant)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <summary>
    /// Retrieves all summaries from the database
    /// </summary>
    /// <returns>Collection of all summaries</returns>
    public async Task<IEnumerable<Summary>> GetAllAsync()
    {
        return await _context.Summaries
            .Include(s => s.Meetings)
            .ThenInclude(m => m.MeetingAttendances)
            .ThenInclude(ma => ma.Attendant)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves summaries with pagination and optional search filtering
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="searchQuery">Optional search query to filter results</param>
    /// <returns>Tuple containing the summaries and total count</returns>
    public async Task<(IEnumerable<Summary> Summaries, int TotalCount)> GetPagedAsync(int page, int pageSize, string? searchQuery = null)
    {
        var query = _context.Summaries
            .Include(s => s.Meetings)
            .ThenInclude(m => m.MeetingAttendances)
            .ThenInclude(ma => ma.Attendant)
            .AsQueryable();

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(s => 
                s.Meetings.Any(m => m.Title.Contains(searchQuery)) ||
                s.Id.ToString().Contains(searchQuery));
        }

        var totalCount = await query.CountAsync();
        
        var summaries = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (summaries, totalCount);
    }
}
