using Repositories.IRepositories;
using Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Repositories
{
    public class LeopardProfileRepository : ILeopardProfileRepository
    {
        private readonly SU25LeopardDBContext _context;

        public LeopardProfileRepository(SU25LeopardDBContext context)
        {
            _context = context;
        }

        public async Task<LeopardProfile> AddAsync(LeopardProfile profile)
        {
            int maxId = await _context.LeopardProfiles.MaxAsync(h => (int?)h.LeopardProfileId) ?? 0;
            profile.LeopardProfileId = maxId + 1;

            _context.LeopardProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var profile = await _context.LeopardProfiles.FindAsync(id);
            if (profile == null) return false;
            _context.LeopardProfiles.Remove(profile);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<LeopardProfile>> GetAllWithTypeAsync()
        {
            return await _context.LeopardProfiles
        .Include(h => h.LeopardType)
        .ToListAsync();
        }

        public IQueryable<LeopardProfile> GetAllWithTypeQueryable()
        {
            return _context.LeopardProfiles
                .Include(h => h.LeopardType)
                .AsQueryable();
        }

        public async Task<LeopardProfile?> GetByIdAsync(int id)
        {
            return await _context.LeopardProfiles.Include(h => h.LeopardType).FirstOrDefaultAsync(h => h.LeopardProfileId == id);
        }

        public async Task<LeopardProfile> UpdateAsync(LeopardProfile profile)
        {
            _context.LeopardProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

    }
}
