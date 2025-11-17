using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IServices
{
    public interface ILeopardProfileService
    {
        Task<List<LeopardProfile>> GetAllWithTypeAsync();
        IQueryable<LeopardProfile> GetAllWithTypeQueryable();
        Task<LeopardProfile?> GetByIdAsync(int id);
        Task<LeopardProfile> AddAsync(LeopardProfile profile);
        Task<LeopardProfile> UpdateAsync(LeopardProfile profile);
        Task<bool> DeleteAsync(int id);
    }
}
