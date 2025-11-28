using Server.Models;

namespace Server.Repositories.Interfaces;

public interface IAttendantRepository
{
    Task<Attendant?> GetByEmailAsync(string email);
    Task<Attendant> CreateAsync(Attendant attendant);
}
