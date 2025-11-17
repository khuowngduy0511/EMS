using Repositories.Models;

namespace Repositories.IRepositories
{
    public interface ILeopardProfileRepository
    {
        Task<List<LeopardProfile>> GetAllWithTypeAsync();
        IQueryable<LeopardProfile> GetAllWithTypeQueryable();
        Task<LeopardProfile?> GetByIdAsync(int id);
        Task<LeopardProfile> AddAsync(LeopardProfile profile);
        Task<LeopardProfile> UpdateAsync(LeopardProfile profile);
        Task<bool> DeleteAsync(int id);
    }
}
