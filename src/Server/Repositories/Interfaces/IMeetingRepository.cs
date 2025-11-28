using Server.Models;

namespace Server.Repositories.Interfaces;

public interface IMeetingRepository
{
    Task<Meeting> CreateAsync(Meeting meeting);
    Task<IEnumerable<Meeting>> GetBySummaryIdAsync(int summaryId);
    Task UpdateAsync(Meeting meeting);
}
