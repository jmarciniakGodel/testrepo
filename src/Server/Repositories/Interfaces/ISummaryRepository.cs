using Server.Models;

namespace Server.Repositories.Interfaces;

public interface ISummaryRepository
{
    Task<Summary> CreateAsync(Summary summary);
    Task<Summary?> GetByIdAsync(int id);
}
